# Spielmodus an

## Zusammenfassung
Stellt sicher, dass der Windows-Spielmodus an ist. Er gibt dem laufenden Spiel Vorrang und blockiert während des Spielens Treiberinstallationen und Neustart-Hinweise von Windows Update.

## So funktioniert es
Erkennt der Spielmodus ein Spiel, priorisiert Windows dessen Threads, begrenzt Hintergrundarbeit und hält Treiberinstallationen und Neustart-Benachrichtigungen von Windows Update zurück [1]. Standardmäßig ist er an. Die App schaltet ihn wieder ein, falls er abgeschaltet wurde.

## Warum es helfen kann
Weniger Unterbrechungen durch Hintergrundaufgaben bedeuten weniger Ruckler, besonders auf CPUs mit wenigen Kernen.

## Belege
Die durchschnittlichen FPS ändern sich kaum. Der Nutzen zeigt sich in weniger Unterbrechungen, nicht in mehr FPS.

## Nachteile & Risiken
Selten verhält sich ein Spiel mit Spielmodus schlechter. Dann kannst du ihn wieder abschalten.

## Wann du es nicht nutzen solltest
Anlassen. Auf Ryzen-X3D-Prozessoren mit zwei Chiplets gehört der Spielmodus dazu, wie der AMD-Treiber Spiele erkennt.

## Quellen
1. https://www.elevenforum.com/t/turn-on-or-off-game-mode-in-windows-11.1447/
