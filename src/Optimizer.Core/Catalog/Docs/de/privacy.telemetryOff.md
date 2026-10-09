# Telemetrie auf Minimum

## Zusammenfassung
Setzt die Diagnosedaten auf die niedrigste Stufe, die die Edition erlaubt, deaktiviert den Dienst DiagTrack und stoppt die geplanten Kompatibilitäts- und CEIP-Aufgaben.

## So funktioniert es
Die Richtlinie AllowTelemetry = 0 fordert die niedrigste Diagnosedatenstufe an (Home und Pro behandeln sie als „Erforderlich“) [1]. Der Dienst Benutzererfahrung und Telemetrie im verbundenen Modus (DiagTrack) wird deaktiviert, ebenso die Aufgaben Compatibility Appraiser, CEIP und Datenträgerdiagnose [2].

## Warum es helfen kann
Die Aufgabe Compatibility Appraiser kann beim Ausführen spürbar CPU- und Laufwerkszeit belegen. Ohne sie passiert das nicht mitten im Spiel.

## Belege
Auf die FPS wirkt sich das meist nicht aus. Der Nutzen ist weniger Hintergrundaktivität zu zufälligen Zeitpunkten und weniger gesendete Daten.

## Nachteile & Risiken
Windows-Insider-Builds brauchen optionale Diagnosedaten, deshalb ist der Tweak auf Insider-PCs gesperrt. Manche Problembehandlungen und Feedbackfunktionen haben weniger Informationen.

## Wann du es nicht nutzen solltest
Nicht auf Windows-Insider-Builds nutzen.

## Quellen
1. https://learn.microsoft.com/en-us/windows/privacy/configure-windows-diagnostic-data-in-your-organization
2. https://github.com/ChrisTitusTech/winutil
