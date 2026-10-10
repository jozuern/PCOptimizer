# Variable Bildwiederholrate für ältere Vollbildspiele

## Zusammenfassung
Lässt einen G-SYNC-, FreeSync- oder Adaptive-Sync-Monitor in DirectX-11-Vollbildspielen ohne eigene Unterstützung der Bildrate folgen. Standardmäßig aus.

## So funktioniert es
Variable Bildwiederholrate (VRR) lässt einen passenden Monitor, etwa mit AMD FreeSync, NVIDIA G-SYNC oder VESA DisplayPort Adaptive-Sync, seine Bildwiederholrate an die Bildrate anpassen [1]. Die Windows-Einstellung "Variable Aktualisierungsrate" schaltet das für DirectX-11-Vollbildspiele ein, die VRR nicht selbst unterstützen [1]. Sie ist standardmäßig aus und erscheint nur mit passenden Treibern und einem VRR-fähigen Monitor [2]. Die App fügt VRROptimizeEnable=1 zu DirectXUserGlobalSettings hinzu und lässt die anderen Einträge in diesem Wert stehen. Microsoft dokumentiert den Registry-Wert hinter dem Schalter nicht; die Anleitung [2] zeigt den Wert, den der Schalter schreibt. Bis ein Test unter echtem Windows die Wirkung bestätigt, ist die Option eine Vorschau.

## Warum es helfen kann
Fällt die Bildrate unter die Bildwiederholrate, wartet der Monitor auf das nächste Bild, statt ein zerrissenes oder wiederholtes zu zeigen.

## Belege
Microsoft beschreibt, was die Einstellung macht [1]; ein messbarer Gewinn hängt von Spiel und Monitor ab, daher ist die Wirkung situationsabhängig. Der VRR-Modus des Monitors und des Grafiktreibers muss ebenfalls an sein.

## Nachteile & Risiken
Ohne VRR-fähigen Monitor und Treiber ändert die Einstellung nichts [2]. Spiele, die VRR selbst unterstützen, betrifft sie nicht [1].

## Wann du es nicht nutzen solltest
Wenn dein Monitor kein VRR kann oder du nur Spiele spielst, die VRR selbst unterstützen.

## Quellen
1. https://devblogs.microsoft.com/directx/navigating-the-redesigned-graphics-settings-page/
2. https://www.elevenforum.com/t/enable-or-disable-variable-refresh-rate-for-games-in-windows-11.12052/
