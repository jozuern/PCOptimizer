# Kürzere Verweilzeit

## Zusammenfassung
Programme, die auf einen ruhenden Mauszeiger warten (Tooltips, Vorschauen), reagieren nach 200 ms statt nach 400 ms. Wirkt sofort, ohne Abmelden.

## So funktioniert es
Programme können sich von Windows melden lassen, wenn der Zeiger für die Verweilzeit des Systems an einer Stelle ruht (TrackMouseEvent mit HOVER_DEFAULT) [1]. Die App stellt diese Zeit über die dokumentierte Funktion SystemParametersInfo (SPI_SETMOUSEHOVERTIME) auf 200 ms und lässt Windows sie in deinem Profil speichern [1]. Rückgängig setzt den vorherigen Wert.

## Warum es helfen kann
In Programmen, die die Verweilzeit des Systems nutzen, erscheinen Tooltips und Hover-Effekte früher.

## Belege
Keine Auswirkung auf die Leistung, und nur Programme, die die Verweilzeit des Systems nutzen, reagieren darauf [1]; viele zeichnen ihre Tooltips selbst mit eigener Zeit.

## Nachteile & Risiken
Tooltips und Hover-Effekte können aufspringen, während du den Zeiger nur über ein Fenster bewegst.

## Wann du es nicht nutzen solltest
Wenn dich Tooltips schon jetzt stören.

## Quellen
1. https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-systemparametersinfow
