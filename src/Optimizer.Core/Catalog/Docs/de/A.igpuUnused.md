# Integrierte Grafik aktiv, aber ungenutzt

## Zusammenfassung
::: status Info
Die integrierte Grafik ({{igpu}}) ist aktiv, aber kein Bildschirm nutzt sie. Das ist in Ordnung und kostet keine Bildrate.
:::
::: status Ok,Unknown,Problem,Unsupported
Prüft, ob die integrierte Grafik des Prozessors auf einem Desktop mit Grafikkarte aktiv ist.
:::

## Warum das wichtig ist
Mit eingebauter Grafikkarte und ohne Bildschirm an der integrierten Grafik ist diese untätig. Sie ist nützlich für Videokodierung in Hardware (zum Beispiel Intel Quick Sync in OBS oder Videoschnittprogrammen) und für zusätzliche Monitoranschlüsse. Sind beide GPUs vorhanden, kann ein Spiel oder Programm die falsche wählen; Windows lässt dich die GPU pro App festlegen [1].

## Wie wir es erkennen
Wir prüfen, ob eine integrierte und eine dedizierte GPU aktiv sind und kein Bildschirm an der integrierten hängt.

## So behebst du es
1. Lass sie aktiv, wenn du Videokodierung in Hardware nutzt oder vielleicht einen weiteren Bildschirmausgang brauchst.
2. Nutzt ein Spiel die falsche GPU, öffne **Einstellungen > System > Bildschirm > Grafik**, wähle das Spiel unter **Benutzerdefinierte Optionen für Apps**, dann **Optionen**, **Hohe Leistung** und **Speichern** [1]. Das ist besser, als die integrierte Grafik abzuschalten.
3. **BitLocker:** Prüfe, bevor du BIOS-Einstellungen änderst, ob BitLocker oder die Geräteverschlüsselung an ist. Wenn ja, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [2][3].
4. Nur wenn du sie gar nicht brauchst: Schalte sie im BIOS ab (oft „iGPU Multi-Monitor“ oder „Integrated Graphics“ auf Disabled, die primäre Anzeige auf PCIe).

## So prüfst du die Behebung
Keine Prüfung nötig, das ist eine Information.

## Quellen
1. https://support.microsoft.com/de-de/windows/hardware/display-graphics/optimizations-for-windowed-games-in-windows-11
2. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
3. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
