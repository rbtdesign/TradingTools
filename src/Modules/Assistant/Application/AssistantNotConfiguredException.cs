namespace TradingTools.Assistant.Application;

public sealed class AssistantNotConfiguredException()
    : InvalidOperationException("The AI assistant is not configured yet.");
