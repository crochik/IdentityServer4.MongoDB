using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using IdentityServer4.MongoDB.Entities;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.MongoDB.Extensions;
using IdentityServer4.MongoDB.Options;
using IdentityServer4.MongoDB.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace IdentityServer4.MongoDB.Tests
{
    public class HostContainer : IDisposable
    {
        private static readonly object SyncObj = new object();
        private static bool _isInitialized;

        public ILifetimeScope Container { get; }

        public HostContainer()
        {
            var services = new ServiceCollection();

            // logging
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .CreateLogger();
            services.AddLogging(builder => builder.AddSerilog(logger));

            // config
            var configServices = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {$"Store:{nameof(StoreOptions.ConnectionString)}", Environment.GetEnvironmentVariable("STORE_CONNECTIONSTRING") ?? "mongodb://localhost/identityserver"},
                    {$"Store:{nameof(StoreOptions.CollectionNamePrefix)}", Environment.GetEnvironmentVariable("STORE_COLLECTIONNAMEPREFIX")}
                })
                .Build();
            configServices.Configure<StoreOptions>(configuration.GetSection("Store"));
            configServices.AddOptions();
            var configServiceProvider = configServices.BuildServiceProvider();

            // services
            services.AddIdentityServer()
                .AddConfigurationStore(options =>
                {
                    var storeOptions = configServiceProvider.GetService<IOptions<StoreOptions>>().Value;
                    options.ConnectionString = storeOptions.ConnectionString;
                    options.CollectionNamePrefix = storeOptions.CollectionNamePrefix;
                })
                .AddOperationalStore(options =>
                {
                    var storeOptions = configServiceProvider.GetService<IOptions<StoreOptions>>().Value;
                    options.ConnectionString = storeOptions.ConnectionString;
                    options.CollectionNamePrefix = storeOptions.CollectionNamePrefix;
                });

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(services);
            Container = containerBuilder.Build();

            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                lock (SyncObj)
                {
                    if (!_isInitialized)
                    {
                        using (var scope = Container.BeginLifetimeScope())
                        {
                            // clear all data
                            var database = scope.Resolve<IRepository<Client>>().Collection.Database;
                            database.Client.DropDatabase(database.DatabaseNamespace.DatabaseName);
                        }

                        _isInitialized = true;
                    }
                }
            }
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}