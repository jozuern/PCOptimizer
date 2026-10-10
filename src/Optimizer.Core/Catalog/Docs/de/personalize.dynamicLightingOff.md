# Dynamische Beleuchtung aus

## Zusammenfassung
Schaltet die dynamische Beleuchtung von Windows aus, damit die RGB-Software von Tastatur, Maus oder PC-Hersteller die Beleuchtung allein steuert.

## So funktioniert es
"Dynamische Beleuchtung auf meinen Geräten verwenden" schaltet die Funktion ein oder aus; ist sie aus, verhalten sich Geräte wie ohne dynamische Beleuchtung [1]. Die App setzt AmbientLightingEnabled auf 0. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Windows und die RGB-Software des Herstellers streiten nicht mehr um die Beleuchtung.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Effekte, die du unter Einstellungen > Personalisierung > Dynamische Beleuchtung eingestellt hast, enden.

## Wann du es nicht nutzen solltest
Wenn du die Beleuchtung über Windows steuerst.

## Quellen
1. https://support.microsoft.com/en-us/windows/hardware/input-devices/control-dynamic-lighting-devices-in-windows
