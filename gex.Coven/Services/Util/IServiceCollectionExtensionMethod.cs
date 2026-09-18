using gex.Common.Services.Util;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Util {

    public static class IServiceCollectionExtensionMethod {

        public static void AddCovenUtils(this IServiceCollection services) {
            services.AddSingleton<IBarMatchBuilderUtil, CovenBarMatchBuilderUtil>();
            services.AddSingleton<BarMatchProcessorUtil>();
            services.AddSingleton<CovenVersionUtil>();
        }

    }
}
