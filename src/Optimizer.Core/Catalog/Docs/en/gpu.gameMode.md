# Game Mode on

## Summary
Makes sure Windows Game Mode is on. It gives the running game priority and blocks driver installs and restart prompts from Windows Update while you play.

## How it works
When Game Mode detects a game, Windows gives it priority access to hardware resources [2] and holds back Windows Update driver installs and restart notifications [1]. Since the October 2018 update it is on by default for all games, with one switch in Settings [1]. The app sets it back on if it was turned off, with the registry values behind that switch; Microsoft does not document the values on their own.

## Why it can help
Fewer interruptions from background work and update prompts during play. Microsoft says this can mean less FPS variability [1].

## Evidence
Microsoft states that players may see improved game performance with less FPS variability, depending on the game and the system, and gives no numbers [1].

## Trade-offs & risks
Rarely, a game behaves worse with Game Mode; it can be turned off again.

## When not to use it
Keep it on. Turn it off only for a game that runs worse with it.

## Sources
1. https://news.xbox.com/en-us/2018/10/02/latest-october-2018-windows-update-gaming-features-3/
2. https://learn.microsoft.com/en-us/previous-versions/windows/desktop/gamemode/game-mode-portal
