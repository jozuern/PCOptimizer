# Klassisches F8-Startmenü

## Zusammenfassung
Bringt das Menü Erweiterte Optionen zurück, das sich beim Start mit F8 öffnet, für den abgesicherten Modus, ohne Windows erst zu starten. Ändert die Startkonfiguration.

## So funktioniert es
Die Startkonfiguration bootmenupolicy bestimmt den Typ des Startmenüs: Standard ist ab Windows 10 voreingestellt, mit Legacy ist das Menü Erweiterte Optionen (F8) verfügbar [1]. Mit Standard erscheint das Menü nur in bestimmten Fällen, etwa nach einem Startfehler [1]. Die App setzt Legacy.

## Warum es helfen kann
Wenn Windows nicht mehr richtig startet, erreichst du den abgesicherten Modus mit F8, statt auf die automatische Reparatur zu warten.

## Belege
Von Microsoft beschrieben [1]. Es ändert nur das Startmenü; kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Auf PCs, die sehr schnell starten, bleibt wenig Zeit für F8. Änderungen an der Startkonfiguration sind Expertenänderungen: Die App sichert den Startspeicher vorher, und Rückgängig setzt das Menü wieder auf Standard.

## Wann du es nicht nutzen solltest
Wenn du den abgesicherten Modus nicht brauchst oder dein PC zu schnell startet, um F8 zu drücken.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
