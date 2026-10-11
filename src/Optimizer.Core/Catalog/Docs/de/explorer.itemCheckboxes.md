# Datei-Explorer: Kontrollkästchen für Elemente

## Zusammenfassung
Zeigt neben Dateien und Ordnern ein Kontrollkästchen, um mehrere auszuwählen, wie Ansicht > Anzeigen > Elementkontrollkästchen.

## So funktioniert es
Elementkontrollkästchen zeigen ein Kästchen neben Dateien und Ordnern [1]. Die App setzt AutoCheckSelect auf 1. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Mehrere Dateien auswählen geht ohne gedrückte Strg-Taste.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Etwas mehr Unruhe neben den Dateinamen.

## Wann du es nicht nutzen solltest
Wenn du Dateien mit Strg und Umschalt auswählst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
