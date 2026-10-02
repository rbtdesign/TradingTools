namespace TradingTools.StrategyEvaluation.Domain;

public enum StrategyKind { MovingAverageCrossover, WeeklyDca }
public enum TransactionSide { Buy, Sell }

public sealed record DailyCandle(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close)
{
    public void Validate()
    {
        if (Open <= 0 || High <= 0 || Low <= 0 || Close <= 0 || High < Low || High < Open || High < Close || Low > Open || Low > Close)
            throw new ArgumentException($"Candle for {Date:yyyy-MM-dd} has invalid OHLC values.");
    }
}

public sealed record StrategySettings(
    string Asset, DateOnly StartDate, DateOnly EndDate, decimal StartingCapital,
    StrategyKind Strategy, int FastSmaPeriod, int SlowSmaPeriod,
    decimal WeeklyPurchaseAmount, DayOfWeek PurchaseWeekday,
    decimal TradingFeePercent, decimal SlippagePercent)
{
    public void Validate(DateOnly latestCompletedUtcDate)
    {
        if (Asset is not ("BTCUSDT" or "ETHUSDT")) throw new ArgumentException("Asset must be BTC/USDT or ETH/USDT.");
        if (StartDate >= EndDate) throw new ArgumentException("Start date must precede end date.");
        if (EndDate > latestCompletedUtcDate) throw new ArgumentException("Only completed UTC daily candles may be evaluated.");
        if (StartingCapital <= 0) throw new ArgumentException("Starting capital must be positive.");
        if (TradingFeePercent < 0 || SlippagePercent < 0 || TradingFeePercent >= 100 || SlippagePercent >= 100)
            throw new ArgumentException("Fee and slippage must be non-negative and below 100%.");
        if (Strategy == StrategyKind.MovingAverageCrossover && (FastSmaPeriod <= 0 || SlowSmaPeriod <= FastSmaPeriod))
            throw new ArgumentException("Fast SMA must be positive and smaller than slow SMA.");
        if (Strategy == StrategyKind.WeeklyDca && WeeklyPurchaseAmount <= 0)
            throw new ArgumentException("Weekly purchase amount must be positive.");
    }
}

public sealed record SimulatedTransaction(DateOnly Date, TransactionSide Side, decimal Quantity,
    decimal FillPrice, decimal Notional, decimal Fee, string Reason, decimal? RealizedProfitLoss = null);
public sealed record EquityPoint(DateOnly Date, decimal Value);
public sealed record PerformanceMetrics(decimal FinalValue, decimal NetProfitLoss, decimal TotalReturnPercent,
    decimal MaximumDrawdownPercent, DateOnly? DrawdownPeakDate, DateOnly? DrawdownTroughDate,
    int PurchaseCount, int SaleCount, int CompletedRoundTrips, decimal TotalFees,
    decimal FinalCash, decimal FinalUnits, decimal FinalUnitsValue);
public sealed record SimulationResult(PerformanceMetrics Metrics, IReadOnlyList<EquityPoint> Equity,
    IReadOnlyList<SimulatedTransaction> Transactions, string Summary);
public sealed record StrategyRun(Guid Id, DateTimeOffset CreatedAtUtc, StrategySettings Settings,
    string DataIdentity, string EvaluationDataIdentity, string CalculationVersion,
    SimulationResult StrategyResult, SimulationResult BenchmarkResult);
