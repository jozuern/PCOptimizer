# Bremsende globale NVIDIA-Einstellungen zurücksetzen

## Zusammenfassung
Entfernt den eigenen Wert des globalen Profils für die NVIDIA-Einstellungen, die Spiele begrenzen (Bildratenlimit, Energiemodus), damit die Treiberstandards gelten.

## So funktioniert es
NVIDIA speichert Treibereinstellungen in Profilen: einem globalen und einem pro Spiel. Die App entfernt die genannten Einstellungen über NVIDIAs Schnittstelle für Treibereinstellungen aus dem globalen Profil [1]. Spielprofile bleiben, wie sie sind. Rückgängig machen schreibt die vorherigen Werte zurück.

## Warum es helfen kann
Ein globales Bildratenlimit weit unter der Bildwiederholrate oder ein auf Minimum gezwungener Energiemodus bremst jedes Spiel. Ohne sie läuft jedes Spiel so, wie der Treiber es vorsieht.

## Belege
Ein Bildratenlimit begrenzt die Bildrate absichtlich, und der minimale Energiemodus hält die Grafikkarte auf niedrigeren Takten. Entfernt man sie, gilt wieder das normale Verhalten.

## Nachteile & Risiken
Hast du das Limit bewusst gesetzt, etwa gegen Wärme oder Lautstärke, verlierst du das. Setze es dann pro Spiel.

## Wann du es nicht nutzen solltest
Wenn du bewusst ein globales Bildratenlimit nutzt.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
