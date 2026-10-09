# Security

PCOptimizer runs as administrator and changes Windows settings, so security problems matter more than in most apps.

## Reporting a vulnerability

Please do not open a public issue. Use **Report a vulnerability** on the repository's Security tab (GitHub private vulnerability reporting). Include the app version (Settings > About), the Windows build and the steps to reproduce.

Fixes ship in a new release, and the report is credited in the release notes if you want.

## In scope

- Anything that lets a non-administrator gain administrator or SYSTEM rights through the app, for example through files or folders a normal user can write (the data folder, temp files, tools the app starts).
- Changes the app makes that it does not show in the confirmation dialog, or that Undo does not restore.
- Downloads or processes the app starts without checking where they come from (winget, PresentMon, PawnIO).
- Leaks of the VirusTotal API key.

## Supported versions

Only the latest release gets fixes.
