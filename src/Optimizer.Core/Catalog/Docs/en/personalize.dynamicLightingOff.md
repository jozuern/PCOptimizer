# Dynamic Lighting off

## Summary
Turns off Windows Dynamic Lighting, so the RGB software of your keyboard, mouse or PC maker controls the lighting alone.

## How it works
"Use Dynamic Lighting on my devices" turns Dynamic Lighting on or off; when it is off, devices use their behavior without Dynamic Lighting [1]. The app sets AmbientLightingEnabled to 0. Microsoft describes the switch [1] but does not document the registry value behind it; the app writes the value Windows itself stores for the switch. Until a test on real Windows confirms the effect, the tweak is a Preview.

## Why it can help
Windows and the manufacturer's RGB software no longer fight over the lights.

## Evidence
A personal preference without effect on frame rate or latency.

## Trade-offs & risks
Effects you set in Settings > Personalization > Dynamic Lighting stop.

## When not to use it
If you control your lighting with Windows.

## Sources
1. https://support.microsoft.com/en-us/windows/hardware/input-devices/control-dynamic-lighting-devices-in-windows
