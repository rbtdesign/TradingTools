# Strategy Evaluator Assistant

## Product requirements and business handoff — Version 1.0

**Date:** 29 September 2026  
**Audience:** Technical lead / technical analyst, then implementation developer  
**Status:** Agreed product scope, with explicit working defaults for technical refinement  
**Deliverable:** Personal web application; Blazor is the requested presentation technology. All other architecture and storage decisions are deferred.

## 1. Purpose and intended outcome

Build a personal strategy evaluation application that tests predefined trading strategies against historical market data, presents their results visually, and provides an optional AI assistant to interpret results and help run comparisons.

The core user question is:

> How did these rules behave during this historical period, and what changed when I adjusted them?

This is a coding and AI-integration demonstration project. Success means completing a coherent, understandable workflow with reproducible calculations and useful AI interaction. Finding a profitable strategy, executing trades, and building a commercial platform are not success criteria for this version.

The application performs the calculations. The AI uses the application's results to explain, investigate, and request further experiments. AI availability must not be required to run a test or read its report.

## 2. Scope baseline

### Included

- Personal use through a web interface.
- Historical simulation of one asset per run: BTC/USDT or ETH/USDT.
- Two selectable predefined strategies: simple moving-average crossover and unconditional weekly DCA.
- A fixed starting pot of simulated USDT, with no fresh contributions.
- An automatic buy-and-hold benchmark for every run.
- Configurable strategy parameters, test dates, starting capital, and cost assumptions.
- A visual report with portfolio values, performance metrics, and transaction details.
- Saved run history and comparison of two selected strategy runs, alongside their benchmarks.
- Optional AI review, follow-up questions, and explicitly requested variation runs.
- Reuse of downloaded historical data and validation before simulation.

### Deferred or excluded

- Recurring contributions of fresh money; this is an explicit later extension.
- Live trading, testnet order execution, exchange account management, or live monitoring.
- Short selling, leverage, derivatives, multiple assets within one portfolio, and order-book simulation.
- Arbitrary strategy generation, a visual rule builder, or AI-generated executable strategy code.
- Conditional DCA, automatic parameter optimisation, and open-ended autonomous experiment loops.
- News analysis, price prediction, and assertions that historical performance predicts future profitability.
- Multi-user accounts, subscriptions, marketplaces, exports, and a historical-data administration screen.

Support for more predefined strategies can be added later. Do not build an extensibility platform as a separate first-version feature.

## 3. Primary user journey

1. **Configure:** choose an asset, dates, starting capital, strategy, settings, and cost assumptions.
2. **Prepare:** reuse available data, retrieve missing data, and validate the full required dataset.
3. **Run:** simulate the selected strategy and its buy-and-hold benchmark.
4. **Inspect:** show the report immediately, without requiring an AI request.
5. **Review optionally:** the user selects “Review with AI” and receives an interpretation grounded in that run.
6. **Explore:** ask follow-up questions or explicitly request a variation or the other supported strategy.
7. **Compare:** inspect two saved strategy runs visually and optionally request an AI comparison.
8. **Return:** reopen the saved report later without recalculating it or refetching its data.

Example completion scenario:

> Run a crossover strategy on BTC, inspect its buy-and-hold comparison, request an AI review, run weekly DCA under the same test conditions, and compare crossover, DCA, and their shared buy-and-hold reference.

## 4. Inputs and configuration

| Input | Business meaning | Validation / behaviour |
| --- | --- | --- |
| Asset | BTC/USDT or ETH/USDT | One asset per run |
| Start and end dates | Historical evaluation period | Start must precede end; only completed daily periods are eligible |
| Starting capital | Cash available at the start, in USDT | Strictly positive; no new deposits during the run |
| Selected strategy | Crossover or weekly DCA | Show only the settings applicable to that strategy |
| Fast SMA period | Number of daily closes in the faster simple moving average | Positive integer, smaller than the slow period |
| Slow SMA period | Number of daily closes in the slower simple moving average | Positive integer, larger than the fast period |
| Weekly purchase amount | DCA cash budget per scheduled purchase | Strictly positive |
| Purchase weekday | Weekly DCA schedule | One weekday selected |
| Trading fee | Assumed percentage fee on every simulated purchase or sale | Non-negative; the same setting applies to the benchmark |
| Slippage | Assumed adverse percentage adjustment to the reference fill price | Non-negative; the same setting applies to the benchmark |

Provide visible, editable defaults so a user can run an example without configuring every field. Exact prefilled amounts, dates, and cost values are design defaults, not claims about current exchange fees.

Strategy settings change values inside an existing rule set. For example, changing SMA periods from 20/50 to 50/200 is supported; adding an RSI condition is outside this version.

## 5. Business rules for simulation

### 5.1 Shared rules

| ID | Requirement |
| --- | --- |
| BR-01 | Each strategy and its benchmark receive the same starting capital at the same start time. No fresh money enters the simulation. |
| BR-02 | Portfolio value includes cash plus the current value of the asset held. Cash earns no interest in this version. |
| BR-03 | The simulation must never use future information to make a decision. Indicator warm-up data is not a trading period and contributes no performance. |
| BR-04 | Fees and adverse slippage apply to each actual simulated purchase or sale. Cash cannot become negative and asset holdings cannot become negative. |
| BR-05 | Simulation uses completed daily candles. Intraday price paths and intraday signals are outside scope. |
| BR-06 | At the end, holdings are valued at the final daily closing price. There is no forced sale and no hypothetical final sale fee. |
| BR-07 | A signal that cannot execute within the selected period must not produce a transaction outside that period. |
| BR-08 | A completed run retains its original settings, assumptions, data identity, and results. Changing settings creates a new run. |

### 5.2 Moving-average crossover

- Calculate a fast and a slow **simple moving average** of daily closing prices.
- Begin in cash; do not buy merely because the fast average is already above the slow average when the evaluation period starts.
- Buy on a qualifying upward crossover while in cash. Use available cash, allowing for fees.
- Sell the entire asset holding on a qualifying downward crossover while holding the asset.
- Maintain at most one asset position. Do not add to a position on repeated buy conditions.
- Evaluate signals at the daily close and execute at the next daily open, adjusted for slippage and fees.
- Obtain enough preceding history to calculate both current and previous averages when testing for a crossover.

### 5.3 Weekly DCA

- Buy on the selected weekday each week, regardless of market price or indicators.
- The first purchase is on the first scheduled weekday within the selected period. No purchase is backdated before the period.
- Deduct each purchase from the original cash pot. No weekly deposit occurs.
- Hold all purchased units until the end; this strategy does not sell.
- If less than the scheduled budget remains, spend the remaining usable cash, allowing for fees. Subsequent purchases stop when no usable cash remains.
- A period containing no scheduled purchase day may legitimately produce zero purchases; explain this in the report.

### 5.4 Buy-and-hold benchmark

- Invest the full starting pot, allowing for costs, at the opening price of the first evaluation day.
- Hold the purchased asset throughout the evaluation period.
- Use the same asset, dates, starting capital, fees, slippage, and historical data as the strategy run.
- Include the benchmark automatically. The user does not configure or launch it separately.

### 5.5 Working conventions to make explicit in technical analysis

These conventions complete the business specification without introducing new features. They should be retained unless the technical lead identifies a conflict, in which case the change must be surfaced rather than silently substituted.

| Topic | Proposed convention |
| --- | --- |
| Calendar | Daily candles, weekdays, and report timestamps use UTC; start and end calendar dates are inclusive. |
| Crossover equality | Upward: previous fast SMA <= previous slow SMA, and current fast SMA > current slow SMA. Downward: previous fast SMA >= previous slow SMA, and current fast SMA < current slow SMA. |
| DCA timing | Scheduled purchases execute at the opening price of the selected UTC weekday. |
| DCA budget | Weekly amount is a total cash debit including purchase fees; slippage affects the units acquired. |
| Fractional holdings | Fractional asset purchases are supported. Full exchange minimum-order and lot-size enforcement is outside this simplified model; define numerical precision and residual-cash treatment explicitly. |
| Equity sampling | Record opening capital and daily closing portfolio values. Maximum drawdown is based on this sampled series, not an intraday estimate. |

## 6. Historical data and pre-run validation

Use real historical Binance market data with simulated money. A testnet trading account is not part of the evaluation workflow.

**FR-01 — Reuse:** historical data already downloaded must be reused. Retrieve additional data when the requested asset, period, or indicator warm-up requires it.

**FR-02 — Validate before simulation:** verify the complete evaluation period and any preceding indicator history before running either the strategy or its benchmark. Detect missing required candles, conflicting or invalid records, and incomplete current candles.

**FR-03 — Block incomplete tests:** if required data remains unavailable after retrieval, block the run and state the reason. Do not skip dates, shorten the requested period, fabricate candles, or present partial simulation results as a completed run.

**FR-04 — Recovery:** distinguish missing local data, which normally triggers retrieval, from data that cannot be obtained. Explain whether the user can retry or must change the requested period.

**FR-05 — Reproducibility:** retain sufficient data identity and calculation-version information to explain which data and rules produced a saved result. Later downloads or calculation changes must not rewrite earlier results.

User-visible progress should distinguish preparing data, running the simulation, completed, and failed/blocked. These are workflow states, not a prescribed technical job architecture.

## 7. Run report

**FR-06 — Independent report:** the report must be available without AI and identify the asset, period, strategy, parameters, initial capital, and simulation assumptions.

| Report element | Required content |
| --- | --- |
| Portfolio-value chart | Strategy and buy-and-hold values over time, including unspent cash |
| Final account value | Cash plus marked-to-market holdings, in USDT |
| Net profit/loss | Final account value minus starting capital |
| Total return | Net profit/loss divided by starting capital, expressed as a percentage |
| Maximum drawdown | Largest percentage decline from a prior portfolio-value peak, with peak and trough dates |
| Activity | Executed purchase and sale counts, clearly distinguished from completed round trips |
| Costs | Total fees; disclose the slippage assumption separately rather than confusing it with fees |
| Final composition | Remaining cash, units held, and the value of those units |
| Transaction history | Time, side, quantity, simulated fill price, notional amount, fee, and reason for execution |
| Benchmark comparison | The same core performance measures for buy-and-hold and the return difference in percentage points |

Show zero-trade runs as valid results where the strategy rules justify them. Do not invent a win rate or other undefined measure when no applicable trades exist.

The report must contain the objective measurements used by an AI review. The AI should not be required to reveal basic facts such as maximum drawdown.

For crossover investigations, supporting detail should allow completed entry/exit trades to be inspected with net realised profit/loss, while any open holding is identified separately. For DCA, distinguish accumulated holdings and unrealised performance from realised trading profit; a purchase is not a completed winning or losing trade.

## 8. Saved history and comparisons

**FR-07 — History:** list completed runs with enough information to distinguish them: creation time, asset, strategy, dates, key settings, and headline result. Reopening a run restores its saved report.

**FR-08 — Selected comparison:** compare two saved strategy runs. They may be two configurations of the same strategy or one crossover and one weekly DCA run.

**FR-09 — Shared benchmark:** when runs use matching test conditions, display the two strategies and one shared buy-and-hold reference. Do not automatically run weekly DCA whenever crossover is selected, or vice versa.

**FR-10 — Fairness of comparison:** show differences in asset, dates, starting capital, fees, slippage, data identity, or simulation conventions. Clearly label mismatched conditions; do not merge distinct benchmarks or present the result as an equivalent comparison. Strategy parameters and DCA purchase amounts are legitimate experiment differences.

Comparison must show aligned metrics and portfolio curves where conditions permit. An AI comparison is optional.

## 9. AI assistant behaviour

### Included capabilities

| ID | Capability | Expected behaviour |
| --- | --- | --- |
| AI-01 | Review a completed run | On explicit request, interpret the result relative to its benchmark, identify meaningful periods or transactions, and explain trade-offs using actual figures. |
| AI-02 | Answer follow-ups | Retain the relevant run context and inspect supporting data as needed. Resolve ambiguous run references before acting. |
| AI-03 | Run a variation | On a direct user request, create a new run using a supported strategy and valid settings. Preserve unchanged settings and disclose the effective configuration and changes. |
| AI-04 | Compare runs | Explain measured differences between selected results, including whether conditions match. |
| AI-05 | Suggest an experiment | Suggest a specific follow-up when useful. A suggestion alone must not start a run. |

Example requests:

- “Review this result against buy-and-hold.”
- “Show which periods account for the largest decline.”
- “Which completed crossover trades contributed most to realised profit?”
- “Repeat this run with 50/200 averages and keep everything else the same.”
- “Run weekly DCA with 100 USDT every Monday under the same test conditions.”
- “Compare these two results and explain the return/drawdown trade-off.”

### Required boundaries

- No automatic AI review on every run.
- Numeric claims must come from application-calculated results or supporting details; the AI must not invent metrics.
- Reviews must identify the run(s) being discussed and reference relevant dates, trades, or measures.
- Distinguish observed facts from possible explanations. Do not assert causality from a summary metric alone.
- Do not turn historical outperformance into a claim of future profitability.
- Do not create unsupported strategy rules, execute arbitrary generated code, or place exchange orders.
- If a requested variation is incomplete and cannot safely inherit a setting, ask for the missing value. Do not silently guess a material parameter.
- AI failure must leave completed reports intact. Explain the failure and allow another review request.
- An unsuccessful variation must be reported as unsuccessful; do not describe it as completed.

AI-generated commentary is separate from the immutable numerical run result. Persisting chat history or AI reviews is not required for the first version; saved run reports are required.

## 10. Acceptance scenarios

| ID | Given / When | Expected result |
| --- | --- | --- |
| AC-01 | Valid crossover configuration and complete data; user runs a test | A saved strategy report and automatic buy-and-hold benchmark appear without invoking AI. |
| AC-02 | Fast SMA already exceeds slow SMA at the start | Strategy stays in cash until a qualifying upward crossover occurs within the period. |
| AC-03 | A crossover is detected at a daily close | Execution occurs no earlier than the next daily open. A final-day signal produces no out-of-period trade. |
| AC-04 | Weekly DCA is scheduled for Monday | Purchases occur on eligible Mondays within the period, with no fresh cash deposited. |
| AC-05 | DCA has 250 USDT and a 100 USDT weekly budget; costs are set to zero for this example | Three eligible purchases spend 100, 100, and 50 USDT, followed by no further purchases. |
| AC-06 | Strategy retains unspent cash or asset holdings at the end | Final value includes both; no forced sale is inserted. |
| AC-07 | Required data is absent locally but available remotely | App fetches it, validates the full required dataset, then starts simulation. |
| AC-08 | Required evaluation or warm-up data remains missing | Run is blocked before simulation, the reason is displayed, and no partial report is produced. |
| AC-09 | User repeats a request covered by stored data | Historical data is reused; completed saved reports remain unchanged. |
| AC-10 | User requests an AI review | AI interprets the selected run using verifiable figures and supporting evidence; it does not start another test automatically. |
| AC-11 | User asks to change 20/50 to 50/200 | A new run is created, retaining the other conditions and leaving the source run unchanged. |
| AC-12 | Matching crossover and DCA runs are compared | View shows both strategies and one shared buy-and-hold reference with aligned core metrics. |
| AC-13 | Compared runs differ in capital or period | Differences are visible; AI and report do not imply a like-for-like result. |
| AC-14 | AI service fails after a successful backtest | The report remains usable; review failure is shown separately. |
| AC-15 | No entry or scheduled purchase occurs | A valid zero-transaction report appears with an explanation consistent with the rules. |
| AC-16 | User closes and later reopens the application | Completed run history and saved reports are still available. |

Acceptance checks must verify actual accounting and event timing, not merely the presence of UI elements. Use simple known-price examples to verify fees, cash exhaustion, portfolio valuation, and return/drawdown calculations.

## 11. Essential quality expectations

- **Determinism:** the same validated data, settings, and calculation version produce the same numerical results, independently of AI wording.
- **Transparency:** configuration, cost assumptions, units, dates, and valuation conventions are visible.
- **Traceability:** metrics can be reconciled with transactions and the portfolio-value history.
- **Usability:** a user can complete the main workflow from the web interface without calling APIs or reading logs.
- **Failure separation:** data preparation, simulation, and AI review failures are distinguished; none is misrepresented as success.
- **Bounded scope:** technical choices should support this personal demonstration without adding commercial-platform requirements.

No hard performance SLA is set in this brief. The technical lead should establish reasonable limits and progress behaviour for the supported daily-data workload.

## 12. Technical lead handoff

The technical lead should produce an implementation specification that maps these requirements to components and delivery tasks. This document does not prescribe architecture.

Decisions to make in technical analysis:

1. Application structure and use of the requested Blazor interface.
2. Reuse of existing indicator or strategy code, only where it reduces complexity and matches these rules.
3. Historical-data provider integration, retrieval limits, validation, and reuse mechanism.
4. Persistent storage for historical data, run inputs, data identity, results, and transaction detail; database versus files remains open.
5. Simulation execution model, numerical precision, fee/slippage arithmetic, and residual-cash handling.
6. Metric calculation definitions and reproducible data/calculation versioning.
7. AI provider integration, application capabilities exposed to it, conversation context, validation, and bounded execution.
8. UI/report composition, charting, comparison presentation, and progress/error handling.
9. Focused verification using the acceptance scenarios and small deterministic datasets.

Do not silently change a business rule to accommodate implementation. Surface any contradiction or material trade-off and propose the smallest resolution.

Suggested delivery sequence, without removing any first-version requirements:

1. Data preparation plus one deterministic strategy report and benchmark.
2. Second strategy, persistent history, and comparisons.
3. Optional AI review and follow-up investigations.
4. AI-requested variations and the complete end-to-end demonstration.

## 13. Definition of done

The first version is complete when both strategies can be configured and run on validated historical data; reports and buy-and-hold benchmarks are correct and saved; matching runs can be compared; AI review is optional and evidence-based; a direct AI variation request creates a new valid run; and the acceptance scenarios above are satisfied.

The technical handoff must explicitly document the working conventions in section 5.5, but there are no outstanding product questions blocking technical analysis.
