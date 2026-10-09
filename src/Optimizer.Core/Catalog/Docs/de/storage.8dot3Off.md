# 8.3-Kurznamen aus

## Zusammenfassung
Verhindert, dass NTFS für neue Dateien DOS-Kurznamen (wie PROGRA~1) anlegt. Kleiner Effekt. Bestehende Kurznamen bleiben erhalten.

## So funktioniert es
Zur Kompatibilität mit sehr alten Programmen kann NTFS für jede Datei einen 8.3-Kurznamen anlegen. Der Wert NtfsDisable8dot3NameCreation = 1 schaltet das auf allen Laufwerken ab [1]. Bestehende Kurznamen werden nicht entfernt, weil das Installationsprogramme beschädigen kann.

## Warum es helfen kann
Das Anlegen vieler Dateien (Spielinstallationen, Shader-Caches) macht etwas weniger Arbeit.

## Belege
Auf SSDs kein messbarer Effekt in Spielen. Der Gewinn zeigt sich nur in Ordnern mit sehr vielen Dateien.

## Nachteile & Risiken
Sehr alte Programme aus der 16-Bit-Zeit, die Kurznamen brauchen, können bei neuen Dateien scheitern.

## Wann du es nicht nutzen solltest
Auf den meisten PCs nicht nötig.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
