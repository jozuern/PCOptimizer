# Programmumleitung (Image File Execution Options, Experte)

## Zusammenfassung
Entfernt eine „Debugger“-Umleitung, die Windows bei jedem Start dieses Programms ein anderes Programm starten lässt.

## So funktioniert es
Unter Image File Execution Options kann ein Debugger eingetragen sein, den Windows statt des Programms startet: Microsoft dokumentiert dafür den Wert Debugger unter Image File Execution Options\<Programmname> [2][3], und Autoruns listet solche Einträge [1]. Tools wie Process Explorer nutzen das bewusst, um den Task-Manager zu ersetzen; Schadsoftware nutzt es, um Programme zu blockieren oder zu kapern. Die App löscht nur den Wert Debugger. Rückgängig machen schreibt ihn zurück.

## Warum es helfen kann
War die Umleitung nicht gewollt, startet das ursprüngliche Programm wieder normal.

## Belege
Kein Einfluss auf Spiele, außer ein Spiel oder Launcher wurde umgeleitet.

## Nachteile & Risiken
Ein bewusst eingerichteter Ersatz (zum Beispiel Process Explorer statt Task-Manager) funktioniert nicht mehr.

## Wann du es nicht nutzen solltest
Wenn du die Umleitung selbst eingerichtet hast.

## Quellen
1. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-details
3. https://learn.microsoft.com/en-us/windows/win32/services/debugging-a-service
