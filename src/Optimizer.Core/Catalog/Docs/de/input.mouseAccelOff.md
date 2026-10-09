# Mausbeschleunigung aus

## Zusammenfassung
Schaltet „Zeigerbeschleunigung verbessern“ ab. Dieselbe Handbewegung bewegt den Zeiger dann immer gleich weit.

## So funktioniert es
Ist die Option an, skaliert Windows die Zeigerbewegung mit einer geschwindigkeitsabhängigen Kurve: Eine schnelle Bewegung schiebt den Zeiger weiter als eine langsame über dieselbe Strecke. Die App setzt MouseSpeed, MouseThreshold1 und MouseThreshold2 in deinem Benutzerprofil auf 0 und übernimmt sie in die laufende Sitzung [1].

## Warum es helfen kann
Gleichbleibende Skalierung macht das Zielen in Spielen berechenbar, die den Windows-Zeiger nutzen (viele Strategiespiele, ältere Spiele, Menüs).

## Belege
Spiele, die Rohdaten der Maus lesen (die meisten aktuellen kompetitiven Shooter), umgehen diese Einstellung. Sie sind so oder so nicht betroffen.

## Nachteile & Risiken
Auf dem Desktop wirkt der Zeiger anfangs langsamer. Passe Zeigergeschwindigkeit oder Maus-DPI nach Geschmack an.

## Wann du es nicht nutzen solltest
Lass die Option an, wenn du auf dem Desktop beschleunigte Bewegung magst und nur Spiele mit Rohdaten spielst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
