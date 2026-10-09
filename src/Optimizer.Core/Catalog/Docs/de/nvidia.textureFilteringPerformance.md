# NVIDIA-Texturfilterung: hohe Leistung

## Zusammenfassung
Stellt die Qualität der Texturfilterung auf „Hohe Leistung“. Der Gewinn an Bildrate liegt meist unter der Messgenauigkeit. Umstritten.

## So funktioniert es
Die Einstellung „Texturfilterung - Qualität“ steuert Optimierungen des Treibers beim Abtasten von Texturen. „Hohe Leistung“ erlaubt die stärksten Optimierungen [1]. Sie wird im globalen Profil gespeichert.

## Warum es helfen kann
Auf älteren oder sehr schwachen Grafikkarten kann eine günstigere Texturfilterung etwas GPU-Zeit sparen.

## Belege
Auf aktuellen Grafikkarten kostet Texturfilterung sehr wenig, Tests zeigen selten einen messbaren Unterschied. Deshalb ist die Wirkung mit 0 bewertet.

## Nachteile & Risiken
Texturen können flimmern oder in der Entfernung unschärfer wirken.

## Wann du es nicht nutzen solltest
Auf jeder aktuellen Grafikkarte, bei der dir Bildqualität wichtiger ist als ein nicht messbarer Gewinn.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
