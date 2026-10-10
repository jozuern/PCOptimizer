# Microsoft Edge: keine Shopping-Funktionen

## Zusammenfassung
Schaltet Preisvergleich, Gutscheine, Cashback und Express-Checkout auf Shopping-Seiten aus. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Mit der Richtlinie EdgeShoppingAssistantEnabled an (Standard) wendet Edge Shopping-Funktionen auf Händlerseiten automatisch an und holt Gutscheine und Preise anderer Händler von einem Server [1]. Auf aus sucht Edge keine Preisvergleiche, Gutscheine, Cashback-Angebote oder Express-Checkout mehr [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Edge schickt die Shops, die du besuchst, nicht mehr an seinen Shopping-Dienst und zeigt auf Shopping-Seiten weniger Einblendungen. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1]; Edge übernimmt sie ohne Neustart [1].

## Nachteile & Risiken
Automatische Gutscheine und Preisvergleiche fallen weg. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn du die Gutscheine oder den Preisvergleich in Edge nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/edgeshoppingassistantenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
