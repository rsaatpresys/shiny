using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Android;
using Android.Bluetooth;


namespace Shiny.BluetoothLE
{
    public class BleManager : AbstractBleManager,
                              ICanControlAdapterState,
                              ICanViewPairedPeripherals
    {
        public const string BroadcastReceiverName = "com.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver";
        readonly ManagerContext context;


        public BleManager(ManagerContext context) => this.context = context;


        bool isScanning;
        public override bool IsScanning => this.isScanning;


        public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid)
        {
            var address = Guid
                .Parse(peripheralUuid)
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray();

            var native = this.context.Manager.Adapter!.GetRemoteDevice(address);
            if (native == null)
                return Observable.Return<IPeripheral?>(null);

            var peripheral = this.context.GetDevice(native);
            return Observable.Return(peripheral);
        }


        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
            => Observable.Return(this.context
                .Manager
                .GetConnectedDevices(ProfileType.Gatt)
                .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le) // just in case
                .Select(this.context.GetDevice)
            );


        static readonly AndroidPermission[] permissions = new []
        {
            new AndroidPermission(Manifest.Permission.Bluetooth, null, null),
            new AndroidPermission(Manifest.Permission.BluetoothAdmin, null, null),
            new AndroidPermission(Manifest.Permission.BluetoothScan, 31, null),
            new AndroidPermission(Manifest.Permission.BluetoothConnect, 31, null),
            new AndroidPermission(Manifest.Permission.AccessFineLocation, null, 31)
        };
        public override IObservable<AccessState> RequestAccess() => Observable.FromAsync(async ct =>
        {
            if (!this.context.Android.EnsureAllManifestEntries(permissions))
                return AccessState.NotSetup;

            var results = await this.context
                .Android
                .RequestFilteredPermissions(permissions)
                .ToTask(ct)
                .ConfigureAwait(false);

            return results.IsSuccess()
                ? this.context.Status // now look at the actual device state
                : AccessState.Denied;
        });


        public override IObservable<ScanResult> Scan(ScanConfig? config = null)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return this
                .RequestAccess()
                .Do(access =>
                {
                    if (access != AccessState.Available)
                        throw new PermissionException("BluetoothLE", access);

                    this.IsScanning = true;
                })
                .SelectMany(_ => this.context.Scan(config ?? new ScanConfig()))
                .Finally(() => this.IsScanning = false);
        }


        public override void StopScan()
        {
            this.context.StopScan();
            this.isScanning = false;
        }


        public IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals() => Observable.Return(this.context
            .Manager
            .Adapter!
            .BondedDevices
            .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
            .Select(this.context.GetDevice)
        );


        public IObservable<bool> SetAdapterState(bool enable)
        {
            var result = false;
            var ad = this.context.Manager.Adapter!;
            if (enable && !ad.IsEnabled)
                result =ad.Enable();

            else if (!enable && ad.IsEnabled)
                result = ad.Disable();

            return Observable.Return(result);
        }
    }
}