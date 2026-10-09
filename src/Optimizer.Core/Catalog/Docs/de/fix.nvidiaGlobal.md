# Bremsende globale NVIDIA-Einstellungen zurücksetzen

## Zusammenfassung
Entfernt den eigenen Wert des globalen Profils für die NVIDIA-Einstellungen, die Spiele begrenzen (Bildratenlimit, Energiemodus), damit die Treiberstandards gelten.

## So funktioniert es
NVIDIA speichert Treibereinstellungen in Profilen: einem globalen und einem pro Spiel. Die App entfernt die genannten Einstellungen über NVIDIAs Schnittstelle für Treibereinstellungen aus dem globalen Profil [1]. Spielprofile bleiben, wie sie sind. Rückgängig machen schreibt die vorherigen Werte zurück.

## Warum es helfen kann
Ein globales Bildratenlimit weit unter der Bildwiederholrate oder ein auf Minimum gezwungener Energiemodus bremst jedes Spiel. Ohne sie läuft jedes Spiel so, wie der Treiber es vorsieht.

## Belege
Ein Bildratenlimit begrenzt die Bildrate absichtlich, und der minimale Energiemodus hält die Grafikkarte auf niedrigeren Takten. Entfernt man sie, gilt wieder das normale Verhalten. Die NVIDIA Systemsteuerung nennt das maximale Bildratenlimit standardmäßig aus [2], und die Header-Datei der Treibereinstellungen legt als Standard für den Energiemodus „Optimale Leistung“ fest [3].

## Nachteile & Risiken
Hast du das Limit bewusst gesetzt, etwa gegen Wärme oder Lautstärke, verlierst du das. Setze es dann pro Spiel.

## Wann du es nicht nutzen solltest
Wenn du bewusst ein globales Bildratenlimit nutzt.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
2. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
3. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
