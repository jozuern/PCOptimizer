# Intel 13./14. Generation: BIOS-Microcode-Update nötig

## Zusammenfassung
::: status Problem
Dein BIOS liefert einen Microcode älter als {{min}}. Intel-Desktop-CPUs der 13./14. Generation brauchen ihn gegen Schäden durch zu hohe Spannung. Aktualisiere das BIOS bald.
:::
::: status Ok
Dein BIOS liefert Microcode {{min}} oder neuer. Er enthält Intels Korrektur für die Instabilität der 13./14. Generation.
:::
::: status Unknown,Info,Unsupported
Prüft, ob Intel-Desktop-CPUs der 13./14. Generation einen BIOS-Microcode mit Intels Korrektur der Instabilität nutzen.
:::

## Warum das wichtig ist
Intel hat festgestellt, dass viele Core-Desktop-Prozessoren der 13. und 14. Generation (ab 65 W) zu hohe Spannungen anforderten. Das kann den Chip dauerhaft schädigen („Vmin Shift“). Typische Anzeichen sind Abstürze in Spielen, Fehler beim Kompilieren von Shadern und beim Entpacken. Intels Microcode-Updates bis {{min}} begrenzen diese Spannungen. Bereits entstandene Schäden macht das nicht rückgängig, deshalb sollte das Update so früh wie möglich drauf. Es geht hier um Stabilität, nicht um einen Leistungs-Tweak. Deshalb wird das Thema als kritisch markiert und bekommt keine Wirkungsbewertung.

## Wie wir es erkennen
Wir lesen das Prozessormodell und die Microcode-Revision, die das BIOS geladen hat (`Firmware Record Version` im Registrierungsschlüssel des Prozessors, bei älteren Builds `Previous Update Revision`). Windows kann selbst neueren Microcode laden. Hier zählt aber die BIOS-Revision, weil die Korrektur ab dem Einschalten aktiv sein muss. Modellliste und Mindestrevision stehen im Katalog.

## So behebst du es
1. Notiere dein Mainboard ({{board}}) und öffne die Support-Seite des Herstellers.
2. Lade das neueste BIOS, das Intel-Microcode {{min}} oder neuer nennt.
3. Spiele das BIOS nach Anleitung des Herstellers ein (ASUS EZ Flash, MSI M-Flash, Gigabyte Q-Flash, ASRock Instant Flash). Schalte den PC während des Updates nicht aus.
4. Wähle nach dem Update im BIOS das Energieprofil **Intel Default Settings**, falls angeboten.

## So prüfst du die Behebung
Starte den Scan erneut. Der Microcode aus dem BIOS sollte {{min}} oder neuer sein.

## Quellen
1. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Desktop-Instability-Root-Cause/m-p/1633442
2. https://www.tomshardware.com/pc-components/cpus/raptor-lake-instability-saga-continues-as-intel-releases-0x12f-update-to-fix-vmin-instability
