using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingTools.MarketData.Application;
using TradingTools.StrategyEvaluation.Application;
namespace TradingTools.Infrastructure.Binance.IoC;

public static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterBinanceApi(IConfiguration configuration)
        {
            services.AddOptions<BinanceOptions>().Bind(configuration.GetSection("Exchanges:Binance"))
                .Validate(options => Uri.TryCreate(options.RestBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http"
                    && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment),
                    "Exchanges:Binance:RestBaseUrl must be an absolute HTTP(S) URL without a query or fragment.").ValidateOnStart();
            services.AddHttpClient<BinanceMarketDataProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<BinanceOptions>>().Value;
                client.BaseAddress = new Uri(options.RestBaseUrl.TrimEnd('/') + "/"); client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddScoped<IMarketDataProvider>(p => p.GetRequiredService<BinanceMarketDataProvider>());
            services.AddScoped<IHistoricalDataProvider>(p => p.GetRequiredService<BinanceMarketDataProvider>());
            return services;
        }
    }
}
