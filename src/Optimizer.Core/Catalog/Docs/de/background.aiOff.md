# Recall-Momentaufnahmen aus

## Zusammenfassung
Verhindert, dass Recall auf Copilot+-PCs Bildschirm-Momentaufnahmen speichert, und löscht bereits gespeicherte. Rückgängig holt gelöschte Momentaufnahmen nicht zurück.

## So funktioniert es
Die Richtlinie DisableAIDataAnalysis = 1 („Speichern von Momentaufnahmen für Recall deaktivieren“) verhindert, dass Recall Momentaufnahmen speichert; vorhandene Momentaufnahmen werden beim Anwenden gelöscht [1]. Sie braucht Windows 11 24H2 mit dem Update vom April 2025 oder neuer [1].

## Warum es helfen kann
Recall erfasst und analysiert Bildschirminhalte im Hintergrund; mit der Richtlinie fällt diese Arbeit weg.

## Belege
Recall gibt es nur auf Copilot+-PCs, anderswo bewirkt die Richtlinie nichts. Keine veröffentlichte Messung in Spielen.

## Nachteile & Risiken
Vorhandene Recall-Momentaufnahmen werden gelöscht, und Rückgängig holt sie nicht zurück [1]. Du verlierst die Recall-Suche. Dieser Tweak wirkt nicht auf die Copilot-App; deinstalliere sie unter Einstellungen > Apps > Installierte Apps, wenn du sie nicht willst.

## Wann du es nicht nutzen solltest
Behalte Recall, wenn du es nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsai
