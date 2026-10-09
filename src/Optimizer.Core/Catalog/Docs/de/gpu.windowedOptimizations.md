# Optimierungen für Spiele im Fenstermodus

## Zusammenfassung
Lässt ältere DirectX-10/11-Spiele im Fenster- oder randlosen Modus das schnellere Flip-Modell nutzen. Senkt die Latenz in randlosen Spielen.

## So funktioniert es
Ältere Spiele geben Bilder mit dem „Blt“-Modell aus, bei dem der Desktop-Compositor jedes Bild kopiert. Mit dieser Einstellung stellt Windows sie auf das Flip-Modell um, das Bilder ohne die zusätzliche Kopie an den Bildschirm gibt [1]. Die App setzt SwapEffectUpgradeEnable=1 in deinen DirectX-Einstellungen und behält deine übrigen Werte.

## Warum es helfen kann
Randlose Spiele erreichen fast die Latenz des exklusiven Vollbilds, und Funktionen wie Auto HDR und variable Bildwiederholrate funktionieren im Fenster.

## Belege
Microsoft dokumentiert den Wechsel des Ausgabemodells. In randlosen DX10/11-Spielen ist der Latenzgewinn messbar, in Spielen, die schon Flip nutzen, gleich null [2].

## Nachteile & Risiken
Selten Probleme mit sehr alten Spielen oder Aufnahmetools. Einzelne Spiele lassen sich unter Einstellungen > Bildschirm > Grafik ausnehmen.

## Wann du es nicht nutzen solltest
Ist Auto HDR an, erzwingt Windows diese Einstellung ohnehin. Dann gibt es nichts zu ändern.

## Quellen
1. https://devblogs.microsoft.com/directx/optimizations-for-windowed-games-in-windows-11/
2. https://support.microsoft.com/en-us/topic/3f006843-2c7e-4ed0-9a5e-f9389e535952
