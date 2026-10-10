# Reservierter Speicher aus

## Zusammenfassung
Gibt den Platz frei, den Windows für Updates, temporäre Dateien und Caches reserviert. Auf kleinen Laufwerken können Updates dann aus Platzmangel scheitern.

## So funktioniert es
Windows reserviert einen Teil des Laufwerks, damit Updates geladen und installiert werden können, ohne dass du Platz schaffen musst, und nutzt ihn bis dahin für temporäre Dateien und Caches [1][2]. Die App schaltet ihn mit DISM /Set-ReservedStorageState /State:Disabled aus [1]. Belegt gerade ein Update den Platz, lehnt DISM ab; versuche es dann später [1]. Rückgängig schaltet ihn wieder ein.

## Warum es helfen kann
Auf einem kleinen Systemlaufwerk kann der reservierte Platz entscheiden, ob ein Spiel-Update noch passt.

## Belege
Von Microsoft beschrieben [1][2]. Die Größe siehst du unter Einstellungen > System > Speicher > System und reserviert [2]; die Leistung ändert sich nicht.

## Nachteile & Risiken
Windows-Updates brauchen dann wieder freien Platz: Bei fast vollem Laufwerk können sie scheitern, bis du Platz schaffst oder die Änderung rückgängig machst. Microsoft rät bei verwalteten PCs, ihn nur rund um ein Update auszuschalten [1].

## Wann du es nicht nutzen solltest
Wenn das Laufwerk viel freien Platz hat oder du vor großen Windows-Updates nicht auf freien Platz achtest.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-storage-reserve?view=windows-11
2. https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/storage-settings-in-windows
