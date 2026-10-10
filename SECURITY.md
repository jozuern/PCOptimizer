# Security

PCOptimizer runs as administrator and changes Windows settings, so security problems matter more than in most apps.

## Reporting a vulnerability

Please do not open a public issue. Use **Report a vulnerability** on the repository's Security tab (GitHub private vulnerability reporting). Include the app version (Settings > About), the Windows build and the steps to reproduce.

Fixes ship in a new release, and the report is credited in the release notes if you want.

## In scope

- Anything that lets a non-administrator gain administrator or SYSTEM rights through the app, for example through files or folders a normal user can write (the data folder, temp files, tools the app starts).
- Changes the app makes that it does not show in the confirmation dialog, or that Undo does not restore.
- Downloads or processes the app starts without checking where they come from (winget, PresentMon, PawnIO).
- An update the app installs although its release signature (`PCOptimizer.exe.sig`) is missing or does not match.
- Leaks of the VirusTotal API key.

## Known limits

- The .NET runtime reads some switches from the user's environment variables before any code of the app runs: a profiler (`CORECLR_ENABLE_PROFILING`, notification profilers), a diagnostic port it connects to (`DOTNET_DiagnosticPorts`, where a client can attach a profiler), EventPipe traces and crash dumps written to a chosen path. The app refuses to continue as administrator when one of them is set, but code loaded through them has run by then; .NET has no setting in the app itself that turns them off. Startup hooks (`DOTNET_STARTUP_HOOKS`) are turned off in the build.

## Supported versions

Only the latest release gets fixes.
