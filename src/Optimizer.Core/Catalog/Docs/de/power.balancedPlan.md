# Auf den Energiesparplan „Ausbalanciert“ wechseln

## Zusammenfassung
Ersetzt den Energiesparmodus durch „Ausbalanciert“, den Windows-Standard. Der Prozessor erreicht wieder seinen vollen Takt.

## So funktioniert es
Windows-Energiesparpläne legen fest, wie der Prozessor seinen Takt hebt und senkt, wann er Kerne parkt und wie Geräte Strom sparen. Der Energiesparmodus begrenzt die Prozessorleistung und reagiert träge auf Last. „Ausbalanciert“ hebt den Takt schnell an, wenn Arbeit anliegt, und senkt ihn im Leerlauf [1].

## Warum es helfen kann
Ein Spiel auf einer CPU, die der Energiesparmodus bremst, bekommt weniger Bilder und unruhigere Frametimes. „Ausbalanciert“ hebt die Grenze auf, ohne den Leerlaufverbrauch nennenswert zu erhöhen.

## Belege
Wie groß der Effekt ist, hängt davon ab, wie stark der Sparplan die CPU begrenzt hat. Auf Desktops ist er meist groß. „Ausbalanciert“ ist auch der Plan, den AMD für Ryzen-X3D-Prozessoren mit zwei Chiplets empfiehlt.

## Nachteile & Risiken
Unter Last etwas höherer Stromverbrauch als im Energiesparmodus. Im Leerlauf auf den meisten Desktops kein Unterschied.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn du bereits „Ausbalanciert“, „Höchstleistung“ oder einen eigenen Leistungsplan nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
