# BIOS älter als ein Jahr

## Zusammenfassung
::: status Info
Das BIOS des {{board}} ist über ein Jahr alt. Prüfe, ob es eine neuere Version gibt; Updates beheben oft Stabilitäts- und Kompatibilitätsprobleme.
:::
::: status Ok,Unknown,Problem,Unsupported
Prüft das Alter des BIOS.
:::

## Warum das wichtig ist
BIOS-Updates bringen neuen Prozessor-Mikrocode, Korrekturen für die Speicherkompatibilität, Sicherheitskorrekturen und manchmal neue Funktionen wie Resizable BAR. Nicht jedes Update ist für Spiele wichtig, und wenn der Hersteller für ein älteres Mainboard keine Updates mehr veröffentlicht, gibt es eben nichts Neueres. Deshalb ist das eine Information, kein Problem.

## Wie wir es erkennen
Wir lesen Datum und Version des BIOS aus Windows.

## So behebst du es
::: ifnot laptop
1. Öffne die Supportseite deines {{board}} und vergleiche die neueste BIOS-Version mit deiner.
::: if supportUrl
   Supportseite: {{supportUrl}}
:::
2. Lies die Versionshinweise. Aktualisiere, wenn sie etwas für dich Wichtiges beheben, mit der Methode aus dem Handbuch (oft ein USB-Stick und das Flash-Tool im BIOS).
3. Schalte den PC während des Updates nicht aus. Danach sind die BIOS-Einstellungen eventuell zurückgesetzt: Schalte XMP/EXPO, Secure Boot und TPM bei Bedarf wieder ein.
:::
::: if laptop
1. BIOS-Updates für Laptops kommen über die Support-App oder Webseite des Herstellers, manchmal auch über Windows Update. Schließe vor dem Update das Netzteil an.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Das BIOS-Datum sollte neuer sein.
