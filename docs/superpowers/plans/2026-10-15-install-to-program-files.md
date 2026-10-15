# Install to Program Files Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** On its first start a downloaded, signed `Tunero.exe` offers a full-window setup that copies it to `C:\Program Files\Tunero`, adds the Start menu entry and registers it under Installed apps; plus the taskbar icon bug the owner saw after a manual move there.

**Architecture:** The file and registry steps live in `Optimizer.Core/Platform/AppInstall.cs` behind injected roots (registry, install folder, signature check), so tests run in the sandbox registry and a temp folder. The app adds a setup view model and view shown over the main window's content, the switches `--installed-from` and `--uninstall`, and the uninstall/cleanup changes.

**Tech Stack:** C# / .NET 10, WPF with WPF-UI 4.3, CommunityToolkit.Mvvm, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-15-install-to-program-files-design.md`

## Global Constraints

- Never change the real PC in tests: registry through `SandboxRegistry` (`tests/Optimizer.Core.Tests/Fakes.cs`), files only in a temp folder, signature check faked. Real install, update and uninstall only in the VM PCO-Test.
- User-visible text: EN and DE in `Strings.resx` and `Strings.de.resx`; no en or em dashes, bullets, middle dots, ellipsis characters or "(s)" plurals.
- UI: Windows 11 Fluent only (cards, check boxes, 4/8/12/16/24 px spacing), light and dark, `Foreground="{DynamicResource TextFillColor...Brush}"` on text, never `Appearance=` on text.
- Install folder `%ProgramW6432%\Tunero`, exe name `Tunero.exe`; registry key `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Tunero`.
- Trusted publisher today: `CN=Open Source Developer Joshua Zürn, O=Open Source Developer, L=Neudenau, S=Baden-Württemberg, C=DE`.
- The app runs elevated: nothing from a user-writable path is trusted after it was checked in place; the copy is checked in the admin-only folder.
- Docs move with the code: README, CHANGELOG (Unreleased), `docs/vm-test-plan.md`, CONTRIBUTING, website (EN and DE) in the commits that change behavior.
- Commits: explicit paths, the pre-commit hook raises the patch version, no Co-Authored-By lines.

## Review Focus

1. The exe in Downloads is swapped between the start and Install: the copy in Program Files fails the signature or version check and Install stops, nothing left behind (Task 3 test `InstallStopsWhenTheCopyIsNotTrusted`).
2. `C:\Program Files\Tunero` already exists with other files or a weak ACL (a planted DLL next to the exe): Install refuses (Task 3 test `InstallRefusesAFolderWithOtherFiles`).
3. Install fails after the registry entry was written (Start menu or start of the new exe fails): entry and copied exe are removed again (Task 3 test `AFailedStepUndoesTheEarlierOnes`).
4. A different file now sits at the old Downloads path when the installed copy starts: it is kept (Task 4 test `TheOldCopyIsKeptWhenItChanged`).
5. Uninstall from Windows Settings while Tunero is open: the single-instance lock shows the open window; `--uninstall` must not be a plain start (Task 5 test in `SingleInstanceTests`: `--uninstall` does not take part).

---

### Task 1: Taskbar icon after a move to Program Files (bug)

**Files:** to be found; likely `src/Optimizer.App/Views/MainWindow.xaml(.cs)` or the Start menu entry.

- [ ] **Step 1: Reproduce in PCO-Test** (superpowers:systematic-debugging). Copy the published exe to `C:\Program Files\Tunero\Tunero.exe` as an administrator would (copy, and separately move from Downloads), start it from there, screenshot the taskbar through the interactive scheduled task (CLAUDE.md, VM gotchas). Also with a Start menu entry and a taskbar pin that still point to Downloads.
- [ ] **Step 2: Name the cause** from the reproduction (which case shows the blank icon, and what Explorer reads then). Expected lead: a pin or Start menu entry whose target was moved away.
- [ ] **Step 3: Fix** what Tunero controls (for example `StartMenuShortcut.RenameLegacy` style retargeting of an entry whose target is gone to the running exe at start), with a unit test where the cause is in code; if the cause is Windows' pin to a moved file, the fix is Task 5's retargeting and a note in the VM plan.
- [ ] **Step 4: Verify in the VM, commit** with a CHANGELOG line under Unreleased.

### Task 2: Registry entry and offer decision (Core)

**Files:**
- Create: `src/Optimizer.Core/Platform/AppInstall.cs`
- Test: `tests/Optimizer.Core.Tests/AppInstallTests.cs`

**Interfaces:**
- Produces:
  - `public static class AppInstall` with `public const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Tunero";`
  - `public static IReadOnlyList<string> TrustedPublishers` (the subject above).
  - `public static string DefaultFolder => Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? programFiles, "Tunero");`
  - `public static bool ShouldOffer(bool publishedExe, bool elevated, bool plainStart, bool packageCopy, bool declined, bool installedHere, bool trustedSignature)`: true only when all of publishedExe, elevated, plainStart, trustedSignature and none of the others.
  - `public static bool IsTrusted(SignatureInfo signature)`: Status Signed, not Catalog, Publisher subject in `TrustedPublishers`. (Check whether `SignatureVerifier` returns the full subject or the CN; compare the same form.)
  - `public static IReadOnlyDictionary<string, object> EntryValues(string exe, string version, long sizeKb)`: DisplayName "Tunero", DisplayVersion, Publisher "Joshua Zürn", DisplayIcon exe, InstallLocation folder, UninstallString `"<exe>" --uninstall`, URLInfoAbout `https://tunero.dev`, EstimatedSize (int), NoModify 1, NoRepair 1.
  - `public static void WriteEntry(IRegistryRoots registry, string exe, string version, long sizeKb)`, `public static void RemoveEntry(IRegistryRoots registry)`, `public static string? InstalledExe(IRegistryRoots registry)` (the entry's DisplayIcon when the file exists).

- [ ] **Step 1: Failing tests**: `ShouldOfferOnlyADownloadedSignedCopy` (theory over each flag), `TheEntryHasTheValuesWindowsShows` (values and types: EstimatedSize and NoModify as DWORD), `WriteThenRemoveLeavesNoKey` (SandboxRegistry, Hive.Machine), `IsTrustedNeedsTheExactPublisher` (Signed with the subject true; another subject, Unsigned, Catalog false).
- [ ] **Step 2: Run** `dotnet test tests/Optimizer.Core.Tests -c Release --filter AppInstallTests`: fails (no type).
- [ ] **Step 3: Implement** the members above.
- [ ] **Step 4: Run** the filter: passes.
- [ ] **Step 5: Commit** `AppInstall: Installed apps entry and when to offer the install`.

### Task 3: Install steps with undo (Core)

**Files:** Modify `src/Optimizer.Core/Platform/AppInstall.cs`; Test `tests/Optimizer.Core.Tests/AppInstallTests.cs`

**Interfaces:**
- Consumes: Task 2.
- Produces:
  - `public sealed record InstallRequest(string RunningExe, string Folder, string Version, Func<string, SignatureInfo> Verify, Func<string, string?> FileVersion, Func<string, bool> FolderIsAdminOnly)`.
  - `public enum InstallStep { Folder, Copy, Check, Entry }` and `public sealed record InstallResult(string? InstalledExe, InstallStep? FailedAt, string? Error)`.
  - `public static InstallResult Install(InstallRequest request, IRegistryRoots registry)`: steps 1 to 3 of the spec in order (folder, copy to `Tunero.exe.new` from a read-sharing handle, check the copy with `Verify` and `FileVersion` equal to `Version`, move to `Tunero.exe`, `WriteEntry`); on failure removes what it created (the `.new`, a `Tunero.exe` it placed, the folder if it created it and it is empty, the entry) and returns the step.
  - `public static void Undo(string installedExe, IRegistryRoots registry)`: removes entry, exe and the folder when empty (for a failure in the app's later steps: Start menu, start of the new exe).

- [ ] **Step 1: Failing tests** (temp folder as `Folder`, fake `Verify`/`FileVersion`/`FolderIsAdminOnly`): `InstallCopiesChecksAndRegisters`, `InstallStopsWhenTheCopyIsNotTrusted` (Verify returns another publisher: no `Tunero.exe`, no `.new`, no entry, FailedAt Check), `InstallStopsOnAnotherVersion`, `InstallRefusesAFolderWithOtherFiles` (a `version.dll` in the folder: FailedAt Folder, the dll untouched), `InstallRefusesAWeakFolder` (FolderIsAdminOnly false), `AFailedStepUndoesTheEarlierOnes` (Install then `Undo`: folder, exe and entry gone), `InstallReplacesAnOlderInstalledExe`.
- [ ] **Step 2: Run** the filter: fails.
- [ ] **Step 3: Implement** `Install` and `Undo`. The folder is created with `Directory.CreateDirectory` (inherits Program Files' ACL); file removal through `SafeDelete.DeleteFile` / `DeleteEmptyDirectory`.
- [ ] **Step 4: Run** the filter: passes.
- [ ] **Step 5: Commit** `AppInstall: copy, check and register, undone on failure`.

### Task 4: Old copy and uninstall cleanup (Core)

**Files:** Modify `AppInstall.cs`, `src/Optimizer.Core/Platform/SelfUninstall.cs`; Tests `AppInstallTests.cs`, the existing SelfUninstall tests (`grep -rl CleanupScript tests`).

**Interfaces:**
- Produces:
  - `public static bool DeleteOldCopy(string oldExe, string installedExe)`: deletes `oldExe` with `SafeDelete.DeleteFile` only when its SHA-256 equals `installedExe`'s and it has no links; false when kept or failed.
  - `SelfUninstall.CleanupScript(..., string? installFolder = null)`: when set, after the files the script removes the folder with `rd` only if empty (no `/s`).

- [ ] **Step 1: Failing tests**: `TheOldCopyIsDeletedWhenItIsTheSameFile`, `TheOldCopyIsKeptWhenItChanged`, `TheCleanupRemovesTheEmptyInstallFolder` (script contains the folder and `rd` without `/s` for it).
- [ ] **Step 2-4:** run (fail), implement, run (pass).
- [ ] **Step 5: Commit** `AppInstall: remove the downloaded copy; the uninstall removes the install folder`.

### Task 5: Setup screen, switches and wiring (App)

**Files:**
- Create: `src/Optimizer.App/ViewModels/SetupViewModel.cs`, `src/Optimizer.App/Views/SetupView.xaml(.cs)`
- Modify: `src/Optimizer.App/Views/MainWindow.xaml` (overlay over `Grid.Row="1"`), `src/Optimizer.App/App.xaml.cs` (start flow near `vm.OfferStartMenuEntry()`, switches), `src/Optimizer.App/SingleInstance.cs`, `src/Optimizer.App/Program.cs`, `src/Optimizer.App/Services/AppSettings.cs` (`SetupDeclined`), `src/Optimizer.App/Services/UninstallService.cs`, `src/Optimizer.App/ViewModels/MainViewModel.StartMenu.cs`, `MainViewModel.Reapply.cs`, `src/Optimizer.App/Views/SettingsPage.xaml`, `Strings.resx`, `Strings.de.resx`
- Test: `tests/Optimizer.App.Tests/SingleInstanceTests.cs`, `tests/Optimizer.App.Tests/SetupTests.cs`, `UpdateAndUninstallTests.cs`

**Interfaces:**
- Consumes: Tasks 2 to 4.
- Produces: `SetupViewModel` with `bool IsShown`, `bool ToProgramFiles = true`, `bool StartMenu = true`, `string Folder`, `IAsyncRelayCommand InstallCommand`, `IRelayCommand RunHereCommand`; `MainViewModel.Setup`; switches `--installed-from <path>`, `--uninstall`, developer `--setup-shot` (shows the screen for `--screenshot` regardless of the conditions).

- [ ] **Step 1: Failing tests**: `SingleInstanceTests`: `--installed-from <path>` takes part and waits like `--after-update`; `--uninstall` does not take part. `SetupTests` (fakes for launch, dialogs, shell; registry sandbox not needed when `Install` is injected as a `Func`): Run from here sets `SetupDeclined` and `StartMenuAsked`; Install with only the Start menu box adds the entry for the running exe and does not start anything; Install with Program Files starts `<installed> --installed-from <old>` and shuts down; a failed later step calls `Undo` and shows the step. `UpdateAndUninstallTests`: the uninstall removes the entry and passes the install folder to the cleanup script when the exe is the installed one.
- [ ] **Step 2: Run** `dotnet test tests/Optimizer.App.Tests -c Release`: fails.
- [ ] **Step 3: Implement.** Start flow: if `AppInstall.ShouldOffer(...)` show the setup instead of `OfferStartMenuEntry`; `--installed-from` waits for the lock (Program: `UpdateWait` when the args are `--after-update` or `--installed-from <path>`), then `DeleteOldCopy` and an info bar when kept; the installed exe at every start calls `WriteEntry` (DisplayVersion, size) when `InstalledExe` is this exe; an existing Start menu entry pointing to the old path is retargeted. `--uninstall` opens the window and runs `UninstallAsync`. Settings > General: "Install in Program Files" card while the conditions except `SetupDeclined` hold; `Reapply_UserFolder` text points to it. Strings (EN, DE): Setup_Title, Setup_Location, Setup_ProgramFiles, Setup_ProgramFilesHint, Setup_StartMenu, Setup_StartMenuHint, Setup_Install, Setup_RunHere, Setup_Failed ("Tunero could not be installed ({0}): {1}. It keeps running from here."), Setup_OldCopyKept, Settings_Install, Settings_InstallHint.
- [ ] **Step 4: Run** all tests (`dotnet build -c Release && dotnet test -c Release --no-build --filter "Category!=Hardware"`): pass.
- [ ] **Step 5: UI check** (ui-check skill): `--screenshot` with `--setup-shot`, light and dark, EN and DE, 1040 px minimum width; Settings page with the new card; `--perf` before and after (the start must not slow down: the signature check runs only when the other conditions hold, off the UI thread).
- [ ] **Step 6: Commit** with CHANGELOG (Unreleased), README (Download), CONTRIBUTING (switches; `TrustedPublishers` on certificate renewal), website download step and FAQ "remove" (EN, DE), `docs/vm-test-plan.md` rows from the spec.

### Task 6: VM run

- [ ] **Step 1:** Publish (`dotnet publish src/Optimizer.App -p:PublishProfile=SingleFile`), sign a test copy as in CONTRIBUTING step 6 (the owner's SimplySign must be connected: ask), copy into PCO-Test's Downloads.
- [ ] **Step 2:** Run the new VM plan rows on 26H2 Pro (and Home if the checkpoint exists): install, taskbar icon, reapply available, Installed apps entry, uninstall from Windows Settings and from Tunero, Run from here, `winget list` / `winget upgrade` with the entry. Note results in the plan rows.
- [ ] **Step 3:** Fix what the VM finds (new test first), commit the plan rows.
