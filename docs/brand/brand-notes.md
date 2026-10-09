# PCOptimizer — Brand

Windows 11 PC optimizer, gaming first, with profiles for laptops, battery, office, quiet and older PCs. Single self-contained exe for Windows 11 24H2+, x64, English and German. UI is Windows 11 Fluent (WPF-UI 4.3), Segoe UI Variable, Mica, light/dark following Windows, Windows accent color.

Source: https://github.com/jozuern/PCOptimizer (explore it for UI and copy context). The repo had no logo or app icon; the mark here is new, created on request. Scope is logo only, not a full design system.

## App icon
Chip square (#0078D4, subtle top-left → bottom-right gradient #2B8EE0 → #0068C2, pins #0058A6) holding a white tuning knob. The knob's blue pointer sits at 45° and lines up exactly with a white index dot on the chip: a measured setting. Transparent background, no outline.
- `assets/icon/pcoptimizer-icon.svg` — master (48 grid, 1024 px)
- `assets/icon/pcoptimizer-icon-mono.svg` — single color, `currentColor`, knob as knockout
- `assets/icon/hinted/` — pixel-grid redraws for 32, 24, 20, 16 (color + mono). 16 px drops the index dot.
- `assets/icon/png/` — 1024, 256, 48, 32, 24, 20, 16
- `assets/logo-light.svg`, `assets/logo-dark.svg` — icon + "PCOptimizer" wordmark

Rules: always use the hinted file at ≤32 px, never a downscale of the master. Don't recolor except the mono version.

Colors (`tokens/brand.css`): tile #0E1726, accent #4CC2FF, accent strong #0F6CBD, ink #1B1B1B.

Type: wordmark uses Segoe UI Variable Display Semibold (system font, not shipped). The SVG wordmarks use live text with a Segoe → system-ui fallback; outline the text before production use.

## Index
- `styles.css` → `tokens/brand.css`
- `assets/` logos
- `guidelines/logo.card.html` logo card
- `thumbnail.html`, `SKILL.md`, `github.md`
