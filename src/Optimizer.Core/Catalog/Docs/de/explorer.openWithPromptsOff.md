# Öffnen mit: keine Store-Suche und keine Hinweise auf neue Apps

## Zusammenfassung
Entfernt "Im Store nach einer App suchen" aus dem Dialog Öffnen mit und beendet den Hinweis, dass eine neue App einen Dateityp öffnen kann.

## So funktioniert es
ShellNoUseStoreOpenWith (NoUseStoreOpenWith = 1) entfernt den Eintrag "Im Store nach einer App suchen" aus dem Dialog Öffnen mit für Dateitypen ohne App [1]. NoNewAppAlert = 1 entfernt die Benachrichtigung, dass eine neu installierte App einen Dateityp oder ein Protokoll öffnen kann [2].

## Warum es helfen kann
Weniger Hinweise nach dem Installieren von Apps und keine Store-Seite, wenn du aus Versehen eine unbekannte Datei öffnest.

## Belege
Von Microsoft beschriebene Windows-Richtlinien [1][2]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Für unbekannte Dateitypen wählst du selbst eine installierte App, und eine neue App meldet nicht, dass sie deine Dateien öffnen kann.

## Wann du es nicht nutzen solltest
Wenn du im Dialog Öffnen mit nach Apps im Store suchst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-icm
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer
