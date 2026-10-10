# BIOS älter als ein Jahr

## Zusammenfassung
::: status Info
Das BIOS des {{board}} ist über ein Jahr alt. Prüfe, ob es eine neuere Version gibt; Updates beheben oft Stabilitäts- und Kompatibilitätsprobleme.
:::
::: status Ok,Unknown,Problem,Unsupported
Prüft das Alter des BIOS.
:::

## Warum das wichtig ist
BIOS-Updates bringen neuen Prozessor-Microcode, Kompatibilitäts- und Sicherheitskorrekturen. Intel und AMD liefern zum Beispiel Korrekturen für die Prozessorstabilität und gegen Systemhänger über BIOS-Updates aus [1][2]. Manche Updates ergänzen Funktionen wie Smart Access Memory (Resizable BAR) [3]. Nicht jedes Update ist für Spiele wichtig, und wenn der Hersteller für ein älteres Mainboard keine Updates mehr veröffentlicht, gibt es eben nichts Neueres. Deshalb ist das eine Information, kein Problem.

## Wie wir es erkennen
Wir lesen Datum und Version des BIOS aus Windows.

## So behebst du es
::: ifnot laptop
1. Öffne die Supportseite deines {{board}} und vergleiche die neueste BIOS-Version mit deiner.
::: if supportUrl
   Supportseite: {{supportUrl}}
:::
2. Lies die Versionshinweise. Aktualisiere, wenn sie etwas für dich Wichtiges beheben, mit der Methode aus dem Handbuch (oft ein USB-Stick und das Flash-Tool im BIOS).
3. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [4][5].
4. Schalte den PC während des Updates nicht aus. Danach sind die BIOS-Einstellungen eventuell zurückgesetzt: Schalte XMP oder EXPO, Secure Boot und TPM bei Bedarf wieder ein.
:::
::: if laptop
1. BIOS-Updates für Laptops kommen über die Support-App oder Webseite des Herstellers, manchmal auch über Windows Update. Schließe vor dem Update das Netzteil an.
2. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update kann Windows beim nächsten Start danach fragen [4][5].
:::

## So prüfst du die Behebung
Starte den Scan erneut. Das BIOS-Datum sollte neuer sein.

## Quellen
1. https://www.intel.com/content/www/us/en/support/articles/000102331/processors.html
2. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
3. https://www.amd.com/en/legal/claims/gaming-details.html
4. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
5. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
