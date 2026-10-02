using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.StrategyEvaluation.Application.Commands.RunStrategy;

public sealed class RunStrategyCommandHandler(IHistoricalDataProvider dataProvider, IStrategyRunStore store, StrategySimulator simulator)
{
    public async Task<StrategyRun> HandleAsync(StrategySettings settings, CancellationToken cancellationToken = default)
    {
        settings.Validate(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        var warmupDays = settings.Strategy == StrategyKind.MovingAverageCrossover ? settings.SlowSmaPeriod : 0;
        var candles = await dataProvider.GetDailyCandlesAsync(settings.Asset, settings.StartDate.AddDays(-warmupDays), settings.EndDate, cancellationToken);
        var (strategy, benchmark) = simulator.Run(settings, candles);
        var evaluationCandles = candles.Where(x => x.Date >= settings.StartDate && x.Date <= settings.EndDate);
        var run = new StrategyRun(Guid.NewGuid(), DateTimeOffset.UtcNow, settings, CreateIdentity(candles), CreateIdentity(evaluationCandles),
            StrategySimulator.CalculationVersion, strategy, benchmark);
        await store.SaveAsync(run, cancellationToken);
        return run;
    }

    private static string CreateIdentity(IEnumerable<DailyCandle> candles)
    {
        var canonical = string.Join('\n', candles.OrderBy(x => x.Date).Select(x => string.Join('|',
            x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), x.Open.ToString(CultureInfo.InvariantCulture),
            x.High.ToString(CultureInfo.InvariantCulture), x.Low.ToString(CultureInfo.InvariantCulture), x.Close.ToString(CultureInfo.InvariantCulture))));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
