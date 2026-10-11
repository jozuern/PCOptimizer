# Variable Refresh-Rate für ältere Vollbildspiele

## Zusammenfassung
Lässt einen G-SYNC-, FreeSync- oder Adaptive-Sync-Monitor in DirectX-11-Vollbildspielen ohne eigene Unterstützung der Framerate folgen. Standardmäßig aus.

## So funktioniert es
Variable Refresh-Rate (VRR) lässt einen passenden Monitor, etwa mit AMD FreeSync, NVIDIA G-SYNC oder VESA DisplayPort Adaptive-Sync, seine Refresh-Rate an die Framerate anpassen [1]. Die Windows-Einstellung „Variable Aktualisierungsrate“ schaltet das für DirectX-11-Vollbildspiele ein, die VRR nicht selbst unterstützen [1]. Sie ist standardmäßig aus und erscheint nur mit passenden Treibern und einem VRR-fähigen Monitor [2]. Die App fügt VRROptimizeEnable=1 zu DirectXUserGlobalSettings hinzu und lässt die anderen Einträge in diesem Wert stehen. Microsoft dokumentiert den Registry-Wert hinter dem Schalter nicht; die Anleitung [2] zeigt den Wert, den der Schalter schreibt. Solange kein Test auf einer echten Windows-Installation die Wirkung bestätigt, bleibt die Option eine Vorschau.

## Warum es helfen kann
Fällt die Framerate unter die Refresh-Rate, wartet der Monitor auf das nächste Bild, statt ein zerrissenes oder wiederholtes zu zeigen.

## Belege
Microsoft beschreibt, was die Einstellung macht [1]; ein messbarer Gewinn hängt von Spiel und Monitor ab, daher ist die Wirkung situationsabhängig. Der VRR-Modus des Monitors und des Grafiktreibers muss ebenfalls an sein.

## Nachteile & Risiken
Ohne VRR-fähigen Monitor und Treiber ändert die Einstellung nichts [2]. Spiele, die VRR selbst unterstützen, betrifft sie nicht [1].

## Wann du es nicht nutzen solltest
Wenn dein Monitor kein VRR kann oder du nur Spiele spielst, die VRR selbst unterstützen.

## Quellen
1. https://devblogs.microsoft.com/directx/navigating-the-redesigned-graphics-settings-page/
2. https://www.elevenforum.com/t/enable-or-disable-variable-refresh-rate-for-games-in-windows-11.12052/
