# Default printer stays as you set it

## Summary
Windows no longer changes your default printer to the one you used last.

## How it works
The policy "Turn off Windows default printer management" (LegacyDefaultPrinterMode = 1) means Windows does not manage the default printer [1].

## Why it can help
Print jobs go to the printer you chose as default, not to the PDF printer you used once.

## Evidence
A documented Windows policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
When you move between places with different printers, you change the default yourself.

## When not to use it
If you like Windows to switch the default printer for you.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-printing
