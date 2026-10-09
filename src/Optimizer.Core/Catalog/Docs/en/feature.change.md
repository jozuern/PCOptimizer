# Turn a Windows feature on or off

## Summary
Turns this optional Windows feature on or off with DISM, like Settings > System > Optional features. Usually needs a restart.

## How it works
The app runs DISM to enable or disable the feature [1]. Turning a feature off keeps its files, so turning it on again works offline in most cases. Undo restores the previous state.

## Why it can help
Features you do not use (for example SMB 1.0) are one less thing that can be attacked. Turning off hypervisor features (Hyper-V, Virtual Machine Platform, Sandbox) stops the hypervisor from starting, unless memory integrity or another feature still needs it.

## Evidence
Most features have no effect on games. The hypervisor ones can, depending on the processor and whether memory integrity is on.

## Trade-offs & risks
Programs that need the feature stop working (for example WSL, virtual machines, old games without DirectPlay).

## When not to use it
If a program you use needs the feature.

## Sources
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/enable-or-disable-windows-features-using-dism
