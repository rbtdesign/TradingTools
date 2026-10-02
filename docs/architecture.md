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

## Strategy Evaluation module

`StrategyEvaluation` owns the immutable run configuration/results and all deterministic
accounting. Its application layer exposes direct command/query handlers and the
provider-owned `IHistoricalDataProvider` and `IStrategyRunStore` ports. It does not
reference Web or Infrastructure. Binance implements completed daily candle retrieval;
FileStorage implements immutable JSON reports. The Blazor `/strategy-evaluator` page
calls those handlers directly and separates data/simulation failures from optional AI
failures.

A run first validates its inputs, requests the inclusive evaluation range plus exactly
`slow SMA period` preceding daily candles for crossover warm-up, and validates every
required calendar day before calculating anything. Binance fills only absent cache
ranges, paginates its `1d` kline endpoint, rejects malformed OHLC data, and atomically
updates a provider-neutral cache under the user's local application-data directory.
No partial run is saved. Completed reports are written once as individual JSON files in
that directory's `TradingTools/runs` folder and reopened without data retrieval or
recalculation. This file storage is deliberately small and personal; concurrent users,
database migrations, retention, and an administration UI remain outside version one.

Each report stores SHA-256 identities over canonical UTC date/OHLC values for both the
complete calculation input (including warm-up) and the shared evaluation range, plus calculation
version `1.0.0`. Later cache updates cannot rewrite an old report. Comparisons treat asset,
period, capital, costs, data identity, and calculation version as fairness conditions.
Only an exact match gets a shared benchmark label; strategy parameters are allowed to
differ. The report retains its own benchmark either way.

### Calculation conventions

The product working conventions are implemented as follows:

- UTC daily candles and weekdays are used; start/end dates are inclusive and the current
  UTC day is rejected as incomplete.
- An upward crossover is previous fast `<=` previous slow and current fast `>` current
  slow. A downward crossover is previous fast `>=` previous slow and current fast `<`
  current slow. Signals use closing prices and execute at the following day's open; a
  last-day signal cannot execute. The strategy deliberately starts in cash and does not
  infer an entry from its initial SMA state.
- DCA executes at the selected UTC weekday's open. Its configured amount is the maximum
  total cash debit including the fee, with the final debit reduced to available cash.
  No contribution is introduced.
- Buy slippage raises the reference open and sell slippage lowers it. Percentage fees
  apply to filled notional. Fractional units are rounded down to 12 decimal places and
  the resulting residual cash is retained; cash and holdings are never rounded negative.
  Exchange lot sizes and minimum notionals are intentionally outside this model.
- Equity contains opening capital and each evaluation day's close value. Final holdings
  are marked at the final close without a forced sale. Maximum drawdown is the greatest
  peak-to-trough percentage decline in that sampled series, with its dates retained.
- Crossover sells retain net realised P/L against entry notional plus entry fee. An open
  position is identified separately. DCA purchases remain accumulated/unrealised and
  are never described as winning trades or completed round trips.

The benchmark receives an independent copy of the same starting pot and buys at the first
evaluation open with the same fee/slippage. Both results expose cash, units, unit value,
fees, purchase/sale/round-trip counts, return, and drawdown, allowing the report and AI
prompt to use application-calculated facts only.

### AI boundary and variation requests

AI review remains explicit and uses the existing provider-neutral `IAssistantProvider`.
The Web page builds a bounded evidence prompt containing the selected run ID and computed
metrics. Commentary is neither stored in nor allowed to mutate the report. Missing Claude
credentials or provider failure affect only the review panel.

Variation execution is local and deterministic rather than AI-generated code. The page
accepts only the supported explicit forms (`50/200 averages`, or an amount and weekday
for weekly DCA), copies all other source settings, validates normally, and creates a new
immutable run. An incomplete request is rejected before retrieval. General strategy
creation, optimisation loops, and order execution remain excluded.

### Workflow and operational limits

The UI reports preparing, running, completed, and failed states. Default dates cover one
year, and Binance pagination supports longer daily ranges, but this personal application
serialises cache writes and report writes to keep file consistency simple. There is no
background job, polling, live monitoring, or API order path. The existing dashboard still
loads BTC/USDT and ETH/USDT testnet prices once per visit. Historical simulations also use
the configured Binance REST base URL and simulated funds only.

Deterministic tests use known daily prices to cover fixed-pot DCA cash exhaustion, next-open
crossover timing/final-period boundaries, fee/slippage accounting, non-negative residual
cash, benchmark costs, and blocking missing candles. Provider HTTP and Claude calls are
not integration-tested without explicit authorisation.
