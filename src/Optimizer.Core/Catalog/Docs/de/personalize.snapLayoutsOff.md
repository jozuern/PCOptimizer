# Andocken: keine Layouts an der Maximieren-Schaltfläche und am oberen Rand

## Zusammenfassung
Beim Zeigen auf die Maximieren-Schaltfläche oder beim Ziehen eines Fensters an den oberen Rand erscheinen keine Andocklayouts mehr.

## So funktioniert es
Die Einstellungen haben eigene Schalter für Andocklayouts an der Maximieren-Schaltfläche (Snap-Flyout) und am oberen Bildschirmrand (Snap-Leiste) [1]. Die App setzt EnableSnapAssistFlyout und EnableSnapBar auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Kein Layoutfeld erscheint, während du Fenster verschiebst oder maximierst.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Layouts bleiben mit Windows + Z erreichbar.

## Wann du es nicht nutzen solltest
Wenn du Fenster mit den Layouts anordnest.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/snap-your-windows
