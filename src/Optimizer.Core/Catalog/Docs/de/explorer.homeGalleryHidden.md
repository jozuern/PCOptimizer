# Datei-Explorer: kein Start und kein Katalog

## Zusammenfassung
Entfernt Start und Katalog aus dem Navigationsbereich des Datei-Explorers für dein Konto. Nicht dokumentierte Werte, getestet.

## So funktioniert es
Start und Katalog sind Shell-Ordner, die im Navigationsbereich angeheftet sind. Eine Kopie ihrer Klassenschlüssel für dein Konto mit System.IsPinnedToNameSpaceTree = 0 löst sie dort [1][2]. Microsoft dokumentiert diese Werte nicht.

## Warum es helfen kann
Ein kürzerer Navigationsbereich ohne Einträge, die du nicht nutzt.

## Belege
In einem VM-Test mit Windows 11 Build 26300 zeigte ein neues Explorer-Fenster nach dem Anwenden weder Start noch Katalog und nach dem Rückgängigmachen beide wieder. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Wenn der Datei-Explorer mit Start öffnet, stell auch „Explorer öffnet ‚Dieser PC‘“ ein. Zuletzt genutzte Dateien und Fotos bleiben in ihren Ordnern.

## Wann du es nicht nutzen solltest
Wenn du Start oder Katalog nutzt.

## Quellen
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Hide_Home_from_Explorer.reg
2. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Hide_Gallery_from_Explorer.reg
