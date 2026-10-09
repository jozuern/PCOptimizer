# Auf den Energiesparplan „Ausbalanciert“ wechseln

## Zusammenfassung
Wechselt auf den Windows-Standard „Ausbalanciert“: statt Energiesparmodus wieder voller CPU-Takt, in den Profilen „Leise und kühl“ und „Akku“ statt Höchstleistung weniger Wärme und Lärm.

## So funktioniert es
Windows-Energiesparpläne legen fest, wie der Prozessor seinen Takt hebt und senkt, wann er Kerne parkt und wie Geräte Strom sparen. Der Energiesparmodus begrenzt die Prozessorleistung und reagiert träge auf Last. „Ausbalanciert“ hebt den Takt schnell an, wenn Arbeit anliegt, und senkt ihn im Leerlauf [1]. „Höchstleistung“ und darauf aufbauende Pläne wie „Ultimative Leistung“ liefern maximale Leistung auf Kosten eines höheren Stromverbrauchs; „Ausbalanciert“ passt Leistung und Verbrauch dem Bedarf an [2].

## Warum es helfen kann
Ein Spiel auf einer CPU, die der Energiesparmodus bremst, bekommt weniger Bilder und unruhigere Frametimes. „Ausbalanciert“ hebt die Grenze auf, ohne den Leerlaufverbrauch nennenswert zu erhöhen.

In den Profilen „Leise und kühl“ und „Akku“ ist es umgekehrt: Ein Höchstleistungsplan verbraucht bei wenig Last mehr Strom als nötig, also mehr Wärme, mehr Lüfterlärm und weniger Akkulaufzeit. „Ausbalanciert“ senkt den Verbrauch, wenn wenig zu tun ist, und erreicht unter Last trotzdem den vollen Takt [2].

## Belege
Vom Energiesparmodus aus hängt der Effekt davon ab, wie stark der Plan die CPU begrenzt hat. Auf Desktops ist er meist groß. Von „Höchstleistung“ aus geht es um Verbrauch und Wärme, nicht um FPS, und wie groß der Unterschied ist, hängt vom Prozessor ab. „Ausbalanciert“ ist auch der Plan, den AMD für Ryzen-X3D-Prozessoren mit zwei Chiplets empfiehlt.

## Nachteile & Risiken
Unter Last etwas höherer Stromverbrauch als im Energiesparmodus. Im Leerlauf auf den meisten Desktops kein Unterschied. Gegenüber „Höchstleistung“ gibt es unter Last keine niedrigere Taktgrenze.

## Wann du es nicht nutzen solltest
Fürs Spielen ist es nicht nötig, wenn du bereits „Ausbalanciert“, „Höchstleistung“ oder einen eigenen Leistungsplan nutzt. Deshalb empfiehlt die App es statt „Höchstleistung“ nur in den Profilen „Leise und kühl“ und „Akku“.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
2. https://learn.microsoft.com/en-us/windows/win32/power/power-policy-settings
