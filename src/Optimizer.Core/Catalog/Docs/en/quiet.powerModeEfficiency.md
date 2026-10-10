# Power mode: Best power efficiency

## Summary
Sets the Windows power mode (Settings > System > Power) to Best power efficiency. The processor favors efficiency over speed, which lowers heat and fan noise.

## How it works
Power modes are overlays on the Balanced plan that the manufacturer and Windows tune for efficiency or performance [1]. The app selects "Best power efficiency" through the power mode interface of Windows (PowerSetActiveOverlayScheme), the same one the Settings page uses; Microsoft does not document this interface. It only exists with the Balanced plan, so the tweak applies only then.

## Why it can help
In this mode Windows lets the processor reach high clocks less eagerly. Short bursts use less power, so the PC produces less heat and the fans spin up less often.

## Evidence
The power modes are documented by Microsoft [1]. On PCs without a battery, Windows has no built-in values for the modes; what a mode changes comes from the PC maker or the processor vendor [1]. On some desktops this mode therefore changes little or nothing.

## Trade-offs & risks
Lower responsiveness and lower performance in games. In the Gaming profiles the scan reports this mode as a problem. Offered on desktop PCs only: laptops keep separate modes for battery and power adapter, which this tweak would not cover.

## When not to use it
For gaming or when you need full speed. On laptops, use the power mode in Settings for battery and power adapter separately.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
