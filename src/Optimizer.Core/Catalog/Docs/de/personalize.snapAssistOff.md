# Andocken: keine Vorschläge für die andere Hälfte

## Zusammenfassung
Nachdem du ein Fenster an eine Seite andockst, bietet Windows keine anderen Fenster für den Rest mehr an. Das Andocken selbst funktioniert weiter.

## So funktioniert es
„Beim Andocken eines Fensters anzeigen, was daneben angedockt werden kann“ schaltet die Andockhilfe ein oder aus [1]. Die App setzt SnapAssist auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Keine Vorschaubilder, die du nach dem Andocken wegklicken musst.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Das zweite Fenster platzierst du selbst.

## Wann du es nicht nutzen solltest
Wenn du mit der Andockhilfe den Bildschirm teilst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/snap-your-windows
