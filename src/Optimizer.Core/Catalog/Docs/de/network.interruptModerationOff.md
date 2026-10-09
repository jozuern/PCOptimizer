# Interrupt-Moderation aus (Experte)

## Zusammenfassung
Lässt den kabelgebundenen Netzwerkadapter jedes Paket sofort melden, statt Pakete zu bündeln. Kann die Verzögerung um Mikrosekunden senken, kostet Prozessorzeit. Umstritten.

## So funktioniert es
Mit Interrupt-Moderation wartet der Adapter kurz und meldet mehrere empfangene Pakete mit einem Interrupt [1]. Ist sie aus, löst er für jedes Paket einen Interrupt aus. Die App ändert das Standard-Schlüsselwort nur, wenn der Treiber deines Adapters es anbietet, und startet den Adapter neu.

## Warum es helfen kann
Jedes Paket erreicht das Spiel etwas früher, um die Wartezeit der Moderation (je nach Treiber typischerweise Mikrosekunden bis Bruchteile einer Millisekunde).

## Belege
Die Netzwerklatenz zu einem Spielserver liegt im Bereich von Millisekunden, der Gewinn geht meist in den normalen Schwankungen unter. Messungen auf Gaming-PCs zeigen keine einheitliche Verbesserung.

## Nachteile & Risiken
Mehr Interrupts bedeuten bei hohen Datenraten (Downloads, Streaming) mehr Prozessorlast. Auf Prozessoren mit wenigen Kernen kann das Bildrate kosten.

## Wann du es nicht nutzen solltest
Auf Prozessoren mit wenigen Kernen oder wenn du beim Spielen herunterlädst.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/interrupt-moderation
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
