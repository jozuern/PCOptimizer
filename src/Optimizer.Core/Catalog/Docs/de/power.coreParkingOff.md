# Core Parking aus

## Zusammenfassung
Hält im Netzbetrieb alle Prozessorkerne aktiv. Wird oft empfohlen, Tests zeigen auf aktuellen CPUs aber keinen einheitlichen Vorteil in Spielen.

## So funktioniert es
Mit Core Parking kann Windows ungenutzte Kerne in einen tiefen Schlafzustand schicken und Threads auf weniger Kerne verteilen. Die Einstellung CPMINCORES legt den Mindestanteil aktiver Kerne fest [1]. 100 % schaltet das Parken ab.

## Warum es helfen kann
Braucht ein Spiel plötzlich mehr Threads, müssen geparkte Kerne erst aufwachen. Ohne Parken entfällt diese Verzögerung.

## Belege
Auf aktuellen Intel- und AMD-Desktop-CPUs liegen die gemessenen Unterschiede meist innerhalb der Messschwankung. Windows holt Kerne unter Last ohnehin schnell zurück.

## Nachteile & Risiken
Höherer Leerlaufverbrauch. Auf Ryzen-X3D-Prozessoren mit zwei Chiplets ist der Tweak gesperrt: Der AMD-Treiber braucht das Parken, um Spiele auf dem V-Cache-Chiplet zu halten.

## Wann du es nicht nutzen solltest
Nicht auf Ryzen-X3D-CPUs mit mehreren Chiplets nutzen. Auf Laptops und hybriden Intel-CPUs (P- und E-Kerne) nicht empfohlen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
