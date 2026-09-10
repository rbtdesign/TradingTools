using TradingTools.MarketData.Domain;

namespace TradingTools.MarketData.Application;

public interface IMarketDataProvider
{
    Task<MarketPrice> GetPriceAsync(string symbol, CancellationToken cancellationToken = default);
}
