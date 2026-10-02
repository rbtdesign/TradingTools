using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.StrategyEvaluation.Application.Queries;

public sealed class GetRunHistoryQueryHandler(IStrategyRunStore store)
{
    public Task<IReadOnlyList<StrategyRun>> HandleAsync(CancellationToken cancellationToken = default) => store.ListAsync(cancellationToken);
}
public sealed class GetStrategyRunQueryHandler(IStrategyRunStore store)
{
    public Task<StrategyRun?> HandleAsync(Guid id, CancellationToken cancellationToken = default) => store.GetAsync(id, cancellationToken);
}
public sealed record RunDifference(string Field, string First, string Second);
public sealed record RunComparison(StrategyRun First, StrategyRun Second, bool ConditionsMatch,
    bool SharedBenchmark, IReadOnlyList<RunDifference> Differences);
public sealed class CompareRunsQueryHandler(IStrategyRunStore store)
{
    public async Task<RunComparison> HandleAsync(Guid firstId, Guid secondId, CancellationToken cancellationToken = default)
    {
        if (firstId == secondId) throw new ArgumentException("Select two different runs.");
        var first = await store.GetAsync(firstId, cancellationToken) ?? throw new KeyNotFoundException("First run was not found.");
        var second = await store.GetAsync(secondId, cancellationToken) ?? throw new KeyNotFoundException("Second run was not found.");
        var differences = Differences(first, second);
        return new(first, second, differences.Count == 0, differences.Count == 0, differences);
    }
    private static List<RunDifference> Differences(StrategyRun a, StrategyRun b)
    {
        var result = new List<RunDifference>();
        Add("Asset", a.Settings.Asset, b.Settings.Asset); Add("Start date", a.Settings.StartDate, b.Settings.StartDate);
        Add("End date", a.Settings.EndDate, b.Settings.EndDate); Add("Starting capital", a.Settings.StartingCapital, b.Settings.StartingCapital);
        Add("Trading fee", a.Settings.TradingFeePercent, b.Settings.TradingFeePercent); Add("Slippage", a.Settings.SlippagePercent, b.Settings.SlippagePercent);
        Add("Evaluation data identity", a.EvaluationDataIdentity, b.EvaluationDataIdentity); Add("Calculation version", a.CalculationVersion, b.CalculationVersion);
        return result;
        void Add<T>(string field, T x, T y) { if (!EqualityComparer<T>.Default.Equals(x, y)) result.Add(new(field, $"{x}", $"{y}")); }
    }
}
