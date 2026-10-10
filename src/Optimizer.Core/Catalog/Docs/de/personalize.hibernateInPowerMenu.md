# Ruhezustand im Ein/Aus-Menü

## Zusammenfassung
Fügt dem Ein/Aus-Menü in Start den Ruhezustand hinzu, wenn der PC ihn unterstützt.

## So funktioniert es
Die Richtlinie ShowHibernateOption = 1 zeigt den Ruhezustand im Menü der Energieoptionen, sofern die Hardware ihn unterstützt [1]. Ohne die Richtlinie wählst du das in den Energieoptionen der Systemsteuerung [1].

## Warum es helfen kann
Der Ruhezustand speichert offene Apps auf dem Datenträger und schaltet den PC ganz aus, anders als der Energiesparmodus braucht er dann keinen Strom.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Die Auswahl in der Systemsteuerung ist gesperrt. Der Eintrag erscheint nur, wo die Hardware den Ruhezustand unterstützt [1].

## Wann du es nicht nutzen solltest
Wenn du den Ruhezustand nie nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsexplorer
