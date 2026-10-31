using gex.Common.Services.Search;
using Microsoft.Extensions.DependencyInjection;

namespace gex.Services.Search {

    public static class IServiceCollectionExtensionMethod {

        public static void AddGexSearchServices(this IServiceCollection services) {
            services.AddSingleton<SearchTokenizer>();
            services.AddSingleton<SearchAstBuilder>();
            services.AddSingleton<SearchAstDbCommanderCompiler>();
        }

    }
}
