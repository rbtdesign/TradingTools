using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingTools.MarketData.Application;
using TradingTools.MarketData.Domain;

namespace TradingTools.Infrastructure.Binance;

internal sealed class BinanceMarketDataProvider(HttpClient httpClient) : IMarketDataProvider
{
    public async Task<MarketPrice> GetPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var ticker = await httpClient.GetFromJsonAsync<BinanceTicker>(
            $"v3/ticker/price?symbol={Uri.EscapeDataString(symbol)}", cancellationToken);

        if (ticker is null || !string.Equals(ticker.Symbol, symbol, StringComparison.Ordinal)
            || ticker.Price <= 0)
        {
            throw new JsonException($"Binance returned an invalid price for {symbol}.");
        }

        return new MarketPrice(ticker.Symbol, ticker.Price);
    }

    private sealed record BinanceTicker(
        string Symbol,
        [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Price);
}
