using gex.Common.Services.Bar;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Bar {

    public static class IServiceCollectionExtentionMethod {

        public static void AddCovenBarServices(this IServiceCollection services) {
            services.AddSingleton<IPrDownloaderService, CovenPrDownloaderService>();
            services.AddSingleton<IBarEngineDownloader, CovenBarEngineDownloader>();
        }

    }
}
