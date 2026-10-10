# Intel Core Ultra 200S: BIOS-Update mit Microcode {{min}}

## Zusammenfassung
::: status Problem
Dein BIOS liefert einen Microcode älter als {{min}}. Intels spätere Updates für Core Ultra 200S beheben Leistungsprobleme in Spielen.
:::
::: status Ok
Dein BIOS liefert Microcode {{min}} oder neuer mit Intels Spieleleistungs-Korrekturen für Core Ultra 200S.
:::
::: status Unknown,Info,Unsupported
Prüft, ob Core-Ultra-200S-Desktop-CPUs einen BIOS-Microcode mit Intels Korrekturen für die Spieleleistung nutzen.
:::

## Warum das wichtig ist
Zum Start blieben Intel-Core-Ultra-200S-Prozessoren (Arrow Lake) in vielen Spielen hinter den Erwartungen zurück. Intel fand fünf Ursachen. Die meisten behebt ein aktuelles BIOS zusammen mit Windows-Updates bis Windows 11 Build 26100.2314 oder neuer. Die letzte braucht ein BIOS mit Microcode {{min}} und Intel CSME Firmware Kit 19.0.0.1854v2.2 oder neuer. Dafür erwartete Intel im Schnitt über etwa 35 Spiele eine weitere Verbesserung im einstelligen Prozentbereich [1]. Mainboards mit älterem BIOS fehlen diese Korrekturen.

## Wie wir es erkennen
Wir lesen das Prozessormodell und die vom BIOS geladene Microcode-Revision. Modellliste und Mindestrevision stehen im Katalog.

## So behebst du es
1. Öffne die Supportseite deines Mainboards ({{board}}).
2. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [2][3].
3. Spiele nach Anleitung des Herstellers das neueste BIOS ein, das Microcode {{min}} und CSME-Firmware 19.0.0.1854v2.2 oder neuer nennt [1].
4. Installiere Windows-Updates, bis Windows 11 mindestens Build 26100.2314 meldet (Einstellungen > System > Info) [1].

## So prüfst du die Behebung
Starte den Scan erneut. Der Microcode aus dem BIOS sollte {{min}} oder neuer sein.

## Quellen
1. https://community.intel.com/t5/Blogs/Tech-Innovation/Client/Field-Update-1-of-2-Intel-Core-Ultra-200S-Series-Performance/post/1650490
2. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
