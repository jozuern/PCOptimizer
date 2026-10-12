# Keine Update-Neustarts von 8 bis 2 Uhr

## Zusammenfassung
Windows startet zwischen 8 und 2 Uhr nicht von selbst für Updates neu, damit ein Spiel am Abend oder ein später Download nicht abbricht. Pro, Enterprise und Education.

## So funktioniert es
Windows startet nach einem Update außerhalb seiner Nutzungszeit neu, standardmäßig 8 bis 17 Uhr [1]. Die Richtlinie gegen automatische Neustarts während der Nutzungszeit (englisch "Turn off auto-restart for updates during active hours") legt die Nutzungszeit fest: Die App setzt SetActiveHours = 1, ActiveHoursStart = 8 und ActiveHoursEnd = 2 [1]. 18 Stunden sind der längste Zeitraum, den Windows erlaubt [1][2]. Updates werden weiter wie gewohnt geladen und installiert; nur der automatische Neustart wartet bis 2 Uhr.

## Warum es helfen kann
Mit der Standardzeit kann ein automatischer Neustart am Abend kommen, wenn die meisten spielen. Mit der Richtlinie rückt er in die Nacht.

## Belege
Eine von Microsoft beschriebene Windows-Update-Richtlinie [1][2]. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Solange die Richtlinie gesetzt ist, gilt die Nutzungszeit, die du in den Einstellungen gewählt hast, nicht mehr [2]. Updates brauchen weiter einen Neustart, um zu wirken: Der PC muss außerhalb dieser Zeit an sein, oder du startest selbst neu [2]. Rückgängig entfernt die Richtlinie.

## Wann du es nicht nutzen solltest
Wenn du den PC vor allem nachts nutzt oder Windows die Nutzungszeit aus deinem Verhalten bestimmen lassen willst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
