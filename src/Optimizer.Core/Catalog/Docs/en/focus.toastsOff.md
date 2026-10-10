# No pop-up notifications from apps

## Summary
Apps can no longer show pop-up notifications (toasts), so messages from chat apps no longer pop up. Windows system notifications are not affected.

## How it works
The policy "Turn off toast notifications" (NoToastApplicationNotification = 1) stops applications from raising toast notifications [1]. Windows system features and taskbar notification balloons are not affected, and no restart is needed [1].

## Why it can help
No pop-ups over games, videos or presentations, also from apps that ignore Do not disturb.

## Evidence
A documented Windows policy [1]. Windows already holds back many notifications in full screen games; this turns them off everywhere.

## Trade-offs & risks
You miss messages, reminders and download notices until you look. The switches in Settings > System > Notifications are locked.

## When not to use it
If you rely on notifications from chat, mail or calendar apps; Do not disturb is the gentler option.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-wpn
