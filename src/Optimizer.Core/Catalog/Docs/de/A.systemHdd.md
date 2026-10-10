# Windows ist auf einer Festplatte installiert

## Zusammenfassung
::: status Problem
Windows läuft von der Festplatte {{disk}}. Start, Updates und Ladezeiten in Spielen sind viel langsamer als von einer SSD.
:::
::: status Ok,Unknown,Info,Unsupported
Prüft, ob Windows auf einer SSD installiert ist.
:::

## Warum das wichtig ist
Windows liest ständig viele kleine Dateien: beim Start, für Updates, für die Auslagerungsdatei, für Shader-Caches und wenn Spiele laden. Eine Festplatte muss vor jedem zufälligen Zugriff warten, bis sich die Scheibe weitergedreht hat: Bei 7200 U/min dauert eine Umdrehung 8,3 ms, die Wartezeit liegt also im Mittel bei etwa 4,2 ms, bevor sich der Kopf überhaupt bewegt [1]. Eine SSD hat keine beweglichen Teile und braucht einen Bruchteil davon. Mit Windows auf einer Festplatte können Spiele ruckeln, sobald das System im Hintergrund auf die Platte zugreift.

## Wie wir es erkennen
Wir ermitteln den Datenträger mit dem Windows-Volume und lesen seinen Medientyp über die Windows-Speicherverwaltung.

## So behebst du es
1. Baue eine SSD ein (NVMe, wenn das Mainboard einen M.2-Steckplatz hat, sonst SATA).
2. Ziehe Windows um: Entweder installierst du es neu auf der SSD (am saubersten) oder du klonst das System mit dem Umzugstool des SSD-Herstellers.
3. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz aus, bevor du klonst oder das Startlaufwerk wechselst (Start > **BitLocker verwalten** > **Schutz anhalten**), oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einer Änderung an der Startkonfiguration kann Windows beim nächsten Start danach fragen [2][3].
4. Stelle danach im BIOS die SSD als erstes Startlaufwerk ein. Behalte die Festplatte für Dateien und Medien.

## So prüfst du die Behebung
Starte den Scan erneut. Der Windows-Datenträger sollte den Medientyp „SSD“ zeigen.

## Quellen
1. https://www.seagate.com/files/www-content/product-content/barracuda-fam/barracuda-new/en-us/docs/100817550m.pdf
2. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
