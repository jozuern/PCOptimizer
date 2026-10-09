# Autostart-Programm (Run-Schlüssel)

## Zusammenfassung
Schaltet dieses Programm bei der Anmeldung ein oder aus, wie der Task-Manager. Das Programm bleibt installiert.

## So funktioniert es
Windows startet Programme aus den Run-Schlüsseln der Registry bei der Anmeldung. Die App löscht den Eintrag nicht, sondern markiert ihn in StartupApproved als deaktiviert, genau wie die Seite Autostart-Apps im Task-Manager und in den Einstellungen [1]. Rückgängig machen stellt den vorherigen Zustand wieder her.

## Warum es helfen kann
Programme, die mit Windows starten, machen die Anmeldung langsamer und belegen Arbeitsspeicher und manchmal Prozessorzeit im Hintergrund.

## Belege
Die Anmeldung wird schneller; der Effekt auf die Bildrate hängt davon ab, was das Programm im Hintergrund tut.

## Nachteile & Risiken
Das Programm startet nicht mehr von selbst; öffne es bei Bedarf über das Startmenü.

## Wann du es nicht nutzen solltest
Bei Hilfsprogrammen von Treibern, die du brauchst (Audio, Touchpad, Controller-Software).

## Quellen
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
