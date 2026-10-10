# Defekte Verknüpfungen: keine Laufwerkssuche

## Zusammenfassung
Fehlt das Ziel einer Verknüpfung, meldet Windows das sofort, statt die Laufwerke nach der Datei zu durchsuchen.

## So funktioniert es
Ohne diese Richtlinien sucht Windows das fehlende Ziel einer Verknüpfung: Es durchsucht alle mit ihr verbundenen Pfade und nutzt die NTFS-Dateiverfolgung [1]. NoResolveSearch = 1 überspringt die gründliche Laufwerkssuche, NoResolveTrack = 1 die Verfolgung über die Datei-ID [1]. Windows zeigt dann eine Meldung, dass die Datei nicht gefunden wurde [1].

## Warum es helfen kann
Kein Warten und keine Datenträgeraktivität auf langsamen oder Netzlaufwerken, wenn du eine alte Verknüpfung anklickst.

## Belege
Von Microsoft beschriebene Windows-Richtlinien [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Eine Verknüpfung zu einer verschobenen Datei wird nicht automatisch repariert; du legst eine neue an.

## Wann du es nicht nutzen solltest
Wenn du oft Dateien verschiebst und darauf setzt, dass Verknüpfungen sie finden.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-startmenu
