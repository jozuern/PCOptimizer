# Brave: no News, Talk and Playlist

## Summary
Removes the Brave News feed from the new tab page, Brave Talk video calls and the Playlist feature for saving media.

## How it works
Three Brave policies [1]: BraveNewsDisabled removes the News feed from the new tab page; BraveTalkDisabled removes the widget and option to start Brave Talk calls; BravePlaylistEnabled set to 0 turns off Playlist, which saves video and audio for offline playback [1]. They need Brave 1.82 to 1.84 or later [1].

## Why it can help
A simpler browser and a new tab page that loads no news feed. It changes neither frame rate nor latency.

## Evidence
Documented Brave policies [1].

## Trade-offs & risks
Because these are policies, Brave shows "Managed by your organization" in its menu, and brave://policy lists them. Undo removes them.

## When not to use it
If you read Brave News, call with Brave Talk or use Playlist.

## Sources
1. https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy
