using Shiny;
using Shiny.BluetoothLE;
using Shiny.Notifications;

namespace ShinyTest;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
            // THIS IS REQUIRED TO BE DONE FOR SHINY TO RUN
            .UseShiny();

        // shiny.notifications
        builder.Services.AddNotifications();


        // shiny.bluetoothle
        builder.Services.AddBluetoothLE<BleClientDelegate>();

        // shiny.bluetoothle.hosting
        builder.Services.AddBluetoothLeHosting();

        builder.Services.AddTransient<MainPage>();


        return builder.Build();
	}
}
