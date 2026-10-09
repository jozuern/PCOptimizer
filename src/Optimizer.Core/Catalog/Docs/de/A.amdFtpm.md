# AMD-fTPM-Ruckler (AM4)

## Zusammenfassung
::: status Problem
Dieser AM4-PC nutzt das Firmware-TPM von AMD mit einem BIOS, das älter ist als die Korrektur gegen zufällige Ruckler (AGESA 1.2.0.7). Aktualisiere das BIOS.
:::
::: status Ok
Das BIOS enthält AMDs Korrektur gegen fTPM-bedingte Ruckler.
:::
::: status Unknown,Info,Unsupported
Prüft, ob ein AM4-PC mit AMD-fTPM die BIOS-Korrektur gegen gelegentliche Ruckler hat.
:::

## Warum das wichtig ist
Auf manchen AM4-Systemen mit Ryzen verursachte das Firmware-TPM (fTPM) kurze Hänger des ganzen Systems mit Tonaussetzern, weil es manchmal langsam auf den BIOS-Flash-Chip zugriff [1]. AMD hat das mit AGESA 1.2.0.7 behoben, das ab Mai 2022 in BIOS-Updates erschien. Da Windows 11 ein TPM braucht, hat fast jeder AM4-PC mit Windows 11 das fTPM eingeschaltet.

## Wie wir es erkennen
Wir prüfen, ob der Prozessor ein AM4-Ryzen ist und das aktive TPM von AMD stammt (Firmware-TPM).
::: variant agesa
Die AGESA-Version stammt aus den Informationstabellen des BIOS.
:::
::: variant date
Das BIOS meldet seine AGESA-Version nicht, daher nutzen wir sein Datum: Versionen ab Mai 2022 enthalten die Korrektur normalerweise.
:::

## So behebst du es
1. Lade das neueste BIOS für dein {{board}} von der Supportseite des Herstellers und achte in den Versionshinweisen auf AGESA 1.2.0.7 oder neuer.
::: if supportUrl
   Supportseite: {{supportUrl}}
:::
2. Aktualisiere das BIOS wie im Handbuch beschrieben. Schalte den PC während des Updates nicht aus.
3. Nach dem Update sind die BIOS-Einstellungen oft zurückgesetzt: Schalte fTPM, XMP/D.O.C.P und Secure Boot bei Bedarf wieder ein.
4. Gibt es für dein Mainboard kein korrigiertes BIOS, vermeidet ein separates TPM-Modul (dTPM) das Problem.

## So prüfst du die Behebung
Starte den Scan erneut. AGESA-Version oder BIOS-Datum sollten neuer sein.

## Quellen
1. https://www.amd.com/en/resources/support-articles/faqs/PA-410.html
