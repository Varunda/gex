using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Code.ExtensionMethods {

    public static class DisplayLoggerExtensionMethod {

        public static ILoggingBuilder AddDisplayLogger(this ILoggingBuilder builder) {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DisplayLoggerProvider>());

            LoggerProviderOptions
                .RegisterProviderOptions<DisplayLogger.DisplayLoggerConfiguration, DisplayLoggerProvider>(builder.Services);

            return builder;
        }

        public static ILoggingBuilder AddDisplayLogger(this ILoggingBuilder builder, Action<DisplayLogger.DisplayLoggerConfiguration> config) {
            builder.AddDisplayLogger();
            builder.Services.Configure(config);

            return builder;
        }

    }
}
