# Optimierungen für Spiele im Fenstermodus

## Zusammenfassung
Lässt ältere DirectX-10- und -11-Spiele im Fenster- oder randlosen Modus das Flip-Ausgabemodell nutzen, was die Latenz meist senkt.

## So funktioniert es
Ältere Spiele geben Bilder mit dem „Blt“-Modell aus, bei dem der Desktop-Compositor jedes Bild kopiert. Mit dieser Einstellung stellt Windows sie auf das Flip-Modell um, das Bilder ohne die zusätzliche Kopie an den Bildschirm gibt [1][2]. Die App setzt SwapEffectUpgradeEnable=1 in deinen DirectX-Einstellungen und behält deine übrigen Werte. Microsoft beschreibt den Schalter [1][2], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Bis ein Test unter echtem Windows die Wirkung bestätigt, ist die Option eine Vorschau.

## Warum es helfen kann
Spiele im Fenster- oder randlosen Modus sparen sich die zusätzliche Kopie, was die Bildlatenz senkt [2].

## Belege
Microsoft dokumentiert den Wechsel vom Blt- zum Flip-Modell und schreibt, dass das Flip-Modell in der Regel die Latenz senkt, nennt aber keine Zahlen [1][2]. Spiele, die schon Flip nutzen, darunter alle DirectX-12-Spiele, ändern sich nicht [1].

## Nachteile & Risiken
Laut Microsoft kann das Flip-Modell bei Bildraten über der Bildwiederholrate zu Tearing führen; ein Bildratenlimit, V-Sync oder ein Bildschirm mit variabler Bildwiederholrate verhindert das [1]. Macht ein einzelnes Spiel Probleme, kannst du es unter Einstellungen > System > Bildschirm > Grafik ausnehmen [2].

## Wann du es nicht nutzen solltest
Ist Auto HDR an, erzwingt Windows diese Einstellung ohnehin. Dann gibt es nichts zu ändern [2].

## Quellen
1. https://devblogs.microsoft.com/directx/updates-in-graphics-and-gaming/
2. https://support.microsoft.com/en-us/topic/3f006843-2c7e-4ed0-9a5e-f9389e535952
