# Ausführliche Statusmeldungen beim Starten und Herunterfahren

## Zusammenfassung
Windows zeigt beim Starten, Herunterfahren, An- und Abmelden, was es gerade tut, etwa auf welchen Dienst es wartet, statt "Bitte warten".

## So funktioniert es
Die Richtlinie "Sehr ausführliche Statusmeldungen anzeigen" (VerboseStatus = 1) lässt das System sehr ausführliche Statusmeldungen anzeigen; Microsoft hat sie für fortgeschrittene Benutzer gedacht, die diese Informationen brauchen [1].

## Warum es helfen kann
Wenn Start oder Herunterfahren hängen, zeigt die Meldung, welcher Schritt so lange dauert.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Sie ändert nur den Text auf dem Bildschirm; kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Technische Meldungen statt der einfachen.

## Wann du es nicht nutzen solltest
Wenn die ausführlichen Meldungen andere Nutzer dieses PCs verwirren.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-logon
