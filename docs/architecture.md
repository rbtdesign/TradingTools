# TradingTools Architecture

TradingTools is a modular monolith: one Blazor Server host, with MarketData and
Assistant modules under `src/Modules`. Layers are folders within each module's
project. Binance and Claude are shared integrations under `src/Infrastructure`.
Both use bound options and a dependency-injection registration extension.

## Projects and dependencies

| Project file | Assembly name and root namespace | Project references |
| --- | --- | --- |
| `src/Apps/TradingTools.WebApp/TradingTools.WebApp.csproj` | `TradingTools.WebApp` | MarketData and Assistant; Binance and Claude for startup registration |
| `src/Modules/MarketData/MarketData.csproj` | `TradingTools.MarketData` | None |
| `src/Modules/Assistant/Assistant.csproj` | `TradingTools.Assistant` | None |
| `src/Infrastructure/Binance/Binance.csproj` | `TradingTools.Infrastructure.Binance` | MarketData |
| `src/Infrastructure/Claude/Claude.csproj` | `TradingTools.Infrastructure.Claude` | Assistant |

Every project explicitly sets `AssemblyName` and `RootNamespace` so the short
project filenames do not determine assembly identities or namespace defaults.

Domain and Application have no framework or provider dependencies. Infrastructure
implements the Application-owned `IMarketDataProvider` interface. Dashboard code
uses only the Application query and its result; the host's `Program.cs` is the
composition root that registers the concrete infrastructure.

Within MarketData, `Domain` owns the immutable `MarketPrice` value object with a
symbol and positive decimal price. `Application` owns the query and provider
contract and may depend on Domain. Domain must not depend on Application.
Their namespaces remain `TradingTools.MarketData.Domain` and
`TradingTools.MarketData.Application`. This boundary is a code convention within
one assembly, not a boundary enforced by separate project references.

Shared Binance infrastructure can later implement an order-execution contract
owned by a Trading module's Application layer. Business rules stay in that module;
Binance HTTP, signing and response mapping belong in the shared integration.
No order-placement code or additional module is introduced for the price query.

This is a small DDD-style module with a CQRS-style read use case.
`GetDashboardPricesQueryHandler.HandleAsync` has no input because it always queries
`BTCUSDT` and `ETHUSDT`. It returns an Application `DashboardPrices` read model.
No empty query object, mediator, repository, persistence or command layer is needed
for this read-only feature.

## Dashboard request flow

1. `/dashboard` initially renders loading placeholders.
2. Its first interactive render calls `GetDashboardPricesQueryHandler` once.
3. The handler requests both symbols concurrently through `IMarketDataProvider`.
4. Binance makes one `GET /api/v3/ticker/price?symbol=...` request per symbol,
   validates the returned symbol and positive price, and maps the response to
   `MarketPrice`. Binance JSON types stay inside Infrastructure.
5. The dashboard displays both prices in USDT. If either request fails, it logs
   the failure and displays an error without substituting fabricated prices.

Loading in `OnAfterRenderAsync(firstRender)` avoids querying during prerendering
and then querying again when the interactive circuit starts. Completion explicitly
triggers a render. Requests have a ten-second HTTP timeout and are cancelled when
the component is disposed. There is no polling or automatic retry; revisiting or
reloading the dashboard starts a new query. Portfolio, indicator and recent-action
cards remain illustrative placeholders.

## AI assistant

The dashboard's Run button calls `AskAssistantQueryHandler`, which validates the
prompt and depends on the Application-owned `IAssistantProvider` interface.
`ClaudeAssistantProvider` implements that interface; Claude request and response
types stay in Infrastructure. Assistant has no Domain objects yet, so it only
contains the Application folder. It does not depend on MarketData or a provider SDK.

Each click sends one prompt and returns plain text, with no conversation history,
tools, order execution, automatic retries or access to the sample portfolio.
The UI prevents concurrent submissions, displays loading and error states, and
cancels the request when the dashboard is disposed. Model text is rendered as
escaped text, not HTML.

`RegisterClaudeApi` follows the Binance options pattern: it binds `ClaudeOptions`,
validates the URL, model, API version, token limit and timeout at startup, and
registers a typed `HttpClient`. A missing or placeholder API key is allowed at
startup and rejected locally before any HTTP request. The UI then shows that the
assistant is not configured. HTTP requests only happen after clicking Run with
a nonempty prompt and a configured key.

To enable Claude, replace `AI:Claude:ApiKey` in the Web app's `appsettings.json`
with a valid API key and restart the app. All other settings are supplied:

```json
"AI": {
  "Claude": {
    "RestBaseUrl": "https://api.anthropic.com",
    "ApiKey": "YOUR_CLAUDE_API_KEY",
    "Model": "claude-sonnet-5",
    "ApiVersion": "2023-06-01",
    "MaxTokens": 2048,
    "TimeoutSeconds": 60
  }
}
```

`AI__Claude__ApiKey` can also supply the key through environment configuration.
Keep real credentials out of source control. The adapter uses `POST /v1/messages`,
the `x-api-key` and `anthropic-version` headers, and concatenates text content
blocks from the response. See the official [API overview](https://platform.claude.com/docs/en/api/overview),
[Messages guide](https://platform.claude.com/docs/en/build-with-claude/working-with-messages)
and [model IDs](https://platform.claude.com/docs/en/models/overview).

To switch to another AI provider later, implement `IAssistantProvider` in a shared
infrastructure project and replace the Claude registration in `Program.cs`.
The dashboard and Assistant module can stay unchanged.

No Claude API call or runtime test was performed during this setup; validation
is limited to source review and building the solution.

## Configuration and running

`src/Apps/TradingTools.WebApp/appsettings.json` supplies
`Exchanges:Binance:RestBaseUrl = https://testnet.binance.vision/api` in all environments.
`RegisterBinanceApi` binds and validates the URL at startup and registers a typed
`HttpClient`. Its base address preserves `/api/` when resolving request paths.
The public price endpoint does not require API keys; the existing optional key,
secret and receive-window options remain unused by this query.

These are Binance Spot Testnet prices. The dashboard labels the source accordingly.

```sh
dotnet build TradingTools.sln
dotnet run --project src/Apps/TradingTools.WebApp
```

Open `/dashboard` at the address printed by the host.

References: [Binance Spot Testnet REST API](https://developers.binance.com/en/docs/products/spot/testnet/rest-api#symbol-price-ticker)
and [Blazor component lifecycle](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0#after-component-render-onafterrenderasync).
