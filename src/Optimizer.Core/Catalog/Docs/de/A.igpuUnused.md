# Integrierte Grafik aktiv, aber ungenutzt

## Zusammenfassung
::: status Info
Die integrierte Grafik ({{igpu}}) ist aktiv, aber kein Bildschirm nutzt sie. Das ist in Ordnung und kostet keine Bildrate.
:::
::: status Ok,Unknown,Problem,Unsupported
Prüft, ob die integrierte Grafik des Prozessors auf einem Desktop mit Grafikkarte aktiv ist.
:::

## Warum das wichtig ist
Mit eingebauter Grafikkarte ist die integrierte Grafik untätig. Sie reserviert etwas Arbeitsspeicher und ist nützlich für Videokodierung in Hardware (zum Beispiel Intel Quick Sync in OBS oder Videoschnittprogrammen) und für zusätzliche Monitoranschlüsse. Selten wählt ein Spiel oder Programm die falsche GPU, wenn beide vorhanden sind.

## Wie wir es erkennen
Wir prüfen, ob eine integrierte und eine dedizierte GPU aktiv sind und kein Bildschirm an der integrierten hängt.

## So behebst du es
1. Lass sie aktiv, wenn du Videokodierung in Hardware nutzt oder vielleicht einen weiteren Bildschirmausgang brauchst.
2. Nutzt ein Spiel die falsche GPU, stelle es unter **Einstellungen > System > Bildschirm > Grafik** auf die Hochleistungs-GPU, statt die integrierte Grafik abzuschalten.
3. Nur wenn du sie gar nicht brauchst: Schalte sie im BIOS ab (oft „iGPU Multi-Monitor“ oder „Integrated Graphics“ auf Disabled, die primäre Anzeige auf PCIe).

## So prüfst du die Behebung
Keine Prüfung nötig, das ist eine Information.
