# Datei-Explorer: Benutzerordner in Dieser PC

## Zusammenfassung
Zeigt Desktop, Dokumente, Downloads, Musik, Bilder und Videos wieder in Dieser PC. Nicht dokumentierte Werte, getestet.

## So funktioniert es
Die Benutzerordner haben Einträge unter MyComputer\NameSpace, und HiddenByDefault entscheidet, ob Dieser PC sie zeigt. Die App setzt HiddenByDefault = 0 für die sechs Ordner, den Wert, mit dem Win11Debloat sie wieder einblendet [1]. Die Änderung gilt für alle Konten. Microsoft dokumentiert diese Werte nicht.

## Warum es helfen kann
Deine Ordner sind in Dieser PC einen Klick entfernt, wie unter Windows 10.

## Belege
In einem VM-Test mit Windows 11 Build 26300 zeigte Dieser PC vorher nur die Laufwerke, nach dem Anwenden die sechs Ordner darüber und nach dem Rückgängigmachen wieder nur die Laufwerke. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Dieser PC wird länger. Im Navigationsbereich bleiben die Ordner so oder so.

## Wann du es nicht nutzen solltest
Wenn dir Dieser PC nur mit Laufwerken lieber ist.

## Quellen
1. https://github.com/Raphire/Win11Debloat/blob/master/Regfiles/Add_All_Folders_Under_This_PC.reg
