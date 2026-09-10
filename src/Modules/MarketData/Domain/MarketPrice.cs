namespace TradingTools.MarketData.Domain;

public sealed record MarketPrice
{
    public string Symbol { get; }
    public decimal Price { get; }

    public MarketPrice(string symbol, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        Symbol = symbol;
        Price = price;
    }
}
