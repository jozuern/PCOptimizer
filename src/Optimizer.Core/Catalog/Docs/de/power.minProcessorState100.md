# Minimaler Leistungszustand 100 %

## Zusammenfassung
Hält den Prozessor auch im Leerlauf auf vollem Takt. Wird oft empfohlen, bringt auf aktuellen CPUs aber keinen einheitlichen Vorteil in Spielen.

## So funktioniert es
Der minimale Leistungszustand (PROCTHROTTLEMIN) ist die niedrigste Leistungsstufe, die Windows von der CPU anfordert [1]. Bei 100 % fordert Windows nie einen niedrigeren Takt an. Moderne CPUs gehen trotzdem in Ruhezustände, wachen aber mit vollem Takt auf.

## Warum es helfen kann
Theoretisch spart sich die CPU das Hochtakten, wenn Last kommt.

## Belege
Aktuelle CPUs takten innerhalb von Millisekunden hoch. Spiele-Benchmarks zeigen selten Unterschiede über die Messschwankung hinaus.

## Nachteile & Risiken
Spürbar höherer Leerlaufverbrauch, mehr Wärme und Lüfterlärm.

## Wann du es nicht nutzen solltest
Auf Laptops nicht sinnvoll, ebenso wenn dir ein leiser Leerlauf wichtig ist. Nimm lieber einen Leistungs-Energiesparplan.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
