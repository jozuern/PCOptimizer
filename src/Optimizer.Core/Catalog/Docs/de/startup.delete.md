# Autostart-Eintrag löschen

## Zusammenfassung
Löscht diesen Autostart-Wert aus der Registry, wie Autoruns. Tunero sichert ihn vorher, damit die Seite Änderungen ihn zurückschreiben kann.

## So funktioniert es
Windows führt die Befehlszeilen in den Run-Schlüsseln bei jeder Anmeldung aus, die in den RunOnce-Schlüsseln einmal [1]. Ein Run-Schlüssel aus einer Gruppenrichtlinie und der Wert AutoRun des Command Processor (läuft bei jedem Start von cmd.exe) funktionieren genauso. Ausschalten behält den Eintrag und markiert ihn als deaktiviert; Löschen entfernt den Wert selbst, so wie Autoruns [2]. Vorher speichert Tunero den Wert mit seinem Typ, damit Rückgängig ihn genau so zurückschreibt.

## Warum es helfen kann
Ein Eintrag, dessen Programm deinstalliert wurde, bleibt in der Liste und zeigt auf eine Datei, die es nicht mehr gibt; Löschen räumt die Liste auf. RunOnce-, Richtlinien-Run- und AutoRun-Einträge lassen sich auf dieser Seite nicht ausschalten, Löschen ist hier also der einzige Weg, sie zu stoppen.

## Belege
Nichts Messbares: Ein Wert, der auf eine fehlende Datei zeigt, kostet bei der Anmeldung fast nichts. Bei Einträgen, die noch ein Programm starten, ist der Effekt derselbe wie beim Ausschalten.

## Nachteile & Risiken
Das Programm startet nicht mehr von selbst, und seine eigenen Einstellungen schreiben den Eintrag beim nächsten Ändern vielleicht wieder. Ein RunOnce-Eintrag, den ein Installer für die nächste Anmeldung hinterlassen hat (um ein Update abzuschließen), läuft nicht; stelle ihn auf der Seite Änderungen wieder her, wenn das Programm danach Probleme macht.

## Wann du es nicht nutzen solltest
Bei Einträgen, die du nur pausieren willst: Schalte sie stattdessen aus, dann bleiben sie in der Liste. Bei RunOnce-Einträgen eines Installers, den du gerade ausgeführt hast: Lass ihn zuerst mit einem Neustart fertig werden.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys
2. https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns
