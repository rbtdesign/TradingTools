using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TradingTools.Infrastructure.Binance.IoC;

public static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterBinanceApi(IConfiguration configuration)
        {
            services.AddOptions<BinanceOptions>()
                .ValidateOnStart();

            return services;
        }
    }
}