# Recall snapshots off

## Summary
Stops Recall from saving screen snapshots on Copilot+ PCs and deletes snapshots already saved. Undo does not bring deleted snapshots back.

## How it works
The policy DisableAIDataAnalysis = 1 (Turn off saving snapshots for use with Recall) stops Recall from saving snapshots; snapshots already on the PC are deleted when the policy is applied [1]. It needs Windows 11 24H2 with the April 2025 update or later [1].

## Why it can help
Recall captures and analyzes screen content in the background; with the policy set, that work stops.

## Evidence
Recall exists only on Copilot+ PCs; elsewhere the policy does nothing. No published gaming measurement.

## Trade-offs & risks
Existing Recall snapshots are deleted and Undo does not bring them back [1]. You lose Recall search. This tweak does not affect the Copilot app; uninstall it in Settings > Apps > Installed apps if you do not want it.

## When not to use it
Keep Recall if you use it.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
