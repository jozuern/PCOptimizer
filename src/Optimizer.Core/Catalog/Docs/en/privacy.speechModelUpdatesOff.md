# No automatic speech model updates

## Summary
Windows stops downloading updated speech recognition and speech synthesis models in the background. Speech features keep working with the models already on the PC. Pro and up.

## How it works
Speech models hold the data the speech engine uses to turn audio into text and text into speech. Windows checks for updated models from time to time and downloads them from a Microsoft service with the Background Intelligent Transfer Service (BITS) [1]. The policy AllowSpeechModelUpdate set to 0 stops these checks and downloads [1]. The models are data files, not programs [1], so this is about network use and fresh data, not about security updates.

## Why it can help
Fewer background downloads and connections to Microsoft, which matters on a metered or slow connection.

## Evidence
A documented Windows policy [1]; Microsoft lists it for Pro, Enterprise and Education [1]. A privacy and network setting without effect on frame rate or latency.

## Trade-offs & risks
Speech recognition and speech output keep the models they have and no longer get the accuracy and performance improvements of newer ones [1].

## When not to use it
If you rely on speech recognition or speech output on this PC and want their models kept current.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-speech
