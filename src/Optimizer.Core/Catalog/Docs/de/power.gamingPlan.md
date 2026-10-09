# Gaming-Energiesparplan (auf Basis von „Höchstleistung“)

## Zusammenfassung
Legt den Plan „PCOptimizer Gaming“ als Kopie von „Höchstleistung“ an und aktiviert ihn. Der Takt bleibt zwischen Lastspitzen höher.

## So funktioniert es
Die App kopiert den eingebauten Plan „Höchstleistung“, benennt die Kopie und aktiviert sie. „Höchstleistung“ hält einen höheren Mindest-Leistungszustand und parkt Kerne weniger aggressiv als „Ausbalanciert“ [1]. Deine anderen Pläne bleiben unverändert.

## Warum es helfen kann
In Spielen mit ungleichmäßiger Last muss der Prozessor nicht bei jeder Lastspitze erst hochtakten. Das kann die Frametimes etwas glätten.

## Belege
Im Vergleich zu „Ausbalanciert“ auf einem aktuellen Desktop sind die gemessenen Unterschiede meist klein, oft innerhalb der Messschwankung. Vom Energiesparmodus aus ist der Gewinn groß.

## Nachteile & Risiken
Höherer Verbrauch und höhere Temperatur im Leerlauf. Nicht verfügbar auf Ryzen-X3D-Prozessoren mit zwei Chiplets (sie brauchen „Ausbalanciert“) und auf Laptops mit Modern Standby.

## Wann du es nicht nutzen solltest
Nicht auf Ryzen-X3D-CPUs mit mehreren Chiplets oder auf Laptops nutzen. Nutzt du schon „Ultimative Leistung“, behalte nur einen der beiden Pläne.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
