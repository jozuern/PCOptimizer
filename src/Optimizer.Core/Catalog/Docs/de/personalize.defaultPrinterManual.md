# Standarddrucker bleibt, wie du ihn festlegst

## Zusammenfassung
Windows ändert deinen Standarddrucker nicht mehr auf den zuletzt verwendeten.

## So funktioniert es
Die Richtlinie "Windows-Standarddruckerverwaltung deaktivieren" (LegacyDefaultPrinterMode = 1) bewirkt, dass Windows den Standarddrucker nicht verwaltet [1].

## Warum es helfen kann
Druckaufträge gehen an den Drucker, den du als Standard gewählt hast, nicht an den PDF-Drucker, den du einmal benutzt hast.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Wenn du zwischen Orten mit verschiedenen Druckern wechselst, änderst du den Standard selbst.

## Wann du es nicht nutzen solltest
Wenn Windows den Standarddrucker für dich wechseln soll.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-printing
