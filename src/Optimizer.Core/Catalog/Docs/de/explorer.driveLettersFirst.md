# Laufwerksbuchstaben zuerst

## Zusammenfassung
Dieser PC und der Navigationsbereich zeigen „(C:) Lokaler Datenträger“ statt „Lokaler Datenträger (C:)“. Der Wert ist nicht dokumentiert; ein VM-Test hat ihn bestätigt.

## So funktioniert es
Der Explorer benennt Laufwerke mit der Bezeichnung zuerst und dem Buchstaben in Klammern dahinter. Der Wert ShowDriveLettersFirst unter HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer auf 4 stellt den Buchstaben bei allen Laufwerken nach vorn [1]. Microsoft hat diesen Wert nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat ihn bestätigt: Nach einem Neustart des Explorers zeigte Dieser PC „(C:) Lokaler Datenträger“, nach dem Entfernen des Werts wieder „Lokaler Datenträger (C:)“. Der Explorer übernimmt die Änderung beim nächsten Start: Nutze „Explorer neu starten“ auf der Seite Tools oder melde dich ab und wieder an.

## Warum es helfen kann
Bei mehreren Laufwerken stehen die Buchstaben am Anfang jedes Namens untereinander und sind schneller zu finden.

## Belege
Keine Auswirkung auf die Leistung; nur die Reihenfolge von Buchstabe und Bezeichnung ändert sich.

## Nachteile & Risiken
Nur das veränderte Aussehen. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn dir die Bezeichnung vorn lieber ist.

## Quellen
1. https://winaero.com/show-drive-letters-before-drive-names-in-this-pc-computer-folder/
