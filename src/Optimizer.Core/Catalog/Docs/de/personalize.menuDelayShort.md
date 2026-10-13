# Schnellere Untermenüs

## Zusammenfassung
Untermenüs öffnen sich nach 100 ms statt nach 400 ms, wenn die Maus auf einem Menüeintrag mit Pfeil ruht. Wirkt sofort, ohne Abmelden.

## So funktioniert es
Ruht der Mauszeiger auf einem Menüeintrag mit Untermenü, wartet Windows, bevor es das Untermenü öffnet. Die App stellt diese Wartezeit über die dokumentierte Funktion SystemParametersInfo (SPI_SETMENUSHOWDELAY) auf 100 ms und lässt Windows sie in deinem Profil speichern; die Änderung wirkt sofort und bleibt erhalten [1]. Rückgängig setzt den vorherigen Wert.

## Warum es helfen kann
Menüs mit Untermenüs, etwa das klassische Kontextmenü und Menüs älterer Programme, öffnen sich schneller und wirken direkter.

## Belege
Keine Auswirkung auf die Leistung: Nur die Wartezeit bis zum Untermenü ändert sich. Die Einstellung selbst ist dokumentiert [1].

## Nachteile & Risiken
Untermenüs öffnen sich auch, wenn der Zeiger nur darüberfährt; manche empfinden das als unruhig.

## Wann du es nicht nutzen solltest
Wenn sich Untermenüs für dich schon zu schnell öffnen.

## Quellen
1. https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-systemparametersinfow
