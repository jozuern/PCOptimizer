# Hardwarebeschleunigte GPU-Planung (HAGS)

## Zusammenfassung
Lässt die Grafikkarte ihre Arbeit selbst planen statt eines CPU-Threads. Nötig für DLSS Frame Generation; ohne Frame Generation ist kein messbarer Gewinn belegt.

## So funktioniert es
Seit WDDM 2.7 kann Windows den Großteil der GPU-Planung an einen eigenen Prozessor auf der Grafikkarte abgeben, statt sie in einem hoch priorisierten CPU-Thread zu erledigen [1]. Das nutzen nur Grafikkarten und Treiber, die es unterstützen. Der Wert HwSchMode = 2 schaltet es ein und wirkt nach einem Neustart. Es ist der Wert hinter dem Schalter „Hardwarebeschleunigte GPU-Planung“ unter Einstellungen > System > Bildschirm > Grafik; Microsoft dokumentiert den Wert selbst nicht.

## Warum es helfen kann
Auf NVIDIA-RTX-40- und -50-Karten funktioniert DLSS Frame Generation nur, wenn diese Einstellung an ist [2]. Ohne Frame Generation beschreibt Microsoft die Änderung als etwas, das du nicht bemerken solltest [1].

## Belege
Microsoft hat die Einstellung 2020 als freiwillige Option eingeführt und erwartet keine spürbare Änderung [1]. NVIDIAs Anleitung zur Einbindung von Frame Generation nennt sie als Voraussetzung: Ist sie aus, steht Frame Generation nicht zur Verfügung [2]. Eine Messung eines Gewinns beim normalen Rendern haben wir von keinem Hersteller gefunden.

## Nachteile & Risiken
Treten nach der Änderung Ruckler, schwarze Bildschirme oder Probleme mit Aufnahme- oder Overlay-Software auf, schalte es wieder ab (auf Karten mit Frame Generation fällt dann Frame Generation weg).

## Wann du es nicht nutzen solltest
Auf Karten mit Frame Generation sollte es an bleiben. Auf anderen Karten ist kein Gewinn belegt. Intel-Arc-Karten der A-Serie unterstützen es nicht [3].

## Quellen
1. https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/
2. https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuideDLSS_G.md
3. https://www.intel.com/content/www/us/en/support/articles/000056788/graphics.html
