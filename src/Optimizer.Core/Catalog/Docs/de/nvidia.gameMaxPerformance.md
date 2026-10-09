# NVIDIA: maximale Leistung für dieses Spiel bevorzugen

## Zusammenfassung
Setzt „Maximale Leistung bevorzugen“ nur im NVIDIA-Profil dieses Spiels. Hält den Takt hoch, solange das Spiel läuft, alles andere bleibt, wie es ist.

## So funktioniert es
NVIDIA speichert Einstellungen pro Spiel in Treiberprofilen [1]. Die App schreibt den Energieverwaltungsmodus in das Profil, zu dem dieses Spiel gehört. „Maximale Leistung bevorzugen“ betreibt die GPU bei den meisten 3D-Anwendungen mit maximaler Leistung [2]. Hat der Treiber kein Profil für das Spiel, legt die App eines mit dem Namen „PCOptimizer: “ und dem Dateinamen des Spiels an. Rückgängig machen entfernt die Einstellung wieder.

## Warum es helfen kann
In leichten oder prozessorlimitierten Spielen kann die Grafikkarte zwischen den Bildern heruntertakten. Das kann ungleichmäßige Frametimes verursachen.

## Belege
Die Wirkung hängt von der Situation ab; in anspruchsvollen Spielen läuft die Karte ohnehin mit vollem Takt.

## Nachteile & Risiken
Mehr Stromverbrauch und Wärme, solange das Spiel läuft. Ein von der App angelegtes, leeres Profil bleibt nach dem Rückgängigmachen bestehen; es hat keine Wirkung.

## Wann du es nicht nutzen solltest
Auf Laptops im Akkubetrieb oder in Spielen, die die GPU ohnehin voll auslasten.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
2. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
