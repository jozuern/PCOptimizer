# Interrupt-Affinität für dieses Gerät (Experte)

## Zusammenfassung
Leitet die Interrupts dieses Geräts auf einen bestimmten Prozessorkern, statt Windows wählen zu lassen. Umstritten: Kann helfen oder schaden, miss es.

## So funktioniert es
Windows verteilt Geräte-Interrupts normalerweise auf die Prozessorkerne. Die Interrupt-Affinitätsrichtlinie in der Registry [1] kann sie auf bestimmte Kerne lenken. Die App setzt die dokumentierte Richtlinie „angegebene Prozessoren“ und eine Maske mit einem logischen Prozessor (im Namen angezeigt), abseits von Kern 0 und 1, auf denen Windows die meiste Systemarbeit erledigt. Wirkt nach einem Neustart.

## Warum es helfen kann
Hält man Interrupts von den Kernen fern, auf denen der Hauptthread des Spiels läuft, können in manchen Systemen Verzögerungsspitzen sinken.

## Belege
Die Ergebnisse unterscheiden sich je nach System und Spiel; einen einheitlich gemessenen Gewinn gibt es nicht. Nutze den Benchmark auf der Seite Zustand vorher und nachher.

## Nachteile & Risiken
Ist der gewählte Kern ausgelastet, warten Interrupts länger, was es schlechter machen kann. Eine falsche Einstellung für ein wichtiges Gerät kann die Stabilität beeinträchtigen, daher ist das eine startkritische Expertenänderung.

## Wann du es nicht nutzen solltest
Ohne Messung vorher und nachher oder auf Prozessoren mit wenigen Kernen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/interrupt-affinity-and-priority
