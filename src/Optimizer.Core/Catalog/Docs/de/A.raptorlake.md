# Intel 13./14. Generation: BIOS-Microcode-Update nötig

## Zusammenfassung
::: status Problem
Dein BIOS liefert einen Microcode älter als {{min}}. Intel empfiehlt für Desktop-CPUs der 13./14. Generation {{min}} oder neuer gegen Alterung durch zu hohe Spannung. Aktualisiere das BIOS bald.
:::
::: status Ok
Dein BIOS liefert Microcode {{min}} oder neuer. Er enthält Intels Korrekturen für die Instabilität der 13./14. Generation.
:::
::: status Unknown,Info,Unsupported
Prüft, ob Intel-Desktop-CPUs der 13./14. Generation einen BIOS-Microcode mit Intels Korrektur der Instabilität nutzen.
:::

## Warum das wichtig ist
Intel hat festgestellt, dass manche Core-Desktop-Prozessoren der 13. und 14. Generation zu hohe Spannungen anforderten. Das kann eine Taktschaltung in den Kernen altern lassen („Vmin Shift“) und zu Instabilität führen [1]. Die Ursache behebt Microcode 0x12B, der per BIOS-Update kommt [1]. Im Mai 2025 folgte 0x12F als Ergänzung für PCs, die tagelang unter leichter Last laufen [2]. Intel empfiehlt das neueste BIOS mit Microcode 0x12F oder neuer zusammen mit den Intel Default Settings [4]. Weil die Alterung unter zu hoher Spannung mit der Zeit zunimmt [1], sollte das Update so früh wie möglich drauf. Es geht hier um Stabilität, nicht um einen Leistungs-Tweak. Deshalb ist das Thema als kritisch markiert und bekommt keine Wirkungsbewertung.

## Wie wir es erkennen
Wir lesen das Prozessormodell und die Microcode-Revision, die das BIOS geladen hat (`Firmware Record Version` im Registrierungsschlüssel des Prozessors, bei älteren Builds `Previous Update Revision`). Windows kann selbst neueren Microcode laden. Hier zählt aber die BIOS-Revision, weil die Korrektur ab dem Einschalten aktiv sein muss. Modellliste und Mindestrevision stehen im Katalog.

## So behebst du es
1. Notiere dein Mainboard ({{board}}) und öffne die Supportseite des Herstellers.
2. Lade das neueste BIOS, das Intel-Microcode {{min}} oder neuer nennt.
3. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [5][6].
4. Spiele das BIOS nach Anleitung des Herstellers ein (ASUS EZ Flash, MSI M-Flash, Gigabyte Q-Flash, ASRock Instant Flash). Schalte den PC während des Updates nicht aus.
5. Wähle nach dem Update im BIOS das Energieprofil **Intel Default Settings**, falls angeboten [4].
6. Stürzt der PC mit dem neuen BIOS und den Intel Default Settings weiter ab, wende dich an den Hersteller deines PCs oder an den Intel-Support. Intel tauscht betroffene Prozessoren aus und hat die Garantie dafür auf bis zu fünf Jahre ab Kauf verlängert [3][4].

## So prüfst du die Behebung
Starte den Scan erneut. Der Microcode aus dem BIOS sollte {{min}} oder neuer sein.

## Quellen
1. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Desktop-Instability-Root-Cause/m-p/1633442
2. https://community.intel.com/t5/Processors/Intel-Core-13th-and-14th-Gen-Vmin-Shift-Instabilty-Update-New/m-p/1686948
3. https://community.intel.com/t5/Processors/July-2024-Update-on-Instability-Reports-on-Intel-Core-13th-and/m-p/1617113
4. https://www.intel.com/content/www/us/en/support/articles/000102331/processors.html
5. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
6. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
