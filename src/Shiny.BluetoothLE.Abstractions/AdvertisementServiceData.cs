namespace Shiny.BluetoothLE;

public record AdvertisementServiceData(
    string ServiceUuid,
    byte[] Data
);
