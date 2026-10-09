# Globale Timer-Auflösung

## Zusammenfassung
Experte: lässt Timer-Auflösungs-Anfragen eines Programms wieder systemweit wirken, wie vor Windows 10 2004. Wirkung umstritten.

## So funktioniert es
Programme können mit timeBeginPeriod einen feineren Systemtimer anfordern. Seit Windows 10 Version 2004 wirkt diese Anfrage nur noch für den anfragenden Prozess [1]. Der Wert GlobalTimerResolutionRequests = 1 stellt das alte, systemweite Verhalten wieder her.

## Warum es helfen kann
Tools, die für FPS-Limits oder Eingabeabfragen einen 0,5-ms-Timer setzen, wirken wieder auf alle Prozesse, wie manche älteren Anleitungen voraussetzen.

## Belege
Die meisten Spiele setzen ihre Timer-Auflösung selbst und brauchen das nicht. Messbare Unterschiede sind selten.

## Nachteile & Risiken
Höherer Leerlaufverbrauch, vor allem auf Laptops, sobald ein Programm einen feinen Timer anfordert.

## Wann du es nicht nutzen solltest
Nur sinnvoll, wenn du auf ein externes Timer-Tool angewiesen bist. Nicht auf Laptops.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/timeapi/nf-timeapi-timebeginperiod
