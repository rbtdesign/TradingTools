namespace TradingTools.Assistant.Application;

public interface IAssistantProvider
{
    Task<string> GetReplyAsync(string prompt, CancellationToken cancellationToken = default);
}
