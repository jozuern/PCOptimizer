# PCIe-Verbindungsenergieverwaltung aus

## Zusammenfassung
Schaltet im Netzbetrieb Active State Power Management (ASPM) für PCIe-Verbindungen ab. Die Verbindungen wechseln im Leerlauf nicht mehr in Stromsparzustände.

## So funktioniert es
ASPM erlaubt PCIe-Verbindungen (Grafikkarte, NVMe-SSD, Netzwerkkarte), im Leerlauf in Stromsparzustände zu wechseln [1]. Die Rückkehr auf volle Leistung dauert Mikrosekunden. Die App setzt den Planwert für den Netzbetrieb auf „Aus“.

## Warum es helfen kann
Die Aufwachverzögerung ungenutzter Verbindungen entfällt. Auf manchen Mainboards gab es außerdem Stabilitätsprobleme mit ASPM, die so vermieden werden.

## Belege
Messungen in Spielen zeigen selten einen Unterschied. Eine GPU unter Last schickt ihre Verbindung ohnehin nicht in den Ruhezustand.

## Nachteile & Risiken
Etwas höherer Leerlaufverbrauch, vor allem bei mehreren PCIe-Geräten.

## Wann du es nicht nutzen solltest
Ohne konkretes Problem nicht nötig. Auf Laptops wegen der Akkulaufzeit eingeschaltet lassen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
