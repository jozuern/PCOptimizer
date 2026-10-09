# Dienst eines anderen Herstellers beim Start (Experte)

## Zusammenfassung
Lässt diesen Dienst eines anderen Herstellers bei Bedarf (Manuell) statt mit Windows starten.

## So funktioniert es
Der Starttyp wird im Dienststeuerungs-Manager auf Manuell gestellt [1]; der Dienst wird nicht deaktiviert, sein Programm kann ihn weiterhin starten. Rückgängig machen stellt den vorherigen Starttyp wieder her.

## Warum es helfen kann
Programme, die mit Windows starten, machen die Anmeldung langsamer und belegen Arbeitsspeicher und manchmal Prozessorzeit im Hintergrund.

## Belege
Die Anmeldung wird schneller; der Effekt auf die Bildrate hängt davon ab, was das Programm im Hintergrund tut.

## Nachteile & Risiken
Funktionen des Programms, die den Dienst direkt nach dem Start brauchen (zum Beispiel Updateprüfung oder Geräteerkennung), starten eventuell später oder gar nicht.

## Wann du es nicht nutzen solltest
Bei Anti-Cheat-, Sicherheits-, VPN- und Treiberdiensten.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/services/service-startup
