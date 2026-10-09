# NVIDIA-Texturfilterung: hohe Leistung

## Zusammenfassung
Stellt die Qualität der Texturfilterung auf „Hohe Leistung“. NVIDIA nennt keine Zahlen zum Gewinn an Bildrate, eine Messung haben wir nicht gefunden. Umstritten.

## So funktioniert es
Die Einstellung „Texturfilterung - Qualität“ steuert Optimierungen des Treibers beim Abtasten von Texturen; „Qualität“ ist der Standard bei GeForce-Produkten [1]. „Hohe Leistung“ erlaubt die stärksten Optimierungen [1]. Die App schreibt den Wert aus NVIDIAs Header-Datei der Treibereinstellungen ins globale Profil [2].

## Warum es helfen kann
Auf älteren oder sehr schwachen Grafikkarten kann eine günstigere Texturfilterung etwas GPU-Zeit sparen.

## Belege
NVIDIA beschreibt „Hohe Leistung“ als Option mit der höchsten Bildrate, nennt aber keine Zahlen [1]. Eine veröffentlichte Messung des Gewinns auf aktuellen Grafikkarten haben wir nicht gefunden. Deshalb ist die Wirkung mit 0 bewertet.

## Nachteile & Risiken
Texturen können schlechter aussehen, etwa in der Entfernung unschärfer wirken.

## Wann du es nicht nutzen solltest
Auf jeder aktuellen Grafikkarte, bei der dir Bildqualität wichtiger ist als ein nicht gemessener Gewinn.

## Quellen
1. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
2. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
