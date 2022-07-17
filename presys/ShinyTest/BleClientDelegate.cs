using Shiny;
using Shiny.BluetoothLE;
using Shiny.Notifications;

namespace ShinyTest;

public class BleClientDelegate : BleDelegate
{
    readonly INotificationManager notifications;


    public BleClientDelegate(INotificationManager notificationManager)
    {
        this.notifications = notificationManager;
    }


    public override async Task OnAdapterStateChanged(AccessState state)
    {
        if (state == AccessState.Disabled)
            await this.notifications.Send("BLE State", "Turn on Bluetooth already");
    }


    public override async Task OnConnected(IPeripheral peripheral)
    {
        //await this.services.Connection.InsertAsync(new BleEvent
        //{
        //    Description = $"Peripheral '{peripheral.Name}' Connected",
        //    Timestamp = DateTime.Now
        //});
        //await this.services.Notifications.Send(
        //    this.GetType(),
        //    true,
        //    "BluetoothLE Device Connected",
        //    $"{peripheral.Name} has connected"
        //);
    }
}
