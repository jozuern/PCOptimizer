# Taskleiste: Schaltflächen nie gruppieren

## Zusammenfassung
Zeigt jedes Fenster als eigene Taskleistenschaltfläche mit Beschriftung, auf der Haupttaskleiste und auf weiteren Bildschirmen.

## So funktioniert es
"Taskleistenschaltflächen gruppieren" bietet immer (Standard), wenn die Taskleiste voll ist, und nie, mit eigener Auswahl für weitere Bildschirme [1]. Die App setzt TaskbarGlomLevel und MMTaskbarGlomLevel auf 2 (nie). Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Du wechselst mit einem Klick ins richtige Fenster, statt es aus einer Gruppe zu wählen.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Mit vielen Fenstern werden die Schaltflächen klein und die Taskleiste scrollt [1].

## Wann du es nicht nutzen solltest
Wenn du oft viele Fenster offen hast.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows
