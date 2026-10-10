# Notepad: AI features off

## Summary
Turns off the AI features in Windows Notepad, such as rewriting and summarizing text.

## How it works
The Notepad policy DisableAIFeaturesInNotepad writes DisableAIFeatures = 1 under SOFTWARE\Policies\WindowsNotepad; with it, users cannot access AI features in Notepad [1]. It needs Windows 11 22H2 or later and Notepad 11.2503.16.0 or later [1].

## Why it can help
Notepad stays a plain text editor, and no text goes to an AI service by a mis-click.

## Evidence
A documented Notepad policy [1]. No effect on frame rate or latency.

## Trade-offs & risks
The AI buttons in Notepad are gone.

## When not to use it
If you rewrite or summarize text with Notepad's AI features.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/manage-notepad
