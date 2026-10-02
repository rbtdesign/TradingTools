using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.StrategyEvaluation.Application;

public sealed class StrategySimulator
{
    public const string CalculationVersion = "1.0.0";

    public (SimulationResult Strategy, SimulationResult Benchmark) Run(StrategySettings settings, IReadOnlyList<DailyCandle> allCandles)
    {
        var ordered = allCandles.OrderBy(x => x.Date).ToArray();
        ValidateCandles(settings, ordered);
        var evaluation = ordered.Where(x => x.Date >= settings.StartDate && x.Date <= settings.EndDate).ToArray();
        var strategy = settings.Strategy == StrategyKind.MovingAverageCrossover
            ? RunCrossover(settings, ordered, evaluation) : RunDca(settings, evaluation);
        return (strategy, RunBenchmark(settings, evaluation));
    }

    private static SimulationResult RunCrossover(StrategySettings s, DailyCandle[] all, DailyCandle[] eval)
    {
        var cash = s.StartingCapital; var units = 0m; var fees = 0m; decimal? entryCost = null;
        var tx = new List<SimulatedTransaction>(); var equity = OpeningEquity(s);
        var index = all.Select((c, i) => (c.Date, i)).ToDictionary(x => x.Date, x => x.i);
        TransactionSide? pendingSide = null;
        for (var day = 0; day < eval.Length; day++)
        {
            var i = index[eval[day].Date];
            if (pendingSide == TransactionSide.Buy)
            {
                var fill = eval[day].Open * (1 + s.SlippagePercent / 100m);
                var debit = cash; var fee = debit * s.TradingFeePercent / (100m + s.TradingFeePercent);
                var quantity = RoundUnits((debit - fee) / fill);
                var notional = quantity * fill; fee = notional * s.TradingFeePercent / 100m;
                cash -= notional + fee; if (cash < 0.00000001m) cash = Math.Max(0, cash);
                units = quantity; fees += fee; entryCost = notional + fee;
                tx.Add(new(eval[day].Date, TransactionSide.Buy, quantity, fill, notional, fee, "Upward SMA crossover"));
            }
            else if (pendingSide == TransactionSide.Sell)
            {
                var fill = eval[day].Open * (1 - s.SlippagePercent / 100m);
                var quantity = units; var notional = quantity * fill; var fee = notional * s.TradingFeePercent / 100m;
                var proceeds = notional - fee; cash += proceeds; units = 0; fees += fee;
                tx.Add(new(eval[day].Date, TransactionSide.Sell, quantity, fill, notional, fee,
                    "Downward SMA crossover", proceeds - (entryCost ?? 0)));
                entryCost = null;
            }
            pendingSide = null;
            equity.Add(new(eval[day].Date, cash + units * eval[day].Close));

            // A final-day signal is deliberately not queued because no in-period open exists.
            if (day > 0 && day + 1 < eval.Length && i >= s.SlowSmaPeriod)
            {
                var previousFast = AverageClose(all, i - 1, s.FastSmaPeriod);
                var previousSlow = AverageClose(all, i - 1, s.SlowSmaPeriod);
                var currentFast = AverageClose(all, i, s.FastSmaPeriod);
                var currentSlow = AverageClose(all, i, s.SlowSmaPeriod);
                if (units == 0 && previousFast <= previousSlow && currentFast > currentSlow)
                    pendingSide = TransactionSide.Buy;
                else if (units > 0 && previousFast >= previousSlow && currentFast < currentSlow)
                    pendingSide = TransactionSide.Sell;
            }
        }
        var summary = tx.Count == 0 ? "No qualifying crossover executed during the evaluation period." :
            units > 0 ? "The final crossover position remains open and is marked at the final close." : "All crossover positions are closed.";
        return BuildResult(s, eval, cash, units, fees, tx, equity, summary);
    }

    private static SimulationResult RunDca(StrategySettings s, DailyCandle[] eval)
    {
        var cash = s.StartingCapital; var units = 0m; var fees = 0m;
        var tx = new List<SimulatedTransaction>(); var equity = OpeningEquity(s);
        foreach (var candle in eval)
        {
            if (candle.Date.DayOfWeek == s.PurchaseWeekday && cash > 0)
            {
                var debit = Math.Min(s.WeeklyPurchaseAmount, cash);
                var fee = debit * s.TradingFeePercent / (100m + s.TradingFeePercent);
                var fill = candle.Open * (1 + s.SlippagePercent / 100m);
                var quantity = RoundUnits((debit - fee) / fill);
                var notional = quantity * fill; fee = notional * s.TradingFeePercent / 100m;
                if (quantity > 0)
                {
                    cash -= notional + fee; if (cash < 0.00000001m) cash = Math.Max(0, cash);
                    units += quantity; fees += fee;
                    tx.Add(new(candle.Date, TransactionSide.Buy, quantity, fill, notional, fee, "Scheduled weekly DCA purchase"));
                }
            }
            equity.Add(new(candle.Date, cash + units * candle.Close));
        }
        var summary = tx.Count == 0 ? "No scheduled purchase weekday occurred with usable cash during the period." :
            "DCA holdings remain unrealised and are marked at the final close; purchases are not completed trades.";
        return BuildResult(s, eval, cash, units, fees, tx, equity, summary);
    }

    private static SimulationResult RunBenchmark(StrategySettings s, DailyCandle[] eval)
    {
        var fill = eval[0].Open * (1 + s.SlippagePercent / 100m);
        var feeBudget = s.StartingCapital * s.TradingFeePercent / (100m + s.TradingFeePercent);
        var units = RoundUnits((s.StartingCapital - feeBudget) / fill);
        var notional = units * fill; var fee = notional * s.TradingFeePercent / 100m;
        var cash = Math.Max(0, s.StartingCapital - notional - fee);
        var transaction = new SimulatedTransaction(eval[0].Date, TransactionSide.Buy, units, fill, notional, fee, "Buy-and-hold benchmark entry");
        var equity = OpeningEquity(s);
        equity.AddRange(eval.Select(c => new EquityPoint(c.Date, cash + units * c.Close)));
        return BuildResult(s, eval, cash, units, fee, [transaction], equity, "Benchmark holding is marked at the final close without a forced sale.");
    }

    private static SimulationResult BuildResult(StrategySettings s, DailyCandle[] eval, decimal cash, decimal units,
        decimal fees, List<SimulatedTransaction> tx, List<EquityPoint> equity, string summary)
    {
        var finalUnitsValue = units * eval[^1].Close; var final = cash + finalUnitsValue;
        var (drawdown, peak, trough) = Drawdown(equity);
        var metrics = new PerformanceMetrics(final, final - s.StartingCapital, (final / s.StartingCapital - 1) * 100,
            drawdown, peak, trough, tx.Count(x => x.Side == TransactionSide.Buy), tx.Count(x => x.Side == TransactionSide.Sell),
            tx.Count(x => x.Side == TransactionSide.Sell), fees, cash, units, finalUnitsValue);
        return new(metrics, equity, tx, summary);
    }

    private static (decimal Value, DateOnly? Peak, DateOnly? Trough) Drawdown(IReadOnlyList<EquityPoint> points)
    {
        var peakValue = points[0].Value; var peakDate = points[0].Date; var max = 0m; DateOnly? resultPeak = null, trough = null;
        foreach (var point in points)
        {
            if (point.Value > peakValue) { peakValue = point.Value; peakDate = point.Date; }
            var decline = peakValue == 0 ? 0 : (peakValue - point.Value) / peakValue * 100;
            if (decline > max) { max = decline; resultPeak = peakDate; trough = point.Date; }
        }
        return (max, resultPeak, trough);
    }

    private static decimal AverageClose(DailyCandle[] candles, int end, int length) => candles.Skip(end - length + 1).Take(length).Average(x => x.Close);
    private static decimal RoundUnits(decimal value) => Math.Floor(value * 1_000_000_000_000m) / 1_000_000_000_000m;
    private static List<EquityPoint> OpeningEquity(StrategySettings s) => [new(s.StartDate, s.StartingCapital)];
    private static void ValidateCandles(StrategySettings s, DailyCandle[] candles)
    {
        foreach (var candle in candles) candle.Validate();
        if (candles.Select(x => x.Date).Distinct().Count() != candles.Length) throw new ArgumentException("Historical data contains conflicting duplicate dates.");
        var requiredStart = s.Strategy == StrategyKind.MovingAverageCrossover ? s.StartDate.AddDays(-s.SlowSmaPeriod) : s.StartDate;
        var required = candles.Where(x => x.Date >= requiredStart && x.Date <= s.EndDate).ToArray();
        if (!candles.Any(x => x.Date == s.StartDate) || !candles.Any(x => x.Date == s.EndDate)) throw new ArgumentException("Evaluation boundary candles are missing.");
        for (var date = requiredStart; date <= s.EndDate; date = date.AddDays(1))
            if (!required.Any(x => x.Date == date)) throw new ArgumentException($"Required daily candle {date:yyyy-MM-dd} is missing.");
    }
}
