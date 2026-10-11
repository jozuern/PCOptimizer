# Datei-Explorer: versteckte Dateien anzeigen

## Zusammenfassung
Zeigt versteckte Dateien und Ordner wie AppData, wie Ansicht > Anzeigen > Ausgeblendete Elemente im Explorer. Geschützte Systemdateien bleiben verborgen.

## So funktioniert es
Der Explorer zeigt ausgeblendete Elemente, wenn du Ansicht > Anzeigen > Ausgeblendete Elemente wählst [1]. Die App setzt Hidden auf 1. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Spielstände, Mods und Konfigurationsdateien liegen oft in versteckten Ordnern wie AppData.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Mehr Einträge in Ordnern und auf dem Desktop, die man aus Versehen löschen kann.

## Wann du es nicht nutzen solltest
Wenn andere diesen PC nutzen und versteckte Ordner nicht sehen sollen.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
