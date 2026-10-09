# Ruhezustand aus

## Zusammenfassung
Schaltet den Ruhezustand ab und löscht hiberfil.sys, deren Größe einem Anteil deines Arbeitsspeichers entspricht. Das gibt diesen Speicherplatz frei. Schaltet auch den Schnellstart ab.

## So funktioniert es
Die App führt powercfg /hibernate off aus [1]. Windows löscht die Ruhezustandsdatei und entfernt Ruhezustand und Schnellstart. Rückgängig machen führt powercfg /hibernate on aus.

## Warum es helfen kann
Gibt den Platz der Ruhezustandsdatei auf dem Systemlaufwerk frei, was bei knappem Platz hilft. Auf die Leistung hat das keinen Einfluss.

## Belege
Kein Effekt auf die FPS.

## Nachteile & Risiken
Kein Ruhezustand und kein Schnellstart mehr. Laptops mit Modern Standby wechseln nach einem festgelegten Akkuverbrauch im Standby in den Ruhezustand, damit der Akku beim Aufklappen nicht leer ist [2]; dort ist der Tweak gesperrt. Rückgängig schaltet den Ruhezustand mit der Standardgröße der Datei wieder ein; eine eigene Größe wird nicht wiederhergestellt.

## Wann du es nicht nutzen solltest
Nicht auf Laptops mit Modern Standby nutzen und nicht, wenn du den Ruhezustand verwendest.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/adaptive-hibernate
