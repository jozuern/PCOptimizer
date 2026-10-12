# Update-Neustarts, wann du willst, bis zu einer Woche

## Zusammenfassung
Nach einem monatlichen Update startet Windows erst 7 Tage nach dem Fund des Updates von selbst neu (frühestens 2 Tage nach der Installation). Den Zeitpunkt wählst du. Pro, Enterprise und Education.

## So funktioniert es
Ohne Richtlinie installiert Windows ein Update und startet außerhalb der Nutzungszeit von selbst neu [3]. Die Stichtag-Richtlinie für Qualitätsupdates (englisch "Specify deadline for automatic updates and restarts for quality update") legt einen Stichtag und eine Toleranzzeit fest [1][2]. Die App setzt SetComplianceDeadlineForQU = 1, ConfigureDeadlineForQualityUpdates = 7, ConfigureDeadlineGracePeriod = 2 und ConfigureDeadlineNoAutoRebootForQualityUpdates = 1, die Option, bis zum Ende der Toleranzzeit nicht automatisch neu zu starten [1]. Updates werden dann installiert, sobald Windows sie anbietet, und Windows wartet mit dem Neustart bis zum späteren Zeitpunkt von 7 Tagen nach dem Fund und 2 Tagen nach der Installation [2]. Bis dahin erinnert es dich und lässt dich eine Zeit wählen [2].

## Warum es helfen kann
Ein automatischer Neustart in der Nacht kann einen langen Download, ein Rendering oder ein laufendes Spiel beenden. Mit der Richtlinie startet etwa eine Woche lang nichts neu, außer du willst es.

## Belege
Eine von Microsoft beschriebene Windows-Update-Richtlinie für Windows 11 22H2 und neuer [1][2]. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Ist der Stichtag vorbei, startet Windows nach 15 Minuten Vorwarnung neu, auch in der Nutzungszeit [2]: Starte innerhalb der Woche selbst neu, um das zu vermeiden. Sicherheitskorrekturen wirken erst nach dem Neustart. Die Richtlinie startet die Installation außerdem, sobald ein Update angeboten wird [2]. Rückgängig entfernt die Richtlinie.

## Wann du es nicht nutzen solltest
Wenn Windows lieber nachts von selbst neu starten soll oder du wochenlang nie selbst neu startest.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
2. https://learn.microsoft.com/en-us/windows/deployment/update/wufb-compliancedeadlines
3. https://learn.microsoft.com/en-us/windows/deployment/update/waas-restart
