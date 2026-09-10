namespace TradingTools.Assistant.Application.Queries.AskAssistant;

public sealed class AskAssistantQueryHandler(IAssistantProvider assistantProvider)
{
    public Task<string> HandleAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        return assistantProvider.GetReplyAsync(prompt.Trim(), cancellationToken);
    }
}
