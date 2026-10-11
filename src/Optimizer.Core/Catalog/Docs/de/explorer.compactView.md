# Datei-Explorer: kompakte Ansicht

## Zusammenfassung
Verringert den Abstand zwischen Dateien im Explorer, wie Ansicht > Kompakte Ansicht.

## So funktioniert es
Die kompakte Ansicht verringert den Abstand zwischen Dateien [1]. Die App setzt UseCompactMode auf 1. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Mehr Dateien passen auf den Bildschirm, praktisch mit Maus und Tastatur.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Kleinere Ziele auf Touchscreens.

## Wann du es nicht nutzen solltest
Auf einem Touchscreen.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/fileexplorer/file-explorer-in-windows
