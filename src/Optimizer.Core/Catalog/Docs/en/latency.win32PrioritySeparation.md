# Foreground priority boost (Win32PrioritySeparation)

## Summary
Sets short, variable time slices with the strongest boost for the foreground window (value 0x26). A classic tweak; effect disputed.

## How it works
Win32PrioritySeparation sets the length of CPU time slices and how much longer the foreground application's slices are than background ones [1]. Windows' default (2) already favors the foreground app; 0x26 uses short, variable slices with a 3:1 foreground boost.

## Why it can help
In theory, the game in the foreground gets the CPU back sooner when other processes compete for it.

## Evidence
Measurements on current multi-core CPUs show no consistent difference, because games rarely compete for a single core.

## Trade-offs & risks
Background tasks (downloads, encoding) get relatively less CPU time while you use another window.

## When not to use it
Not needed on CPUs with many cores. It is harmless to try and easy to undo.

## Sources
1. https://learn.microsoft.com/en-us/previous-versions/cc976120(v=technet.10)
