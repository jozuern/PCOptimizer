# Taskleistenuhr mit Sekunden

## Zusammenfassung
Zeigt Sekunden in der Uhr der Taskleiste. Laut Microsoft braucht das mehr Strom.

## So funktioniert es
Die Einstellungen bieten „Sekunden in der Uhr im Infobereich anzeigen“ und weisen darauf hin, dass es mehr Strom braucht [1]. Die App setzt ShowSecondsInSystemClock auf 1. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Praktisch zum Abpassen, etwa wenn ein Match oder ein Verkauf beginnt.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Etwas mehr Stromverbrauch, was im Akkubetrieb zählt [1].

## Wann du es nicht nutzen solltest
Auf einem Notebook, bei dem die Akkulaufzeit am wichtigsten ist.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
