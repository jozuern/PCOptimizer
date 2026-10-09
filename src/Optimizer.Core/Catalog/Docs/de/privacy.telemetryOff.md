# Telemetrie auf Minimum

## Zusammenfassung
Setzt die Diagnosedaten auf die niedrigste Stufe deiner Edition und deaktiviert den Dienst DiagTrack sowie die Kompatibilitäts- und CEIP-Aufgaben. Auf Insider-Builds nicht verfügbar.

## So funktioniert es
Die Richtlinie AllowTelemetry = 0 fordert „Diagnosedaten aus“ an. Nur Enterprise, Education und Server beachten diese Stufe; auf Pro gilt der Wert als „Erforderliche Diagnosedaten“ [1][2]. Die Richtlinie sperrt außerdem den Diagnosedaten-Schalter in den Einstellungen [2]. Der Dienst „Benutzererfahrungen und Telemetrie im verbundenen Modus“ (DiagTrack) wird deaktiviert und stoppt nach dem nächsten Neustart. Die Aufgaben Compatibility Appraiser, CEIP, Autochk-Proxy und Datenträgerdiagnose werden deaktiviert; Aufgaben, die es auf deinem Build nicht gibt, werden übersprungen.

## Warum es helfen kann
Diese Aufgaben und der Dienst laufen zu Zeiten, die Windows bestimmt, etwa um installierte Programme auf Upgrade-Kompatibilität zu prüfen [1]. Ohne sie fällt diese Hintergrundarbeit weg und es werden weniger Daten gesendet.

## Belege
Es gibt keine veröffentlichte Messung, die einen Effekt auf die FPS zeigt. Der Nutzen ist Datenschutz und weniger Hintergrundaufgaben, nicht Tempo.

## Nachteile & Risiken
Windows-Insider-Builds verlangen optionale Diagnosedaten, deshalb ist der Tweak auf Insider-PCs gesperrt [3]. Solange die Richtlinie gesetzt ist, zeigen die Einstellungen die Diagnosedaten-Seite als von deiner Organisation verwaltet an. Problembehandlungen und Feedback-Hub haben weniger Informationen.

## Wann du es nicht nutzen solltest
Nicht auf Windows-Insider-Builds nutzen und nicht auf PCs, deren Administrator Windows-Update-Berichte braucht.

## Quellen
1. https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system#allowtelemetry
3. https://learn.microsoft.com/en-us/windows-insider/data-settings
