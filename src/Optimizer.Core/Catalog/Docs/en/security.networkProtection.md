# Microsoft Defender: network protection

## Summary
Blocks connections from any app, not only the browser, to domains Microsoft rates as dangerous, such as phishing and exploit sites. Pro, Enterprise and Education.

## How it works
Network protection prevents any application from accessing dangerous domains that may host phishing scams, exploit-hosting sites and other malicious content [1]. The policy EnableNetworkProtection has three values: 0 disabled (default), 1 block, 2 audit [1]. The app sets 1. It works when Microsoft Defender is the active antivirus.

## Why it can help
Protection against malicious sites also in apps that have no SmartScreen, such as chat programs and game launchers.

## Evidence
A documented Defender policy [1]. Microsoft publishes no measurement of its effect on games.

## Trade-offs & risks
A site wrongly rated as dangerous is blocked in every app until Microsoft corrects the rating.

## When not to use it
If you use another antivirus, or a blocked site you need cannot be reached.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-defender
