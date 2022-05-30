using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Stores;
using Shiny.Web.Infrastructure;
using Shiny.Web.Stores;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseShinyWasm(this IServiceCollection services)
        {
            //services.TryAddSingleton<IPlatform, WasmPlatform>();
            //services.TryAddSingleton<IRepository, IndexDbRepository>();
            //services.TryAddSingleton<IConnectivity, Connectivity>();
            services.TryAddSingleton<ISerializer, SystemTextJsonSerializer>();
            services.TryAddSingleton<IKeyValueStore, LocalStorageStore>();
            services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
            services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        }
    }
}
