# Andocken: keine Layouts an der Maximieren-Schaltfläche und am oberen Rand

## Zusammenfassung
Beim Zeigen auf die Maximieren-Schaltfläche oder beim Ziehen eines Fensters an den oberen Rand erscheinen keine Andocklayouts mehr.

## So funktioniert es
Die Einstellungen haben eigene Schalter für Andocklayouts an der Maximieren-Schaltfläche (Snap-Flyout) und am oberen Bildschirmrand (Snap-Leiste) [1]. Die App setzt EnableSnapAssistFlyout und EnableSnapBar auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Kein Layoutfeld erscheint, während du Fenster verschiebst oder maximierst.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Layouts bleiben mit Windows + Z erreichbar.

## Wann du es nicht nutzen solltest
Wenn du Fenster mit den Layouts anordnest.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/snap-your-windows
