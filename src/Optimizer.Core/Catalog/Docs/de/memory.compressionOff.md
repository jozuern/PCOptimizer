# Speicherkomprimierung aus

## Zusammenfassung
Verhindert, dass Windows Speicherseiten im Arbeitsspeicher komprimiert. Nur ab 16 GB angeboten. Wirkung umstritten.

## So funktioniert es
Wird der Speicher knapp, komprimiert Windows selten genutzte Seiten im RAM, statt sie in die Auslagerungsdatei zu schreiben. Die App schaltet das mit Disable-MMAgent -MemoryCompression ab [1]. Es wirkt nach einem Neustart.

## Warum es helfen kann
Komprimieren und Entpacken kosten CPU-Zeit. Mit viel RAM wird ohnehin wenig komprimiert, und diese CPU-Zeit fällt weg.

## Belege
Messungen zeigen auf PCs mit genug RAM keinen einheitlichen Unterschied in Spielen. Mit wenig RAM kann das Abschalten schaden, deshalb wird es unter 16 GB nicht angeboten.

## Nachteile & Risiken
Mehr Zugriffe auf die Auslagerungsdatei, wenn der Speicher voll wird.

## Wann du es nicht nutzen solltest
Nicht mit 8 GB oder weniger nutzen.

## Quellen
1. https://learn.microsoft.com/en-us/powershell/module/mmagent/disable-mmagent
