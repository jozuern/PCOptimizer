# Change a service's start type

## Summary
Changes when this service starts: automatically with Windows, on demand (Manual) or never (Disabled).

## How it works
The start type is changed through the Service Control Manager [1], the same as in the Services console. A service on Manual can still be started by an app or by a Windows trigger. Undo restores the previous start type.

## Why it can help
Services that start with Windows use memory and can do work in the background. On demand, they only run when something needs them.

## Evidence
Most services use almost no processor time while idle, so the frame rate effect is usually not measurable.

## Trade-offs & risks
A disabled service cannot be started by anything that needs it, which can break features [1]. Manual is the safer choice.

## When not to use it
For services you do not know; Windows services the app does not explain are read-only. Disabled is offered only for services from other publishers.

## Sources
1. https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfigw
