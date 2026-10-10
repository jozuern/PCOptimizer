# Keine Animation bei der ersten Anmeldung

## Zusammenfassung
Neue Benutzerkonten und die erste Anmeldung nach der Einrichtung überspringen die "Hallo"-Animation. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie EnableFirstLogonAnimation steuert, ob Benutzer bei ihrer ersten Anmeldung am Computer die Animation sehen [1]. Die App setzt sie auf 0 [1].

## Warum es helfen kann
Praktisch, wenn du Konten für andere anlegst oder mit neuen Konten testest: Die erste Anmeldung startet ohne die Animationsbildschirme.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Sie ändert nur die erste Anmeldung eines Kontos; kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Keine, außer dass die Animation fehlt.

## Wann du es nicht nutzen solltest
Wenn du die Begrüßungsanimation magst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowslogon
