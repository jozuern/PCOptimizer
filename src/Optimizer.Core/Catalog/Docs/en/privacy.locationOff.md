# Location access off

## Summary
Denies location access for apps system-wide.

## How it works
The consent store value for location is set to Deny, which turns off location access for all apps, as the switch in Settings > Privacy > Location does [1].

## Why it can help
Fewer background location lookups. No performance effect.

## Evidence
Privacy setting; no frame-rate effect.

## Trade-offs & risks
Maps, weather and Find my device stop using your location.

## When not to use it
Keep location on if you use apps that need it.

## Sources
1. https://github.com/ChrisTitusTech/winutil
