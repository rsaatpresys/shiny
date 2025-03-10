﻿using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class CharacteristicTests : AbstractBleTests
{
    IList<IGattCharacteristic> characteristics;
    public CharacteristicTests(ITestOutputHelper output) : base(output) {}


    async Task Setup()
    {
        this.Peripheral = await this.Manager
            .ScanUntilPeripheralFound(this.Config.PeripheralName)
            .Timeout(this.Config.DeviceScanTimeout)
            .ToTask();

        await this.Peripheral
            .WithConnectIf()
            .Timeout(this.Config.ConnectTimeout) // android can take some time :P
            .ToTask();

        this.characteristics = await this.Peripheral
            .GetCharacteristicsByService(this.Config.ServiceUuid)
            .Take(5)
            .Timeout(this.Config.OperationTimeout)
            .ToTask();
    }


    [Fact]
    public async Task WriteWithoutResponse()
    {
        await this.Setup();

        var value = new byte[] { 0x01, 0x02 };
        foreach (var ch in this.characteristics)
        {
            await ch.Write(value, false).Timeout(this.Config.OperationTimeout);
            //Assert.True(write.Success, "Write failed - " + write.ErrorMessage);

            // TODO: enable write back on host
            //var read = await ch.Read();
            //read.Success.Should().BeTrue("Read failed - " + read.ErrorMessage);

            //read.Data.Should().BeEquivalentTo(value);
        }
    }


    //[Fact]
    //public async Task BlobWriteTest()
    //{
    //    await this.Setup();

    //    this.characteristics[0].BlobWrite()
    //}

    [Fact]
    public async Task Concurrent_Notifications()
    {
        await this.Setup();
        var list = new Dictionary<string, int>();

        var sub = this.characteristics
            .ToObservable()
            .Select(x => x.Notify())
            .Merge()
            .Synchronize()
            .Subscribe(x =>
            {
                var id = x.Characteristic.Uuid;
                if (list.ContainsKey(id))
                {
                    list[id]++;
                    this.Log("Existing characteristic reply - " + id);
                }
                else
                {
                    list.Add(id, 1);
                    this.Log("New characteristic reply - " + id);
                }
            });

        await Task.Delay(this.Config.OperationTimeout);
        sub.Dispose();

        Assert.True(list.Count >= 2, "There were not at least 2 characteristics in the replies");
        Assert.True(list.First().Value >= 2, "First characteristic did not speak at least 2 times");
        Assert.True(list.ElementAt(2).Value >= 2, "Second characteristic did not speak at least 2 times");
    }


    [Fact]
    public async Task Concurrent_Writes()
    {
        await this.Setup();
        var bytes = new byte[] { 0x01 };
        var list = new List<Task<GattCharacteristicResult>>();

        foreach (var ch in this.characteristics)
            list.Add(ch.Write(bytes).Timeout(this.Config.OperationTimeout).ToTask());

        await Task.WhenAll(list);
    }


    [Fact]
    public async Task Concurrent_Reads()
    {
        await this.Setup();
        var list = new List<Task<GattCharacteristicResult>>();
        foreach (var ch in this.characteristics)
            list.Add(ch.Read().Timeout(this.Config.OperationTimeout).ToTask());

        await Task.WhenAll(list);
    }


    //[Fact(Skip = "TODO")]
    //public async Task Reconnect_ReadAndWrite()
    //{
    //    await this.Setup();

    //    var tcs = new TaskCompletionSource<object>();
    //    IDisposable floodWriter = null;
    //    Observable
    //        .Timer(TimeSpan.FromSeconds(5))
    //        .Subscribe(async _ =>
    //        {
    //            try
    //            {
    //                floodWriter?.Dispose();
    //                this.Peripheral.!CancelConnection();

    //                await Task.Delay(1000);
    //                await this.Peripheral
    //                    .WithConnectIf()
    //                    .Timeout(this.Config.ConnectTimeout);

    //                await this.Peripheral
    //                    .WriteCharacteristic(
    //                        this.Config.ServiceUuid,
    //                        Constants.ScratchCharacteristicUuid1,
    //                        new byte[] {0x1}
    //                    )
    //                    .Timeout(Constants.OperationTimeout);

    //                tcs.SetResult(null);
    //            }
    //            catch (Exception ex)
    //            {
    //                tcs.SetException(ex);
    //            }
    //        });

    //    // this is used to flood queue
    //    floodWriter = this.characteristics
    //        .ToObservable()
    //        .Select(x => x.Write(new byte[] { 0x1 }))
    //        .Merge(4)
    //        .Repeat()
    //        //.Switch()
    //        .Subscribe(
    //            x => { },
    //            ex => Console.WriteLine(ex)
    //        );

    //    await tcs.Task;
    //}


    [Fact]
    public async Task NotificationFollowedByWrite()
    {
        await this.Setup();

        var rx = this.characteristics.First();
        var tx = this.characteristics.Last();

        var r = await rx
            .Notify()
            .Take(1)
            .Select(_ => tx.Write(new byte[] {0x0}))
            .Switch()
            .Timeout(this.Config.OperationTimeout)
            .FirstOrDefaultAsync();

        Assert.Equal(tx, r.Characteristic);
    }


    //[Fact]
    //public async Task CancelConnection_RegisterAndNotify()
    //{
    //    await this.Setup();

    //    var sub = this.characteristics
    //        .First()
    //        .RegisterAndNotify()
    //        .Subscribe();

    //    this.Peripheral.CancelConnection();
    //    sub.Dispose();

    //    await Task.Delay(1000);
    //}

    //[Fact]
    //public async Task BlockWrite_TestBufferClearing()
    //{
    //    const int mtuSize = 512;
    //    var transaction = new MockGattReliableWriteTransaction();
    //    var service = new MockGattService()
    //    {
    //        Peripheral = new MockPeripheral()
    //        {
    //            MtuSize = mtuSize,
    //            Uuid = Guid.NewGuid(),
    //            Transaction = transaction
    //        }
    //    };
    //    var characteristic = new MockGattCharacteristic(service, service.Uuid, CharacteristicProperties.Write);

    //    // Ensure write will span multiple packets
    //    var blob = new byte[mtuSize + (mtuSize / 2)];
    //    // Fill first packet's worth with 1s
    //    for (var i = 0; i < mtuSize; i++)
    //    {
    //        blob[i] = 1;
    //    }

    //    // Fill second packet's worth with 2s
    //    for (var i = mtuSize; i < blob.Length; i++)
    //    {
    //        blob[i] = 2;
    //    }

    //    await characteristic.BlobWrite(new MemoryStream(blob)).FirstAsync(segment => segment.Position == segment.TotalLength);

    //    // First packet should be all 1s
    //    Assert.True(transaction.WrittenValues.First().All(val => val == 1));
    //    Assert.True(transaction.WrittenValues.First().Where(val => val == 1).Count() == blob.Where(val => val == 1).Count());
    //    // Second packet should be half 2s and half 0s
    //    Assert.True(transaction.WrittenValues.Last().Take(mtuSize / 2).All(val => val == 2));
    //    Assert.True(transaction.WrittenValues.Last().Where(val => val == 2).Count() == blob.Where(val => val == 2).Count());
    //    Assert.True(transaction.WrittenValues.Last().Skip(mtuSize / 2).All(val => val == 0));
    //}
}
