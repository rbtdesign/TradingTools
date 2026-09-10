# TradingTools instructions

## Architecture

- Keep a small .NET 10 modular monolith using lightweight DDD and CQRS-style use cases. Inject concrete query/command handlers directly; no mediator framework, generic repositories, base classes or speculative abstractions.
- Use one project per module under `src/Modules/<Module>`, with `Application` and, when needed, `Domain` folders. Do not split these layers into separate projects or create empty layers.
- Use short module/provider filenames (`MarketData.csproj`, `Claude.csproj`) and explicit `AssemblyName`/`RootNamespace`: `TradingTools.<Module>` and `TradingTools.Infrastructure.<Provider>`. Keep paths, namespaces and solution entries consistent.
- Shared provider integrations belong under `src/Infrastructure/<Provider>`. They implement interfaces owned by a module's Application layer. Application may depend on Domain; Domain must not depend on Application. Neither depends on Web or Infrastructure.
- Web calls Application use cases. Infrastructure references in Web are for startup registration in `Program.cs`. Reuse existing provider classes and registration extensions.
- Follow Binance's options pattern: provider options, configuration binding, startup validation and a typed `HttpClient` registered by an `IoC/ServiceRegistration.cs` extension. Keep provider payloads inside Infrastructure.

## Existing behavior

- Preserve BTC/USDT and ETH/USDT loading once per dashboard visit, including loading/error states and cancellation. Avoid duplicate prerender requests and polling. Use Binance testnet: `https://testnet.binance.vision/api`.
- Keep AI interchangeable through `IAssistantProvider`; Claude is the current adapter and a future OpenAI adapter should not require UI/use-case changes. `AI:Claude:ApiKey` stays a placeholder in committed settings. Missing credentials must allow startup and block outbound requests locally. Do not run Claude API calls or runtime integration tests without explicit user authorization.
- Keep the AI prompt and response central to the existing MudBlazor dashboard. Use responsive grids, cards, theme colors and built-in spacing. Avoid custom CSS, extra components/dependencies and animations. Label sample indicators, portfolio and action history; do not add backend behavior for them or order execution unless requested.

## Working conventions

- Inspect affected code first; clarify material architecture, naming or behavior ambiguities. Honor requested plan/approval steps, but do not seek approval again for agreed scope. Preserve unrelated and staged user changes.
- After code/project changes, run `dotnet build TradingTools.sln --nologo -m:1` and fix introduced compilation errors and warnings. Report what was actually verified; a successful build is not a tested API connection.
- Keep changes and documentation compact. Update `docs/architecture.md` when boundaries, project layout or configuration change; use it for implementation details instead of expanding this file.
