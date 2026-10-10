# Unerwartete Abschaltungen

## Zusammenfassung
::: variant stop
Unerwartete Abschaltungen in den letzten 30 Tagen: {{count}}. Mindestens eine folgte auf einen Stop-Fehler ({{stopCode}}), Windows ist also abgestürzt.
:::
::: variant power
Unerwartete Abschaltungen in den letzten 30 Tagen: {{count}}. Kein Stop-Fehler aufgezeichnet: Stromausfall, ein Hänger oder ein erzwungenes Ausschalten.
:::
::: variant default
Prüft das System-Ereignisprotokoll auf unerwartete Abschaltungen (Kernel-Power-Ereignis 41) in den letzten 30 Tagen.
:::

## Warum das wichtig ist
Schaltet sich der PC unerwartet ab, protokolliert Windows beim nächsten Start das Ereignis 41. Ursache ist eine Unterbrechung der Stromversorgung oder ein Stop-Fehler (Bluescreen) [1]. Ohne Stop-Fehlercode nennt Microsoft das Netzteil, einen Hänger, der mit dem Netzschalter beendet wurde, Übertaktung, den Arbeitsspeicher und Überhitzung als Prüfpunkte [1]. Jede unerwartete Abschaltung kostet ungespeicherte Arbeit und kann Dateien beschädigen [1].

## Wie wir es erkennen
Wir lesen die Kernel-Power-Ereignisse mit der ID 41 der letzten 30 Tage aus dem System-Protokoll. Enthält das Ereignis einen Stop-Fehlercode (`BugcheckCode`), ist Windows abgestürzt; ist `PowerButtonTimestamp` gesetzt, wurde der PC durch langes Drücken des Netzschalters ausgeschaltet [1].
::: if powerButton
Mindestens eine dieser Abschaltungen erfolgte über den Netzschalter.
:::

## So behebst du es
::: variant stop
1. Schlage den Stop-Fehlercode {{stopCode}} in Microsofts Referenz der Fehlerprüfcodes nach [2], um zu sehen, auf welchen Teil des Systems er hinweist.
2. **BitLocker:** Prüfe vor einem BIOS-Update oder einer Änderung im BIOS, ob BitLocker oder die Geräteverschlüsselung an ist. Wenn ja, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [3][4].
3. Aktualisiere Grafik-, Chipsatz- und Netzwerktreiber sowie das BIOS.
4. Schalte Übertaktung und Speicherprofile (XMP oder EXPO) testweise ab. Hören die Abstürze auf, waren diese Einstellungen nicht stabil [1].
:::
::: variant power
1. Ist der Strom ausgefallen oder hast du den PC absichtlich mit dem Netzschalter ausgeschaltet, gibt es nichts zu beheben.
2. Prüfe sonst, ob das Netzteil genug Leistung für die eingebauten Teile hat [1].
3. **BitLocker:** Prüfe vor einer Änderung im BIOS, ob BitLocker oder die Geräteverschlüsselung an ist. Wenn ja, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert [3][4].
4. Schalte Übertaktung und Speicherprofile (XMP oder EXPO) testweise ab und prüfe die Temperaturen unter Last (Drosselungsprüfung auf der Seite Zustand) [1].
:::
::: variant default
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan nach ein paar Tagen normaler Nutzung erneut. Es sollten keine neuen unerwarteten Abschaltungen aufgelistet sein.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/event-id-41-restart
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-code-reference2
3. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
4. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
