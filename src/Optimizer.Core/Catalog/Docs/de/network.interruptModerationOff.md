# Interrupt-Moderation aus (Experte)

## Zusammenfassung
Lässt den kabelgebundenen Adapter jedes Paket sofort melden, statt Pakete zu bündeln. Spart Mikrosekunden im PC, kostet Prozessorzeit. Umstritten.

## So funktioniert es
Mit Interrupt-Moderation wartet der Adapter kurz und meldet mehrere empfangene Pakete mit einem Interrupt [1]. Ist sie aus, löst er für jedes Paket einen Interrupt aus. Die App ändert das Standard-Schlüsselwort [2] nur, wenn der Treiber deines Adapters es anbietet, und startet den Adapter neu.

## Warum es helfen kann
Jedes empfangene Paket erreicht das Spiel etwas früher, um die Zeit, die der Adapter sonst wartet [1]. Microsoft gibt solche Verzögerungen innerhalb des PCs meist in Mikrosekunden an [3].

## Belege
Die Laufzeit eines Pakets zum Spielserver liegt im Bereich von Millisekunden, und diese Einstellung verkürzt sie nicht [3]. Der Gewinn ist daher viel kleiner als die normalen Ping-Schwankungen. Messungen mit einem Vorteil in Spielen sind uns nicht bekannt.

## Nachteile & Risiken
Jedes Paket löst einen Interrupt aus. Das kostet bei hohen Datenraten wie Downloads oder Streaming Prozessorzeit [3].

## Wann du es nicht nutzen solltest
Wenn du beim Spielen herunterlädst oder streamst.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/interrupt-moderation
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
3. https://learn.microsoft.com/en-us/windows-server/networking/technologies/network-subsystem/net-sub-performance-tuning-nics
