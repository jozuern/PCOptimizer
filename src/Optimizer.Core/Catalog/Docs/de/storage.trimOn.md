# TRIM an

## Zusammenfassung
Schaltet TRIM wieder ein, damit Windows SSDs mitteilt, welche Blöcke frei sind. Hält die Schreibgeschwindigkeit dauerhaft hoch.

## So funktioniert es
Löschst du eine Datei, markiert Windows den Platz nur als frei. TRIM gibt diese Information an die SSD weiter, die die Blöcke dann im Hintergrund löschen kann. Der Wert DisableDeleteNotification = 0 schaltet es ein (wie fsutil behavior set DisableDeleteNotify 0) [1].

## Warum es helfen kann
Ohne TRIM muss die SSD bei späteren Schreibvorgängen aufräumen. Das bremst Installationen und Spiele-Updates.

## Belege
Windows schaltet TRIM standardmäßig ein. Dieser Tweak ist nur wichtig, wenn es abgeschaltet wurde.

## Nachteile & Risiken
Keine.

## Wann du es nicht nutzen solltest
Nichts zu beachten. TRIM sollte immer an sein.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
