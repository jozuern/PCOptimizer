# Windows-Suche: keine Webergebnisse und kein Standort

## Zusammenfassung
Die Windows-Suche sucht nicht mehr im Web, zeigt keine Webergebnisse und darf deinen Standort nicht nutzen. Pro, Enterprise und Education.

## So funktioniert es
Drei Richtlinien der Windows-Suche, die Microsoft zum Begrenzen von Verbindungen nennt: "Websuche nicht zulassen" (DisableWebSearch = 1), "Keine Websuche durchführen und keine Webergebnisse anzeigen" (ConnectedSearchUseWeb = 0) und "Suche darf Standort verwenden" auf 0 [1][2]. Sie ergänzen die Option "Keine Webergebnisse in der Windows-Suche", die die neuere Suchfeld-Richtlinie nutzt.

## Warum es helfen kann
Was du in die Suche tippst, bleibt auf dem PC, und die Suche kann deinen Standort nicht für Ergebnisse nutzen.

## Belege
Von Microsoft beschriebene Windows-Richtlinien [1][2]. Eine Datenschutzeinstellung ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Die Suche findet nur Apps, Einstellungen und Dateien auf diesem PC; für Webergebnisse öffnest du einen Browser.

## Wann du es nicht nutzen solltest
Wenn du Webergebnisse in der Windows-Suche nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search
