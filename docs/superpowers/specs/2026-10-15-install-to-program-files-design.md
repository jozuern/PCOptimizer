# Install to Program Files: design

Approved in chat on 2026-10-15. Status: spec, waiting for the owner's review.

## Goal

A downloaded `Tunero.exe` runs from wherever it was saved, usually Downloads, a folder the signed-in user can change.
There the reapply task cannot be offered (it starts the exe elevated without asking, so the exe must be in a folder only
administrators can change), Windows does not list the app as installed, and the Start menu entry points to Downloads.
On its first start Tunero offers to install itself: copy the exe to `C:\Program Files\Tunero`, add the Start menu entry
and register under Settings > Apps > Installed apps. Running from Downloads stays possible.

What the owner decided: a full-window setup screen (not an info bar or dialog), both options checked by default, the
copy in Downloads removed after the move, and the registration under Installed apps.

## When the setup screen appears

Shown at start, instead of the pages, when all of these hold:

- the published exe (`InstallFacts.IsPublishedExe`), running elevated, from a plain start (`SingleInstance.Applies`);
- not a package manager copy (`InstallFacts.Package` is null) and not installed already (the exe is not
  `InstallLocation.Exe`, see below);
- the setting `SetupDeclined` is false;
- the running exe's file carries a valid Authenticode signature by a publisher in `TrustedPublishers` (below). An
  unsigned or foreign-signed exe never offers Install: it runs from where it is, with the old Start menu question.

Current users who update from 1.2.x get it once too. It replaces the Start menu question (`OfferStartMenuEntry`) for
the copies that get the screen; the others keep that question as today. Developer switches and command line runs never
show it.

## The screen

A view that fills the window's content area (the navigation is hidden while it shows), Fluent cards and spacing, light and
dark, EN and DE:

- Title "Set up Tunero", a line "Tunero runs from {folder} right now."
- Check box "Put Tunero in Program Files", checked. Hint: only administrators can change it there, so the reapply task
  can start it and Windows lists it under Installed apps; the copy in {folder} is removed.
- Check box "Add it to the Start menu", checked. Hint: Windows Search then finds Tunero.
- Buttons: "Install" (primary) and "Run from here".

With only the Start menu box checked, Install adds the entry for the exe where it is (today's behavior). With neither box
checked, Install acts as "Run from here". "Run from here" sets `SetupDeclined` (and `StartMenuAsked`) and opens the
Overview. Settings > General gets "Install in Program Files" (shown while the screen's conditions except
`SetupDeclined` hold), which opens the same screen; the reapply hint `Reapply_UserFolder` points to it.

## What Install does

In order, all elevated; each step that fails undoes the earlier ones and Tunero keeps running from where it is, with an
info bar naming the step and the error:

1. `C:\Program Files\Tunero` (`%ProgramW6432%`): created if missing; if it exists it must pass
   `TrustedPath.IsAdminOnlyWritableFolder` and hold no files other than `Tunero.exe` and `Tunero.exe.old`, or Install
   stops. A new folder inherits Program Files' permissions (users read and run, only administrators change), so the
   taskbar can read the exe's icon.
2. Copy the running exe to `Tunero.exe.new` in that folder, from a handle opened with read sharing only. Check the copy,
   now in an admin-only place: valid Authenticode signature, signer in `TrustedPublishers`, file version equal to the
   running app's. Then move it to `Tunero.exe` (replacing an older one).
3. Registry: `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Tunero` with DisplayName "Tunero",
   DisplayVersion, Publisher "Joshua Zürn", DisplayIcon (the exe), InstallLocation, UninstallString
   `"<exe>" --uninstall`, URLInfoAbout `https://tunero.dev`, EstimatedSize (KB), NoModify 1, NoRepair 1.
4. Start menu: the entry for the installed exe (`StartMenuShortcut.Create`, or an existing entry moved to the new target).
5. Start the installed exe with `--installed-from "<old exe>"` (a hardened start like the update's) and close. The new
   process waits for the single-instance lock like `--after-update`, then deletes the old exe with `SafeDelete.DeleteFile`
   when it is still the same file (its SHA-256 equals the installed exe's; a different file at that path stays). A failure to
   delete leaves it and shows an info bar with the path.

`TrustedPublishers`: the certificate subjects accepted as Tunero's publisher, compiled in. Today
`CN=Open Source Developer Joshua Zürn, O=Open Source Developer, L=Neudenau, S=Baden-Württemberg, C=DE`; a renewed
certificate with another subject is added in the release before it is used.

## Later

- Every start of the installed exe sets DisplayVersion and EstimatedSize in its Installed apps entry, so a self-update
  (which replaces the exe in place, as today) shows the new version.
- `--uninstall` (Windows Settings' Uninstall button) opens the window and starts Settings > Uninstall's flow, with its
  question. The uninstall removes the Installed apps entry (before the cleanup starts) and the cleanup script removes the
  `Program Files\Tunero` folder once it is empty (exe, `.old`), not following links.
- A copy moved by hand into another admin-only folder keeps working as today (reapply allowed, no entry).

## Code

- `Optimizer.Core/Platform/AppInstall.cs`: the steps above against `IRegistryRoots`, a file system root and a signature
  check passed in, so tests run in the sandbox registry and a temp folder. Pure decisions (`ShouldOffer`, the registry
  values) are separate functions.
- `Optimizer.App`: `SetupView` (XAML) and its view model, the Settings entry, `--installed-from` and `--uninstall` in
  `App.xaml.cs`, `SingleInstance.Applies` accepts `--installed-from <path>` like `--after-update`.
- Strings in `Strings.resx` and `Strings.de.resx`, following the text rules (no dashes, no "(s)").

## Tests

- Unit (no real adapter): when the screen shows; the registry values; install into a temp folder with a fake signature
  check (success, a signature that fails, an existing folder with other files, undo after a failed step); the cleanup
  script removes the install folder; `--installed-from` deletes only the same file.
- VM (`docs/vm-test-plan.md`, new rows): install from Downloads on 26H2 Pro and Home; the taskbar icon; reapply task
  available afterwards; Start menu entry; Installed apps shows name, version, publisher, icon; Uninstall from Windows
  Settings and from Tunero removes folder, entry and shortcut; update from the previous version updates DisplayVersion;
  "Run from here"; `winget list` and `winget upgrade` with an installed copy (does winget take the entry for
  `jozuern.Tunero`; adjust the entry if it misleads winget).

## Docs

README (Download: Install on first start), CHANGELOG, `docs/vm-test-plan.md`, CONTRIBUTING (the new switches, the
publisher list for certificate renewals), website download step and FAQ "Can I remove it completely?" (EN and DE).
PRIVACY needs no change: nothing goes online.

## Out of scope

Per-user installs, choosing another folder, an MSI or separate installer, repair or modify, and installing package
manager copies.
