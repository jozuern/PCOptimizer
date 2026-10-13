# Shorter hover time

## Summary
Programs that wait for the pointer to rest (tooltips, previews) react after 200 ms instead of Windows' 400 ms. Takes effect at once, no sign-out.

## How it works
Programs can ask Windows to tell them when the pointer has rested on one spot for the system's hover time (TrackMouseEvent with HOVER_DEFAULT) [1]. The app sets this time with the documented SystemParametersInfo function (SPI_SETMOUSEHOVERTIME) to 200 ms and lets Windows save it to your profile [1]. Undo sets the value you had before.

## Why it can help
In programs that use the system hover time, tooltips and hover effects appear sooner.

## Evidence
No performance effect, and only programs that use the system hover time react to it [1]; many draw their own tooltips with their own timing.

## Trade-offs & risks
Tooltips and hover effects can pop up while you only move the pointer across a window.

## When not to use it
If tooltips already get in your way.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
