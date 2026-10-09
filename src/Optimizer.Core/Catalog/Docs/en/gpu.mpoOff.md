# Multiplane overlay (MPO) off

## Summary
Troubleshooting only: turns off multiplane overlay with the registry value NVIDIA documents, to fix flicker or black screens in some apps. No performance gain.

## How it works
Multiplane overlay lets the display hardware combine several layers, for example a video and the desktop, instead of the desktop compositor doing it. NVIDIA describes this as a way to improve performance and lower power use [1]. On some driver and monitor combinations it causes flicker. The app sets OverlayTestMode = 5 under HKLM\SOFTWARE\Microsoft\Windows\Dwm, the same value as NVIDIA's mpo_disable.reg; undo deletes it, like NVIDIA's mpo_restore.reg [1]. It takes effect after a restart.

## Why it can help
Removes the cause of MPO-related flicker and freezes when they occur.

## Evidence
NVIDIA documents this value for Windows 11 [1]. Microsoft does not document it. Without MPO problems there is nothing to gain.

## Trade-offs & risks
The compositor does more work, which can raise power draw, for example during video playback [1]. Needs a restart.

## When not to use it
Only if you see flicker or black flashes. Update the graphics driver and Windows first: NVIDIA notes improved MPO support from Release 610 drivers with Windows build 26100.7705 or 26200.7705 and later [1].

## Sources
1. https://nvidia.custhelp.com/app/answers/detail/a_id/5157
