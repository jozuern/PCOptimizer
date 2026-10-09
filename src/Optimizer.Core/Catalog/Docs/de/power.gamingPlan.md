# Gaming-Energiesparplan (auf Basis von „Höchstleistung“)

## Zusammenfassung
Legt den Plan „PCOptimizer Gaming“ als Kopie von „Höchstleistung“ an und aktiviert ihn. Der Takt bleibt zwischen Lastspitzen höher.

## So funktioniert es
Die App kopiert den eingebauten Plan „Höchstleistung“, benennt die Kopie und aktiviert sie. Microsoft beschreibt „Höchstleistung“ als Plan mit maximaler Leistung auf Kosten eines höheren Stromverbrauchs [1]. Im Netzbetrieb hält er laut Windows-Standardwerten den minimalen Leistungszustand des Prozessors bei 100 % statt bei 5 % wie „Ausbalanciert“, der Takt bleibt also auch bei wenig Last hoch [2]. Deine anderen Pläne bleiben unverändert.

## Warum es helfen kann
In Spielen mit ungleichmäßiger Last muss der Prozessor nicht bei jeder Lastspitze erst hochtakten. Das kann die Frametimes etwas glätten.

## Belege
Im Vergleich zu „Ausbalanciert“ auf einem aktuellen Desktop sind die Unterschiede meist klein, oft innerhalb der Messschwankung. Vom Energiesparmodus aus hängt der Gewinn vom Prozessor und vom Spiel ab.

## Nachteile & Risiken
Höherer Verbrauch und höhere Temperatur im Leerlauf. Die Einstellung „Energiemodus“ gibt es nur mit „Ausbalanciert“ oder davon abgeleiteten Plänen, sie verschwindet also, solange dieser Plan aktiv ist [3]. Nicht verfügbar auf Laptops, auf PCs mit Modern Standby (sie erlauben nur „Ausbalanciert“ [1]) und auf Ryzen-X3D-Prozessoren mit zwei Chiplets.

## Wann du es nicht nutzen solltest
Nicht auf Ryzen-X3D-CPUs mit mehreren Chiplets nutzen. Nutzt du schon „Ultimative Leistung“, behalte nur einen der beiden Pläne.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/power/power-policy-settings
2. https://learn.microsoft.com/en-us/windows-server/administration/performance-tuning/hardware/power/power-performance-tuning
3. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
