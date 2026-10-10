# Explanation style guide

Every finding, advisor item and tweak has one page per language in
`src/Optimizer.Core/Catalog/Docs/<lang>/<id>.md`. The docs lint enforces the structure. A lint issue is a warning in Debug builds and an error in Release builds.

## Structure (findings and advisor items)

| English | German | Notes |
|---|---|---|
| `# Title` | `# Titel` | may use `{{param}}` |
| `## Summary` | `## Zusammenfassung` | ≤ 200 characters per status/variant, shown in the list |
| *(generated)* What we found | *(generated)* Was wir gefunden haben | from the finding's facts, never hand-written |
| `## Why it matters` | `## Warum das wichtig ist` | mechanism, effect in games, conditions |
| `## How we detected it` | `## Wie wir es erkennen` | data source and how certain it is |
| `## How to fix` | `## So behebst du es` | numbered steps, vendor-specific via params |
| `## How to check the fix` | `## So prüfst du die Behebung` | usually: run the scan again |
| `## Sources` | `## Quellen` | numbered URLs; every `[n]` in the text must resolve |

Conditional blocks: `::: status Problem,Ok`, `::: variant smr`, `::: if param`, `::: ifnot param`, closed with `:::`.

## Writing rules

- Facts over adjectives. No "boost", "unleash", "massive", "guaranteed" (lint list in `DocLint.BannedWords`).
- Give numbers (%, ms) only with a source, or as plain arithmetic (60 Hz = 16.7 ms).
- Disputed effects say so in the summary.
- Explain the mechanism in three steps: what Windows normally does, what this setting changes, and when that matters in games.
- German: write natural German, using "du", not a word-for-word translation. A native speaker reviews it before a release.

- No middle dots, en or em dashes, or ellipsis characters in user-visible text. Menu paths use > (Settings > System).
