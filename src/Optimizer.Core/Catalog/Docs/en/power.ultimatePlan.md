# Ultimate Performance power plan

## Summary
Creates and activates a copy of the hidden Ultimate Performance plan, a step beyond High performance. Any gaming benefit over High performance is disputed.

## How it works
Windows contains a hidden plan for workstations, Ultimate Performance. Microsoft describes it as building on High performance and going a step further to remove micro-latencies caused by fine-grained power management [1]. The app duplicates it under the name "PCOptimizer Ultimate" and activates the copy.

## Why it can help
It can shave off small wake-up delays of the processor and devices. Microsoft built it for demanding workstation workloads [1].

## Evidence
We found no reliable gaming measurement that shows a difference to High performance beyond run-to-run variance. Treat it as disputed.

## Trade-offs & risks
More power use than Balanced; Microsoft warns that the plan may directly affect hardware [1]. Higher idle heat and fan noise. Not offered on laptops, on PCs with Modern Standby [2] or on Ryzen X3D processors with two chiplets.

## When not to use it
Do not use on multi-chiplet Ryzen X3D CPUs, on laptops, or if quiet and cool idle operation matters to you.

## Sources
1. https://blogs.windows.com/windows-insider/2018/02/14/announcing-windows-10-insider-preview-build-17101-fast-build-17604-skip-ahead/
2. https://learn.microsoft.com/en-us/windows/win32/power/power-policy-settings
