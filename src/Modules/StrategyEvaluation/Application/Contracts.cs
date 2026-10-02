using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.StrategyEvaluation.Application;

public interface IHistoricalDataProvider
{
    Task<IReadOnlyList<DailyCandle>> GetDailyCandlesAsync(string asset, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}
public interface IStrategyRunStore
{
    Task SaveAsync(StrategyRun run, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StrategyRun>> ListAsync(CancellationToken cancellationToken = default);
    Task<StrategyRun?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
