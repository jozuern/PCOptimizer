# Recall and Copilot off

## Summary
Turns off Recall snapshots (on Copilot+ PCs) and Windows Copilot via policy.

## How it works
The policy DisableAIDataAnalysis = 1 stops Recall from saving snapshots of your screen [1]. TurnOffWindowsCopilot = 1 in your user policies turns off Windows Copilot.

## Why it can help
Recall analyzes screen content in the background; turning it off removes that work.

## Evidence
Recall only exists on Copilot+ PCs; elsewhere the policy does nothing. No measurable gaming effect on other PCs.

## Trade-offs & risks
You lose Recall search and the Copilot integration in Windows.

## When not to use it
Keep them if you use Recall or Copilot.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
