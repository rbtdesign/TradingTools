# Strategy Evaluator implementation plan

Status legend: **DONE**, **IN PROGRESS**, **NOT DONE**.

| Step | Status | Work |
| --- | --- | --- |
| 1 | **DONE** | Inspect the existing modular monolith, provider patterns, dashboard lifecycle, and architecture guidance. |
| 2 | **DONE** | Add the Strategy Evaluation domain and application use cases, including validation, deterministic crossover/DCA/benchmark accounting, metrics, comparison, and immutable run models. |
| 3 | **DONE** | Extend Binance for completed daily candle retrieval and add durable local candle/run storage with validation, reuse, identity, and calculation versioning. |
| 4 | **DONE** | Build the Blazor configure/report/history/comparison workflow and optional evidence-grounded AI review/variation experience. |
| 5 | **DONE** | Add deterministic acceptance-focused tests and update the architecture handoff, including all section 5.5 conventions and implementation trade-offs. |
| 6 | **DONE** | Build/test the solution, inspect the final diff, update all statuses, commit the changes, and create the pull request. |

## Technical defaults

- Use decimal arithmetic, fractional units rounded down to 12 decimal places, and retain residual cash.
- Use UTC inclusive dates and completed Binance daily candles only.
- Persist provider-neutral candle snapshots and completed run reports as JSON files under the user's local application-data directory; write atomically and never mutate completed runs.
- Identify a validated candle set with a SHA-256 digest and use calculation version `1.0.0`.
- Keep AI commentary outside immutable run reports. Only explicit review or supported variation requests may invoke an action.
