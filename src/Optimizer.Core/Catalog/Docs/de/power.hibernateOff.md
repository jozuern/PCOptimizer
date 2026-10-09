# Ruhezustand aus

## Zusammenfassung
Schaltet den Ruhezustand ab und löscht hiberfil.sys. Das gibt Speicherplatz in der Größe eines großen Teils deines Arbeitsspeichers frei. Schaltet auch den Schnellstart ab.

## So funktioniert es
Die App führt powercfg /hibernate off aus [1]. Windows löscht die Ruhezustandsdatei und entfernt Ruhezustand und Schnellstart. Rückgängig machen führt powercfg /hibernate on aus.

## Warum es helfen kann
Gibt mehrere Gigabyte auf dem Systemlaufwerk frei, was bei knappem Platz hilft. Auf die Leistung hat das keinen Einfluss.

## Belege
Kein Effekt auf die FPS.

## Nachteile & Risiken
Kein Ruhezustand und kein Schnellstart mehr. Laptops mit Modern Standby nutzen den Ruhezustand, um im langen Standby den Akku zu schonen. Dort ist der Tweak gesperrt.

## Wann du es nicht nutzen solltest
Nicht auf Laptops mit Modern Standby nutzen und nicht, wenn du den Ruhezustand verwendest.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
