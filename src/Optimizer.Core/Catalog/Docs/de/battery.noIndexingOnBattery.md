# Keine Suchindizierung im Akkubetrieb

## Zusammenfassung
Die Indizierung der Windows-Suche pausiert, solange das Notebook im Akkubetrieb läuft, und macht am Netzteil weiter.

## So funktioniert es
Die Richtlinie "Indizierung im Akkubetrieb verhindern, um Energie zu sparen" pausiert die Indizierung, solange der Computer im Akkubetrieb läuft; ohne sie folgt die Indizierung dem Standardverhalten [1]. Laut Microsoft fährt die Indizierung schon seit Windows Vista bei niedrigem Energiestand zurück [1].

## Warum es helfen kann
Das Indizieren neuer Dateien liest den Datenträger und nutzt den Prozessor; im Akkubetrieb wartet diese Arbeit auf das Netzteil.

## Belege
Eine von Microsoft beschriebene Richtlinie der Windows-Suche [1]. Wie viel Akku es spart, hängt davon ab, wie viele Dateien sich ändern; Microsoft nennt keine Zahl.

## Nachteile & Risiken
Dateien, die im Akkubetrieb hinzukommen, findet die Suche erst, wenn das Notebook am Netz hängt.

## Wann du es nicht nutzen solltest
Wenn neue Dateien im Akkubetrieb sofort in der Suche auftauchen sollen.

## Quellen
1. https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-server-2008-R2-and-2008/cc732491(v=ws.10)
