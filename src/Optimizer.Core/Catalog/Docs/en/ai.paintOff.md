# Paint: Cocreator, generative fill and Image Creator off

## Summary
Turns off the generative AI features Cocreator, generative fill and Image Creator in Windows Paint. Pro, Enterprise and Education.

## How it works
Three Windows policies under SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint: DisableCocreator, DisableGenerativeFill and DisableImageCreator set to 1 make these features unavailable in Paint [1]. Microsoft lists them for Pro, Enterprise and Education [1].

## Why it can help
Paint stays a plain drawing app, and nothing you draw or type there goes to an AI service.

## Evidence
Documented Windows policies [1]. Generative erase and background removal in Paint have no documented policy, so they stay. The values some tools set for them (DisableGenerativeErase, DisableRemoveBackground) changed nothing in a VM test with Paint 11.2605, while Image Creator disappeared with this tweak. No effect on frame rate or latency.

## Trade-offs & risks
The AI buttons in Paint are gone.

## When not to use it
If you create images with Paint's AI features.

## Sources
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
