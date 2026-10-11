# Taskleiste: keine blinkenden Schaltflächen

## Zusammenfassung
Taskleistenschaltflächen blinken nicht mehr, wenn eine App Aufmerksamkeit will, wie der Schalter „Blinken bei Taskleisten-Apps anzeigen“.

## So funktioniert es
Apps lassen ihre Taskleistenschaltfläche blinken, wenn sie eine Eingabe brauchen, etwa wenn sie hinter einem anderen Fenster öffnen [1]. Die App setzt TaskbarFlashing auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Keine blinkende Taskleiste, während du im randlosen Fenstermodus spielst oder streamst.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Du merkst später, dass eine App auf dich wartet.

## Wann du es nicht nutzen solltest
Wenn du an blinkenden Schaltflächen neue Chatnachrichten erkennst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
