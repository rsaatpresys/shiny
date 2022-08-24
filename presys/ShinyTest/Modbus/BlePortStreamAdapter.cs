using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using NModbus.IO;
using Shiny.BluetoothLE;
using System.Threading;

namespace ShinyTest.Modbus;

/// <summary>
///   NModbus Stream Implemented based in Nordic UART service over BLE
/// </summary>
/// <remarks>
///   Nordic UART Service (NUS)
///   The Bluetooth® LE GATT Nordic UART Service is a custom service that receives 
///   and writes data and serves as a bridge to the UART interface.
///   https://developer.nordicsemi.com/nRF_Connect_SDK/doc/latest/nrf/libraries/bluetooth_services/services/nus.html
/// 
/// 
///   https://github.com/nkolban/ESP32_BLE_Arduino/blob/master/examples/BLE_uart/BLE_uart.ino
/// 
/// </remarks>
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
        BleScanTimeoutMiliSeconds = 5000;
        BleConnectTimeouttMiliSeconds = 5000;
        InfiniteTimeout = 1000000;
    }

    public int BleScanTimeoutMiliSeconds { get; set; }
    public int BleConnectTimeouttMiliSeconds { get; set; }

    public Task<int> ReadRssiAsync()
    {
        var rssi = _peripheral.ReadRssi().ToTask();
        return rssi;
    }


    private IObservable<ScanResult> ScanForDevice(int timeout, string deviceName)
    {

        var bleScan = this._bleManager
        .Scan()
       .FirstAsync(x =>
       {
           var name = x.Peripheral.Name;
           if (string.IsNullOrEmpty(name))
           {
               name = "";
           }
           var foundDeviece = name.Contains(deviceName);
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
            var scanDeviceObservable = ScanForDevice(BleScanTimeoutMiliSeconds, deviceName);
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
        await _peripheral.ConnectAsync(connectionConfig, timeout: new TimeSpan(0, 0, 0, 0, BleConnectTimeouttMiliSeconds));

    }

    public Task<Shiny.AccessState> CheckPermissionsAsync()
    {
        var accessState = this._bleManager.RequestAccess().ToTask();
        return accessState;
    }

    private BlockingCollection<byte> _rxBufferQueue;

    public async Task OpenConnectionAsync(string deviceName)
    {
        var accessState = await CheckPermissionsAsync();

        if (accessState != Shiny.AccessState.Available)
        {
            throw new PermissionException("Bluetooth State Not Available");
        }

        await FindPeripheral(deviceName);

        if (_nordicUartService == null)
        {
            _nordicUartService = await FindNordicUartService();

        }

        if (_nordicUartRxDatatCharacteristic == null)
        {
            _nordicUartRxDatatCharacteristic = await FindNordicUarRxDatatCharacteristic();
        }

        if (_nordicUartTxDatatCharacteristic == null)
        {
            _nordicUartTxDatatCharacteristic = await FindNordicUarTxDatatCharacteristic();
        }


        if (_nordicUartTxDataNotificationsDispose != null)
        {
            _nordicUartTxDataNotificationsDispose.Dispose();
        }

        _nordicUartTxDataNotifications = _nordicUartTxDatatCharacteristic.Notify(useIndicationsIfAvailable: true);

        _rxBufferQueue = new BlockingCollection<byte>();

        this._nordicUartTxDataNotificationsDispose = this._nordicUartTxDataNotifications.Subscribe((x) =>
        {
            ProcessRxBleData(x);

        });

    }

    private void ProcessRxBleData(GattCharacteristicResult x)
    {
        foreach (var item in x.Data)
        {
            _rxBufferQueue.Add(item);
        }
    }

    #endregion

    #region Nordic Uart Over Ble Service 

    IObservable<GattCharacteristicResult> _nordicUartTxDataNotifications;
    IDisposable _nordicUartTxDataNotificationsDispose;
    IGattService _nordicUartService;
    IGattCharacteristic _nordicUartRxDatatCharacteristic;
    IGattCharacteristic _nordicUartTxDatatCharacteristic;


    private async Task<IGattCharacteristic> FindNordicUarRxDatatCharacteristic()
    {
        var allCharecteristics = await _nordicUartService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var nordicUartRxDataUuid = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";

        var nordicUartRxData = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == nordicUartRxDataUuid.ToLower());

        if (nordicUartRxData == null)
        {
            throw new InvalidOperationException("nordic Characteristic RxData Not Found ");
        }

        return nordicUartRxData;
    }

    private async Task<IGattCharacteristic> FindNordicUarTxDatatCharacteristic()
    {
        var allCharecteristics = await _nordicUartService.GetCharacteristicsAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var nordicUartRxDataUuid = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

        var nordicUartRxData = allCharecteristics.FirstOrDefault(s => s.Uuid.ToLower() == nordicUartRxDataUuid.ToLower());

        if (nordicUartRxData == null)
        {
            throw new InvalidOperationException("nordic Characteristic TxData Not Found ");
        }

        return nordicUartRxData;
    }

    private async Task<IGattService> FindNordicUartService()
    {
        var nordicUartServiceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";


        var allServices = await _peripheral.GetServicesAsync().WaitAsync(TimeSpan.FromMilliseconds(5000));

        var nordicUartService = allServices.FirstOrDefault(s => s.Uuid.ToLower() == nordicUartServiceUuid.ToLower());

        if (nordicUartService == null)
        {
            throw new InvalidOperationException("nordic UART Service Not Found ");
        }

        return nordicUartService;
    }

    #endregion

    #region NModbus Interface 

    public int InfiniteTimeout { get; }

    public int ReadTimeout { get; set; }
    public int WriteTimeout { get; set; }

    public void DiscardInBuffer()
    {
        while (this._rxBufferQueue.TryTake(out _)) { }
    }
    public void Dispose()
    {

        try
        {

            if (_nordicUartTxDataNotificationsDispose != null)
            {
                _nordicUartTxDataNotificationsDispose.Dispose();
            }

            DisconnectPeripheral();

        }
        catch
        {
             //TODO: Log dispose exception
        }

        _nordicUartService = null;
        _nordicUartRxDatatCharacteristic = null;
        _nordicUartTxDatatCharacteristic = null;

    }

    public int Read(byte[] buffer, int offset, int count)
    {

        byte byteToRead = 0;
        int bytesRead = 0;

        for (int i = 0; i < count; i++)
        {
            if (!_rxBufferQueue.TryTake(out byteToRead, ReadTimeout))
            {
                throw new TimeoutException($"Error Reading Device {_peripheral.Name}");
            }

            buffer[i + offset] = byteToRead;
            bytesRead++;
        }

        var resp = bytesRead;

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
            Exception txException = null;

            var result = _nordicUartRxDatatCharacteristic.Write(item, false)
                        .Timeout(TimeSpan.FromMilliseconds(WriteTimeout)).Catch<GattCharacteristicResult, Exception>(tx =>
                        {
                            if (txException == null)
                            {
                                txException = tx;
                            }
                            return Observable.Return<GattCharacteristicResult>(null);
                        }).GetAwaiter().GetResult();

            if (txException != null)
            {
                throw txException;
            }
        }


    }

    #endregion

}
