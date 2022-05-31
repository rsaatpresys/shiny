using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Infrastructure.Impl;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public static class HostExtensions
{
    public static IServiceCollection AddRepository<TStoreConverter, TEntity>(this IServiceCollection services)
        where TStoreConverter : class, IStoreConverter<TEntity>, new()
        where TEntity : IStoreEntity
    {
        services.AddSingleton<IRepository<TEntity>, JsonFileRepository<TStoreConverter, TEntity>>();
        return services;
    }


    public static void AddCommon(this IServiceCollection services)
    {
        services.TryAddSingleton<ISerializer, DefaultSerializer>();
        services.TryAddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
        services.TryAddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
        services.TryAddSingleton<IMessageBus, MessageBus>();
    }
}