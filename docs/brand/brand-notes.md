# PCOptimizer brand

## App icon

A blue chip square (#0078D4, with a light gradient from #2B8EE0 at the top left to #0068C2 at the bottom right, pins #0058A6) holding a white tuning knob. The knob's blue pointer sits at 45 degrees and points at a white index dot on the chip. Transparent background, no outline.

| File | Use |
|---|---|
| `pcoptimizer-icon.svg` | Master drawing (48 unit grid) |
| `pcoptimizer-icon-mono.svg` | Single color (`currentColor`), knob as knockout |
| `hinted/pcoptimizer-icon-{16,20,24,32}.svg`, `hinted/pcoptimizer-icon-mono-{16,20,24,32}.svg` | Pixel-grid redraws for small sizes; the 16 px version drops the index dot |
| `pcoptimizer-icon-1024.png`, `pcoptimizer-icon-256.png` | Raster exports (README, release page) |
| `logo-light.svg`, `logo-dark.svg` | Icon with the "PCOptimizer" wordmark |

The app's own icon files are in `src/Optimizer.App/Assets` (`app.ico` and 16, 24 and 32 px PNGs).

Rules: at 32 px and below, use the hinted files, not a scaled master. Recolor only the mono version.

## Colors and type

Colors are in `brand.css`: tile #0E1726, accent #4CC2FF, strong accent #0F6CBD, ink #1B1B1B. The app itself uses the Windows accent color and the WPF-UI theme brushes.

The wordmark uses Segoe UI Variable Display Semibold, a Windows system font that is not shipped. The SVG wordmarks use live text with a Segoe UI to system-ui fallback; convert the text to outlines before using them outside Windows.
