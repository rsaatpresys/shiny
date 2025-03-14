﻿using FluentAssertions;

using Shiny.Hosting;
using Shiny.Stores;
using Shiny.Stores.Impl;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.Core.Infrastructure;


public class DependencyInjectionTests
{
    readonly ITestOutputHelper output;
    public DependencyInjectionTests(ITestOutputHelper output) => this.output = output;


    static IServiceProvider Create(Action<IKeyValueStore>? addSettings = null, Action<IServiceCollection>? addServices = null)
    {
        var settings = new MemoryKeyValueStore();
        addSettings?.Invoke(settings);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.AddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.AddSingleton<IKeyValueStore>(settings);
        addServices?.Invoke(services);

        return services.BuildServiceProvider(true);
    }


    static void SetCountKey(IKeyValueStore settings, int value)
    {
        var key = $"{typeof(FullService).FullName}.{nameof(FullService.Count)}";
        settings.Set(key, value);
    }


    [Fact(DisplayName = "DI - Services Restore & Startup")]
    public void ServiceRestoresStateAndStartsUp()
    {
        var setValue = new Random().Next(1, 9999);
        var postValue = setValue + 1;

        var services = Create(
            s => SetCountKey(s, setValue),
            s => s.AddSingleton<IFullService, FullService>()
        );
        services
            .GetRequiredService<IFullService>()
            .Count
            .Should()
            .Be(postValue);
    }


    [Fact(DisplayName = "DI - Startup Tasks Run")]
    public void StartupTaskRuns()
    {
        var sp = Create(null, x =>
        {
            x.AddSingleton<TestStartupTask>();
        });
        TestStartupTask.Value.Should().Be(99);
        TestStartupTask.Value = 0;
    }
}


public class TestStartupTask : IShinyStartupTask
{
    public static int Value { get; set; }


    public void Start()
    {
        Value = 99;
    }
}

