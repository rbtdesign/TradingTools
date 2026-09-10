using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingTools.Assistant.Application;

namespace TradingTools.Infrastructure.Claude;

internal sealed class ClaudeAssistantProvider(HttpClient httpClient, IOptions<ClaudeOptions> options)
    : IAssistantProvider
{
    public async Task<string> GetReplyAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var settings = options.Value;
        var apiKey = settings.ApiKey?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == ClaudeOptions.ApiKeyPlaceholder)
        {
            throw new AssistantNotConfiguredException();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model = settings.Model,
                max_tokens = settings.MaxTokens,
                messages = new[] { new { role = "user", content = prompt } }
            })
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", settings.ApiVersion);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var message = await response.Content.ReadFromJsonAsync<ClaudeMessage>(cancellationToken);
        var textBlocks = message?.Content?
            .Where(block => block is { Type: "text" } && !string.IsNullOrWhiteSpace(block.Text))
            .Select(block => block.Text);
        var reply = textBlocks is null ? string.Empty : string.Join("\n\n", textBlocks);

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new JsonException("Claude returned no text response.");
        }

        return reply;
    }

    private sealed record ClaudeMessage(ClaudeContentBlock[]? Content);
    private sealed record ClaudeContentBlock(string? Type, string? Text);
}
