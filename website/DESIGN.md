# tunero.dev design rules

The site is the app's manual cover, not a startup landing page, and it must never look generated: no gradients, glow,
glass, icon grids, pill badges, emoji, centered heroes or invented numbers. It stays interesting by showing the work:
real catalog data drawn at build time, real registry values, real screenshots, the tweaks it crosses out. It should feel like it belongs on Windows 11 next to the
app: the same type, the same accent button, the same screenshots. Every rule below can be checked.

## Voice

- Same rules as the app: no en or em dashes, no middle dots, no ellipsis characters (the tests check the copy), no emoji.
  Strong or promotional wording is fine where it is true; claims we cannot back stay out.
- The positioning: built to be the only PC optimizer you need. Say it as the aim ("built to be"), never as a measured
  fact, and back it on the page with what the app holds.
- Say what it does with real numbers and real examples taken from the catalog, never invented ones.
- Numbers come from the repository at build time (`src/lib/data.ts`): never type a count by hand.
- Admit limits on the page (preview status, unsigned exe, what it leaves out).
- Install commands only for package managers that already offer the app (`packages` in `src/lib/data.ts`).

## Type

- Text: `Segoe UI Variable Text`, display: `Segoe UI Variable Display`, code: `Cascadia Mono`. All ship with Windows 11,
  which is every visitor who can run the app. Other systems fall back to `system-ui` and `ui-monospace`.
- No web fonts, so no request leaves the page.
- Scale (px): 13, 15, 17 (body), 20, 26, 34, 48, 64. Line height 1.6 for body, 1.1 for display.
- Display weight 600 with -0.02em tracking; body 400; labels in Cascadia Mono 13 px, uppercase only for section numbers.
- Measure: body text at most 66ch.

## Color

| Token | Light | Dark |
|---|---|---|
| `--bg` | #FAFAF9 | #0B121E |
| `--ink` | #1B1B1B | #EEF1F5 |
| `--ink-2` (secondary) | #505050 | #AEB7C4 |
| `--rule` | #E2E2DF | #1F2B40 |
| `--accent` (links) | #0F6CBD | #4CC2FF |
| button | #0F6CBD on white text | #4CC2FF with #0E1726 text |
| `--code` | #F1F1EF | #111B2B |
| `--stage` (hero band) | #EFEFEC | #111C2F |

The dark page uses the brand tile navy, not black. Two accents only, the Windows ones. No gradients anywhere.

## Shape

- No cards. Sections are separated by a 1 px rule and space; content sits on the page.
- Corner radius: 4 px on buttons and code (as Windows 11 controls), 8 px on window screenshots (as Windows 11 windows).
- One shadow in the whole site: the window shadow under screenshots.
- One band: the hero sits on `--stage` with the header, and the Overview screenshot hangs over its lower edge.
- Heavy rules (2 px, ink) open things you read as a set: the spec sheet, steps, the impact chart, the counts, the
  explanation page. Light rules (1 px) separate items inside a list and the sections.
- No icons next to headings, no colored left borders, no pills, no badges invented for the site.

## Layout

- Container 1160 px, gutter 24 px (16 px under 600 px). No horizontal scroll at 320 px.
- Sections: a 1 px rule, then a two column grid at 960 px and wider: a 200 px margin column with the section number and
  name in Cascadia Mono, the content on the right. One column below 960 px, the label above the heading.
- Spacing steps (px): 4, 8, 12, 16, 24, 32, 48, 64, 96.
- Left aligned throughout. Nothing centered except nothing.

## Components

- **Check list:** every check by the title of its explanation page, numbered, in 1 to 3 columns (`checkTitles`; the build
  fails when the number differs from the README).
- **Steps:** numbered columns under a heavy rule (undo, install).
- **Impact chart:** one 12 px square per tweak in rows from 5 to 0, filled `--ink` for measured, accent for situational, `--ink-3` for disputed,
  an accent outline in the row of the highest rating a condition gives it on some PCs (`tweakImpacts`). The square's
  title names the tweak; each row has its count and a screen reader sentence. Legend always shown.
- **Explanation page:** the app's page set like print, the sources in the margin next to the text, references as small
  numbers that jump to them.
- **Left out:** the tweak's name crossed out with a 2 px ink line, the reason and its source below.
- **Counts:** the four catalog counts at 48 to 64 px, tabular figures, plain, no counting animation.
- **App areas:** the app's pages by their names in the app, one line each, in 1 to 3 columns under light rules. They back
  the claim in the hero (built to be the only optimizer you need) with what the app holds.

## Images

- Only real screenshots of the app, rendered with `--screenshot` (see CONTRIBUTING), never mockups or illustrations.
- Light screenshots in light mode and dark ones in dark mode when both exist (`<picture>` with `prefers-color-scheme`).
- Every screenshot has alt text that says what it shows.

## Motion and script

- No animation. No script except copying the winget command.
