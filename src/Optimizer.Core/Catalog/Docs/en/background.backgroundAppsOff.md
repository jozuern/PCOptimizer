# Background apps off

## Summary
Stops Store apps from running in the background. Effect disputed on current Windows 11 builds.

## How it works
The value GlobalUserDisabled = 1 blocks background activity of packaged (Store) apps for your user [1]. Windows 11 manages background permissions per app, and not every build honors the global value.

## Why it can help
Fewer Store apps waking up in the background.

## Evidence
Desktop programs (Steam, Discord, launchers) are not affected. The measurable effect on games is usually zero.

## Trade-offs & risks
Store apps such as Mail, Calendar or Phone Link stop sending notifications and syncing in the background.

## When not to use it
Keep background apps if you rely on Store app notifications.

## Sources
1. https://github.com/ChrisTitusTech/winutil
