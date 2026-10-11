# Einstellungen: keine Startseite

## Zusammenfassung
Blendet die Startseite der Einstellungen-App mit einer dokumentierten Richtlinie aus, sodass die Einstellungen auf einer anderen Seite öffnen. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie „Sichtbarkeit der Seite ‚Einstellungen‘“ blendet die Seiten aus, die sie aufführt; der Wert hide:home blendet die Seite mit der Adresse ms-settings:home aus [1]. Microsoft nennt die Richtlinie für Pro, Enterprise und Education [1]. Der Wert ist SettingsPageVisibility unter SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer.

## Warum es helfen kann
Die Einstellungen öffnen ohne die Startseite und ihre Karten, direkt auf einer Einstellungsseite.

## Belege
Dokumentierte Richtlinie [1]. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Wenn ein anderes Programm oder ein Administrator mit dieser Richtlinie schon Seiten ausblendet, ersetzt die Option diese Liste; Rückgängig machen stellt sie wieder her.

## Wann du es nicht nutzen solltest
Wenn du die Startseite der Einstellungen nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-settings
