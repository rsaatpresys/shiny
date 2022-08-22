using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using NModbus.IO;
using Shiny.BluetoothLE;

namespace ShinyTest.Modbus;
public class BlePortStreamAdapter : IStreamResource
{

    #region Bluetooth Interfaces

    private readonly IBleManager _bleManager;
    IPeripheral _peripheral;

    public BlePortStreamAdapter(IBleManager bleManager)
    {
        this._bleManager = bleManager;
        this._peripheral = null;
        ReadTimeout = 2000;
        WriteTimeout = 2000;
        InfiniteTimeout = 1000000;
    }

    private IObservable<ScanResult> ScanForDevice(int timeout, string deviceName)
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


    private async Task FindPeripheral(string deviceName)
    {
        if (_peripheral == null)
        {
            var scanDeviceObservable = ScanForDevice(5000, deviceName);
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
        var connectionConfig = new ConnectionConfig(AutoConnect: false);
        // connectionConfig.AutoConnect = false;
        await _peripheral.ConnectAsync(connectionConfig, timeout: new TimeSpan(0, 0, 5));

        var rssi = await _peripheral.ReadRssi();

    }

    public IObservable<Shiny.AccessState> CheckPermissions()
    {
        var accessState = this._bleManager.RequestAccess();
        return accessState;

    }

    private byte[] _rxBuffer=new byte[256];
    private MemoryStream _rxBufferStream;
    private Queue _rxBufferQueue;

    public async Task OpenConnection(string deviceName)
    {
        var accessState = await CheckPermissions();

        if (accessState != Shiny.AccessState.Available)
        {
            throw new PermissionException("Bluetooth State Not Available");
        }

        await FindPeripheral(deviceName);

        if (_mcsxvUartService == null)
        {
            _mcsxvUartService = await FindMcsxvUartService();

        }

        if (_mcsXvUarRxDatatCharacteristic == null)
        {
            _mcsXvUarRxDatatCharacteristic = await FindMcsXvUarRxDatatCharacteristic();
        }

        if (_mcsXvUartTxDatatCharacteristic == null)
        {
            _mcsXvUartTxDatatCharacteristic = await FindMcsXvUarTxDatatCharacteristic();
        }


        if (_mcsXvTxDataNotificationsDispose != null)
        {
            _mcsXvTxDataNotificationsDispose.Dispose();
        }

        _mcsXvTxDataNotifications = _mcsXvUartTxDatatCharacteristic.Notify(useIndicationsIfAvailable: true);

        _rxBufferStream = new MemoryStream();
        _rxBufferQueue = new Queue();

        this._mcsXvTxDataNotificationsDispose = this._mcsXvTxDataNotifications.Subscribe((x) =>
        {
            ProcessRxBleData(x);

        });

    }

    private void ProcessRxBleData(GattCharacteristicResult x)
    {
        foreach (var item in x.Data)
        {
            _rxBufferStream.WriteByte(item);
            _rxBufferQueue.Enqueue(item);
        }
        _rxBufferStream.Flush();
    }

    #endregion


    #region ESP32 Presys Protocol 

    IObservable<GattCharacteristicResult> _mcsXvTxDataNotifications;
    IDisposable _mcsXvTxDataNotificationsDispose;

    IGattService _mcsxvUartService;
    IGattCharacteristic _mcsXvUarRxDatatCharacteristic;
    IGattCharacteristic _mcsXvUartTxDatatCharacteristic;


    private async Task<IGattCharacteristic> FindMcsXvUarRxDatatCharacteristic()
    {
        var allCharecteristics = await _mcsxvUartService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var mcsxvUartRxDataUuid = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";

        var mcsxvUartRxData = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == mcsxvUartRxDataUuid.ToLower());

        if (mcsxvUartRxData == null)
        {
            throw new InvalidOperationException("MCSXV Characteristic RxData Not Found ");
        }

        return mcsxvUartRxData;
    }

    private async Task<IGattCharacteristic> FindMcsXvUarTxDatatCharacteristic()
    {
        var allCharecteristics = await _mcsxvUartService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var mcsxvUartRxDataUuid = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

        var mcsxvUartRxData = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == mcsxvUartRxDataUuid.ToLower());

        if (mcsxvUartRxData == null)
        {
            throw new InvalidOperationException("MCSXV Characteristic TxData Not Found ");
        }

        return mcsxvUartRxData;
    }

    private async Task<IGattService> FindMcsxvUartService()
    {
        var mcsXvUartServiceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";


        var allServices = await _peripheral.GetServicesAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var mcsxvUartService = allServices.FirstOrDefault(s => s.Uuid.ToLower() == mcsXvUartServiceUuid.ToLower());

        if (mcsxvUartService == null)
        {
            throw new InvalidOperationException("MCSXV UART Service Not Found ");
        }

        return mcsxvUartService;
    }

    #endregion


    #region NModbus Interface 

    public int InfiniteTimeout { get; }

    public int ReadTimeout { get; set; }
    public int WriteTimeout { get; set; }

    public void DiscardInBuffer()
    {
        this._rxBufferStream.Position = 0;
    }
    public void Dispose()
    {

        if (_mcsXvTxDataNotificationsDispose != null)
        {
            _mcsXvTxDataNotificationsDispose.Dispose();
        }

        DisconnectPeripheral();

        _mcsxvUartService = null;
        _mcsXvUarRxDatatCharacteristic = null;
        _mcsXvUartTxDatatCharacteristic = null;

    }

    public int Read(byte[] buffer, int offset, int count)
    {
        var start = DateTime.Now;

        while (_rxBufferQueue.Count < count)
        {
            Thread.Sleep(10);

            var timeElapsed = DateTime.Now - start;

            if (timeElapsed.TotalMilliseconds > ReadTimeout)
            {
                throw new TimeoutException($"Error reading device Timeout of {ReadTimeout} ms {_peripheral.Name}");
            }

        }

        for (int i = 0; i < count; i++)
        {
            buffer[i+offset]=(byte)_rxBufferQueue.Dequeue();
        }

        var resp =count; //  _rxBufferStream.Read(buffer, offset, count);

        return resp;

    }
    public void Write(byte[] buffer, int offset, int count)
    {

        var mtuSize = 20;

        mtuSize = 20; //_peripheral.MtuSize;

        var data = buffer.Skip(offset).Take(count).ToArray();

        var chunksMessage = data.Chunk(mtuSize);

        foreach (var item in chunksMessage)
        {
            var result = _mcsXvUarRxDatatCharacteristic.Write(item, false).GetAwaiter().Wait();
        }


    }

    #endregion

}
