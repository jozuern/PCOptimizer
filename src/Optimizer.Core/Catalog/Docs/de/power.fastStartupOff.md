# Schnellstart aus

## Zusammenfassung
Macht „Herunterfahren“ wieder zu einem echten Herunterfahren. Treiber starten beim nächsten Einschalten frisch, statt aus einer Ruhezustandsdatei geladen zu werden.

## So funktioniert es
Mit Schnellstart speichert Windows beim Herunterfahren Kernel und Treiber in die Ruhezustandsdatei und stellt sie beim nächsten Start wieder her [1]. Ohne Schnellstart initialisiert Windows bei jedem Start alle Treiber neu.

## Warum es helfen kann
Probleme, die sich in einem Treiber aufbauen (etwa nach einem GPU-Treiberupdate), verschwinden dann auch beim normalen Herunterfahren, nicht nur beim Neustart. Dual Boot und BIOS-Änderungen verhalten sich ebenfalls berechenbarer.

## Belege
Kein Effekt auf die FPS. Der Start dauert auf den meisten PCs mit SSD ein paar Sekunden länger.

## Nachteile & Risiken
Etwas langsamerer Kaltstart.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn du selten herunterfährst oder dir die Startzeit wichtiger ist als ein sauberer Treiberzustand.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/distinguishing-fast-startup-from-wake-from-hibernation
