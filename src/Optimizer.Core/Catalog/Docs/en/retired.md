# Removed tweak

## Summary
An earlier version of PCOptimizer made this change. The tweak is no longer offered. Undo puts back the values from before the change.

## How it works
The backup of this change keeps every original value and the exact step that changed it. Undo uses that backup, so it works although the tweak is gone from the list.

## Why it can help
Undo returns Windows to the state before the change, which is the safe choice for a tweak that was removed.

## Evidence
Tweaks are removed when the evidence does not support them: no measurable effect, settings Microsoft does not document, or a cost that outweighs the gain. The list of removed tweaks and the reasons is in docs/not-included.md in the project repository [1].

## Trade-offs & risks
None from undoing it. Values that Windows has changed since are left alone.

## When not to use it
Keep the change only if you know you want it; the app no longer checks whether it is still in place.

## Sources
1. https://github.com/jozuern/PCOptimizer/blob/main/docs/not-included.md
