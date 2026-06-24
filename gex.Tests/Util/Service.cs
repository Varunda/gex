using gex.Common.Models.Options;
using gex.Common.Services.Bar;
using gex.Services.BarApi;
using gex.Services.Db;
using gex.Services.Metrics;
using gex.Services.Parser;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Util {

    public class Service {

        public static async Task<ServiceCollection> Standard() {
            ServiceCollection services = new();
            services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, TestLoggerFactory>());
            services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestLogger<>)));
            services.AddSingleton<IMeterFactory, TestMeterFactory>();
            services.AddSingleton<IMemoryCache, NonCachingCache>();
            services.AddSingleton<BarMapApi>();
            services.AddSingleton<BarApiMetric>();
            services.AddSingleton<PrDownloaderService>();
            services.AddSingleton<IDbHelper>(await DbUtil.Create());
            services.AddSingleton<IOptions<FileStorageOptions>>(Options.Create<FileStorageOptions>(new FileStorageOptions() {
                EngineLocation = "./engines/",
                MapLocation = "./maps/"
            }));
            services.AddDatabasesServices();
            services.AddUtilServices();
            services.AddGexRepositories();

            return services;
        }

    }
}
