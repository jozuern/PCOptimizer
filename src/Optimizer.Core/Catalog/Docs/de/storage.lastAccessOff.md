# NTFS-Zeitstempel für den letzten Zugriff aus

## Zusammenfassung
Verhindert, dass NTFS die Zeit des letzten Zugriffs auf Dateien und Ordner aktualisiert. Wirkt nach einem Neustart. Kein messbarer Effekt in Spielen.

## So funktioniert es
NTFS speichert, wann eine Datei oder ein Ordner zuletzt aufgerufen wurde. Die Zeit liegt zunächst im Arbeitsspeicher und wird später auf das Laufwerk geschrieben, spätestens nach einer Stunde [1]. Seit Windows 10 Version 1803 kann Windows diese Einstellung selbst verwalten („System Managed“). Der Wert 0x80000001 bedeutet „vom Benutzer verwaltet, Aktualisierung aus“ [2], damit bleibt deine Wahl bestehen. Wirkt nach einem Neustart [1].

## Warum es helfen kann
Laut Microsoft beschleunigt das Abschalten den Zugriff auf Dateien und Ordner [1], weil NTFS weniger Metadaten schreibt.

## Belege
Eingespart werden kleine Metadaten-Schreibvorgänge. Wir haben keine Messung gefunden, die einen Effekt auf Spiele zeigt.

## Nachteile & Risiken
Programme, die Zugriffszeiten auswerten, etwa manche Backup- und Archivierungsprogramme, verlieren diese Information [1].

## Wann du es nicht nutzen solltest
Auf den meisten PCs nicht nötig. Lass es weg, wenn du ein Backup- oder Aufräumprogramm nutzt, das mit Zugriffszeiten arbeitet.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
2. https://support.citrix.com/external/article/CTX338425/pvs-and-mcs-devices-cache-disk-quickly-c.html
