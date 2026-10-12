<p align="center"><img src="docs/brand/tunero-icon-256.png" width="96" alt="Tunero icon"></p>

# Tunero

A Windows 11 PC optimizer, gaming first, with profiles for laptops, battery, office, quiet and older PCs. It explains every finding and every change with sources, backs up what it changes and can undo it. A single exe for Windows 11 24H2 or newer, x64 only, in English and German.

> **Status: preview.** Every change is tested for apply and undo against a registry sandbox, and the [VM test plan](docs/vm-test-plan.md) runs on Windows 11 26H2 in a Hyper-V virtual machine before releases. A few parts need hardware a virtual machine does not have (NVIDIA driver settings, network adapter properties, laptop and battery checks, AMD-specific checks): tweaks that are not proven yet carry a **Not yet tested on a real PC** badge and are never part of "Apply recommended". Create a restore point or a backup before you change anything; the app creates one before its first change if System Protection is on.

<p align="center"><img src="docs/screenshots/flow.gif" width="800" alt="The scan, the recommended changes as a checklist, the confirmation with the exact values, the Changes page, and Undo all"></p>

## Download

Get `Tunero.exe` from the [Releases](https://github.com/jozuern/Tunero/releases) page and compare its SHA-256 with the `.sha256` file next to it (`Get-FileHash Tunero.exe`). The in-app update also checks the release signature (`Tunero.exe.sig`) and installs nothing without a valid one. The exe asks for administrator rights because it changes system settings. It is not code signed yet, so Windows SmartScreen may warn on first start; code signing by SignPath Foundation is applied for ([Code signing policy](#code-signing-policy)). The exe needs no installation; on the first start it offers an entry in the Start menu, so Windows Search finds it.

## What it does

**For everyday use**

- **Scan and fix:** 48 read-only checks find real problems (a monitor at 60 Hz, memory without XMP, the wrong power plan, a game library on a hard disk) and give BIOS and hardware advice (on the Manual checks page, which starts with a summary) and game access per anti-cheat. Each comes with what was found, why it matters, how to fix it and sources, in English and German. A check that cannot read something says "unknown" and gives no advice.
- **Recommended for this PC:** after the scan, the Overview lists the fixes and changes that fit this PC and your profile as a checklist. Untick what you do not want; one confirmation lists every change before it is made. Expert, boot-critical, anti-cheat sensitive, Preview and not fully reversible items are never included.
- **Profiles:** Gaming, Laptop gaming, Battery, Office, Quiet and cool, Older PC. A profile changes nothing by itself; it decides what is recommended and which impact is shown.
- **Undo:** every change is backed up first (plus a restore point once per session), verified after applying and listed on the Changes page with Undo, also after a restart. After a Windows update the app lists changes Windows reset and offers to apply them again.
- **Remove apps and clean up:** a reviewed list of 130 removable apps (inbox apps, older Microsoft apps, third-party promotions and manufacturer apps, with a warning on vendor update tools), desktop programs with their own uninstaller, and 12 cleanup categories that never follow links.
- **Windows Update without surprises** (Pro and higher, documented policies): no update restarts in the evening, restarts when you choose for up to a week, monthly updates one week later.
- **Search:** Ctrl+F searches the whole app: tweaks, checks, tools, settings, services, startup entries and apps. Looking for a popular tweak the app leaves out shows why.

**For power users**

- **222 catalog tweaks** with gaming impact from 0 to 5, how solid the evidence is (situational or disputed), risk and sources, grouped by area with "Turn on N" per group and a Show filter (for this profile, recommended for this PC, changed by this app, all that apply, or all), plus tweaks built for this PC: per-game NVIDIA profiles, MSI mode per device and interrupt affinity for the graphics card (Expert), startup entries, services, Windows features.
- **Exact changes:** the confirmation lists every registry value, service, task or setting with its current and new value; blocked changes say why.
- **Privacy, browsers and Windows:** documented policies for Microsoft Edge, Google Chrome and Brave (data collection, AI features, promotions), Windows AI features (Recall, Paint, Notepad), app permissions, notifications, the taskbar and Start, and security options such as LSA protection and Defender network protection.
- **Graphics & network:** NVIDIA driver settings through the documented driver settings interface (NVAPI), VRR state, network adapter power saving, a DNS server choice and an opt-in DNS benchmark.
- **Startup and services:** an Autoruns-style list of everything that starts with Windows with signature checks, VirusTotal lookups (your own key) and snapshots to compare, and service start types with explanations.
- **Apps that were not reviewed:** on request, Remove apps also lists the other removable apps, with a warning.
- **Tools and health:** storage analyzer, Windows features, program start priorities, network fixes, Windows Update repair, SFC and DISM, a frame time benchmark with PresentMon, throttle check, drive health, opt-in sensors.
- **Export, Import and command line:** move your changes to another PC or a fresh install, in the app or with `--apply` ([Command line](#command-line)).
- **System info:** everything the scan read, from processor and memory modules to displays, drives, firmware security and the power plan, with a button that copies it all for a forum post or a bug report.
- **Keyboard:** Ctrl+F search (Down into the results, Enter opens, Esc clears), F5 reads the page again or scans again, Esc closes the details pane.

The app uses settings that Microsoft or the hardware vendor documents. A few harmless, fully reversible tweaks use a value Microsoft does not document (the value behind a Windows Settings switch, or a widely used one such as the classic right-click menu): they carry an "Undocumented value" badge, their explanation page says so, and they stay previews until a test has shown that the value works. It never recommends turning off security features that anti-cheats require, and keeps changes that turn off security features or touch the boot configuration in Expert mode.

<p align="center"><img src="docs/screenshots/overview-en-dark.png" width="800" alt="Overview page: readiness score, what the scan found and the recommended changes as a checklist"></p>

<p align="center"><img src="docs/screenshots/tweaks-en-light.png" width="800" alt="Tweaks page: the Show and Profile filters, groups with Turn on, and the explanation of the selected tweak"></p>

<p align="center"><img src="docs/screenshots/system-info-en-dark.png" width="800" alt="System info page: summary tiles and one card per area"></p>

## What it does differently

- **It shows its work.** Every check and every change has a page with what it does, why, how solid the evidence is and the sources, in English and German.
- **Everything it changes can be undone.** Each change is backed up first and can be undone on the Changes page, also after a restart or an update of the app; Uninstall offers to undo everything before it removes the app. Removed apps and deleted files are the exception, and the app says so before.
- **No placebo.** Popular tweaks without evidence are left out, and the search says why when you look for one. The full list with sources is in [docs/not-included.md](docs/not-included.md).
- **It looks at your PC first.** It recommends only what fits this PC and your profile, and keeps risky changes behind Expert mode.
- **Nothing runs in the background.** No service, no tray icon, no telemetry. Windows applies the settings itself; the app runs only when you start it.

<p align="center"><img src="docs/screenshots/search-en-light.png" width="800" alt="Search for defender: the reason why turning off Defender is not offered comes first, then the Defender drivers and services"></p>

## Command line

For a fresh install or several PCs, export your changes on the Changes page and apply the file without the window:

```powershell
Start-Process Tunero.exe -ArgumentList '--apply', 'my-changes.tunero.json', '--log', 'result.txt' -Verb RunAs -Wait
```

| Switch | Effect |
|---|---|
| `--apply <file>` | Applies an exported changes file with the rules of Import: Preview, Expert and anti-cheat sensitive changes are listed in the log, not applied. |
| `--undo-all` | Undoes every change of this app. |
| `--no-restore-point` | Continues when no restore point can be created (System Protection off); without it, nothing is changed then. |
| `--log <file>` | Writes the result to a new text file; an existing file is never overwritten. |

Exit codes: 0 done, 1 some changes failed, 2 not a valid changes file, 3 no administrator rights, 4 no restore point.

## Privacy

No telemetry, no accounts. The app goes online only for features you start or turn on (update check, VirusTotal lookup, DNS benchmark, app installs). After an unexpected error it asks whether to open a GitHub bug report filled in with the error details, without your user or computer name; nothing is sent unless you submit it there. Details: [docs/PRIVACY.md](docs/PRIVACY.md).

## Uninstall

Settings > Uninstall removes Tunero from this PC. When changes of the app are still active, it offers to undo them first; if one cannot be undone, it removes nothing. Then the app closes, and a moment later its exe, its data folder `%ProgramData%\Tunero` (backups, logs, settings) and its Start menu entry are gone. A copy installed with winget is removed by winget (`winget uninstall jozuern.Tunero`), so winget's list stays right.

What stays: restore points, apps you installed through Tunero and the PawnIO sensor driver. Remove them in Windows under Settings > Apps > Installed apps if you no longer need them. To remove Tunero by hand: undo your changes on the Changes page, then delete the exe, the folder `%ProgramData%\Tunero` and `Tunero` in the Start menu folder (`%AppData%\Microsoft\Windows\Start Menu\Programs`).

## FAQ

**Was this called PCOptimizer?** Yes, up to version 0.7.3. Links to the old repository address and the in-app update keep working, and the data folder with your backups moves to the new name on the first start.

**Why is tweak X not included?** Many popular tweaks do nothing on Windows 11, use settings nobody documents, or cost more than they bring. Search for it in the app (Ctrl+F) to see the reason; [docs/not-included.md](docs/not-included.md) lists them all with sources, including the ones earlier versions offered.

**What does "Not yet tested on a real PC" mean?** This badge (a "Preview" in the catalog) marks tweaks that are risky or whose effect is not proven on real Windows yet: every Expert and boot-critical change, undocumented values without proof, and settings not yet tested on real hardware (such as the NVIDIA driver settings). "Apply recommended" never includes it. Apply it on its own, after a restore point, and check the result.

**Will it make my games faster?** Sometimes. The biggest gains come from fixing real problems the scan finds (a monitor at 60 Hz, memory without XMP, the wrong power plan). Most tweaks change little; each page says how solid the evidence is, and the benchmark on the Health page lets you measure your own games.

**Can I undo everything?** Every change the app makes is backed up and can be undone on the Changes page, also after a restart or an update of the app. Removing an inbox app cannot be undone by the app; the Store link stays where the Store still offers it.

**Does it work on Windows 11 Home?** Yes. Some Group Policy based tweaks only work on Pro, Enterprise or Education; the app does not offer them on Home.

**Why does it need administrator rights?** Most settings it changes are machine-wide. Its data folder (`%ProgramData%\Tunero`) is locked to administrators, and links or files planted there by other accounts are removed before use.

## License

Copyright (c) 2026 Joshua Zürn. Tunero is free software: you can use, study, change and share it under the [GNU General Public License version 3](LICENSE) (GPL-3.0-only). Versions you share, changed or not, must stay under the same license and come with their source code. It comes without warranty. Releases up to 0.7.1 were published under the PolyForm Strict License 1.0.0; later releases are under the GPL.

The exe contains third-party components under their own licenses (MIT, BSD-2-Clause, BSD-3-Clause, MPL-2.0, LGPL-2.1-or-later, Apache-2.0); the full texts are in the app under Settings > About > Licenses and listed in [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md).

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/). The project is applying to SignPath Foundation; until it is accepted, releases are not code signed, and this section describes how signing will work.

- **Who is involved:** the project has one member, [Joshua Zürn](https://github.com/jozuern), who is committer, reviewer and approver. Changes from anyone else (pull requests) are reviewed by him before they are merged. Signing requests are approved by him, one release at a time.
- **What gets signed:** only `Tunero.exe` as built by the [release workflow](.github/workflows/release.yml) on GitHub's runners from a version tag on `main`. SignPath checks that the file comes from that build and that its product name and version match the release ([artifact configuration](.signpath/artifact-configuration.xml)). Nothing built elsewhere and no third-party program is signed.
- **Accounts:** GitHub and SignPath accounts of the team use multi-factor authentication.
- **Privacy:** This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it. The features that go online when you start or turn them on are listed in [docs/PRIVACY.md](docs/PRIVACY.md).

Windows shows "SignPath Foundation" as the publisher of a signed release. To check a download: right-click the exe, Properties > Digital Signatures, or `Get-AuthenticodeSignature Tunero.exe` in PowerShell; and compare its SHA-256 with the `.sha256` file of the release.

## Support

Tunero is free. If it helps you and you want to give something back, you can support its development on [Ko-fi](https://ko-fi.com/jozuern) (also linked in the app under Settings > About). Bug reports and ideas help just as much.

## Credits

Tunero was built with the help of Claude, an AI assistant by Anthropic.

## Feedback and security

Bugs and ideas: open an issue. Security problems: see [SECURITY.md](SECURITY.md). Building from source and project layout: [CONTRIBUTING.md](CONTRIBUTING.md).
