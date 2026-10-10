# AMD-fTPM-Ruckler (AM4)

## Zusammenfassung
::: status Problem
Dieser AM4-Desktop nutzt das Firmware-TPM von AMD mit einem BIOS, das älter ist als AMDs Korrektur gegen kurze Systemhänger (AGESA 1.2.0.7). Aktualisiere das BIOS.
:::
::: status Ok
Das BIOS enthält AMDs Korrektur gegen Hänger durch das Firmware-TPM.
:::
::: status Unknown,Info,Unsupported
Prüft, ob ein AM4-Desktop mit AMD-fTPM die BIOS-Korrektur gegen gelegentliche Systemhänger hat.
:::

## Warum das wichtig ist
AMD hat festgestellt, dass das Firmware-TPM (fTPM) auf bestimmten Ryzen-Systemen manchmal lange auf den Flash-Chip des Mainboards zugreift. Dabei reagiert das ganze System kurz nicht mehr [1]. AMDs Korrektur steckt in BIOS-Versionen auf Basis von AGESA 1.2.0.7 oder neuer, je nach Mainboard-Hersteller ab Anfang Mai 2022 [1]. Windows 11 verlangt TPM 2.0 [2], daher ist das fTPM auf AM4-PCs mit Windows 11 meist eingeschaltet.

## Wie wir es erkennen
Wir prüfen, ob der PC ein Desktop mit AM4-Ryzen ist und das aktive TPM von AMD stammt (Firmware-TPM). Laptops und Mini-PCs mit mobilen Ryzen-Prozessoren prüfen wir nicht, weil AMDs AGESA-Version für die Desktop-Plattform AM4 gilt.
::: variant agesa
Die AGESA-Version stammt aus den Informationstabellen des BIOS.
:::
::: variant date
Das BIOS meldet keine AGESA-Version für Desktop-AM4, daher nutzen wir sein Datum: Versionen ab Mai 2022 enthalten die Korrektur normalerweise.
:::

## So behebst du es
1. Lade das neueste BIOS für dein {{board}} von der Supportseite des Herstellers und achte in den Versionshinweisen auf AGESA 1.2.0.7 oder neuer [1].
::: if supportUrl
   Supportseite: {{supportUrl}}
:::
2. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [3][4].
3. Aktualisiere das BIOS wie im Handbuch beschrieben. Schalte den PC während des Updates nicht aus.
4. Nach dem Update sind die BIOS-Einstellungen oft zurückgesetzt: Schalte fTPM, das Speicherprofil (XMP oder DOCP) und Secure Boot bei Bedarf wieder ein.
5. Gibt es für dein Mainboard kein korrigiertes BIOS, nennt AMD ein separates TPM-Modul (dTPM) als Ausweg. Prüfe, ob dein Mainboard eines unterstützt, und schalte BitLocker oder die Geräteverschlüsselung vor dem Wechsel aus oder sichere deine Daten [1].

## So prüfst du die Behebung
Starte den Scan erneut. AGESA-Version oder BIOS-Datum sollten neuer sein.

## Quellen
1. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
2. https://support.microsoft.com/de-de/windows/windows-11-system-requirements-86c11283-ea52-4782-9efd-7674389a7ba3
3. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
4. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
