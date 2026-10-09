# Transparency effects off

## Summary
Turns off the blurred, translucent backgrounds (acrylic and Mica) in Start, taskbar and apps.

## How it works
Acrylic and Mica blur the content behind a surface, which the compositor has to render continuously [1]. EnableTransparency = 0 switches them to solid colors, like Settings > Personalization > Colors > Transparency effects.

## Why it can help
Slightly less work for the GPU on the desktop; most relevant on integrated graphics.

## Evidence
No measurable effect on games with a dedicated graphics card.

## Trade-offs & risks
Purely visual: solid backgrounds instead of translucent ones.

## When not to use it
Keep it on if you like the look and have a dedicated graphics card.

## Sources
1. https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic
