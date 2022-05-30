using System;


namespace Shiny.Web
{
    public enum Permission
    {
        Bluetooth, // https://webbluetoothcg.github.io/web-bluetooth/#permission-api-integration
        Geolocation,
        Push
    }


    public interface IPermissionsApi
    {
        IObservable<AccessState> Query(Permission permission);
        IObservable<AccessState> WhenChanged(Permission permission);
    }
}
