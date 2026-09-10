namespace TradingTools.Infrastructure.Claude;

public sealed class ClaudeOptions
{
    public const string ApiKeyPlaceholder = "YOUR_CLAUDE_API_KEY";

    public required string RestBaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public required string Model { get; init; }
    public string ApiVersion { get; init; } = "2023-06-01";
    public int MaxTokens { get; init; } = 2048;
    public int TimeoutSeconds { get; init; } = 60;
}
