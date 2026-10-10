# Automatische Wiedergabe auf allen Laufwerken aus

## Zusammenfassung
Eingesteckte USB-Sticks, Discs und Speicherkarten starten keine Programme und öffnen keine Abfrage mehr von selbst.

## So funktioniert es
Die automatische Wiedergabe liest ein Laufwerk, sobald ein Medium eingelegt wird, sodass Setup-Programme und Musik sofort starten [1]. Die Richtlinie "Automatische Wiedergabe deaktivieren" für alle Laufwerke (NoDriveTypeAutoRun = 255) schaltet sie auf allen Laufwerken aus [1].

## Warum es helfen kann
Ein fremder USB-Stick kann beim Einstecken nichts von selbst starten.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Neue Laufwerke öffnest du selbst im Explorer; die Auswahl zur automatischen Wiedergabe in den Einstellungen gilt nicht mehr.

## Wann du es nicht nutzen solltest
Wenn du die Abfrage beim Einlegen einer Kamerakarte oder Disc magst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-autoplay
