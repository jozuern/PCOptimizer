# NVIDIA-Energiemodus: maximale Leistung bevorzugen (global)

## Zusammenfassung
Hält die Grafikkarte auf hohen Takten, solange eine 3D-Anwendung läuft. Kann Frametimes in leichten Spielen glätten, erhöht den Stromverbrauch. Pro Spiel ist meist besser.

## So funktioniert es
Standardmäßig senkt der Treiber den Grafiktakt bei geringer Last. „Maximale Leistung bevorzugen“ im globalen Profil [1] weist ihn an, auf der höchsten Leistungsstufe zu bleiben, solange eine 3D-Anwendung läuft.

## Warum es helfen kann
In leichten oder älteren Spielen kann die Karte zwischen den Bildern heruntertakten und braucht Zeit zum Hochtakten. Das kann ungleichmäßige Frametimes verursachen.

## Belege
Die Wirkung hängt von der Situation ab; in anspruchsvollen Spielen läuft die Karte ohnehin mit vollem Takt, und es ändert sich nichts.

## Nachteile & Risiken
Mehr Stromverbrauch, Wärme und Lüftergeräusch, auch bei Browsern oder Launchern mit 3D-Beschleunigung. Mit mehreren Monitoren bleibt die Karte eventuell auch auf dem Desktop hoch getaktet.

## Wann du es nicht nutzen solltest
Nutze lieber die Variante pro Spiel auf der Grafikseite, die nur die Spiele betrifft, die du auswählst.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
