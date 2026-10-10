# Keine Schaltfläche zum Anzeigen von Kennwörtern

## Zusammenfassung
Entfernt die Augen-Schaltfläche, die ein getipptes Kennwort in Windows-Kennwortfeldern anzeigt. Pro, Enterprise und Education.

## So funktioniert es
Mit der Richtlinie DisablePasswordReveal = 1 erscheint die Schaltfläche zum Anzeigen nicht mehr, nachdem ein Kennwort eingegeben wurde [1].

## Warum es helfen kann
Jemand an deinem PC kann ein getipptes, aber noch nicht bestätigtes Kennwort nicht anzeigen lassen.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Ein langes Kennwort lässt sich vor dem Absenden nicht prüfen.

## Wann du es nicht nutzen solltest
Wenn du getippte Kennwörter oft mit der Augen-Schaltfläche prüfst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-credentialsui
