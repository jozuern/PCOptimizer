# Mausbeschleunigung aus

## Zusammenfassung
Schaltet „Zeigerbeschleunigung verbessern“ ab. Dieselbe Handbewegung bewegt den Zeiger dann immer gleich weit.

## So funktioniert es
Ist die Option an, beschleunigt Windows den Zeiger: Bewegst du die Maus schneller als zwei Schwellenwerte, vervielfacht Windows die Strecke [2]. Die App setzt MouseSpeed (die Beschleunigungsstufe), MouseThreshold1 und MouseThreshold2 in deinem Benutzerprofil auf 0 und übernimmt sie in die laufende Sitzung [1]. In Windows heißt die Option „Zeigerbeschleunigung verbessern“ und steht im Dialog Eigenschaften von Maus auf der Registerkarte Zeigeroptionen.

## Warum es helfen kann
Gleichbleibende Skalierung macht das Zielen in Spielen berechenbar, die den Windows-Zeiger nutzen (viele Strategiespiele, ältere Spiele, Menüs).

## Belege
Spiele, die Rohdaten der Maus lesen (WM_INPUT), bekommen die Bewegung ohne Zeigerbeschleunigung. Am Zielen ändert diese Einstellung dort nichts [3]. Sie wirkt in Spielen und Menüs, die den Windows-Zeiger nutzen.

## Nachteile & Risiken
Auf dem Desktop wirkt der Zeiger anfangs langsamer. Passe Zeigergeschwindigkeit oder Maus-DPI nach Geschmack an.

## Wann du es nicht nutzen solltest
Lass die Option an, wenn du auf dem Desktop beschleunigte Bewegung magst und nur Spiele mit Rohdaten spielst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
2. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mouse_event
3. https://learn.microsoft.com/en-us/windows/win32/dxtecharts/taking-advantage-of-high-dpi-mouse-movement
