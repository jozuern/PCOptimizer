# Game Mode on

## Summary
Makes sure Windows Game Mode is on. It gives the running game priority and blocks driver installs and restart prompts from Windows Update while you play.

## How it works
When Game Mode detects a game, Windows gives it priority access to hardware resources [2] and holds back Windows Update driver installs and restart notifications [1]. Since the October 2018 update it is on by default for all games, with one switch in Settings [1]. The app sets it back on if it was turned off, with the registry values behind that switch; Microsoft does not document the values on their own. While Game Mode is on and a game runs, Windows also switches to the "GameMode" processor power profile, which PC makers can tune [3].

## Why it can help
Fewer interruptions from background work and update prompts during play. Microsoft says this can mean less FPS variability [1]. On Ryzen 9 X3D processors with two CCDs (7900X3D, 7950X3D, 9900X3D, 9950X3D), AMD asks for Game Mode on and the Xbox Game Bar installed, because its driver uses them to keep games on the CCD with the extra cache [4].

## Evidence
Microsoft states that players may see improved game performance with less FPS variability, depending on the game and the system, and gives no numbers [1]. For dual-CCD X3D processors, TechSpot showed what happens when that scheduling fails: a 7950X3D without the required updates was slower than a 7950X in all games tested, and in Factorio it gained almost 80 % once the game ran on the cache CCD [5]. That measures the whole chain (BIOS, chipset driver, Game Mode, Game Bar), not Game Mode alone, so the rating on these processors is situational.

## Trade-offs & risks
Rarely, a game behaves worse with Game Mode; it can be turned off again.

## When not to use it
Keep it on. Turn it off only for a game that runs worse with it.

## Sources
1. https://news.xbox.com/en-us/2018/10/02/latest-october-2018-windows-update-gaming-features-3/
2. https://learn.microsoft.com/en-us/previous-versions/windows/desktop/gamemode/game-mode-portal
3. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
4. https://www.tomshardware.com/news/amd-ryzen-7000x3d-install-guide
5. https://www.techspot.com/review/2636-amd-ryzen-9-7950x3d/
