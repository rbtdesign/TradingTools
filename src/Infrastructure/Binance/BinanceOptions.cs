namespace TradingTools.Infrastructure.Binance;

public sealed class BinanceOptions
{
    public required string RestBaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public string? ApiSecret { get; init; }
    public int RecvWindowMs { get; init; } = 5000;
}
