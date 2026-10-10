# Microsoft Defender in einer Sandbox

## Zusammenfassung
Führt den Teil von Microsoft Defender, der Dateiinhalte prüft, in einem eigenen abgeschotteten Prozess aus, damit ein Fehler im Dateiparser nicht das System übernehmen kann. Braucht einen Neustart.

## So funktioniert es
Microsoft beschreibt die systemweite Umgebungsvariable MP_FORCE_USE_SANDBOX = 1 mit anschließendem Neustart; danach läuft ein Inhaltsprozess MsMpEngCP.exe neben MsMpEng.exe [1]. Defender muss im aktiven Modus laufen [1].

## Warum es helfen kann
Angreifer haben Wege gefunden, Defenders Parser für Dateiinhalte auszunutzen; in der Sandbox erreicht so ein Fehler den Rest des Systems nicht [1].

## Belege
Von Microsoft beschrieben [1]. Die Sandbox startet einen zusätzlichen Prozess neben dem Virenschutzmodul [1].

## Nachteile & Risiken
Ein Prozess mehr läuft. Rückgängig entfernt die Variable; nach einem Neustart läuft Defender wieder ohne Sandbox.

## Wann du es nicht nutzen solltest
Wenn du ein anderes Virenschutzprogramm nutzt; dann ist Defender nicht im aktiven Modus und die Einstellung wirkungslos.

## Quellen
1. https://learn.microsoft.com/en-us/defender-endpoint/sandbox-mdav
