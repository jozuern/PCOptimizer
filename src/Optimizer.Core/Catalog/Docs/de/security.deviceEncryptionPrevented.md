# Keine automatische Geräteverschlüsselung

## Zusammenfassung
Verhindert, dass Windows die BitLocker-Geräteverschlüsselung von selbst einschaltet. Ein bereits verschlüsseltes Laufwerk wird nicht entschlüsselt.

## So funktioniert es
Die Geräteverschlüsselung schaltet BitLocker auf geeigneten PCs nach der Einrichtung automatisch ein; seit Windows 11 24H2 sind mehr PCs geeignet [1]. Microsoft nennt PreventDeviceEncryption = 1 unter HKLM\SYSTEM\CurrentControlSet\Control\BitLocker, um das zu verhindern [1].

## Warum es helfen kann
Sinnvoll vor einer Neuinstallation oder einem Laufwerkswechsel, wenn du kein verschlüsseltes Laufwerk willst, dessen Wiederherstellungsschlüssel du eventuell nicht hast.

## Belege
Von Microsoft beschrieben [1]. Auf PCs, auf denen die Verschlüsselung schon an ist, ändert sich nichts; sieh unter Einstellungen > Datenschutz und Sicherheit > Geräteverschlüsselung nach.

## Nachteile & Risiken
Ein unverschlüsseltes Laufwerk kann jeder lesen, der es aus dem PC nimmt. Um später zu verschlüsseln, mach die Änderung rückgängig und schalte Geräteverschlüsselung oder BitLocker ein.

## Wann du es nicht nutzen solltest
Auf einem Notebook, das das Haus verlässt, oder wenn auf dem Laufwerk Daten liegen, die privat bleiben müssen.

## Quellen
1. https://learn.microsoft.com/en-us/windows/security/operating-system-security/data-protection/bitlocker/
