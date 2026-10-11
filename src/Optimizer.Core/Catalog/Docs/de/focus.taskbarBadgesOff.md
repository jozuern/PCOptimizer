# Taskleiste: keine Badges auf Apps

## Zusammenfassung
Blendet die kleinen Zähler und Status-Badges auf Taskleistensymbolen aus, wie der Schalter „Badges auf Taskleisten-Apps anzeigen“.

## So funktioniert es
Zum Verhalten der Taskleiste gehört das Anzeigen von Badges auf Schaltflächen [1]. Die App setzt TaskbarBadges auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Weniger Ablenkung durch Zähler für Ungelesenes.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Ungelesenes siehst du nicht mehr auf einen Blick.

## Wann du es nicht nutzen solltest
Wenn du über die Zähler Nachrichten im Blick behältst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
