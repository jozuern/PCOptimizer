# Game-Bar-Aufnahme per Richtlinie aus

## Zusammenfassung
Schaltet die Spielaufnahme und Übertragung von Windows mit der dokumentierten Richtlinie ab, damit die Hintergrundaufnahme aufhört. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie AllowGameDVR = 0 unter SOFTWARE\Policies\Microsoft\Windows\GameDVR schaltet die Funktionen „Windows-Spielaufzeichnung und -übertragung“ ab [1]. Microsoft nennt sie für Pro, Enterprise und Education [1]. Sie ist der dokumentierte Weg zu dem, was „Game-Bar-Aufnahmen aus“ mit den Werten hinter den Schaltern in den Einstellungen macht.

## Warum es helfen kann
Mit eingeschalteter Hintergrundaufnahme zeichnet die Game Bar ständig die letzten Minuten eines Spiels auf und nutzt dafür den Video-Encoder der Grafikkarte und das Laufwerk. Mit der Richtlinie zeichnet die Game Bar nichts auf.

## Belege
Dokumentierte Richtlinie [1]. Ein Effekt auf Spiele ist nur möglich, solange die Hintergrundaufnahme an war, und hängt dann von der Situation ab.

## Nachteile & Risiken
Die Game Bar kann keine Clips mehr aufnehmen und nicht mehr übertragen. Aufnahmen mit anderen Programmen wie der NVIDIA App oder OBS funktionieren weiter.

## Wann du es nicht nutzen solltest
Wenn du Clips mit der Game Bar aufnimmst oder damit streamst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
