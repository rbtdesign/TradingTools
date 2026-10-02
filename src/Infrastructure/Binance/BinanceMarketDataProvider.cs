using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingTools.MarketData.Application;
using TradingTools.MarketData.Domain;
using TradingTools.StrategyEvaluation.Application;
using TradingTools.StrategyEvaluation.Domain;

namespace TradingTools.Infrastructure.Binance;

internal sealed class BinanceMarketDataProvider(HttpClient httpClient) : IMarketDataProvider, IHistoricalDataProvider
{
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static string CacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TradingTools", "candles");

    public async Task<MarketPrice> GetPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var ticker = await httpClient.GetFromJsonAsync<BinanceTicker>($"v3/ticker/price?symbol={Uri.EscapeDataString(symbol)}", cancellationToken);
        if (ticker is null || !string.Equals(ticker.Symbol, symbol, StringComparison.Ordinal) || ticker.Price <= 0)
            throw new JsonException($"Binance returned an invalid price for {symbol}.");
        return new MarketPrice(ticker.Symbol, ticker.Price);
    }

    public async Task<IReadOnlyList<DailyCandle>> GetDailyCandlesAsync(string asset, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        if (asset is not ("BTCUSDT" or "ETHUSDT")) throw new ArgumentException("Unsupported historical asset.", nameof(asset));
        if (start > end) throw new ArgumentException("Historical start date must not follow end date.");
        var latestCompleted = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        if (end > latestCompleted) throw new ArgumentException("Historical requests may include completed UTC days only.");

        await CacheLock.WaitAsync(cancellationToken);
        try
        {
            var path = Path.Combine(CacheDirectory, $"{asset}-1d.json");
            var cached = await ReadCacheAsync(path, cancellationToken);
            var byDate = cached.ToDictionary(x => x.Date);
            var missing = Dates(start, end).Where(date => !byDate.ContainsKey(date)).ToArray();
            foreach (var range in ContiguousRanges(missing))
                foreach (var candle in await DownloadAsync(asset, range.Start, range.End, cancellationToken)) byDate[candle.Date] = candle;
            var requested = Dates(start, end).Select(date => byDate.TryGetValue(date, out var candle) ? candle : null).ToArray();
            if (requested.Any(x => x is null))
                throw new InvalidOperationException($"Binance could not provide every required completed candle for {asset}; retry or change the period.");
            Directory.CreateDirectory(CacheDirectory);
            var temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(byDate.Values.OrderBy(x => x.Date), JsonOptions), cancellationToken);
            File.Move(temp, path, true);
            return requested.Select(x => x!).ToArray();
        }
        finally { CacheLock.Release(); }
    }

    private async Task<IReadOnlyList<DailyCandle>> DownloadAsync(string asset, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var result = new List<DailyCandle>(); var cursor = start;
        while (cursor <= end)
        {
            var startMs = new DateTimeOffset(cursor.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
            var endExclusive = end.AddDays(1);
            var endMs = new DateTimeOffset(endExclusive.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToUnixTimeMilliseconds() - 1;
            using var response = await httpClient.GetAsync($"v3/klines?symbol={asset}&interval=1d&startTime={startMs}&endTime={endMs}&limit=1000", cancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            var page = document.RootElement.EnumerateArray().Select(ParseCandle).Where(x => x.Date >= cursor && x.Date <= end).ToArray();
            if (page.Length == 0) break;
            result.AddRange(page); cursor = page[^1].Date.AddDays(1);
        }
        return result;
    }

    private static DailyCandle ParseCandle(JsonElement item)
    {
        var values = item.EnumerateArray().ToArray();
        var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(values[0].GetInt64()).UtcDateTime);
        decimal Read(int index) => decimal.Parse(values[index].GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var candle = new DailyCandle(date, Read(1), Read(2), Read(3), Read(4)); candle.Validate(); return candle;
    }
    private static async Task<IReadOnlyList<DailyCandle>> ReadCacheAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return [];
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<DailyCandle>>(stream, JsonOptions, token) ?? [];
    }
    private static IEnumerable<DateOnly> Dates(DateOnly start, DateOnly end) { for (var d = start; d <= end; d = d.AddDays(1)) yield return d; }
    private static IEnumerable<(DateOnly Start, DateOnly End)> ContiguousRanges(DateOnly[] dates)
    {
        if (dates.Length == 0) yield break; var start = dates[0]; var previous = start;
        foreach (var date in dates.Skip(1)) { if (date != previous.AddDays(1)) { yield return (start, previous); start = date; } previous = date; }
        yield return (start, previous);
    }
    private sealed record BinanceTicker(string Symbol, [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Price);
}
