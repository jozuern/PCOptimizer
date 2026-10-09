# Game DVR background recording off

## Summary
Turns off the Xbox Game Bar capture features, including "Record what happened", which records the last minutes of gameplay in the background all the time.

## How it works
With background recording on, Windows continuously encodes the last minutes of gameplay into a buffer using the GPU's video encoder. The app turns off Game DVR, app capture and background recording [1]. The Game Bar itself stays installed.

## Why it can help
Continuous recording uses GPU encoder time, memory and disk writes. Turning it off removes that load while gaming.

## Evidence
Background recording is off by default; if it is off, this tweak changes nothing measurable. With it on, the cost is small on current GPUs but measurable on older ones.

## Trade-offs & risks
You lose Game Bar clips and the "Record that" shortcut. Recording with NVIDIA ShadowPlay or OBS is not affected.

## When not to use it
Keep it if you use Game Bar clips.

## Sources
1. https://github.com/ChrisTitusTech/winutil
