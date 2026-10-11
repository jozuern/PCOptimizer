# Desktop wallpaper without JPEG recompression

## Summary
Windows keeps JPEG wallpapers at full quality instead of recompressing them to 85 %. Sign in again and set the wallpaper again. Value Microsoft does not document, tested.

## How it works
When you set a JPEG image as the wallpaper, Windows saves a recompressed copy at 85 % quality [1]. JPEGImportQuality = 100 under Control Panel\Desktop keeps full quality [1]. It applies to the next wallpaper you set after signing in again [1]. A wallpaper Windows has already converted stays as it is until you set a new image. Microsoft does not document the value.

## Why it can help
Sharper wallpapers without JPEG artifacts, visible on large or high-resolution screens.

## Evidence
In a VM test on Windows 11 build 26300, a JPEG wallpaper set after signing in again was stored at quality 100; without the value a new one was stored at about 85. No effect on frame rate or latency.

## Trade-offs & risks
The saved copy of the wallpaper gets larger. PNG wallpapers are not affected [1].

## When not to use it
If you do not use a JPEG wallpaper.

## Sources
1. https://winaero.com/how-to-disable-wallpaper-jpeg-quality-reduction-in-windows-10
