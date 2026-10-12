<p align="center"><img src="docs/brand/pcoptimizer-icon-256.png" width="96" alt="PCOptimizer icon"></p>

# PCOptimizer

A Windows 11 PC optimizer, gaming first, with profiles for laptops, battery, office, quiet and older PCs. It explains every finding and every change with sources, backs up what it changes and can undo it. A single exe for Windows 11 24H2 or newer, x64 only, in English and German.

> **Status: preview (0.6).** Apply and undo are tested against a registry sandbox and fake system interfaces, and the [VM test plan](docs/vm-test-plan.md) was run on Windows 11 26H2 in a Hyper-V virtual machine: the catalog tweaks (some do not apply in a virtual machine), the general flow and the pages that change the PC. Not checked on real hardware yet: NVIDIA driver settings, network adapter properties, laptop profiles, battery checks and AMD-specific checks. Tweaks marked **Preview** are the risky ones. Create a restore point or a backup before you change anything, and start with the recommended items.

<p align="center"><img src="docs/screenshots/overview-en-dark.png" width="800" alt="Overview page: readiness score, profile, recommended changes and findings"></p>

## Download

Get `PCOptimizer.exe` from the [Releases](https://github.com/jozuern/PCOptimizer/releases) page and compare its SHA-256 with the `.sha256` file next to it (`Get-FileHash PCOptimizer.exe`). The in-app update also checks the release signature (`PCOptimizer.exe.sig`) and installs nothing without a valid one. The exe asks for administrator rights because it changes system settings. It is not code signed yet, so Windows SmartScreen may warn on first start; code signing by SignPath Foundation is applied for ([Code signing policy](#code-signing-policy)). The exe needs no installation; on the first start it offers an entry in the Start menu, so Windows Search finds it.

## What it does

- **Scan:** 48 read-only checks (problems, BIOS and hardware advice, game access per anti-cheat). Each comes with what was found, why it matters, how to fix it and sources, in English and German. A check that cannot read something says "unknown" and gives no advice.
- **Tweaks:** 219 catalog tweaks with gaming impact from 0 to 5, how solid the evidence is (situational or disputed), risk and sources, plus tweaks built for this PC: per-game NVIDIA profiles, MSI mode per device and interrupt affinity for the graphics card (Expert), startup entries, services, Windows features.
- **Profiles:** Gaming, Laptop gaming, Battery, Office, Quiet and cool, Older PC. A profile changes nothing by itself; it decides what "Apply recommended" includes and which impact is shown.
- **Apply recommended:** one confirmation for the fixes of detected problems and the tweaks that fit this PC, each with its reason. Expert, boot-critical, anti-cheat sensitive, Preview and not fully reversible items are never included.
- **Undo:** every change is backed up first (plus a restore point once per session), verified after applying and listed on the Changes page with Undo, also after a restart. After a Windows update the app lists changes Windows reset and offers to apply them again. Export and Import move the catalog tweaks and start priorities to another PC or a fresh install.
- **Privacy, browsers and Windows:** documented policies for Microsoft Edge, Google Chrome and Brave (data collection, AI features, promotions), Windows AI features (Recall, Paint, Notepad), app permissions, Windows Update, notifications, the taskbar and Start, and security options such as LSA protection and Defender network protection. Edge policies that Edge ignores in profiles signed in with a personal Microsoft account say so.
- **Graphics & network:** NVIDIA driver settings through the documented driver settings interface (NVAPI), VRR state, network adapter power saving, a DNS server choice and an opt-in DNS benchmark.
- **Debloat, cleanup, startup, services:** a reviewed list of 130 removable apps (inbox apps, older Microsoft apps, third-party promotions and manufacturer apps, with a warning on vendor update tools and a note when no Store listing is confirmed for an app), 12 cleanup categories that never follow links, an Autoruns-style startup list with signature checks, service start types with explanations.
- **Tools and health:** storage analyzer, Windows features, Windows Update repair, SFC and DISM, a frame time benchmark with PresentMon, throttle check, drive health, opt-in sensors.
- **System info:** everything the scan read, from processor and memory modules to displays, drives, firmware security and the power plan: summary tiles on top, one card per area, IDs and raw values on request, and a button that copies it all for a forum post or a bug report.

The app uses settings that Microsoft or the hardware vendor documents. A few harmless, fully reversible tweaks use a value Microsoft does not document (the value behind a Windows Settings switch, or a widely used one such as the classic right-click menu): they carry an "Undocumented value" badge, their explanation page says so, and they stay previews until a test has shown that the value works. It never recommends turning off security features that anti-cheats require, and keeps changes that turn off security features or touch the boot configuration in Expert mode.

<p align="center"><img src="docs/screenshots/tweaks-en-light.png" width="800" alt="Tweaks page with search, tweaks grouped by category, badges and the explanation of the selected tweak"></p>

<p align="center"><img src="docs/screenshots/system-info-en-dark.png" width="800" alt="System info page: summary tiles and one card per area"></p>

## Privacy

No telemetry, no accounts. The app goes online only for features you start or turn on (update check, VirusTotal lookup, DNS benchmark, app installs). Details: [docs/PRIVACY.md](docs/PRIVACY.md).

## Uninstall

Settings > Uninstall removes PCOptimizer from this PC. When changes of the app are still active, it offers to undo them first; if one cannot be undone, it removes nothing. Then the app closes, and a moment later its exe, its data folder `%ProgramData%\PCOptimizer` (backups, logs, settings) and its Start menu entry are gone. A copy installed with winget is removed by winget (`winget uninstall jozuern.PCOptimizer`), so winget's list stays right.

What stays: restore points, apps you installed through PCOptimizer and the PawnIO sensor driver. Remove them in Windows under Settings > Apps > Installed apps if you no longer need them. To remove PCOptimizer by hand: undo your changes on the Changes page, then delete the exe, the folder `%ProgramData%\PCOptimizer` and `PCOptimizer` in the Start menu folder (`%AppData%\Microsoft\Windows\Start Menu\Programs`).

## FAQ

**Why is tweak X not included?** Many popular tweaks do nothing on Windows 11, use settings nobody documents, or cost more than they bring. [docs/not-included.md](docs/not-included.md) lists them with the reason and sources, including the ones earlier versions offered.

**What does "Preview" mean?** The tweak is risky, or its effect is not proven on real Windows yet: every Expert and boot-critical change, undocumented values without proof, and settings not yet tested on real hardware (such as the NVIDIA driver settings). "Apply recommended" never includes it. Apply it on its own, after a restore point, and check the result.

**Will it make my games faster?** Sometimes. The biggest gains come from fixing real problems the scan finds (a monitor at 60 Hz, memory without XMP, the wrong power plan). Most tweaks change little; each page says how solid the evidence is, and the benchmark on the Health page lets you measure your own games.

**Can I undo everything?** Every change the app makes is backed up and can be undone on the Changes page, also after a restart or an update of the app. Removing an inbox app cannot be undone by the app; the Store link stays where the Store still offers it.

**Does it work on Windows 11 Home?** Yes. Some Group Policy based tweaks only work on Pro, Enterprise or Education; the app does not offer them on Home.

**Why does it need administrator rights?** Most settings it changes are machine-wide. Its data folder (`%ProgramData%\PCOptimizer`) is locked to administrators, and links or files planted there by other accounts are removed before use.

## License

Copyright (c) 2026 Joshua Zürn. PCOptimizer is free software: you can use, study, change and share it under the [GNU General Public License version 3](LICENSE) (GPL-3.0-only). Versions you share, changed or not, must stay under the same license and come with their source code. It comes without warranty. Releases up to 0.7.1 were published under the PolyForm Strict License 1.0.0; later releases are under the GPL.

The exe contains third-party components under their own licenses (MIT, BSD-2-Clause, BSD-3-Clause, MPL-2.0, LGPL-2.1-or-later, Apache-2.0); the full texts are in the app under Settings > About > Licenses and listed in [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md).

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/). The project is applying to SignPath Foundation; until it is accepted, releases are not code signed, and this section describes how signing will work.

- **Who is involved:** the project has one member, [Joshua Zürn](https://github.com/jozuern), who is committer, reviewer and approver. Changes from anyone else (pull requests) are reviewed by him before they are merged. Signing requests are approved by him, one release at a time.
- **What gets signed:** only `PCOptimizer.exe` as built by the [release workflow](.github/workflows/release.yml) on GitHub's runners from a version tag on `main`. SignPath checks that the file comes from that build and that its product name and version match the release ([artifact configuration](.signpath/artifact-configuration.xml)). Nothing built elsewhere and no third-party program is signed.
- **Accounts:** GitHub and SignPath accounts of the team use multi-factor authentication.
- **Privacy:** This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it. The features that go online when you start or turn them on are listed in [docs/PRIVACY.md](docs/PRIVACY.md).

Windows shows "SignPath Foundation" as the publisher of a signed release. To check a download: right-click the exe, Properties > Digital Signatures, or `Get-AuthenticodeSignature PCOptimizer.exe` in PowerShell; and compare its SHA-256 with the `.sha256` file of the release.

## Support

PCOptimizer is free. If it helps you and you want to give something back, you can support its development on [Ko-fi](https://ko-fi.com/jozuern). Bug reports and ideas help just as much.

## Credits

PCOptimizer was built with the help of Claude, an AI assistant by Anthropic.

## Feedback and security

Bugs and ideas: open an issue. Security problems: see [SECURITY.md](SECURITY.md). Building from source and project layout: [CONTRIBUTING.md](CONTRIBUTING.md).
