# Xbox-Taste öffnet die Game Bar nicht

## Zusammenfassung
Die Xbox-Taste eines Controllers öffnet die Game Bar nicht mehr, ein Druck im Spiel blendet also kein Overlay ein. Nicht dokumentierter Wert, getestet.

## So funktioniert es
Die Game Bar hat eine Einstellung, mit der die Xbox-Taste eines Controllers sie öffnet. Die App setzt UseNexusForGameBarEnabled = 0, den Wert, den Win11Debloat für diese Einstellung schreibt [1]. Es ist der Wert hinter dem Schalter für den Controller unter Einstellungen > Spielen > Game Bar. Microsoft dokumentiert den Wert nicht.

## Warum es helfen kann
Ein versehentlicher Druck auf die Xbox-Taste unterbricht kein Spiel mehr, und die Taste bleibt frei für Launcher wie Steam Big Picture.

## Belege
In einem VM-Test mit Windows 11 Build 26300 zeigten die Einstellungen diesen Schalter nach dem Anwenden aus und nach dem Rückgängigmachen an. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Windows + G öffnet die Game Bar weiterhin.

## Wann du es nicht nutzen solltest
Wenn du die Game Bar mit dem Controller öffnest.

## Quellen
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Disable_Game_Bar_Integration.reg
