# Interrupt-Affinität für dieses Gerät (Experte)

## Zusammenfassung
Leitet die Interrupts dieser Grafikkarte auf einen logischen Prozessor, statt der Standardrichtlinie von Windows zu folgen. Umstritten: Kann helfen oder schaden, miss es.

## So funktioniert es
Normalerweise entscheiden der Treiber und die Standardrichtlinie von Windows, welche Prozessoren die Interrupts eines Geräts bearbeiten. Die Interrupt-Affinitätsrichtlinie in der Registry kann das überschreiben [1]. Die App setzt die dokumentierte Richtlinie „angegebene Prozessoren“ und eine Maske mit einem logischen Prozessor (im Namen angezeigt), abseits der ersten Kerne. Wirkt nach einem Neustart. Die App bietet das nur für Grafikkarten an.

## Warum es helfen kann
Hält man Interrupts von den Kernen fern, auf denen der Hauptthread des Spiels läuft, können in manchen Systemen Verzögerungsspitzen sinken.

## Belege
Die Ergebnisse unterscheiden sich je nach System und Spiel. Einen einheitlich gemessenen Gewinn gibt es nicht. Nutze den Benchmark auf der Seite Zustand vorher und nachher.

## Nachteile & Risiken
Microsoft empfiehlt die Standardrichtlinie, wo sie passt [1]. Ist der gewählte Prozessor ausgelastet, warten Interrupts länger, was es schlechter machen kann. Eine falsche Einstellung für ein wichtiges Gerät kann die Stabilität beeinträchtigen, daher ist das eine startkritische Expertenänderung.

## Wann du es nicht nutzen solltest
Ohne Messung vorher und nachher oder auf Prozessoren mit wenigen Kernen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/interrupt-affinity-and-priority
