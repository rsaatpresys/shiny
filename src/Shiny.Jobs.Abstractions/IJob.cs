namespace Shiny.Jobs;


public interface IJob
{
    /// <summary>
    /// Runs your code
    /// </summary>
    /// <param name="jobInfo"></param>
    /// <param name="cancelToken"></param>
    Task Run(JobInfo jobInfo, CancellationToken cancelToken);
}
