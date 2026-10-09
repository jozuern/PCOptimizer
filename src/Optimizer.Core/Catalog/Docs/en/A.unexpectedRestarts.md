# Unexpected shutdowns

## Summary
::: variant stop
Unexpected shutdowns in the last 30 days: {{count}}. At least one followed a Stop error ({{stopCode}}), so Windows crashed.
:::
::: variant power
Unexpected shutdowns in the last 30 days: {{count}}. No Stop error was recorded: power loss, a hang or a forced power off.
:::
::: variant default
Checks the System event log for unexpected shutdowns (Kernel-Power event 41) in the last 30 days.
:::

## Why it matters
When the PC shuts down unexpectedly, Windows logs event 41 at the next start. Such a shutdown is caused by an interruption of the power supply or by a Stop error (blue screen) [1]. Without a Stop error code, Microsoft points to the power supply, a hang that was ended with the power button, overclocking, the memory and overheating [1]. Every unexpected shutdown loses unsaved work and can corrupt files [1].

## How we detected it
We read the Kernel-Power events with ID 41 of the last 30 days from the System log. When the event holds a Stop error code (`BugcheckCode`), Windows crashed; when `PowerButtonTimestamp` is set, the PC was turned off by holding the power button [1].
::: if powerButton
At least one of these shutdowns was done with the power button.
:::

## How to fix
::: variant stop
1. Look up the Stop error code {{stopCode}} in Microsoft's bug check reference [2] to see which part of the system it points to.
2. Update the graphics, chipset and network drivers and the BIOS.
3. Turn off overclocking and memory profiles (XMP or EXPO) for a test. If the crashes stop, those settings were not stable [1].
:::
::: variant power
1. If the power went out or you turned the PC off with the power button on purpose, there is nothing to fix.
2. Otherwise check that the power supply has enough wattage for the installed parts [1].
3. Turn off overclocking and memory profiles (XMP or EXPO) for a test, and check temperatures under load (throttle check on the Health page) [1].
:::
::: variant default
Nothing to do.
:::

## How to check the fix
Run the scan again after a few days of normal use. No new unexpected shutdowns should be listed.

## Sources
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/event-id-41-restart
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-code-reference2
