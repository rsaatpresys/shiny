﻿using System.Reactive.Linq;
using FluentAssertions;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Xunit;

namespace Shiny.Tests.Core.Infrastructure;


public class BusTest
{
    public string Value { get; set; }
}


public class MessageBusTests
{
    [Fact]
    public async Task EndToEnd()
    {
        var bus = new MessageBus();
        var count = 0;

        bus.Listener<BusTest>().Subscribe(_ => count++);
        bus.Publish(new BusTest { Value = "1" });
        bus.Publish(new BusTest { Value = "2" });
        bus.Publish(new object());
        await Task.Delay(1000);
        count.Should().Be(2);
    }


    [Fact]
    public async Task NamedEndToEnd()
    {
        var bus = new MessageBus();
        var count = 0;

        bus.NamedListener<BusTest>("test").Subscribe(_ => count++);
        bus.PublishNamedMessage("test", new BusTest { Value = "1" });
        bus.PublishNamedMessage("test", new BusTest { Value = "2" });
        bus.PublishNamedMessage("test", new object());
        bus.Publish(new BusTest());
        await Task.Delay(1000);
        count.Should().Be(2);
    }
}
