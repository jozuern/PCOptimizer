# Windows ist auf einer Festplatte installiert

## Zusammenfassung
::: status Problem
Windows läuft von der Festplatte {{disk}}. Start, Updates und Ladezeiten in Spielen sind viel langsamer als von einer SSD.
:::
::: status Ok,Unknown,Info,Unsupported
Prüft, ob Windows auf einer SSD installiert ist.
:::

## Warum das wichtig ist
Windows liest ständig viele kleine Dateien: beim Start, für Updates, für die Auslagerungsdatei, für Shader-Caches und wenn Spiele laden. Eine Festplatte braucht für jeden zufälligen Zugriff einige Millisekunden, eine SSD einen Bruchteil davon. Mit Windows auf einer Festplatte können Spiele ruckeln, sobald das System im Hintergrund auf die Platte zugreift.

## Wie wir es erkennen
Wir ermitteln den Datenträger mit dem Windows-Volume und lesen seinen Medientyp über die Windows-Speicherverwaltung.

## So behebst du es
1. Baue eine SSD ein (NVMe, wenn das Mainboard einen M.2-Steckplatz hat, sonst SATA).
2. Ziehe Windows um: Entweder installierst du es neu auf der SSD (am saubersten) oder du klonst das System mit dem Umzugstool des SSD-Herstellers.
3. Stelle danach im BIOS die SSD als erstes Startlaufwerk ein. Behalte die Festplatte für Dateien und Medien.

## So prüfst du die Behebung
Starte den Scan erneut. Der Windows-Datenträger sollte den Medientyp „SSD“ zeigen.
