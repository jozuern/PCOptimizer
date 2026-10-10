# Recall removed

## Summary
Makes Recall unavailable and removes its components after a restart. Snapshots Recall saved are deleted; Undo does not bring them back. Pro, Enterprise and Education.

## How it works
The policy AllowRecallEnablement set to 0 means Recall is not available: the Recall component is disabled and its files are removed from the PC, and snapshots saved before are deleted [1]. Removing Recall needs a restart [1]. Microsoft lists the policy for Windows 11 24H2 with the April 2025 update and later, on Pro, Enterprise and Education [1].

## Why it can help
Recall cannot be turned on again by accident, and its components no longer take space on the PC.

## Evidence
A documented Windows policy [1]. Recall exists only on Copilot+ PCs; elsewhere the policy has nothing to remove. No published gaming measurement.

## Trade-offs & risks
Saved Recall snapshots are deleted and Undo does not bring them back [1]. After Undo, Recall can be added again from Windows features. For only stopping new snapshots, use the tweak "Recall snapshots off" instead.

## When not to use it
If you use Recall or might want it later.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
