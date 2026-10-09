# Schnellstart aus

## Zusammenfassung
Macht „Herunterfahren“ wieder zu einem echten Herunterfahren. Treiber starten beim nächsten Einschalten frisch, statt aus einer Ruhezustandsdatei geladen zu werden.

## So funktioniert es
Mit Schnellstart schließt Windows beim Herunterfahren alle Apps und meldet alle Benutzer ab, speichert dann Kernel und geladene Treiber in die Ruhezustandsdatei und stellt sie beim nächsten Start wieder her [1]. Die App setzt HiberbootEnabled auf 0, den dokumentierten Wert zum Abschalten des Schnellstarts [2]. Ohne Schnellstart initialisiert Windows bei jedem Start alle Treiber neu. Erzwingt deine Organisation den Schnellstart per Richtlinie, gilt die Richtlinie [3].

## Warum es helfen kann
Probleme, die sich in einem Treiber aufbauen (etwa nach einem GPU-Treiberupdate), verschwinden dann auch beim normalen Herunterfahren, nicht nur beim Neustart. Dual Boot und BIOS-Änderungen verhalten sich ebenfalls berechenbarer.

## Belege
Kein Effekt auf die FPS. Ein Kaltstart dauert länger als ein Schnellstart [1]; wie viel, hängt vom PC ab.

## Nachteile & Risiken
Langsamerer Kaltstart.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn du selten herunterfährst oder dir die Startzeit wichtiger ist als ein sauberer Treiberzustand.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation
2. https://learn.microsoft.com/en-us/windows/configuration/unified-write-filter/hibernate-once-resume-many-horm
3. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-wininit
