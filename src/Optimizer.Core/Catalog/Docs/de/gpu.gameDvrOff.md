# Game-DVR-Hintergrundaufnahme aus

## Zusammenfassung
Schaltet die Aufnahmefunktionen der Xbox Game Bar ab, auch „Aufzeichnen, was passiert ist“, das ständig die letzten Spielminuten im Hintergrund aufnimmt.

## So funktioniert es
Ist die Hintergrundaufnahme an, kodiert Windows die letzten Spielminuten laufend mit dem Video-Encoder der GPU in einen Puffer. Die App schaltet Game DVR, App-Aufnahme und Hintergrundaufnahme ab [1]. Die Game Bar selbst bleibt installiert.

## Warum es helfen kann
Die Daueraufnahme belegt Encoder-Zeit der GPU, Arbeitsspeicher und Schreibzugriffe. Ohne sie fällt diese Last beim Spielen weg.

## Belege
Standardmäßig ist die Hintergrundaufnahme aus. Dann ändert dieser Tweak nichts Messbares. Ist sie an, sind die Kosten auf aktuellen GPUs klein, auf älteren aber messbar.

## Nachteile & Risiken
Du verlierst Game-Bar-Clips und die Tastenkombination „Das aufzeichnen“. Aufnahmen mit NVIDIA ShadowPlay oder OBS sind nicht betroffen.

## Wann du es nicht nutzen solltest
Behalte die Funktion, wenn du Game-Bar-Clips nutzt.

## Quellen
1. https://github.com/ChrisTitusTech/winutil
