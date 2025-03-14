﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Jobs;


public abstract class AbstractJobManager : IJobManager
{
    readonly IRepository<JobInfo> repository;
    readonly IServiceProvider container;
    readonly Subject<JobRunResult> jobFinished;
    readonly Subject<JobInfo> jobStarted;


    protected AbstractJobManager(
        IServiceProvider container,
        IRepository<JobInfo> repository,
        ILogger<IJobManager> logger
    )
    {
        this.container = container;
        this.repository = repository;
        this.Log = logger;
        this.jobStarted = new Subject<JobInfo>();
        this.jobFinished = new Subject<JobRunResult>();
    }


    protected ILogger<IJobManager> Log { get; }
    public abstract Task<AccessState> RequestAccess();
    protected abstract void RegisterNative(JobInfo jobInfo);
    protected abstract void CancelNative(JobInfo jobInfo);


    public virtual async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        try
        {
            this.LogTask(JobState.Start, taskName);
            await task(CancellationToken.None).ConfigureAwait(false);
            this.LogTask(JobState.Finish, taskName);
        }
        catch (Exception ex)
        {
            this.LogTask(JobState.Error, taskName, ex);
        }
    }


    public virtual async Task<JobRunResult> Run(string jobName, CancellationToken cancelToken)
    {
        JobRunResult result;
        JobInfo? actual = null;
        try
        {
            var job = await this.repository
                .Get(jobName)
                .ConfigureAwait(false);

            if (job == null)
                throw new ArgumentException("No job found named " + jobName);

            result = await this.RunJob(job, cancelToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Log.LogError(ex, "Error running job " + jobName);
            result = new JobRunResult(actual, ex);
        }

        return result;
    }


    public Task<IList<JobInfo>> GetJobs()
        => this.repository.GetList();


    public Task<JobInfo?> GetJob(string jobIdentifier)
        => this.repository.Get(jobIdentifier);


    public async Task Cancel(string jobIdentifier)
    {
        var job = await this.repository.Get(jobIdentifier).ConfigureAwait(false);
        if (job != null)
        {
            this.CancelNative(job);
            await this.repository.Remove(jobIdentifier).ConfigureAwait(false);
        }
    }


    public virtual async Task CancelAll()
    {
        var jobs = await this.repository.GetList().ConfigureAwait(false);
        foreach (var job in jobs)
        {
            if (!job.IsSystemJob)
            {
                await this
                    .CancelJob(job)
                    .ConfigureAwait(false);
            }
        }
    }


    Task CancelJob(JobInfo job)
    {
        this.CancelNative(job);
        return this.repository.Remove(job.Identifier);
    }


    public bool IsRunning { get; protected set; }
    public TimeSpan? MinimumAllowedPeriodicTime { get; }
    public IObservable<JobInfo> JobStarted => this.jobStarted;
    public IObservable<JobRunResult> JobFinished => this.jobFinished;


    public async Task Register(JobInfo jobInfo)
    {
        this.ResolveJob(jobInfo);
        this.RegisterNative(jobInfo);
        await this.repository.Set(jobInfo).ConfigureAwait(false);
    }


    public async Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken, bool runSequentially)
    {
        var list = new List<JobRunResult>();

        if (!this.IsRunning)
        {
            try
            {
                this.IsRunning = true;
                var jobs = await this.repository.GetList().ConfigureAwait(false);
                var tasks = new List<Task<JobRunResult>>();

                if (runSequentially)
                {
                    foreach (var job in jobs)
                    {
                        var result = await this
                            .RunJob(job, cancelToken)
                            .ConfigureAwait(false);
                        list.Add(result);
                    }
                }
                else
                {
                    foreach (var job in jobs)
                    {
                        tasks.Add(this.RunJob(job, cancelToken));
                    }

                    await Task
                        .WhenAll(tasks)
                        .ConfigureAwait(false);
                    list.AddRange(tasks.Select(x => x.Result));
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex, "Error running job batch");
            }
            finally
            {
                this.IsRunning = false;
            }
        }
        return list;
    }


    protected async Task<JobRunResult> RunJob(JobInfo job, CancellationToken cancelToken)
    {
        this.jobStarted.OnNext(job);
        var result = default(JobRunResult);
        var cancel = false;

        try
        {
            this.LogJob(JobState.Start, job);
            var jobDelegate = this.ResolveJob(job);

            await jobDelegate
                .Run(job, cancelToken)
                .ConfigureAwait(false);

            if (!job.Repeat)
            {
                await this.Cancel(job.Identifier);
                cancel = true;
            }
            this.LogJob(JobState.Finish, job);
            result = new JobRunResult(job, null);
        }
        catch (Exception ex)
        {
            this.LogJob(JobState.Error, job, ex);
            result = new JobRunResult(job, ex);
        }
        finally
        {
            if (!cancel)
            {
                job.LastRun = DateTimeOffset.UtcNow;
                await this.repository.Set(job).ConfigureAwait(false);
            }
        }
        this.jobFinished.OnNext(result);
        return result;
    }


    protected virtual IJob ResolveJob(JobInfo jobInfo)
    {
        var type = Type.GetType(jobInfo.TypeName);
        if (type == null)
            throw new ArgumentException($"Job '{jobInfo.Identifier}' - Job type '{jobInfo.TypeName}' not found - did you delete / move this class from your library?");

        return (IJob)ActivatorUtilities.GetServiceOrCreateInstance(this.container, type);
    }


    protected virtual void LogJob(JobState state,
                                  JobInfo job,
                                  Exception? exception = null)
    {
        if (exception == null)
            this.Log.LogInformation(state == JobState.Finish ? "Job Success" : $"Job {state}", ("JobName", job.Identifier));
        else
            this.Log.LogError(exception, "Error running job " + job.Identifier);
    }


    protected virtual void LogTask(JobState state, string taskName, Exception? exception = null)
    {
        if (exception == null)
            this.Log.LogInformation(state == JobState.Finish ? "Task Success" : $"Task {state}", ("TaskName", taskName));
        else
            this.Log.LogError(exception, "Task failed - " + taskName);
    }
}
