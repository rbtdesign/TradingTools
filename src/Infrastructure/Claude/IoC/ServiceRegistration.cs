using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingTools.Assistant.Application;

namespace TradingTools.Infrastructure.Claude.IoC;

public static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection RegisterClaudeApi(IConfiguration configuration)
        {
            services.AddOptions<ClaudeOptions>()
                .Bind(configuration.GetSection("AI:Claude"))
                .Validate(options => Uri.TryCreate(options.RestBaseUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == "https"
                    && string.IsNullOrEmpty(uri.Query)
                    && string.IsNullOrEmpty(uri.Fragment),
                    "AI:Claude:RestBaseUrl must be an absolute HTTPS URL without a query or fragment.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "AI:Claude:Model is required.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.ApiVersion), "AI:Claude:ApiVersion is required.")
                .Validate(options => options.MaxTokens > 0, "AI:Claude:MaxTokens must be positive.")
                .Validate(options => options.TimeoutSeconds > 0, "AI:Claude:TimeoutSeconds must be positive.")
                // Allow startup without a key; the provider rejects unconfigured requests locally.
                .ValidateOnStart();

            services.AddHttpClient<IAssistantProvider, ClaudeAssistantProvider>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<ClaudeOptions>>().Value;
                client.BaseAddress = new Uri(options.RestBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            return services;
        }
    }
}
