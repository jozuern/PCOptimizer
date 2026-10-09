# Vordergrund-Prioritätsboost (Win32PrioritySeparation)

## Zusammenfassung
Stellt kurze, variable Zeitscheiben mit dem stärksten Boost für das Vordergrundfenster ein (Wert 0x26). Ein klassischer Tweak, Wirkung umstritten.

## So funktioniert es
Win32PrioritySeparation legt die Länge der CPU-Zeitscheiben fest und wie viel länger die Zeitscheiben der Vordergrundanwendung sind als die im Hintergrund [1]. Der Windows-Standard (2) bevorzugt bereits die Vordergrund-App. 0x26 nutzt kurze, variable Zeitscheiben mit einem Vordergrund-Boost von 3:1.

## Warum es helfen kann
Theoretisch bekommt das Spiel im Vordergrund die CPU schneller zurück, wenn andere Prozesse um sie konkurrieren.

## Belege
Messungen auf aktuellen Mehrkern-CPUs zeigen keinen einheitlichen Unterschied, weil Spiele selten um einen einzelnen Kern konkurrieren.

## Nachteile & Risiken
Hintergrundaufgaben (Downloads, Kodierung) bekommen weniger CPU-Zeit, solange du ein anderes Fenster nutzt.

## Wann du es nicht nutzen solltest
Auf CPUs mit vielen Kernen nicht nötig. Ausprobieren ist harmlos und leicht rückgängig zu machen.

## Quellen
1. https://learn.microsoft.com/en-us/previous-versions/cc976120(v=technet.10)
