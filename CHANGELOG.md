# Changelog

## Unreleased

### Changed

- **Ignore a scan result:** a result you keep on purpose (a game library on a hard disk as a backup, or because copying is faster than downloading) can be ignored in its details. It leaves the list, the score and the problem count, and stays under "Ignored results" on the Overview with Stop ignoring. Only that result is ignored: another library on a hard disk still shows.
- **Apply my changes again when Windows resets them** (Settings, off by default): a scheduled task starts Tunero two minutes after you sign in, as your account with administrator rights, and applies again only the changes the Overview lists as reset (what "Apply again" offers; no Expert, boot-critical or anti-cheat sensitive change). Nothing keeps running. The task is a change on the Changes page, and undo removes it. It can only be turned on when Tunero is in a folder only administrators can change (Program Files, or winget or Chocolatey for all users), since the task starts it elevated without asking. The same run is `--reapply` on the command line.
- **Drive letters first and no "- Shortcut" suffix:** two Explorer tweaks, as in Winaero Tweaker. This PC shows "(C:) Local Disk" (after Explorer restarts), and new shortcuts are named without "- Shortcut". Microsoft does not document either value; a VM test showed both working and both gone again after Undo.
- **Faster submenus and a shorter hover time:** two new Personalize tweaks set the submenu delay to 100 ms and the hover time to 200 ms (Windows: 400 ms each) through SystemParametersInfo, the documented way: they take effect at once without signing out, and Undo sets the earlier values. Checked in the VM: the running session had the new values right away, and the old ones after Undo.
- **Two more documented policies:** "Start without Recommended" removes the Recommended section (recent files, new apps) from Start on Pro and up; a VM test on Windows 11 Pro confirmed that Pro honors it. "Click to Do off" turns off the screen analysis of Copilot+ PCs.
- **Start priorities, low memory priority:** a rule can now also give a program low memory priority (PagePriority), so Windows takes memory from it first when memory runs short; useful for a launcher or chat program that stays open next to a game. A VM test showed that Windows applies the value at program start (memory priority 1, 2 and 3 for those values, normal again without it).
- **Installed programs, unused program folders:** "Find unused program folders" looks in Program Files for folders of programs uninstalled long ago, as Bulk Crap Uninstaller does. A folder is listed only when no uninstall entry (also hidden components and Windows Installer records), service, autostart entry, App Paths entry, COM server, running program or shortcut points into it, its name matches no installed program or publisher, and nothing in it changed for 30 days. The folders appear under Left behind without a tick, with their size and last change, and go to the Recycle Bin only when you tick them.
- **Install & update, search winget:** besides the 91 reviewed apps, a search finds any package of the winget community repository by name or id (at most 50 results). Installing one asks first and says that Tunero has not reviewed it; it installs for the whole PC where the package offers that, otherwise winget opens for your account.
- **Startup page, Delete for files and tasks:** Delete entry now also works for a file in a Startup folder and for a scheduled task of another program, as in Autoruns. The file's content or the task's XML goes into the backup first, and Undo on the Changes page writes the file back or registers the task again under the same name and account. A task that keeps a stored password is not offered, since it could only come back with that password; Windows' own tasks stay on Services & tasks.
- **Tools, four more quick fixes:** "Rebuild the icon cache" for blank or wrong icons (Explorer ends, the signed-in user's IconCache.db is deleted as Microsoft's troubleshooting article does, Explorer starts again as that user). "Update Microsoft Defender" downloads the newest definitions now (Update-MpSignature). "Reset winget sources" restores winget's default sources when Install & update cannot reach the catalog; sources you added are removed, so it asks first. "Restart to Advanced startup" restarts into the menu where Startup Settings offers Safe Mode, for DDU for example; nothing in the boot settings changes, so the next start is normal again without a way back to set up.

### Fixed

- **Screen readers and voice control, clickable cards:** the cards that open details or a group were reported as a plain "pane" without a way to click them through UI Automation; they are now buttons that Narrator announces as such and Voice Access can click by name.
- **German, change targets and values:** targets that stayed English ("Task Scheduler\...", registry values changed bit by bit, a network adapter without a name) and stored states ("Disabled", a service's "Manual") now read in German in the confirmation, the explanation page and the change log, as the other targets already did.

## 1.0.0

The first release out of preview.

### Changed

- **New check, restart pending:** the scan now says when Windows waits for a restart to finish updates (Windows Update's RebootRequired) or a feature change (component based servicing's RebootPending), the two flags Configuration Manager shows as "Pending Restart". Restart before you change settings or compare speed. Queued file renames do not count: programs leave them behind all the time.
- **Command line, `--apply-recommended` and `--export`:** `--apply-recommended` applies the Overview's recommended list without the window, for the profile named with `--profile` (or the saved one), with the same rules as the button; changes that need their own confirmation are listed in the log. `--export <file>` writes the changes file of the Changes page. A profile this PC does not offer ends with the new exit code 9. Start priorities in an exported file now apply with `--apply` too, since they are no longer a preview.
- **Startup page, Delete entry:** like Autoruns, a value in a Run, RunOnce or policy Run key and the cmd.exe AutoRun can now be deleted, for example one whose program was uninstalled. Unlike Autoruns, Tunero backs the value up first: the Changes page lists the deletion and Undo writes it back with its type. RunOnce, policy Run and AutoRun entries had no switch before, so this is the first way to stop them here.
- **Installed programs, several at once:** tick programs and select "Uninstall selected" to remove them one after the other, as Bulk Crap Uninstaller does. With "Quietly where possible" on, Windows Installer programs and programs that register their own quiet command run without questions; the others show their window, and the next one starts when the program has left the list (or when you select Continue). Afterwards the page lists what the programs left behind: their folder in Program Files, ProgramData or AppData (also for Windows Installer programs that record no install folder) and Start menu or desktop shortcuts into it, but only folders no installed program, hidden component or service still uses. You review the list and move it to the Recycle Bin. A single uninstall gets the same check.
- **Program start priorities are no longer a preview:** a test in the VM confirmed that Windows applies them (a program with a rule for Below normal and low disk priority started with both, and with Normal again after Undo). They keep the "Undocumented value" badge, because Microsoft does not document the values.
- **Faster start:** the first scan result shows about half a second sooner (1.95 s to 1.4 s on a test PC). Scheduled tasks and shortcuts are read through their COM interfaces directly instead of late-bound calls, which compiled a binder on first use (the task list takes half as long, a shortcut a fifth). Memory compression and the DoH servers are read from the WMI classes behind Get-MMAgent and Get-DnsClientDohServerAddress instead of starting PowerShell twice per scan (milliseconds instead of about half a second each; the changes still run through the cmdlets). The tweak states are read in the same background step as the scan, while the window is still being drawn. The embedded catalogs are read on a background thread while WPF starts, instead of on the UI thread after it.

- **Health, live sensor readings:** instead of one long list of lines, every component (processor, graphics card, memory, mainboard, drives) has its own card with its main values set large: the hottest temperature, the total load with a bar, the fastest core clock, power and fan, each with the sensor it comes from and its peak since you switched the readings on. "All readings" opens a table of every sensor of that component by kind, with the value now and the peak. The values change in place every two seconds, so an open table stays open, and a line under the switch says when the sensors are still opening. The library's two memory entries share one card. New developer switch `--sensors on|all` for screenshots.

- **Windows 11 25H2 or newer:** Home and Pro 24H2 get no updates since 13 October 2026, so Tunero now supports 25H2 (build 26200) and newer. On 24H2 the app still starts, with a note that the version is no longer supported, so changes made there can still be undone; update Windows before you make new changes.
- **Startup page, scheduled tasks:** like Autoruns, the page now lists every task of other programs (an updater that runs daily, not only tasks at sign-in), and each program a task runs gets its own row, so a second program cannot hide behind a signed first one. Tasks that run a COM handler show the DLL behind it. Windows' own tasks (`\Microsoft\Windows\`) are listed but switched only on Services & tasks, where the catalog says which are safe to turn off: turning off TextServicesFramework\MsCtfMonitor, for example, stops typing in Start search.
- **Search, what Process Lasso does that Tunero leaves out:** live process rules (ProBalance, a power plan while a game runs, memory trimming) need a program in the background, and Windows has no setting that applies a processor affinity at every start; the search and docs/not-included.md now say so and point to the start priorities, which Windows applies itself.
- **Three more privacy tweaks** from documented Windows policies (Pro and up): Windows apps can no longer take screenshots of other windows or hide the capture border (a preview until a test shows the Snipping Tool and Game Bar keep working), Windows apps can no longer use presence sensing, and Windows no longer downloads speech model updates in the background. Microsoft Rewards in Edge stays out: its policy does not apply to profiles signed in with a Microsoft account, the only ones that see Rewards, and the search now says so.
- **Install & update, 57 more apps:** 91 apps in all, in two new groups (game streaming and recording; peripherals, lighting and audio) and the existing ones: Playnite, Heroic, itch, the Rockstar and Minecraft launchers, Prism Launcher, Modrinth, CurseForge, GeForce NOW, PS Remote Play, Moonlight, Sunshine, Streamlabs, CapFrameX, PresentMon, HWMonitor, CrystalDiskMark, System Informer, Process Explorer, Autoruns, Fan Control, SignalRGB, Logitech G HUB, Razer Synapse, SteelSeries GG, Voicemeeter, EarTrumpet, DDU, NVCleanstall, Notepad++, ShareX, Greenshot, Everything, WizTree, TreeSize, PowerShell 7, Windows Terminal, NanaZip, PeaZip, KeePassXC, Bitwarden, qBittorrent, Rufus, Ventoy, LibreOffice, PotPlayer, SumatraPDF, Signal, Telegram, Brave, LibreWolf, Vivaldi, the Visual C++ 2005 and 2008 runtimes and XNA. Each winget id and the install scopes its manifest offers were checked; per-user installers run as the signed-in user.
- **Startup page, export the list:** "Export list" saves every entry with its kind, state, publisher, signature, command, file and location as a CSV file in your language (semicolons where Excel expects them), for a forum post or to compare later. A cell that starts like a formula is written as text.
- **Startup page, more Autoruns locations:** the cmd.exe AutoRun, Explorer add-ons (icon overlays, shell service objects, shared task schedulers, copy hooks), context menu handlers registered for your account only, changed open commands of programs and batch files, SilentProcessExit monitors, sign-in providers, AppCertDlls, DirectShow filters, the 32-bit Image File Execution Options and Winlogon values Windows does not set. They are listed with their file and signature; the ones Windows never sets are marked for a look.
- **Update all apps:** programs installed for the whole PC update in the app as before; programs installed only for your account now update as you, in a winget window of their own, instead of with administrator rights (which installed them for the wrong account, or failed). A failed update says "Update failed" instead of "Install failed".
- **Backup update key:** every version now also accepts updates signed with a second key that is kept offline, so losing the release key would not strand installed copies. The release workflows check against both keys.
- **Sources are links:** the numbered sources at the end of every explanation page open in your browser (as the signed-in user, never as administrator) and can be reached with the keyboard; before, the address was only in a tooltip.
- **High contrast:** with a Windows contrast theme on, the app uses it (Windows' own colors, no Mica) instead of the Fluent light or dark colors, and follows when you turn it on or off.
- **Small screens:** the window and the confirmation dialog fit the screen's free area: on a laptop at 150 % (about 1280 x 672) the window no longer reaches under the taskbar, and the list of changes in the confirmation scrolls so its buttons stay on screen.
- **Accessibility:** the search and key boxes have names for screen readers, and dialogs carry their title as the window title (Alt+Tab, screen readers).
- **Signing out during a change:** Windows is asked to wait until the change has finished, as closing the window already did.
- **Delivery Optimization cleanup** asks once before it deletes, like the other cleanups.
- **README and website:** they say "every setting it changes can be undone" instead of "everything" (removed apps and deleted files are the exception), mark the winget and Chocolatey commands as pending until the first version has passed review, describe the Startup page by its locations instead of "everything that starts with Windows", mark program start priorities as preview, state how far the tests reach, and list what Tunero does not replace (a driver cleaner, live per-process rules, security trade-offs, installing Windows).
- **Release notes** are the version's section of this changelog; a release without one stops before it is drafted.
- **Command line:** `--apply` and `--undo-all` no longer show a message box when the start is refused for safety (a profiler variable, or a data or runtime folder other accounts can change); they end with the new exit code 8, so a script does not wait on a dialog.

### Fixed

- **Keyboard and screen readers:** Tab no longer stops on cards and lists that do nothing (a screen reader read them as an empty "pane" or "list", and on Tools every card before its own button), the group headings in the navigation are no Tab stops and are read by their text instead of a control type name, and the navigation button, the window buttons and the explanation in the details pane have names. Checked on every page and in the confirmation: every stop now has a name. Screen readers also reach the switches and buttons inside list rows (Startup, Services, Tweaks, Installed programs and the other lists): UI Automation showed each row as an empty item, so Narrator's scan mode and Voice Access could not find them.
- **Change log after an undo:** undoing a change built for this PC (a deleted or switched-off startup entry, a service, a device setting, a fix from the scan) logged its internal id, such as `startup.delete.runkey.runuserrun...`, on the Changes page. It now shows the title, as in the list above it ("Delete startup entry: Steam").
- **Small screens:** at 1920 x 1080 with 150 % scaling (a common laptop setting) the navigation rail cut off its last page, Manual checks, under the footer. On short windows the pages now take the compact item height of Windows Settings.
- **System info, memory:** without a reported speed (virtual machines, some firmware) the tile read "Unknown,  MT/s"; it now leaves the speed out.
- **Contrast themes:** text in the accent color (the "Recommended" lines on the Tweaks page, the place of a search result) stayed dark blue on black. It now uses Windows' link color, like Windows' own apps.
- **Undo all and `--undo-all`** passed over a backup they could not read (damaged, locked by another program, or written by a newer version), so the change it records stayed in place without a word. It is now reported as not undone, with the file and the reason.
- **Restore point:** within Windows' limit (one restore point a day unless the limit is lifted) Windows skips a new one and still reports success, so the app counted an older restore point as made for this change. It now checks the limit first and asks you, as when System Protection is off.
- **Backup not updated after a change:** when the backup could not be updated after an apply (the originals are saved before the first write), the result said only "Applied". It now says so; Undo still restores the original values.
- **Startup page, shell extensions:** one blocked in your account's own list (HKCU) showed as on. It now shows as off, and turning it on clears it there.
- **Scans** read the backup folder once instead of once per tweak, and the history of undone changes keeps its newest 500 files instead of growing with every undo.
- **Undo across updates is tested:** backup files now record their format, and the tests restore a saved backup of every kind of change (registry, services, power plans, boot options, tasks, DNS, network adapter, NVIDIA and the others) the way an earlier version wrote it. A backup from a newer version of Tunero is left alone instead of being misread, and the stored catalog version now changes with every catalog change.
- **A setting that could not be read** during an apply was skipped, and the change was still reported as applied. The apply now stops before it writes anything and says which setting it could not read.
- **Power plan undo after "Apply again":** when you had switched to another plan and applied Tunero's plan again, undo switched back but left the plan Tunero created in Power Options. It is removed again.
- **Backups not owned by Administrators** counted as "no backup", so undo reported success without restoring anything, and the next start deleted the file. Every backup is now given to the Administrators group when it is saved (or the change is not made), and a file owned by another account blocks apply and undo of that change instead.
- **Network adapter undo** could fail for a change made with an adapter that is no longer connected, when the driver spelled a keyword differently.
- **Undo all** now also restores the settings of the two "permanent" AI tweaks (what they deleted does not come back), so undoing everything before uninstalling no longer leaves them, and it reports a change it cannot undo instead of skipping it silently.
- **Expert mode:** its question no longer opens inside the switch while it is still being set; screen readers could hang on it.
- **Settings file:** one that cannot be read is kept as settings.json.bad instead of being overwritten with the defaults at the next save.
- **Explanation pane:** an explanation that failed to build once is built again on the next selection, and a check without a page says so in your language.
- **Sensors** are closed when the app ends.
- **Location:** "Location service off for everything" and "Location access off" wrote the same policy value, so undoing one left a false "reset by Windows" note on the other. The first is no longer offered; changes made with it can still be undone.
- **NVIDIA settings undo** while the driver was being updated reported success and dropped the originals; it now fails and can be repeated.
- **Gaming power plan undo** could delete a plan of the same name that you made yourself, when the app could not tell which plan it had created; it now leaves such plans alone.
- **Error reports** no longer contain your account's security id (SID) from registry messages.
- **Self-update:** when the new version could not start after the update (antivirus took the new exe away or blocked it), only the renamed old exe was left and the shortcut started nothing. The version you were using is now put back, and the message says so. The new version also no longer inherits a profiler or startup hook from the user's environment.
- **Startup page, services:** without a valid signature (the signature service turned off, a failed check) a Windows service could become switchable here in Expert mode; the Services page's rule, which counts a file in the Windows folder as Windows' own, now applies here too.
- **Startup page, script hosts:** a variable of your own account could make a script look like it lives in System32, so the entry was hidden as Microsoft's. The check now uses the machine's variables only.
- **A hung part of the scan** (a startup entry on a network share that does not answer, a stuck WLAN service or WMI query) emptied the whole extra section of the scan for up to two minutes, and with it most checks. Each part now has its own time limit of 30 seconds and only its own result stays empty; a probe that did not answer is not started a second time by "Scan again". The memory compression read and the DNS change have time limits too.
- **Memory integrity check:** when Windows could not report it, the check said memory integrity was off. It now says unknown.
- **Ethernet speed check:** a link at 100 Mbit/s that the adapter negotiated by itself is shown as a note instead of a problem: often the other end (a router port, a powerline adapter, an older switch) is the limit, which the scan cannot see. A speed forced lower in the driver stays a problem.
- **Games on a hard disk:** two game libraries on the same hard disk counted as two problems; they are now one finding for the disk.
- **Start menu entry:** an old entry that points to a folder or a web address could stop the app at start while it renamed old entries.
- **Virtual machines** on Amazon EC2 and Google Compute Engine are recognized as such, so the checks for real hardware are skipped there.
- **Import:** a file with two or more changes that need their own confirmation (Expert, preview or anti-cheat sensitive) applied only the first one and refused the others as "busy", because each change starts a scan. A change now waits for the running scan instead.
- **Ryzen AI 300 laptops** (for example the Ryzen AI 9 HX 370) were taken for a Ryzen X3D with two chiplets, because their two core clusters have L3 caches of different size. The app then blocked the High performance plan, reported a problem with impact 4 and guarded the Xbox Game Bar. Only processors named X3D count now.
- **A failed scan** was never shown: the Overview kept saying "The scan is reading this PC". The verdict now says the scan could not finish, and the result bar shows the error.
- **Leftovers check:** the boot option useplatformclock set to No counted as a problem; only Yes forces the slower timer.

## 0.9.14 (preview)

### Changed

- **Website:** a Support row with buttons for Ko-fi and GitHub Sponsors, reached from "Support" in the header, and GitHub Sponsors next to Ko-fi in the footer and the README. The README and website screenshots and the apply and undo GIF show the current UI.
- **Rating mark:** every tweak and check shows its impact on the profile's goal as a light numeral and five bars, in one column beside its switch or Fix button, the same 0 to 5 scale as the rating board on tunero.dev. A rating on disputed evidence draws its bars as outlines; tweaks without effect show none. The grey line under a row no longer spells out "Impact 2 of 5", and the details pane puts the mark next to those words, so it explains the mark. On a narrow page the switches go without their "Applied" label, as in Windows Settings, so the rows keep room for their text.
- **Overview, the verdict first:** the page opens with one sentence on what holds this PC back ("2 problems hold this PC back."), with those problems right under it and their rating marks. The readiness score, the passed checks and the way to Manual checks share one quiet line below the sentence; the two cards with large numbers are gone, and the profile follows the findings.
- **Overview, no problems but recommended changes:** the sentence says both ("No problems found. 2 recommended changes can still help.") instead of only "nothing holds this PC back", and the line under it says that the readiness score is counted from the problems the scan finds. 100 of 100 next to two recommended changes read as a contradiction; the score never counted tweaks.
- **Start menu entry "Tunero PC Optimizer":** Windows Search finds an app only by the name of its Start menu entry, so a search for "optimizer" found nothing. The entry is now called "Tunero PC Optimizer" (also in Scoop), and an entry "Tunero" from an earlier version gets the new name on the next start. The app and its window keep the name Tunero.

### Fixed

- **Hand cursor on whole pages:** on every page with tabs (Services & tasks, Remove apps, Install & update, Tools, for example the storage analyzer or the driver list) the mouse showed the hand everywhere, also over text and cards that do nothing. The hand cursor of a tab now applies to its header only; a test keeps it so.
- **Developer switches:** an elevated `--screenshot` run with `--search` on a real desktop kept running after both pictures. The search results close again after their picture, and a `--screenshot` or `--perf` run that is still running 10 seconds after it finished notes that in the log and ends.
- **Result bar:** a message without a link or button ran under the bar's close button ("99 %", "Fehlgeschlagen: 0." were cut). The close button now has its own room.
- **Search for "update tunero":** the in-app search knew the update setting only as "update pcoptimizer"; it now finds it under the new name too.

## 0.9.0 (preview)

### Changed

- **Website:** a new design for tunero.dev on the Windows 11 surfaces of the app. The scan leads: a board puts checks and tweaks on one scale, where only problems the scan finds reach 5, and names every tweak that can matter for games. One tweak, mouse acceleration off, is followed end to end: its explanation page with the registry values it writes (the old value struck through) and its sources, the steps of an apply, and the same tweak in the app; under it, the page of the popular Ultimate Performance plan says its evidence does not hold up. The app's pages follow in two columns in the groups of its navigation, privacy, install and questions close the page as quieter rows, the claim fits on two lines, and the privacy pages no longer lose the space before their links.
- **Chocolatey workflow:** while the first version of the package waits for moderation, Chocolatey refuses newer versions; the workflow now says so and passes instead of failing.
- **Code signing:** SignPath Foundation turned down the first application, because it signs only projects that are already widely known; the project will apply again later. The README and the website say so and explain how to start the exe past the SmartScreen warning.
- **Settings that work against your profile** have their own section on the Overview, "Works against the Gaming profile", instead of a yellow heading inside the recommendations, where they contradicted "nothing to recommend". Each one has a warning icon and a button: Undo when Tunero made the change, otherwise Details, whose page says how to change it back.
- **Tweaks page, cleaner rows:** the switches of a list stand in one column, and "Already on" starts where they do. A row's grey line no longer repeats "Off" or "On" next to the switch, and the warning icon of a row that needs attention sits in front of its title instead of an empty icon column on every other row. Counts leave out zeros ("202 tweaks: 91 already set", "12 of 34 on"), and the links under the search no longer get cut off in a narrow window.
- **Details pane in a narrow window:** it takes at most 45 % of the window and leaves the list at least 640 px, so a row's text no longer runs over six lines; the width you drag it to comes back when the window is wider again.
- **Overview order:** the findings now follow right after the counts and the game access card, so "2 problems" is answered right below the number; the recommendations and the settings against your profile come after them. Each count shows a small arrow, since a click opens its list, and the changes the one-click list leaves out sit under a line in the same card instead of a second card inside it.
- **Graphics & DNS** is the new name of Graphics & network: the page holds the NVIDIA settings, the profiles per game and DNS, while the other network settings are tweaks and the network repairs are tools. Two links on the DNS card lead there. The profiles per game show the first six games and every game whose profile is on, with "Show all" and a search for long libraries, and the facts they share read like a row's line ("Impact 1 of 5. Affects: 1 % lows").
- **Startup page:** the search and the kind of entry share the first line, the filters the second, and below a line the counts read as a sentence next to the snapshot buttons. "Not signed" is a tag under the name and no longer repeated in the line below it, and the warning icon sits in front of the name. With services or scheduled tasks chosen, a line points to Services & tasks, which explains their start types.
- **Services:** every row shows its start type in the same box: all four start types for services from other vendors, the current one and Manual for a Windows service the app explains (instead of a separate button), and a disabled box with a lock when there is nothing to choose.
- **Frame rate benchmark:** four marks and "Step 2 of 4" show where the measurement stands, the step reads as an instruction instead of a blue line that looked like a link, the game list says "Pick the game" until you do, and the buttons follow the steps: A needs a game, B a run of A, and Clear appears once there are runs. The live sensors switch says On or Off.
- **Install & update** is the new name of Apps & drivers, so it no longer reads like a second Remove apps; "Installed" stays in line whether a row has a button or not.
- **Smaller things:** Manual checks counts as "9 checked: 8 passed, 1 could not be checked" and has its own icon instead of one like Settings; checks without effect leave "Impact 0 of 5" out of their line, as tweaks do. System info and the Overview name the processor "Intel Core i7-9700K" (the full name stays in its row) and the architecture "x64". The System Protection card shows a check only when it is on. Cleanup lists every folder of a category instead of "and 2 more (hover for all)". Remove apps gives each group of the reviewed list its own icon and puts the switch for apps nobody reviewed under its own heading. On Tools, Reset Winsock and Reset TCP/IP no longer say "Needs a restart." next to their "Restart needed" tag.
- **Scroll bars in one place:** pages whose list scrolls below a fixed header (Tweaks, Startup, Services, Cleanup and the tabs of Remove apps, Install & update and Tools) had their scroll bar in the middle of the window, right after the cards, and the mouse wheel did nothing beside them. Their lists now reach the window edge like every other page, with the scroll bar at the edge.

### Fixed

- **App search:** results for checks showed the page's raw markup, such as "::: status Problem" or "{{antiCheats}}". They now show the check's text for a problem, or the first text that needs no fact of your PC.
- **Overlay check in German:** the names of running overlay programs came with English words ("Discord (overlay may be on)"). They are now plain product names in every language.

## 0.8.8 (preview)

### New

- **Memory integrity check:** the scan tells you when memory integrity (core isolation) is on and what it costs: ComputerBase measured 6 % lower average frame rates and 8 % lower 1 % lows in games limited by the processor, more on processors without MBEC/GMET. Windows updates turn it on for more PCs from October 2026. It stays information: the fix is the Expert tweak, with its security and anti-cheat warnings.
- **Frame limit for G-SYNC:** with G-SYNC and V-Sync forced on but no frame limit, the NVIDIA check offers a global limit 3 FPS below the refresh rate, which keeps games out of the V-Sync delay that Blur Busters measured at 2 to 6 frames. Marked "Not yet tested on a real PC".
- **Dialogs:** the uninstall and Expert mode questions now say what happens in one sentence, put what cannot be undone in a warning bar and the rest in small text, and keep every button label on one line.
- **Update found at start:** when the check at start finds a new version, a bar at the bottom of the window says so on any page, with "Update now" (or "Open download page" for a copy that cannot update itself), and stays until it is closed; Settings in the navigation carries a badge until the update is installed. A copy from winget, Chocolatey or Scoop shows the bar once per version and says that the package manager offers it once its package is updated, which can take a few days after the release.

### Fixed

- **Uninstall keeps your backups when the package manager fails:** Settings > Uninstall of a copy from winget, Chocolatey or Scoop deleted the data folder even when the package manager did not remove the app, or when Tunero was started again in the meantime. The data folder now goes only once the app is gone and no copy of it runs.
- **One change at a time, also across windows:** the window and a command line run, or two Windows accounts, no longer change the PC at the same time, so no backup can record another change's value as the original. A second window says so; the command line waits up to 5 minutes (exit code 5 when it gives up). A second start no longer empties the tools' temporary folder while another copy uses it.
- **Startup page and Windows services:** in Expert mode a Windows service such as DHCP or the firewall could be set to Manual on the Startup page, although the Services page keeps it read only. The Startup page now follows the same rule, and a service it set to Manual stays listed, so it can be switched back.
- **Updates:** a download that receives nothing for 60 seconds ends, a download larger than 300 MB is refused, a new version that cannot be started after the update says so, and an installed exe that changed before the restart is replaced by the previous version again. For Chocolatey and Scoop, "Update now" shows the note that a new version can take a few days to arrive when the package manager installed nothing new, instead of quietly starting the old version, and the confirmation no longer says that Windows asks for administrator rights: Chocolatey's window already has them.
- **Command line:** an unexpected error ends `--apply` and `--undo-all` with exit code 6 and a line in the log instead of waiting for a dialog; a Windows version the app does not support gives exit code 7.
- **Closing the window during a change** is refused with a note, so a batch, an undo or an app removal does not stop halfway.
- **Undo and backups:** power plan and boot exports of two changes in the same second no longer share one file; a backup that cannot be saved after a change is tried again and logged instead of ending the apply with an error; a newer catalog value for the same setting is no longer reported as "reset by Windows"; the undo of a network adapter setting writes nothing when another adapter took over its settings key; Import stops when the restore point question is cancelled.
- **Startup entries that run scripts:** paths with forward slashes, doubled backslashes, "." or short names, a launcher started by a script host, and rundll32 entry points that open a Control Panel item or an INF file are marked. Entries of the signed-in user are read with that user's folders when Tunero runs under another administrator account, and a signature is checked on the same open file it is remembered for.
- **Tools the app starts with administrator rights** get Windows' own program and profile folders instead of values the user can set, without compatibility shims, and a batch file never runs elevated with command operators in its arguments.
- **Remove apps:** a list of removed apps that could not be read is no longer replaced by one with only the new app, and apps nobody reviewed get no Store link.
- **Hardware:** restarting a device (network adapter, graphics card) tries to enable it again five times, and the display list is read again when a display was added or removed in between.

### Changed

- **Ratings from measurements:** Resizable BAR off rates 4 on Intel Arc, 2 on AMD and 1 on NVIDIA (measured 23 %, up to 17 % and 0 to 2 %). Memory below its XMP or EXPO speed rates 3, or 1 on Ryzen X3D and Intel 13th and 14th gen (TechSpot: 11 to 12 % against 4 %). Game Mode rates 3 on Ryzen 9 X3D processors with two CCDs, whose driver needs it. The memory integrity tweak now cites measurements.
- **Values that are already set** say "Already set" in the confirmation instead of showing the same number under Now and New, and come after the real changes.
- **No count on Changes in the navigation:** the number of changes stayed there all the time, read like something to fix and could push towards undoing changes made on purpose. The Changes page itself still lists them.
- **Releases** carry only Tunero.exe: the PCOptimizer.exe copy helped versions up to 0.7.3 only until the repository was renamed.
- **Package scripts** check the release signature (`scripts/verify-release.ps1`) before they write a hash, and the Scoop manifest no longer updates itself from the release's checksum file alone.

### Development

- **Truth, not tone:** the list of banned marketing words and its tests are gone. Strong wording is fine as long as it is true; claims about an effect still need sources (docs/explanation-style-guide.md).

## 0.8.0 (preview)

PCOptimizer is now called **Tunero**.

### Renamed

- **Tunero** is the new name of the app, the exe (`Tunero.exe`), the window and the documentation; the repository moves to `github.com/jozuern/Tunero` (links to the old address keep working). The old name was too close to other programs called "PC Optimizer".
- **Nothing is lost on the way:** the data folder moves once from `%ProgramData%\PCOptimizer` to `%ProgramData%\Tunero`, with the backups, so every change made before can still be undone; if an older version still runs, the old folder is used until the next start. Power plans created as "PCOptimizer Gaming" and "PCOptimizer Ultimate" are still found, NVIDIA profiles named "PCOptimizer: game.exe" are still cleaned up on undo, the Start menu entry is replaced, and export files from before (`.pcoptimizer.json`) still import.
- **Updates keep working:** the update check asks GitHub by the repository's id, which a rename does not change, and accepts downloads from both repository names. Releases also carry the exe as `PCOptimizer.exe`, with its checksum and signature, for versions up to 0.7.3. The update signature still covers the same statement, so installed versions accept the new releases.
- **winget:** the package id becomes `jozuern.Tunero` (the first submission under the old id was not published yet).

### New

- **Chocolatey and Scoop:** `choco install tunero`, or the bucket `jozuern/scoop-bucket` for Scoop, next to winget (`jozuern.Tunero`). A copy installed by a package manager is updated and removed by it: "Update now" and Settings > Uninstall run `choco upgrade`, `scoop update` or `winget upgrade` and the matching uninstall, so the package manager's list stays right. Chocolatey runs elevated only from a folder that only administrators can change, else the app shows the command to type.
- **Search** finds tweaks, findings, tools and settings across the app, in English and German, ignoring accents and umlauts, and also matches registry value names and service names. Popular tweaks the app leaves out show up with the reason ("Defender off: not included, because..."), from the new data file `notincluded.json`, which a test keeps in step with `docs/not-included.md`.
- **Windows Update control** (Pro and higher, documented policies): no update restarts from 8 AM to 2 AM (active hours), update restarts when you choose for up to a week (deadline 7 days, grace period 2 days, no automatic restart before), and monthly updates one week later (Moderate risk: security fixes arrive later too).
- **Command line:** `Tunero.exe --apply <changes file>` applies an exported changes file without the window, with the rules of Import (previews, Expert and anti-cheat sensitive changes are listed, not applied), and `--undo-all` undoes every change. Both create a restore point first or stop (`--no-restore-point` continues without one); `--log <file>` writes the result, and the exit code says how it went (0 done, 1 some failed, 2 invalid file, 3 no administrator rights, 4 no restore point).
- **Error reports without telemetry:** after an unexpected error, or on the next start after a crash, the app asks whether to open a GitHub bug report filled in with the app version, the Windows build and the error details, without user or computer name. Nothing is sent until you submit it on GitHub (`docs/PRIVACY.md`).
- **Apps that were not reviewed** (Remove apps, off by default): a switch lists the other removable apps on this PC, with a warning that nobody checked what they do. Parts of Windows, language and handwriting packs, video and audio codecs and what games and the Store sign-in need are never listed (`unreviewedExcluded` in `appx.json`).
- **Changes page log and scan errors in the UI language:** the log shows "Show file extensions, HKCU\...\HideFileExt: from 1 to 0" in English or German instead of the engine's English lines with raw data, and a failed scan says so in the UI language instead of showing the exception text.

### Changed (UI)

- **Navigation:** the pages are grouped as Optimize, Clean up and Maintain and check. "BIOS & hardware" is now **Manual checks**, "Debloat" is **Remove apps** with two tabs (Preinstalled apps, and the Installed programs that were on Apps & drivers), and Apps & drivers moved to Maintain and check with tabs for apps and drivers. Tools has one tab per tool (Fixes and repair, Storage analyzer, Windows features, Program start priorities). On a short window the pane drops its group headings and then folds to the rail, so no page slides under the footer.
- **Search across the app** in the title bar (Ctrl+F): Down goes into the results, Enter opens one on its page, tab and item. F5 reads the current page again or scans again, Esc closes the details or the search.
- **Overview:** a first-start note on how the app works (dismissed with "Got it"); "Recommended for this PC" is a checklist, and the button applies only the ticked lines; the counts are clickable, a zero is grey, and the findings are sorted (problems, then unknown, then notes); Game access sits above the findings.
- **Tweaks:** "Turn on N" in a group heading turns on its safe, fully reversible tweaks that are off after one confirmation; the summary separates changes this app made from settings that were already on, and those show "Already on" instead of a switch that looked disabled; the profile picker has a label, and a link leads to Expert mode in Settings. One "Show" choice replaces the three switches that overlapped (for this profile, recommended for this PC, changed by this app, all that apply, all), and search, filters, counts and group buttons share one card, so more of the list is in view. The details pane can be made wider or narrower by dragging its left edge and says "from Power saver to Tunero Gaming" instead of "now ..., new ...".
- **Confirmation:** what stops a change (Expert mode off, a policy) comes first in red and keeps Apply off with the reason; values that already are what the change writes are dimmed, and a change whose values are all set already says there is nothing to apply. Facts about the change show as tags.
- **Lists:** pages are at most about 1000 pixels wide, so lines stay readable on wide windows. A new setting, Technical details in lists (on by default), hides paths, registry locations and package names on Startup, Cleanup, Services and Remove apps. Startup shows Enabled or Disabled next to each switch and links to Settings when no VirusTotal key is set; Services offers "Set to Manual" instead of a list with one entry, explains the lock and labels the service's own description; Cleanup keeps the total and its button in view and shortens long folder lists; Tools buttons name what they do (Flush, Reset, Renew, Restart, Check) and mark fixes that need a restart, Move to Recycle Bin waits for a selection, and start priorities have a Browse button; per-game NVIDIA profiles name impact and tags once above the list.
- **Manual checks** starts with a summary: whether anything needs a change in the BIOS or on the hardware, how many checks passed or could not be checked, and a button that shows the passed ones.
- **Settings:** language and theme are drop-downs, and the log and data folders open from Diagnostics. Health lists the steps of the before and after benchmark; Changes shows Move to another PC under its own heading.

- **Second review pass:** the navigation shows counts (problems on the Overview, things to change by hand on Manual checks, the changes made) and keeps Changes next to Settings, always in view. The smallest window is 720 pixels high so every page stays reachable. Tweak switches say "Applied" or "Not applied", which settles titles like "Power Throttling off"; a status icon marks only rows that need attention, and detail lines read "Off. Impact 2 of 5. Affects: FPS" (privacy and Explorer tweaks without a performance effect leave the impact out). The "Preview" tag now says "Not yet tested on a real PC". The details pane shows the row's tags and, for a setting that was already on, why it has no switch. The Tweaks filter bar wraps on narrow windows and names the profile with a link to the Overview; DNS servers are chosen only on Graphics & network, where the NVIDIA settings now stay together and the router shows as "Your router". Errors and warnings stay until closed, a change offers "View changes", screen readers hear the result, and dialogs put the primary button first as in Windows 11. Pages end at the same width and scroll bars sit beside the cards. Manual checks and the Overview use the same words ("to change by hand"); German uses "Prüfungen" instead of "Checks". Remove apps has one Refresh and one note; Cleanup lists empty categories in one line; Startup can show only the entries that need attention; Changes lists what can be undone first and the log in plain rows; the benchmark shows the next step and picks no game in advance; System info keeps developer fields under Technical details; repeated buttons and drop-downs have names for screen readers.

- **Cleanup** lists the biggest categories first, once their sizes are known.
- **Startup:** an entry that needs attention says why in short tags under its name ("Not signed", "Invalid signature", "Runs a script", "Unusual place"), not only with the warning icon.
- **Apps & drivers:** every installed app (installed for the whole PC) has its own Update button next to Update all apps; it says "Up to date" when winget has no newer version.
- **Power plans in the app's language:** Windows names its own plans in the language of Windows, so an English app said "from Energiesparmodus to Tunero Gaming". Balanced, Power saver, High performance and Ultimate Performance now show in the app's language, in the details, the confirmation, the Changes page log and the report; other plans keep the name Windows gives them.

### Fixed

- **Recommended for this PC** no longer says "Nothing to recommend" while a problem with a fix is listed below it: a fix that needs something first (administrator rights, Expert mode) is named among the changes left out, with the reason.
- **Driver dates:** Intel chipset packages carry the placeholder date 07/18/1968 and Windows' own drivers 06/21/2006; they no longer show as decades old ("The package gives no real release date"). Ages read as "about 8 years old" instead of a number of days, dates follow the app language, and drivers are grouped by where to get updates with one link per vendor instead of one per driver.

## 0.7.3 (preview)

PCOptimizer is now open source under the GNU General Public License version 3, and it can remove itself from a PC.

### New

- **License:** PCOptimizer is now under the GNU General Public License version 3 (GPL-3.0-only) instead of the PolyForm Strict License 1.0.0. You can use, change and share it; shared versions stay under the GPL with their source code. Releases up to 0.7.1 keep the PolyForm license.
- **Uninstall** (Settings > Uninstall): offers to undo the app's active changes first and stops without removing anything when one cannot be undone. Then the app closes and, once it has ended, its exe, its data folder `%ProgramData%\PCOptimizer` with backups, logs and settings, and its Start menu entry are deleted. A copy installed with winget is removed with `winget uninstall`, run as the signed-in user. Restore points, apps installed through PCOptimizer and the PawnIO driver stay. The README describes how to remove it by hand.
- **Code signing:** the project is applying to SignPath Foundation for free code signing. The release workflow has the signing step, which runs once SignPath accepts the project; the README has the code signing policy with the team roles. The exe's product version no longer carries the commit id ("0.7.3" instead of "0.7.3+1a2b3c..."), because SignPath checks it against the release version.
- **Donations:** Settings > About has a quiet "Support on Ko-fi" link next to the source code link; the README and the repository's Sponsor button link there too. The app shows it nowhere else and never asks.

## 0.7.1 (preview)

This release adds Export and Import of your changes and new tweaks from a comparison with other tweak tools, closes several security gaps in what the app runs as administrator, and fixes or proves five tweaks that were in Preview.

### New

- **Export and Import of changes:** on the Changes page, Export saves the catalog tweaks that are on and the start priorities to a file, and Import applies such a file on another PC or after a reinstall. The file holds only tweak ids and program names, never registry values, so it can only select what this version of the app offers. An import applies the safe changes after one confirmation, like Apply recommended, and asks for each Expert, anti-cheat sensitive or Preview change on its own. Changes made for one PC only (startup entries, services, device tweaks) and removed apps are not part of the file.
- **New tweaks**, from a comparison with other tweak tools (`docs/not-included.md` records what stays out and why): Game Bar recording off by policy and Settings without the Home page (both Pro and higher, documented by Microsoft), File Explorer without Home and Gallery, user folders in This PC, Xbox button does not open Game Bar, desktop wallpaper without JPEG recompression, and blue screen with the stop code parameters. The last five use values Microsoft does not document; a VM test showed the effect of each after apply and its absence after undo. Silent app installs off (the Start menu suggestions some editions install without asking) is a Preview, because its effect shows only over time.
- **Apps & drivers:** the Visual C++ runtimes 2010, 2012 and 2013 (x64 and x86), the 2015 and later runtime for x86 programs, and the DirectX End-User Runtime (June 2010) that older games need for D3DX, XAudio 2.7 and XInput 1.3, installed through winget.
- **Debloat:** Microsoft PC Manager is found under its current package name, and Xbox Game Bar overlay (old version), Cross Device Experience Host, Start Experiences App and X (Twitter) can be removed.
- **Cleanup:** a new category, not preselected, for the old SoftwareDistribution and catroot2 folders that Windows Update repair under Tools renamed, once they are older than a week.
- **Administrator protection** is recognized by its policy (Admin Approval Mode with Administrator protection) together with the system-managed ADMIN_ account. An ordinary account that happens to be named ADMIN_ gets the separate-admin notice again.

### Tweaks fixed or proven

- **Inking and typing personalization off** now also writes the values the Settings switch writes for your account, so Settings shows "Custom inking and typing dictionary" off. Until now only the policies were set and the switch stayed on.
- **Location access off** now uses the documented policy "Turn off location" (Pro, Enterprise and Education). Windows 11 26H2 ignores the consent value the tweak wrote before.
- **Game Bar captures off** is no longer Preview: a VM test of apply and undo showed all three values staying in place, and community sources prove each of them.
- **New rule for sources** (`CONTRIBUTING.md`, "Undocumented values"): several independent community sources that show a value working count as proof. Hardware-accelerated GPU scheduling and Optimizations for windowed games are no longer Preview; their pages cite the tutorials that show the values. Network throttling off (no proof on Windows 11), Auto HDR and Variable refresh rate (one tutorial each) and the lock screen tips (they also turn off Windows spotlight) stay Preview.

### Security

- An uninstaller that runs as administrator and goes through cmd.exe (a batch file, or `cmd /c`) no longer runs the AutoRun command of the signed-in user (`HKCU\Software\Microsoft\Command Processor\AutoRun`, which any program of that user can set): every cmd.exe the app starts gets `/D`. An uninstall line that hands a script to Python, Node.js, Java, Perl or AutoHotkey runs as administrator only when the script is in a folder only administrators can change. The tools the app starts no longer get the user's `PYTHONPATH`, `NODE_OPTIONS`, `JAVA_TOOL_OPTIONS` and similar variables.
- The storage analyzer keeps the folders of a file held while it moves the file to the Recycle Bin, so no folder can be swapped for a link in that moment. The Start menu entry is written into a new file whose real place is checked first, and removed without following links. A self-update copies the new exe next to the old one before it swaps them, so a full disk or a scanner holding the file no longer leaves a damaged exe; the swap also never follows a link.
- An autostart entry or service counts as Microsoft's only when its certificate chain ends in one of Microsoft's own roots, not only by the signer's name. A service in a folder below Windows that standard users can write to (Temp, Tasks and similar) is no longer locked as a Windows service.
- Services, drivers and other entries Windows starts itself are checked with the machine's environment variables, not the signed-in user's. The privilege to read the Secure Boot certificates is switched off again after the read.

### Fixes

**PC checks and advice**

- Leftover boot settings of other tools (F21, `useplatformclock`) are checked again: the check never got the boot configuration and always showed Unknown.
- Memory below its rated speed: with an 8th to 10th generation Intel processor on a board without a Z chipset, memory at the speed Intel specifies for the processor (2400, 2666 or 2933 MT/s) is information, not a problem, because Intel's B and H chipsets of that generation do not support memory overclocking. Four DDR5 modules on a 13th or 14th generation Intel Core or a Core Ultra 200S processor run below the kit's rating by Intel's specification (4000 and 4800 MT/s for single-rank modules): information, no XMP advice. Part numbers such as Kingston KF426 and Patriot 266 read as DDR4-2666 instead of 2600 and 2660.
- Monitor supports more than Windows offers (F2): a 60 Hz monitor whose range limits end at 75 or 76 Hz is no longer reported; a monitor whose preferred mode is above what Windows offers still is.
- Graphics driver older than six months (F15): graphics whose maker no longer releases drivers (NVIDIA's Kepler desktop GPUs such as the GTX 600 and 700 series, AMD's Radeon HD 5000 to 8000, R5, R7 and R9 200 and 300 series, R9 Fury and Nano) get information instead of a problem that could never be fixed, with NVIDIA's and AMD's support pages as sources.
- Ethernet slower than the adapter supports (F24): a laptop on battery gets information instead of a problem, because the driver's power saving can lower the speed; check again on mains power.
- In a virtual machine the advice about physical hardware (memory channels and speed, BIOS age, Resizable BAR, PCIe and NVMe links, drive health, microcode) is left out.
- Advice for a mini PC with a mobile Ryzen no longer suggests desktop memory kits. With Storage Spaces, a volume whose disk is not certain gets no verdict about the wrong disk. A battery that reports its capacity in its own units no longer shows a health value, and a network adapter without advanced driver settings still counts for the Wi-Fi band check.
- A rescan after a change reuses the TPM, BIOS, board and installed games readings, which no change of the app can alter (about a second faster); Scan again reads everything.

**Changes and undo**

- Undo of the Gaming power plan keeps a plan with the same name that existed before (left by an earlier run, or your own); it is only switched away from.
- Undo of "feature off" turns on only that Windows feature, not also parent features you turned off since (DISM's `/All` is used only when Windows needs it).
- Registry values of unusual types (for example REG_DWORD_BIG_ENDIAN) come back with their own type on undo.
- Backups: a damaged backup that cannot be moved aside stops apply and undo instead of counting as missing, and a backup another Windows account left for a change blocks that change here instead of being overwritten.
- Tweaks for Pro and Enterprise policies are no longer offered when the Windows edition cannot be read.
- Services: DiagTrack (Connected User Experiences and Telemetry) is changed only through the tweak Telemetry to minimum, which turns off its policy too and undoes both; the Services page shows it read-only with a note, so the two never keep different originals.

**Network**

- Switching from one DNS preset of the app to another works in one go. Until now the first preset was undone and the second was refused as "another change is running".
- The DNS test and the DNS servers in use also list IPv6 servers.

**Apps, startup, cleanup and storage**

- The Startup page and its check no longer fail on an autostart command that ends in a lone backslash.
- "Update all" on the Apps page ends when winget ends, also when an installer it started keeps running.
- The confirmation for removing an app without a confirmed Store listing says that it may not be installable again, instead of saying that Microsoft no longer offers it. One unreadable OneDrive folder no longer stops the Debloat page from loading.
- App crash dumps are cleaned only when older than a week.
- Storage analyzer: a selection that would recycle every copy of a duplicate file is refused, and the move to the Recycle Bin runs where Windows can ask before deleting a file that does not fit in the bin.

**Interface**

- Pages opened while the first scan runs (Debloat, Apps & drivers, Tools, Health) wait for its result instead of showing empty lists, and the drive list on Health follows every scan.
- A tweak switch that was turned on and confirmed stays on until the list is rebuilt. The selected tweak stays selected while you search or filter.
- The service and program searches wait for a short pause in typing. The Changes page no longer reads backup files while it redraws.
- "Report a problem" says that GitHub receives the app version and Windows build in the link when the page opens (`docs/PRIVACY.md`).

### Licenses

- The 12 PawnIO modules inside LibreHardwareMonitorLib (LGPL-2.1) and the PCI and USB ID databases inside DiskInfoToolkit (BSD-3-Clause) are listed with their license texts under Settings > About > Licenses and in `docs/THIRD-PARTY-NOTICES.md`.

### Development

- A merge stops before its commit and `git commit --no-edit` finishes it with a version above both sides; a commit of named paths no longer leaves an old version staged. CI checks every pushed commit for a raised version.
- The winget workflow submits only a release whose exe matches its signed checksum.
- The checks of this PC (Category=Hardware) run with adapters that refuse every change, enforced by a test.
- The scan pipeline is one Core service (`ScanService`) for the window and `--report`, the Tweaks page sections are Core data with a test that every category has one, and small data files are written through one durable writer.

## 0.6.8 (preview)

- The Tweaks page lists its tweaks in groups instead of one long list: Recommended for this PC and On, but works against this profile first and open, then seven sections, closed: Performance and power, Network, Background and notifications, Privacy, Security and system, Desktop and Explorer, Browsers. Each section combines related categories of the catalog (Performance and power holds power plans, graphics, input, memory, storage, battery and noise). Each group shows how many tweaks it has and how many are on; a click opens or closes it, and Expand all and Collapse all open or close every group. A search opens every group with a match. The category list above the tweaks is gone, because the sections take its place. Within a group the tweaks with the larger impact come first.
- The section headings in the details pane (Summary, What changes, How it works and the others) are semibold in the text color and a little larger than the text below them. Until now they were smaller and paler than the text, so they were hard to find when skimming.
- In a narrow window the navigation pane folds to the icon rail while the details pane is open, and opens again when the details close or the window gets wider. The Tweaks filters fit next to the details pane also in the smallest window.
- Tools shows the result of a quick fix (Flush DNS cache, Restart Explorer, Reset TCP/IP and the others) in the result bar at the bottom of the window. Until now it appeared as a line under the last card of the page, out of view for the fixes at the top. The report of Check the system drive and Turn on the recovery environment stays in the card that ran it. Every Run button waits until the running fix has finished.
- Health: the sensors card has a title (Live readings) and an icon like the other cards.
- Apps & drivers marks installed apps with a check mark and readable text instead of small green text.
- Cleanup greys out "Empty" together with the check box of an empty category.
- German text reads more naturally and uses the English terms German gamers use: Scan for the whole run and Check for a single item ("Erneut scannen", "Bestandene Checks zeigen"), FPS and Framerate, Refresh-Rate, RAM, CPU-Takt, Throttling-Test, Input-Lag, Registry, Debloat and Tools as page names. Windows menu names stay as German Windows shows them, so paths still match the screen. Three German texts said the opposite of the English one and are fixed: removing an app cannot be undone by PCOptimizer (it read as if the app could not be undone), PCOptimizer does not remove Edge (it read as if Edge did not remove the app), and the Older PC profile is for a PC with little RAM or a hard disk. Many stiff, word-for-word sentences were rewritten, quotation marks are German („ and “) throughout, and policy options use Microsoft's German names („Verweigern erzwingen“).

## 0.6.5 (preview)

- System info shows the names of its summary tiles (Processor, Graphics, Memory, Displays, Mainboard, Windows) again. Since 0.6.0 they showed internal text keys such as `Sum_Cpu`. The check for missing texts now also finds texts picked in a condition or read through a short local name, which is how this one got through.
- The details pane starts with the title of the item, then its state, the action button and the explanation. Until now the button sat above the title.
- The card whose details are open has an accent border, so it stays clear which entry the pane belongs to.
- "What changes" for power plans shows the plan names as Windows shows them instead of their GUIDs, also for the settings of a plan.
- "What changes", the confirmation and the undo dialog name their targets in German when the app is in German ("Dienst Spooler (Starttyp)", "Aktiver Energiesparplan"). Registry paths stay as they are.
- Cleanup shows under each category the exact folders it deletes from, for example `C:\Users\<name>\AppData\Local\Temp` for the temporary files of your user account (the category had been called "Your temporary files"). Both temp categories say that files created or changed in the last 24 hours stay. An empty category shows its check box unticked instead of ticked but greyed out.
- The group headings in the navigation pane (Optimize, Clean up, Maintain) are small, semibold and grey, so they no longer look like pages.
- The Tweaks filters are laid out in two rows: search and the two lists on top, the switches below.
- The Overview count card shows its numbers in the size of the score next to it and says where the manual checks are.
- Storage analyzer, Throttle check, Frame rate benchmark and VirusTotal have their description under the section heading, like the other sections. In Settings, Expert mode sits under the heading Tweaks instead of a heading with its own name.
- Startup lines up the text of every entry, also of the entries without a warning icon. On Graphics & network the DNS test card has an icon like the DNS servers card above it, so both titles start at the same place.

## 0.6.4 (preview)

- On the first start the app asks once whether to add an entry to the Start menu, because Windows Search finds a portable exe only through such an entry. Settings > Appearance has a switch to add or remove it later. The entry goes into the Start menu of the signed-in user, also when the app runs under another administrator account.
- Prepared for winget (package `jozuern.PCOptimizer`, not yet in winget). A copy installed with winget no longer replaces its own exe: "Update now" closes the app, runs `winget upgrade` in its own window as the signed-in user and starts the app again, so winget's record of the installed version stays right.

## 0.6.3 (preview)

- The question dialogs (update, restore point and the others) and the confirmation dialog of a change are laid out like Windows 11 dialogs: title and text on top, the buttons side by side in a footer band at the bottom, without the empty space under the buttons. Long button labels wrap instead of being cut off ("Einschalten und fortfahren" in German).
- In dark mode the footer band is set off from the text above it, as it already was in light mode.

## 0.6.1 (preview)

- The in-app update is offered also when the exe has another name than `PCOptimizer.exe`, for example `PCOptimizer (1).exe` after a second download in the browser. Until now such a copy got only the link to the download page. The update replaces the file under the name it has. Versions up to 0.6.0 still need the exe renamed to `PCOptimizer.exe` to update themselves to this one.

## 0.6.0 (preview)

A pass over the interface for public use.

### Interface

- The navigation pane groups its pages under Optimize, Clean up and Maintain; System info sits next to Settings. Graphics & network and Services & tasks have icons that no longer look like the Settings gear or a phone.
- The Tweaks list has headings: recommended for this PC, changes that work against the profile, then one per impact level.
- The tags on a tweak (Preview, Undocumented value, Moderate risk, Anti-cheat and the others) explain themselves in a tooltip.
- DNS is one choice (Automatic, Cloudflare, Google, Quad9, OpenDNS, AdGuard DNS) instead of six switches of which only one could be on. Choosing another set of servers undoes the one this app set first, and "Use the fastest" after the DNS test works the same way.
- The NVIDIA profiles per game and the device interrupt settings take one line per entry, and the per-game list comes after DNS.
- The details pane and the confirmation dialog show registry paths of the signed-in user as `HKCU\...` when the app runs as that user, and a value that is not set as "not set (Windows default)".
- Tools groups its quick fixes into network fixes, restarting a part of Windows and repairing Windows; the system file check (SFC, DISM) moved there from Health, next to the Windows Update repair and the drive check.
- Startup opens on the programs that start at sign-in; every other location is still one choice away in the list.
- Apps & drivers shows a heading per category and a label while it loads, and the list of installed programs passes the mouse wheel on to the page at its top and bottom.
- Overview no longer repeats the hardware summary (System info has it), and an empty recommendation box takes one line.
- Smaller things: Cleanup dims empty categories and writes file counts with thousands separators, Debloat shows no greyed-out button when OneDrive is not installed, the app's notes on the Services page are no longer drawn like links, read-only start types show a lock, Settings has an intro and keeps the update switch and "Check now" in one card, and the Changes page names its list "Changes you can undo".

## 0.5.0 (preview)

Fixes from the first VM test run and from a code audit (open items in [TODO](docs/TODO.md)), plus a speed pass.

### Speed

- A scan takes about 2 seconds instead of 15: the TPM is read once (through the TPM Base Services API when the app is not elevated) instead of waiting twice for a slow WMI query, firmware, startup and extra checks run in parallel, and a rescan after a change reuses the parts no change can affect.
- The page is ready before the 3 second background CPU sample ends; the background activity check follows when the sample is done, without reading every tweak state again.
- Reading all tweak states takes under a second instead of about 8: NVIDIA driver settings share one driver session for a few seconds instead of opening one per setting, and backups are read once per pass.
- The Tweaks and Advisor lists build only the cards in view. Opening Tweaks, switching the category, the profile or "Only recommended" and turning on Expert mode take 3 to 6 times less time.
- Explanations in the details pane are built in the background, and slow work (process lists, page loads, changes) no longer runs on the UI thread.
- The first results show about 3 seconds after the start instead of 5.5 (published exe on the test PC, without administrator rights). The scan starts while the window is still being built, the extra checks run beside the hardware probes, the tweak reads that start PowerShell, DISM or bcdedit begin with the scan instead of one after another after it, the scheduled task list is read from each task's XML on four threads, and the steps after the scan return to the window only once.
- Switching the language builds the rows of the opened pages from what they already read, instead of reading startup entries, services, programs, drivers, AppX packages, folder sizes and Windows features again: about 0.3 seconds instead of 2.
- Switching the theme no longer builds the rows again (status colors follow the theme by themselves), and profile and Expert mode switches leave the hardware rows, and the Network rows until that page is opened, as they are: each takes about half the time.
- After the first scan, the Startup, Services, Apps, Debloat and Cleanup pages load their data in the background, one after another while the window is idle, so they open with their rows instead of a loading state. Tools still reads its Windows features when it is opened (DISM takes about 12 seconds).
- The background CPU sample starts after the scan and the tools the app started, so neither counts as background activity. The log file stays open for the session instead of being opened for every entry, and one Storage query serves the three checks that read the drives.

### VM test run of 2026-10-11

- 28 Preview tweaks are no longer Previews: a test on Windows 11 26H2 showed their effect where you see it (Settings, the taskbar, File Explorer, the desktop, the Print Screen key, the DNS settings, Windows Security) and its removal after undo. Game Mode has a visible-effect proof too.
- "Location access off" showed no effect in the test and "Lock screen: no fun facts and tips" also turned off Windows Spotlight: both stay Previews, and their explanations now say so.
- The DISM checks on the Health page show umlauts again on German Windows: DISM writes the console code page, which was read as UTF-8.

### Security

- An elevated start refuses to run when the runtime folder is not protected, and files the app creates are owned by Administrators.
- Updates are checked against an ECDSA signature of the release (`PCOptimizer.exe.sig`) besides the hash; an update without a valid signature is not installed.
- Child processes (PowerShell, system tools) start in System32 with a cleaned environment, so variables like `COR_PROFILER` or `PSModulePath` cannot load foreign code into them.
- Plain DLL names load only from System32. The signature check cache is keyed to the file's identity, so a replaced file is checked again. Builds restore locked package versions, CI actions are pinned to commits, and releases are built only from `main`.

### Undo and engine

- When one step of a tweak fails, the steps already done are rolled back, including the failing step when it changed something before failing; the result says when something could not be rolled back.
- A damaged backup file is set aside (`.damaged`) and blocks a new change to that tweak instead of being overwritten. Backups are written to disk before the change starts.
- A tweak whose conditions cannot be checked on this PC is blocked instead of applied.
- Undo restores only what the tweak changed: per power source for power settings and power modes, per keyword for network adapter properties, and only the registry keys the tweak created. Power plans are restored by GUID, and a scheduled task, NVIDIA profile, DNS setting or display mode that no longer exists is skipped instead of failing.
- The restore point frequency is set back when no restore point was created.

### Checks

- RAM speed, channel layout, the AMD AGESA version, Resizable BAR platform support, the microcode age with an unknown BIOS date, X3D processors and the battery report on desktops no longer give wrong results.
- The readiness score counts only findings that matter on their own, not prerequisites of other findings.

### App

- Switches and change buttons are disabled while a change, scan or update runs, and only one change runs at a time.
- Numbers follow the app language everywhere: results that appear after a click (storage analysis, benchmark, cleanup sizes) no longer use the Windows regional format, such as "52,6 GB" on an English page.
- Failures show as errors instead of information. Explorer restarts ask first. Frame time captures can be stopped. Escape closes message windows. Settings are saved atomically.
- System info starts with tiles for processor, graphics, memory, displays, mainboard and Windows. Each area is a card with an icon and divided rows. Disks, volumes (with a used space bar), memory modules and network adapters get one row each with a second line instead of comma lists. Values are translated (graphics kind, disk health, boost mode, power plan type, HDR, G-SYNC), and IDs and raw values (PnP ID, GUID, microcode, EDID) sit behind "Technical details". "Copy" puts the whole page on the clipboard.
- Tweak rows show Preview, Undocumented value, Disputed, Restart and Sign out as tags, and risks (Moderate risk, Expert only, Boot configuration, Anti-cheat, not reversible) as tags in the caution color, instead of at the end of the detail line. Service descriptions from Windows are cut to two lines; the whole text is in the tooltip.
- The Tweaks and Startup pages have a search box. Startup shows its filters and its actions on separate rows, and each entry is one line shorter.
- With the details pane open, the switches on the right of long lists were cut off; cards now take the visible width.
- After a theme switch, switches that were on turned gray on PCs with a gray Windows accent: the accent is now set before the theme loads. "System" follows Windows through one listener for the app mode and the accent.
- Debloat says once at the top that removing apps needs administrator rights, instead of on every app. Cleanup shows the selected size and "Clean selected" above the list. "Apply recommended" is hidden while there is nothing to apply. BIOS & hardware says how many passed checks are hidden. "Show passed checks" is a switch like the other filters, and the Services and Scheduled tasks tabs mark the selected tab.

### Content

- Tweaks that use an undocumented value are marked "Undocumented value" and either cite proof that it works or stay in Preview; the lint enforces it.
- Explanation pages need a Sources section, and every source must be cited in the text.

### Second audit

- **Security:** the data folder is created locked in one step and never takes over files another account put there. The app refuses to run elevated with a .NET profiler set in the environment, and startup hooks are off. Tools the app starts get an admin-only TEMP and Windows' own paths instead of values from the user's environment. A per-user uninstall entry for a Windows Installer package runs msiexec as the user. Uninstallers in folders with generic write rights for users no longer run elevated.
- **Shared PCs:** backups of an account's own settings are kept per account, so a second Windows account cannot take over or undo the first account's changes.
- **One change at a time:** removing apps, cleanup, uninstalls, winget, quick fixes, repairs and the sensor driver install now wait for each other, for tweaks, for scans and for the self-update.
- **Fixes:** "Move to the Recycle Bin" asks before a file too large for the Recycle Bin is deleted for good; system files such as pagefile.sys are no longer offered. The OneDrive guard checks every account. "Renew IP address" always renews after a release. The Windows Update repair reports a service that did not start. DNS over HTTPS reports a server that could not be set. Startup entries that run PowerShell through cmd or a file through rundll32 are marked, logon tasks with quoted paths and shortcuts are checked by their file, and one bad StartupApproved value no longer hides the Run entries. "RX Vega 10/11 Graphics" counts as integrated graphics. XMP and EXPO speeds reported in MHz are read correctly. Rules for program start priorities set by other programs are listed and can be removed.
- **Speed:** DISM and PowerShell reads are shared within a scan and an apply, and the hardware scan does not wait for threads on PCs with few cores.

### Third audit

- **Security:** the elevated start also refuses .NET diagnostic ports, notification profilers, EventPipe traces and crash dumps set in the user's environment. Elevated tools find Windows PowerShell on their PATH again. Files signed for other vendors through Microsoft's driver program no longer count as Microsoft's. Startup entries that start a file through explorer, msiexec, pcalua and similar Windows programs are marked, as are PowerShell without ".exe" and scripts in alternate data streams; unquoted paths are resolved the way Windows runs them. An uninstaller run through cmd or a script host runs elevated only when the script is in an admin-only folder. Paths on network shares are never trusted.
- **Undo:** applying a tweak again keeps the undo step of the first apply, so a later catalog version cannot leave part of the change behind. A backup that lost its per-account entries leaves no stale copy. A failed apply no longer leaves entries that later show as "reset by Windows", a backup found damaged during an apply blocks it, an apply whose values could not be read changes nothing, a rollback removes only a power plan it created, and the DirectX settings value keeps everything else stored in it.
- **Checks and tools:** the frame time benchmark reads the capture of the included PresentMon (it reported "no frames" for every run). The throttle check says when the processor was not busy enough to judge it, and reports a graphics card slowed by its power supply. The storage analysis protects the real Xbox game folders and Epic, GOG and Ubisoft games, and waits for the PC scan; hard links no longer count as duplicates, and Stop ends the hashing of a large file. The background CPU check no longer overstates processes.
- **App:** turning on sensors no longer freezes the window. A failed service start type change shows the real start type again. Startup and driver rows no longer show empty parts. Switches have names for screen readers.
- **Content:** "Optimizations for windowed games", "End task" in the taskbar, Location access off and Network throttling off use values Microsoft does not document: marked as such and in Preview until tested. "Prevent device encryption" is an Expert option. Show file extensions and Transparency effects off cite the Microsoft pages that name their values.
- **Build:** the publish restore runs in locked mode again, merges raise the version, the hooks keep LF line endings, and the release signing key is kept in a GitHub environment that allows only version tags and waits for approval.

### New tweaks (documented settings)

100 tweaks added from the review of other tweak tools, each a setting Microsoft, Google or Brave documents, with an explanation page in English and German:

- **Browsers:** Microsoft Edge (diagnostic data, personalization, shopping, sidebar, setup and default browser prompts, new tab page, tips and offers, tracking prevention, address bar suggestions, error page services, network prediction, feedback, payment queries, password saving, sign-in, AI features, cloud text prediction, tab services, visual search), Google Chrome (AI features off or without model training, URL-keyed data collection, promotions, background mode) and Brave (Rewards, Wallet and VPN, Leo, analytics and pings, News, Talk and Playlist). Chrome and Brave tweaks appear only when the browser is installed. Edge pages say when Edge ignores a policy in profiles signed in with a personal Microsoft account.
- **AI features:** Recall removed, Paint and Notepad AI features off.
- **App permissions:** camera, microphone, notifications, voice activation, motion, phone calls, radios and wireless devices for Windows apps.
- **Privacy:** location service off, Windows Search without web and location, no infection reports from the Malicious Software Removal Tool, Spotlight and cloud content (Enterprise and Education), OneSettings downloads, the privacy page at sign-in, recent files, the lock screen camera, Windows Media DRM, OneDrive folder backup.
- **Windows Update and Store:** feature updates one year later, no automatic Store app updates, fewer update notifications, no automatic archiving of unused apps.
- **Personalize and Explorer:** taskbar search and Task View button, Start lists, clear sign-in background, first sign-in animation, detailed status messages, Hibernate in the power menu, Open with prompts, shortcut search, Ink Workspace, default printer.
- **Security:** Defender potentially unwanted app blocking, network protection and sandbox, LSA protection, password reveal button, LLMNR, automatic sign-in after restarts, Remote Assistance, wireless projection, AutoPlay, automatic device encryption.
- **Network:** DNS presets for OpenDNS and AdGuard, a preset that sets DNS back to automatic, automatic DNS over HTTPS for known servers, and "Use the fastest" after the DNS benchmark.
- **Other:** no wake timers, notifications and the notification center, account notifications in Start, Edge tabs in Alt+Tab (Microsoft marks that policy as a preview), reserved storage off, Sticky Keys, Filter Keys and Toggle Keys shortcuts (through the documented SystemParametersInfo flags, changed only when the app runs under your own account), long paths, Program Compatibility Assistant, OneDrive sync, SMB bandwidth throttling, indexing on battery, the classic F8 boot menu (Expert).
- Every registry and accessibility tweak in the catalog is now applied and undone in the registry sandbox by a test.

### New tweaks (values behind Settings switches, Preview)

25 tweaks for values behind a Settings or Folder Options switch that Microsoft does not document. They carry the "Undocumented value" badge and stay Preview until the VM test confirms the effect: dark mode, accent color on title bars, taskbar on the left, seconds in the clock, never combine taskbar buttons, no taskbar flashing and badges, no show desktop corner, Snap suggestions and layouts, desktop icon for This PC, Dynamic Lighting, communications ducking, notification sounds, Auto HDR, variable refresh rate for DirectX 11 full screen games, hidden files, compact view, item check boxes, full path in the File Explorer title, no sync provider notifications, no search history on this device, Print Screen copies the screen instead of opening Snipping Tool, no fun facts on the lock screen and no "Let's finish setting up your device" screen.

### Tools and pages

- **Quick fixes** on the Tools page: renew the IP address, reset TCP/IP, restart Windows Audio, Bluetooth, Windows Search or the graphics driver, sync the clock, rebuild performance counters, check the system drive online (chkdsk /scan) and turn the recovery environment back on.
- **Program start priorities** on the Tools page: a CPU priority and optionally a low disk priority that Windows applies every time a program starts, without a background program (Preview, undocumented values).
- **Windows features:** Client for NFS and optional capabilities such as PowerShell ISE, WordPad, WMIC, the OpenSSH client, Math Recognizer and Print and Scan.
- **Apps and drivers:** uninstall desktop programs (with a restore point first), "Update all" through winget, .NET Desktop Runtime 8 and 10, and a card that explains when Display Driver Uninstaller is the right tool.
- **Debloat:** 126 removable apps instead of 34, with groups for discontinued apps, third-party promotions and PC maker apps, and Edge Game Assist. The page says when a removed app came back after a Windows update.
- **Startup:** more locations (RunOnce, Active Setup, the Load value, boot execute, Known DLLs, Winsock providers, print monitors, LSA packages, network providers, codecs, WMI consumers), a snapshot to compare against later with "Only new", and buttons to show the file and copy the location.
- **Changes:** "Open System Restore" opens the Windows dialog for restore points.

### Fixes

- Explanations showed "1][2]" instead of "[1][2]" where two sources are cited side by side.

### Undo and engine (VM test run)

- Undo of "Memory compression off" did nothing: the change takes effect only after a restart, so the engine recorded the old value and later took the new one for a reset by Windows. It now shows "pending restart" after apply, and undo turns memory compression back on, also before the restart and for changes made with 0.4.0. After the undo the tweak shows "Off after a restart" until Windows restarts, and turning it on again before then works.
- A Windows default no longer blocks a conflicting tweak. With the default Balanced plan, the Gaming and Ultimate Performance plans were blocked, and with the default boost mode "Processor boost off" could never be applied. Only changes made by this app still block.
- "Hibernation off" is unsupported where the firmware cannot hibernate (virtual machines, some firmware). Before, undo failed there and the change stayed on the Changes page; such a change can now be undone.
- When Windows denies writing a protected value even with administrator rights (the Widgets policy on newer builds), the error names the value instead of "Attempted to perform an unauthorized operation".
- DNS presets and delayed TCP acknowledgements work inside Hyper-V virtual machines. Hardware properties of network adapters still change only on PCI and USB adapters.

### Pages (second VM test run)

- The three DNS presets are named after their resolver (Cloudflare, Google, Quad9). Before, all three rows read "Public DNS servers".
- Startup: a Startup folder shortcut with arguments showed "File not found" and no publisher, because the whole command line was checked as a file. Windows tasks that call rundll32 with a switch first (Autochk) showed the same.
- The storage analyzer no longer offers the page file, swap file, hibernation file and boot dump log in the root of a drive for deletion.
- 13 tweaks are no longer marked Preview: the VM test showed that apply, the visible effect and undo work. Expert and boot-critical tweaks, the NVIDIA and network adapter settings, HAGS, MPO, Game DVR and inking and typing stay Preview.
- Startup entries, services, tasks and the restore point frequency show On or Off on the Changes page like the other changes.
- Counts read "Changes: 1" and "Files: 1" instead of "1 changes" and "1 files".
- The undo confirmation no longer says that the original values are saved; it says that values which cannot be restored stay on the Changes page.
- Scheduled tasks that run on a schedule are no longer titled "at sign-in or boot".
- The SFC and DISM output follows the dark theme and shows each progress line once instead of one line per percent.
- The throttle check says when the graphics card reports no limits instead of showing "? %".
- The DNS confirmation names the network adapter, and the list of removed apps shows app names instead of package names.
- Screen readers announce list and combo box items by name, the language buttons work with a screen reader's select action, and a row switch no longer waits for its confirmation to close.
- Windows Update repair stops Cryptographic Services again and retries when renaming catroot2 is denied.
- The PawnIO install text says that uninstalling it in Settings can leave the driver, and how to remove it.
- Tweaks that are off show a grey minus instead of an empty circle, which looked like a button to click; tweaks that do not apply to this PC show a prohibited sign.

## 0.4.0 (preview)

A full review of every tweak, check, data file and text against Microsoft and vendor documentation, plus security hardening. Apply and undo are still only tested against the registry sandbox; run the [VM test plan](docs/vm-test-plan.md) before relying on it.

### Security

- The data folder `%ProgramData%\PCOptimizer` is secured before the first log or settings access. A link there is removed, a folder created by another account is deleted, and files that Administrators or SYSTEM do not own are removed instead of being adopted (a planted backup could otherwise make Undo write chosen values as administrator).
- Native libraries load only from System32 or the protected runtime folder; a DLL of the same name next to the exe is refused.
- The single-file exe no longer loads its native WPF libraries from `%TEMP%`: it restarts once with the extraction folder inside the protected data folder.
- Self-update starts the new exe only if it still has the checked hash, and keeps it locked against changes until it has started. Release download links must point into this repository in canonical form.
- Only web and Microsoft Store links are opened.

### Tweaks

- 15 tweaks removed because the evidence does not support them, they use undocumented settings, or they cost more than they bring: MMCSS profile, Win32PrioritySeparation, dynamic tick off, global timer resolution, Spectre and Meltdown mitigations off, SmartScreen off, OverlayTestMode removal, fullscreen optimizations off, Teredo off, 8.3 names off, handwriting data sharing off, WPBT off, Print Spooler on Manual, core parking off, minimum processor state 100 %. Reasons and sources: [docs/not-included.md](docs/not-included.md). Changes made with them can still be undone.
- 3 tweaks added, all documented by Microsoft: drivers not included with Windows Update, no automatic download of device apps, registry backup to RegBack.
- New **Preview** marker for risky tweaks that still need testing on real Windows. Preview tweaks show a badge and a warning and are never part of "Apply recommended".
- Corrected actions: multiplane overlay off now uses NVIDIA's documented OverlayTestMode = 5; delayed TCP acknowledgements no longer write the undocumented TCPNoDelay; NIC power saving changes only the standard *EEE and *SelectiveSuspend keywords; background apps use the documented LetAppsRunInBackground policy; Recall off no longer writes the deprecated Copilot policy; best-performance visual effects change only their own bits of UserPreferencesMask; the consumer features policy is offered only on Enterprise and Education.
- Impact, evidence basis, restart flags, sources and EN/DE explanation pages corrected for every tweak. No tweak claims "measured" without a cited measurement.

### Checks

- Fixes for laptops with embedded DisplayPort panels, the primary monitor on integrated graphics, unknown AC power state, virtual network adapters, Wi-Fi details withheld without location permission, unreadable virtualization-based security state, the Raptor Lake model list, Resizable BAR support per GPU family and on laptops, Intel Arc classification, TRIM default, low disk space on data drives, four DDR5 modules on Ryzen, and checks that fail partway.
- Anti-cheat requirements now follow the vendors' own pages (Vanguard, FACEIT with VBS and IOMMU, Easy Anti-Cheat, BattlEye, EA Javelin as "sometimes").
- BIOS menu paths checked against the board makers' manuals; GPU, CPU, storage (SMR list), app and inbox app data verified.

### Undo and engine

- Changes made with a removed tweak remain undoable.
- Power plan settings keep one backup per plan, so switching plans no longer loses an original or reports a false reset; a plan the app created is removed on undo even after a plan switch.
- Undo removes only registry keys that are empty afterwards, keeps the registry type of network adapter keywords, and succeeds for services that were uninstalled.
- Interrupt affinity is offered for graphics cards only; DNS presets and per-adapter values apply to physical adapters only.

### Other

- Debloat no longer promises a Store reinstall for apps the Store dropped, and links use the Store product ID.
- winget installs the machine-wide package for apps that offer both installers.
- Counts in the README and the VM test plan tables are now checked against the catalog by tests.
