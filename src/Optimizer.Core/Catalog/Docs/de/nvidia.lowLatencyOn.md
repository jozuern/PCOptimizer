# NVIDIA-Modus für niedrige Latenz: Ein

## Zusammenfassung
Begrenzt die Bilder, die der Prozessor der Grafikkarte vorausberechnet, auf eines. Senkt die Eingabeverzögerung in GPU-limitierten DirectX-11-Spielen.

## So funktioniert es
Normalerweise lässt der Treiber den Prozessor einige Bilder vorausplanen, damit die Grafikkarte nie wartet. „Modus für niedrige Latenz: Ein“ in der NVIDIA Systemsteuerung speichert im globalen Profil ein Limit von einem vorausberechneten Bild (früher „Maximale Anzahl vorgerenderter Bilder“) [1]. DirectX-12- und Vulkan-Spiele steuern ihre Warteschlange selbst und sind nicht betroffen.

## Warum es helfen kann
Ist die Grafikkarte der Engpass, verzögert jedes Bild in der Warteschlange den Weg von deiner Eingabe zum Bild um ein Bild. Mit einem statt mehreren Bildern sinkt die Verzögerung um die Dauer der weggefallenen Bilder.

## Belege
Der Effekt folgt aus der Länge der Warteschlange und ist nur spürbar, wenn die Grafikkarte limitiert. Spiele mit NVIDIA Reflex nutzen ein eigenes, stärkeres Verfahren und übergehen diese Einstellung.

## Nachteile & Risiken
In prozessorlimitierten Spielen kann eine kürzere Warteschlange die Bildrate leicht senken oder die Frametimes ungleichmäßiger machen. Die Stufe „Ultra“ nutzt eine undokumentierte Einstellung und wird hier nicht angeboten.

## Wann du es nicht nutzen solltest
In Spielen mit NVIDIA Reflex (schalte dort Reflex ein) oder wenn die Frametimes ungleichmäßig werden.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
