using Shiny.BluetoothLE;
using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace ShinyTest;

public partial class MainPage : ContentPage
{
    private readonly IBleManager _bleManager;

    public MainPage(IBleManager bleManager)
    {
        InitializeComponent();
        this._bleManager = bleManager;
        _peripheral = null;
    }

    IPeripheral _peripheral;

    #region "Teste Leitura Bateria"

    IObservable<GattCharacteristicResult> _batteryLevelNotifications;
    IDisposable _batteryLevelNotificationsDipose;

    private async void cmdTestBatteryServiceNotify_Clicked(object sender, EventArgs e)
    {
        try
        {

            this.cmdTestBatteryServiceNotify.IsEnabled = false;

            var batteryLevel = await FindBatteryLevelCharacteristic();

            if (!batteryLevel.CanNotify())
            {
                throw new InvalidOperationException("Não pode notificar bateria");
            }

            if (_batteryLevelNotificationsDipose != null)
            {
                _batteryLevelNotificationsDipose.Dispose();
            }

            _batteryLevelNotifications = batteryLevel.Notify(useIndicationsIfAvailable: false);

            _batteryLevelNotificationsDipose = _batteryLevelNotifications.Subscribe(async (x) =>
            {
                var data = x.Data;

                await MainThread.InvokeOnMainThreadAsync(() =>
               {
                   lblBatteryLevelNotify.Text = $"Nível:{data[0]}%";
               });

            });

        }
        catch (Exception ex)
        {
            DisconnectPeripheral();

            await DisplayAlert("Alert", "Error connecting to device:" + ex.Message, "OK");
        }
        finally
        {
            this.cmdTestBatteryServiceNotify.IsEnabled = true;
        }


    }

    private async void cmdTestBatteryService_Clicked(object sender, EventArgs e)
    {
        try
        {

            this.cmdTestBatteryService.IsEnabled = false;

            var batteryLevel = await FindBatteryLevelCharacteristic();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = await batteryLevel.ReadAsync();
            stopWatch.Stop();

            var resultType = result.Type;

            var data = result.Data;

            var rssi = await _peripheral.ReadRssi();

            lblBatteryLevel.Text = $"Nível:{data[0]}% Tempo:{stopWatch.ElapsedMilliseconds}ms rssi:{rssi}";

        }
        catch (Exception ex)
        {
            DisconnectPeripheral();

            await DisplayAlert("Alert", "Error connecting to device:" + ex.Message, "OK");
        }
        finally
        {
            this.cmdTestBatteryService.IsEnabled = true;
        }


    }

    private async Task<IGattCharacteristic> FindBatteryLevelCharacteristic()
    {
        await FindPeripheral();

        var bateryServiceUuid = "0000180f-0000-1000-8000-00805f9b34fb";


        var allServices = await _peripheral.GetServicesAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var batteryService = allServices.FirstOrDefault(s => s.Uuid.ToLower() == bateryServiceUuid);

        if (batteryService == null)
        {
            throw new InvalidOperationException("Battery Service Not Found ");
        }

        var allCharecteristics = await batteryService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var batteryLevelUuid = "00002a19-0000-1000-8000-00805f9b34fb";

        var batteryLevel = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == batteryLevelUuid);

        if (batteryLevel == null)
        {
            throw new InvalidOperationException("Battery Level Charecteristic Not Found ");
        }

        return batteryLevel;
    }

    #endregion

    #region Teste Leitura Periferico

    private async void cmdTestPeriphericalReadClicked(object sender, EventArgs e)
    {

        try
        {
            this.cmdTestPeriphericalRead.IsEnabled = false;

            await FindPeripheral();

            var listServices = await _peripheral.GetServicesAsync();

            Debug.WriteLine("Total Number Services:" + listServices.Count);

            foreach (var service in listServices)
            {
                Debug.WriteLine("Service Uuid:" + service.Uuid);

                var allCharacteristics = await service.GetCharacteristicsAsync();
                Debug.WriteLine("Total GetCharacteristicsAsync:" + allCharacteristics.Count);

                foreach (var characteristic in allCharacteristics)
                {
                    Debug.WriteLine("characteristic Uuid:" + characteristic.Uuid);
                    Debug.WriteLine("characteristic IsNotifying:" + characteristic.IsNotifying);
                    Debug.WriteLine("characteristic CanRead:" + characteristic.CanRead());
                    Debug.WriteLine("characteristic CanWrite:" + characteristic.CanWrite());
                    Debug.WriteLine("characteristic CanNotify:" + characteristic.CanNotify());
                    Debug.WriteLine("characteristic CanIndicate:" + characteristic.CanIndicate());

                    var properties = characteristic.Properties;

                    if (characteristic.CanRead())
                    {
                        var value = await characteristic.ReadAsync();
                        var data = value.Data;
                    }

                }
            }


        }
        catch (Exception ex)
        {
            DisconnectPeripheral();

            await DisplayAlert("Alert", "Error connecting to device:" + ex.Message, "OK");
        }
        finally
        {
            this.cmdTestPeriphericalRead.IsEnabled = true;
        }


    }

    #endregion

    #region "Conexão Bluetooth"

    private async Task FindPeripheral()
    {
        if (_peripheral == null)
        {
            var scanDeviceObservable = ScanForDevice(5000, txtDeviceName.Text);
            var result = await scanDeviceObservable;
            _peripheral = result.Peripheral;

        }

        if (_peripheral.IsDisconnected())
        {
            await ConnectPeripheral();
        }


    }

    private void DisconnectPeripheral()
    {
        if (_peripheral != null)
        {
            _peripheral.CancelConnection();
        }
        _peripheral = null;
    }

    private async Task ConnectPeripheral()
    {
        var connectionConfig = new ConnectionConfig();
        connectionConfig.AutoConnect = false;
        await _peripheral.ConnectAsync(connectionConfig, timeout: new TimeSpan(0, 0, 5));

        var rssi = await _peripheral.ReadRssi();

        lblDeviceName.Text = $"Dispositivo:{_peripheral.Name} {rssi}";


    }

    public IObservable<Shiny.AccessState> CheckPermissions()
    {
       var accessState =   this._bleManager.RequestAccess();
        return accessState;
        
    }

    public IObservable<ScanResult> ScanForDevice(int timeout, string deviceName)
    {

        var bleScan = this._bleManager
        .Scan()
       .FirstAsync(x =>
        {
            var foundDeviece = x.Peripheral.Name.Contains(deviceName);
            return foundDeviece;
        })
        .Timeout(TimeSpan.FromMilliseconds(timeout)).Catch<ScanResult, Exception>(tx =>
        {
            this._bleManager.StopScan();
            throw tx;
        });

        return bleScan;

    }
    #endregion

    #region HM-10 

    IObservable<GattCharacteristicResult> _hm10Notifications;
    IDisposable _hm10NotificationsDipose;

    private async Task<IGattCharacteristic> FindHM10UarRxTxDatatCharacteristic()
    {
        await FindPeripheral();

        var hm10UartServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";


        var allServices = await _peripheral.GetServicesAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var hm10UartService = allServices.FirstOrDefault(s => s.Uuid.ToLower() == hm10UartServiceUuid.ToLower());

        if (hm10UartService == null)
        {
            throw new InvalidOperationException("HM-10 Service Not Found ");
        }

        var allCharecteristics = await hm10UartService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var hm10UartRxTxDataUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";

        var hm10UartRxTxData = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == hm10UartRxTxDataUuid.ToLower());

        if (hm10UartRxTxData == null)
        {
            throw new InvalidOperationException("HM-10 Characteristic RxTxData Not Found ");
        }

        return hm10UartRxTxData;
    }


    private async void cmdHM10Service_Clicked(object sender, EventArgs e)
    {

        try
        {

            this.cmdHM10Service.IsEnabled = false;

            var hm10UartRxTxData = await FindHM10UarRxTxDatatCharacteristic();


            if (!hm10UartRxTxData.CanNotifyOrIndicate())
            {
                throw new InvalidOperationException("Não pode notificar Característica do HM-10");
            }

            if (!hm10UartRxTxData.CanIndicate())
            {
                Debug.WriteLine("Não pode indicar Característica com confirmação do HM-10");
            }

            if (_hm10NotificationsDipose != null)
            {
                _hm10NotificationsDipose.Dispose();
            }

            _hm10Notifications = hm10UartRxTxData.Notify(useIndicationsIfAvailable: true);


            //prepara para receber notificação de resposta 
            string responseText = "";

            _hm10NotificationsDipose = _hm10Notifications.Subscribe((x) =>
            {
                var data = x.Data;
                responseText += Encoding.ASCII.GetString(data);
                Debug.WriteLine($"Text Received:{responseText}");
            });

            var stopWatch = new Stopwatch();

            stopWatch.Start();

            Debug.WriteLine("Start Sending Data");


            var textData = "000000000011111111112";


            for (int i = 0; i < 5; i++)
            {

            }

            byte[] textDataBytes = Encoding.ASCII.GetBytes(textData);

            var mtuSize = 20;

            mtuSize = _peripheral.MtuSize;

            var chunksMessage = textDataBytes.Chunk(mtuSize);

            foreach (var item in chunksMessage)
            {
                var result = await hm10UartRxTxData.Write(item, false);
                Thread.Sleep(2);
            }

            var textlen = textData.Length;

            var start = DateTime.Now;

            Debug.WriteLine("Wait Reponse Data");
            while (responseText.Length < textlen)
            {
                await Task.Delay(1);

                var timeElapsed = DateTime.Now - start;

                if (timeElapsed.TotalMilliseconds > 3000)
                {
                    _hm10NotificationsDipose.Dispose();
                    _hm10NotificationsDipose = null;
                    throw new TimeoutException("Error reading device Timeout of 3000 ms" + _peripheral.Name);
                }

            }

            Debug.WriteLine("Reponse Received Data");

            stopWatch.Stop();

            var rssi = await _peripheral.ReadRssi();

            lbHM10Service.Text = $" Loop Back Texto:{responseText} Tempo:{stopWatch.ElapsedMilliseconds}ms rssi:{rssi}";

        }
        catch (Exception ex)
        {
            DisconnectPeripheral();

            await DisplayAlert("Alert", "Error connecting to device:" + ex.Message, "OK");
        }
        finally
        {
            this.cmdHM10Service.IsEnabled = true;
        }



    }


    #endregion

    #region ESP-32 

    IObservable<GattCharacteristicResult> _mcsXvNotifications;
    IDisposable _mcsXvNotificationsDipose;

    private async void cmdMcsXvService_Clicked(object sender, EventArgs e)
    {

        try
        {
           var accessState =  await CheckPermissions(); 

            if (accessState!=Shiny.AccessState.Available)
            {
                throw new PermissionException("Bluetooth State Not Available");
            }

            await FindPeripheral();


        }
        catch (Exception ex)
        {
            DisconnectPeripheral();

            await DisplayAlert("Alert", "Error connecting to device:" + ex.Message, "OK");
        }
        finally
        {

        }


    }


    #endregion

}

