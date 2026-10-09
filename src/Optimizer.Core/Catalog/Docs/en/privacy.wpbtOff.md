# Windows Platform Binary Table off

## Summary
Stops Windows from running programs the firmware provides at boot (WPBT). Some board vendors use it to install their apps automatically.

## How it works
WPBT is an ACPI table in which the firmware can place a program that Windows runs at every start. DisableWpbtExecution = 1 tells Windows to ignore it [1]. Takes effect after a restart.

## Why it can help
Prevents vendor tools from reinstalling themselves after you removed them.

## Evidence
No frame-rate effect.

## Trade-offs & risks
OEM recovery or anti-theft agents that rely on WPBT stop working.

## When not to use it
Keep it if you use an OEM anti-theft or recovery service.

## Sources
1. https://github.com/ChrisTitusTech/winutil
