namespace TradingTools.MarketData.Application.Queries.GetDashboardPrices;

public sealed class GetDashboardPricesQueryHandler(IMarketDataProvider marketDataProvider)
{
    public async Task<DashboardPrices> HandleAsync(CancellationToken cancellationToken = default)
    {
        var btcTask = marketDataProvider.GetPriceAsync("BTCUSDT", cancellationToken);
        var ethTask = marketDataProvider.GetPriceAsync("ETHUSDT", cancellationToken);

        var prices = await Task.WhenAll(btcTask, ethTask);

        return new DashboardPrices(prices[0].Price, prices[1].Price);
    }
}
