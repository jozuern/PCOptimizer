# Keine Treiber über Windows Update

## Zusammenfassung
Windows Update installiert mit Qualitätsupdates keine Treiber mehr, ein selbst installierter Grafiktreiber wird also nicht ersetzt. Alle Treiber aktualisierst du dann selbst.

## So funktioniert es
Standardmäßig bietet Windows Update auch Updates der Klasse „Treiber“ an. Die App schaltet die dokumentierte Richtlinie „Keine Treiber in Windows-Updates einschließen“ ein (Computerkonfiguration > Windows-Komponenten > Windows Update > Vom Windows Update angebotene Updates verwalten): ExcludeWUDriversInQualityUpdate = 1 im Richtlinienschlüssel von Windows Update [1]. Windows liest sie bei der nächsten Updatesuche.

## Warum es helfen kann
Wenn du Grafiktreiber selbst von NVIDIA, AMD oder Intel installierst, kann Windows Update sie nicht mehr durch eine ältere oder andere Version ersetzen. Der Treiber, den du gewählt hast, bleibt samt Einstellungen erhalten.

## Belege
Microsoft dokumentiert, was die Richtlinie bewirkt, aber keinen Effekt auf Spiele [1]. Der Nutzen ist Planbarkeit: Der Treiber ändert sich nur, wenn du ihn änderst.

## Nachteile & Risiken
Sie gilt für alle Treiber, nicht nur für den Grafiktreiber: Auch Chipsatz-, Netzwerk-, Audio- und andere Treiberupdates über Windows Update fallen weg, und du installierst sie von den Herstellerseiten (die Seite Apps & Treiber hilft dabei). Am stärksten betroffen sind Laptops, die ihre Treiber über Windows Update bekommen. Microsoft führt die Richtlinie für Pro, Enterprise und Education, deshalb bietet die App sie unter Home nicht an.

## Wann du es nicht nutzen solltest
Wenn du Treiber nicht selbst aktualisierst, oder auf einem Laptop, dessen Hersteller Treiber über Windows Update liefert.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-update
