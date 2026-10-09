# PCIe-Verbindungsenergieverwaltung aus

## Zusammenfassung
Schaltet im Netzbetrieb die PCIe-Verbindungsenergieverwaltung (ASPM) ab, die Verbindungen bleiben im Leerlauf voll aktiv. Ein Vorteil in Spielen ist umstritten, der Leerlaufverbrauch steigt.

## So funktioniert es
ASPM lässt PCIe-Verbindungen (Grafikkarte, NVMe-SSD, Netzwerkkarte) im Leerlauf in Stromsparzustände wechseln; „Mittlere Energieeinsparungen“ nutzt einen leichten, „Maximale Energieeinsparungen“ einen tieferen Zustand [1]. Das Verlassen eines Stromsparzustands kostet eine kurze Verzögerung. In den Windows-Standardwerten nutzt „Ausbalanciert“ im Netzbetrieb „Mittlere Energieeinsparungen“, „Höchstleistung“ „Aus“. Die App setzt den Wert für den Netzbetrieb auf „Aus“.

## Warum es helfen kann
Die Aufwachverzögerung ungenutzter Verbindungen entfällt.

## Belege
Messungen in Spielen zeigen selten einen Unterschied. Eine GPU unter Last schickt ihre Verbindung ohnehin nicht in den Ruhezustand.

## Nachteile & Risiken
Etwas höherer Leerlaufverbrauch, vor allem bei mehreren PCIe-Geräten.

## Wann du es nicht nutzen solltest
Ohne konkretes Problem nicht nötig. Auf Laptops wegen der Akkulaufzeit eingeschaltet lassen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
