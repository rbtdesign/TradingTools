using System.Text.Json;
using TradingTools.StrategyEvaluation.Application;
using TradingTools.StrategyEvaluation.Domain;
namespace TradingTools.Infrastructure.FileStorage;

internal sealed class JsonStrategyRunStore : IStrategyRunStore
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static string DirectoryPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TradingTools", "runs");
    public async Task SaveAsync(StrategyRun run, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(DirectoryPath); var path = PathFor(run.Id);
            if (File.Exists(path)) throw new InvalidOperationException("Completed strategy runs are immutable.");
            var temp = path + ".tmp";
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(run, Options), cancellationToken); File.Move(temp, path);
        }
        finally { Gate.Release(); }
    }
    public async Task<IReadOnlyList<StrategyRun>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(DirectoryPath)) return [];
        var result = new List<StrategyRun>();
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.json"))
            if (await ReadAsync(path, cancellationToken) is { } run) result.Add(run);
        return result.OrderByDescending(x => x.CreatedAtUtc).ToArray();
    }
    public Task<StrategyRun?> GetAsync(Guid id, CancellationToken cancellationToken = default) => ReadAsync(PathFor(id), cancellationToken);
    private static async Task<StrategyRun?> ReadAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return null; await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<StrategyRun>(stream, Options, token);
    }
    private static string PathFor(Guid id) => Path.Combine(DirectoryPath, $"{id:N}.json");
}
