# Spielmodus an

## Zusammenfassung
Stellt sicher, dass der Windows-Spielmodus an ist. Er gibt dem laufenden Spiel Vorrang und blockiert während des Spielens Treiberinstallationen und Neustart-Hinweise von Windows Update.

## So funktioniert es
Erkennt der Spielmodus ein Spiel, gibt Windows ihm bevorzugten Zugriff auf die Hardware [2] und hält Treiberinstallationen und Neustart-Benachrichtigungen von Windows Update zurück [1]. Seit dem Update vom Oktober 2018 ist er standardmäßig für alle Spiele an und hat einen Schalter in den Einstellungen [1]. Die App schaltet ihn wieder ein, falls er abgeschaltet wurde, mit den Registry-Werten hinter diesem Schalter; Microsoft dokumentiert die Werte selbst nicht. Solange der Spielmodus an ist und ein Spiel läuft, wechselt Windows außerdem in das Prozessor-Energieprofil „GameMode“, das PC-Hersteller anpassen können [3].

## Warum es helfen kann
Weniger Unterbrechungen durch Hintergrundarbeit und Update-Hinweise beim Spielen. Laut Microsoft kann das zu gleichmäßigeren FPS führen [1]. Bei Ryzen-9-X3D-Prozessoren mit zwei CCDs (7900X3D, 7950X3D, 9900X3D, 9950X3D) verlangt AMD den Spielmodus und die installierte Xbox Game Bar, weil der Treiber damit Spiele auf dem CCD mit dem zusätzlichen Cache hält [4].

## Belege
Microsoft schreibt, dass Spiele je nach Spiel und System besser laufen können, mit weniger schwankenden FPS, nennt aber keine Zahlen [1]. Für X3D-Prozessoren mit zwei CCDs zeigte TechSpot, was passiert, wenn diese Zuteilung scheitert: Ein 7950X3D ohne die nötigen Updates war in allen getesteten Spielen langsamer als ein 7950X, und in Factorio gewann er fast 80 %, sobald das Spiel auf dem Cache-CCD lief [5]. Das misst die ganze Kette (BIOS, Chipsatztreiber, Spielmodus, Game Bar), nicht den Spielmodus allein, deshalb ist die Bewertung auf diesen Prozessoren situationsabhängig.

## Nachteile & Risiken
Selten verhält sich ein Spiel mit Spielmodus schlechter. Dann kannst du ihn wieder abschalten.

## Wann du es nicht nutzen solltest
Anlassen. Schalte ihn nur für ein Spiel ab, das damit schlechter läuft.

## Quellen
1. https://news.xbox.com/en-us/2018/10/02/latest-october-2018-windows-update-gaming-features-3/
2. https://learn.microsoft.com/en-us/previous-versions/windows/desktop/gamemode/game-mode-portal
3. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
4. https://www.tomshardware.com/news/amd-ryzen-7000x3d-install-guide
5. https://www.techspot.com/review/2636-amd-ryzen-9-7950x3d/
