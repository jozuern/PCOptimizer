# Transparency effects off

## Summary
Turns off the blurred, translucent backgrounds (acrylic and Mica) in Start, taskbar and apps.

## How it works
Acrylic blurs the content behind menus, flyouts and panels, which Microsoft calls GPU-intensive [1]. Mica tints window backgrounds with the wallpaper, which it samples only once [2]. EnableTransparency = 0 switches both to solid colors, like Settings > Personalization > Colors > Transparency effects [1][2].

## Why it can help
Slightly less GPU work for acrylic surfaces on the desktop; most relevant on integrated graphics and on battery.

## Evidence
No measurable effect on games with a dedicated graphics card.

## Trade-offs & risks
Purely visual: solid backgrounds instead of translucent ones.

## When not to use it
Keep it on if you like the look and have a dedicated graphics card.

## Sources
1. https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic
2. https://learn.microsoft.com/en-us/windows/apps/design/style/mica
