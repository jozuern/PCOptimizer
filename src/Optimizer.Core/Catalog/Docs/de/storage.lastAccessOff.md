# NTFS-Zeitstempel für den letzten Zugriff aus

## Zusammenfassung
Verhindert, dass NTFS bei jedem Lesen die Zeit des letzten Zugriffs aktualisiert. Windows macht das auf großen Laufwerken bereits selbst. Kleiner Effekt.

## So funktioniert es
NTFS kann speichern, wann eine Datei zuletzt gelesen wurde. Das kostet pro Zugriff einen zusätzlichen Metadaten-Schreibvorgang. Der Wert 0x80000001 schaltet die Aktualisierung für alle Laufwerke ab und legt die Wahl fest [1].

## Warum es helfen kann
Weniger kleine Metadaten-Schreibvorgänge bei vielen Dateizugriffen, etwa beim Laden vieler kleiner Spieldateien.

## Belege
Seit Windows 10 1803 schaltet Windows die Aktualisierung auf großen Laufwerken automatisch ab. Meist ist der Standard also schon aus. Kein messbarer Effekt in Spielen.

## Nachteile & Risiken
Tools, die Zugriffszeiten auswerten (manche Backup- oder Aufräumprogramme), verlieren diese Information.

## Wann du es nicht nutzen solltest
Auf den meisten PCs nicht nötig.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
