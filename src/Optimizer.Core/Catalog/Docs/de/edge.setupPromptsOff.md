# Microsoft Edge: keine Einrichtungs- und Standardbrowser-Hinweise

## Zusammenfassung
Überspringt die Einrichtungsseiten von Edge, das Angebot, bei jedem Start Daten aus einem anderen Browser zu übernehmen, und die Bitten, Edge und Bing als Standard zu setzen.

## So funktioniert es
Drei Edge-Richtlinien: HideFirstRunExperience blendet die Einrichtung beim ersten Start und den Begrüßungsbildschirm aus [1]; ImportOnEachLaunch auf aus beendet das Angebot, beim Start Daten aus dem Standardbrowser zu übernehmen [2]; DefaultBrowserSettingsCampaignEnabled auf aus beendet die Bitten, Edge als Standardbrowser und Bing als Standardsuche zu setzen [3]. Alle drei gelten auch für Profile mit Microsoft-Konto [1][2][3].

## Warum es helfen kann
Weniger Unterbrechungen beim Öffnen von Edge, etwa nach einer frischen Windows-Installation oder einem Edge-Update.

## Belege
Von Microsoft beschriebene Edge-Richtlinien [1][2][3]. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz. Die ersten beiden greifen nach einem Neustart von Edge [1][2].

## Nachteile & Risiken
Edge führt dich nicht mehr durch seine Einrichtung; Anmeldung, Synchronisierung und Import wählst du selbst in den Edge-Einstellungen. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du die Einrichtungsseiten von Edge willst, etwa um Favoriten aus einem anderen Browser zu übernehmen.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hidefirstrunexperience
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/importoneachlaunch
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/defaultbrowsersettingscampaignenabled
