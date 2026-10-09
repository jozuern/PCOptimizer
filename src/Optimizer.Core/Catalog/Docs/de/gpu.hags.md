# Hardwarebeschleunigte GPU-Planung (HAGS)

## Zusammenfassung
Lässt die Grafikkarte ihre Arbeit selbst planen, statt dass Windows das auf der CPU erledigt. Nötig für DLSS Frame Generation.

## So funktioniert es
Seit WDDM 2.7 kann Windows den Großteil der GPU-Planung an einen eigenen Prozessor auf der Grafikkarte abgeben, statt sie in einem hoch priorisierten CPU-Thread zu erledigen [1]. Das nutzen nur Grafikkarten und Treiber, die es unterstützen. Der Wert HwSchMode = 2 schaltet es ein und wirkt nach einem Neustart.

## Warum es helfen kann
Die CPU wird etwas entlastet, und die Latenz kann leicht sinken. Auf NVIDIA-RTX-40- und -50-Karten ist HAGS Voraussetzung für DLSS Frame Generation.

## Belege
Beim normalen Rendern zeigen unabhängige Tests meist Unterschiede innerhalb der Messschwankung. Die Voraussetzung für Frame Generation ist von NVIDIA und Microsoft dokumentiert.

## Nachteile & Risiken
Selten Probleme mit älterer Aufnahme- oder Overlay-Software. Treten Ruckler oder schwarze Bildschirme auf, schalte es wieder ab (auf Karten mit Frame Generation fällt dann Frame Generation weg).

## Wann du es nicht nutzen solltest
Auf Karten mit Frame Generation sollte es an bleiben. Auf älteren Karten nur nutzen, wenn keine Probleme auftreten.

## Quellen
1. https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/
