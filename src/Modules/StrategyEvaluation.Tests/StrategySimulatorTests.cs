using Xunit;
using TradingTools.StrategyEvaluation.Application;
using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.StrategyEvaluation.Tests;

public sealed class StrategySimulatorTests
{
    [Fact]
    public void Dca_UsesFixedPot_AndSpendsRemainingCash()
    {
        var settings = Settings(StrategyKind.WeeklyDca) with { StartingCapital = 250m, WeeklyPurchaseAmount = 100m, PurchaseWeekday = DayOfWeek.Monday };
        var candles = Candles(new DateOnly(2026, 1, 5), 15, 10m);
        var result = new StrategySimulator().Run(settings with { StartDate = candles[0].Date, EndDate = candles[^1].Date }, candles).Strategy;
        Assert.Equal(3, result.Transactions.Count); Assert.Equal([100m, 100m, 50m], result.Transactions.Select(x => x.Notional));
        Assert.Equal(0m, result.Metrics.FinalCash); Assert.Equal(25m, result.Metrics.FinalUnits);
    }

    [Fact]
    public void Crossover_ExecutesAtNextOpen_AndIgnoresFinalDaySignal()
    {
        var start = new DateOnly(2026, 1, 4);
        var closes = new[] { 3m, 2m, 1m, 1m, 2m, 3m, 1m, 1m };
        var candles = closes.Select((close, i) => new DailyCandle(start.AddDays(i - 3), close, close, close, close)).ToArray();
        var settings = Settings(StrategyKind.MovingAverageCrossover) with { StartDate = start, EndDate = start.AddDays(4), FastSmaPeriod = 1, SlowSmaPeriod = 3 };
        var result = new StrategySimulator().Run(settings, candles).Strategy;
        Assert.Single(result.Transactions); Assert.Equal(start.AddDays(2), result.Transactions[0].Date); Assert.Equal(TransactionSide.Buy, result.Transactions[0].Side);
    }

    [Fact]
    public void Costs_NeverMakeCashNegative_AndBenchmarkUsesSameAssumptions()
    {
        var candles = Candles(new DateOnly(2026, 2, 2), 2, 100m);
        var settings = Settings(StrategyKind.WeeklyDca) with { StartDate = candles[0].Date, EndDate = candles[^1].Date, StartingCapital = 100m, WeeklyPurchaseAmount = 100m, TradingFeePercent = 1m, SlippagePercent = 1m };
        var result = new StrategySimulator().Run(settings, candles);
        Assert.True(result.Strategy.Metrics.FinalCash >= 0); Assert.True(result.Benchmark.Metrics.FinalCash >= 0);
        Assert.True(result.Strategy.Metrics.TotalFees > 0); Assert.True(result.Benchmark.Metrics.TotalFees > 0);
    }

    [Fact]
    public void MissingCandle_BlocksSimulation()
    {
        var candles = Candles(new DateOnly(2026, 1, 5), 3, 10m).Where(x => x.Date.Day != 6).ToArray();
        var settings = Settings(StrategyKind.WeeklyDca) with { StartDate = new(2026, 1, 5), EndDate = new(2026, 1, 7) };
        Assert.Throws<ArgumentException>(() => new StrategySimulator().Run(settings, candles));
    }

    private static StrategySettings Settings(StrategyKind kind) => new("BTCUSDT", new(2026, 1, 5), new(2026, 1, 19), 1000m, kind, 2, 3, 100m, DayOfWeek.Monday, 0m, 0m);
    private static DailyCandle[] Candles(DateOnly start, int count, decimal price) => Enumerable.Range(0, count).Select(i => new DailyCandle(start.AddDays(i), price, price, price, price)).ToArray();
}
