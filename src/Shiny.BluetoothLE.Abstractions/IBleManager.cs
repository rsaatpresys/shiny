namespace Shiny.BluetoothLE;

public interface IBleManager
{
    /// <summary>
    /// Get a known peripheral
    /// </summary>
    /// <param name="peripheralUuid">Peripheral identifier.</param>
    IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid);

    /// <summary>
    /// Get current scanning status
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Stop any current scan - use this if you didn't keep a disposable endpoint for Scan()
    /// </summary>
    void StopScan();

    /// <summary>
    /// Gets a list of connected peripherals by your app
    /// </summary>
    /// <param name="serviceUuid">(iOS only) Service UUID filter to see peripherals that were connected outside of application</param>
    /// <returns></returns>
    IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null);

    /// <summary>
    /// Start scanning for BluetoothLE peripherals
    /// WARNING: only one scan can be active at a time.  Use IsScanning to check for active scanning
    /// </summary>
    /// <returns></returns>
    IObservable<ScanResult> Scan(ScanConfig? config = null);
}

//public bool AndroidUseInternalSyncQueue { get; set; } = true;

///// <summary>
///// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
///// DO NOT CHANGE this if you don't know what this is!
///// </summary>
//public bool AndroidShouldInvokeOnMainThread { get; set; }


///// <summary>
///// This will display an alert dialog when the user powers off their bluetooth adapter
///// </summary>
//public bool iOSShowPowerAlert { get; set; }


///// <summary>
///// CBCentralInitOptions restoration key for background restoration
///// </summary>
//public string iOSRestoreIdentifier { get; set; }