# Taskleiste: keine Desktop-Ecke

## Zusammenfassung
Ein Klick in die äußerste rechte Ecke der Taskleiste minimiert nicht mehr alle Fenster.

## So funktioniert es
Zum Verhalten der Taskleiste gehört „Ecke der Taskleiste auswählen, um den Desktop anzuzeigen“ [1]. Die App setzt TaskbarSd auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Ein Fehlklick in die Ecke minimiert nicht mehr dein Spiel oder deine Arbeit.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Den Desktop zeigst du stattdessen mit Windows + D.

## Wann du es nicht nutzen solltest
Wenn du über die Ecke zum Desktop wechselst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
