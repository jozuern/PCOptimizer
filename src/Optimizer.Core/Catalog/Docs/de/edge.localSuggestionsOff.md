# Microsoft Edge: keine Vorschläge aus Verlauf und Favoriten

## Zusammenfassung
Die Adressleiste schlägt keine Seiten aus deinem Verlauf und deinen Favoriten mehr vor. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Die Richtlinie LocalProvidersEnabled auf aus schaltet Vorschläge aus lokalen Quellen wie Verlauf und Favoriten in der Adressleiste aus [1]. Edge übernimmt sie nach einem Neustart [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Wer deinen PC mitbenutzt, sieht beim Tippen einer Adresse nicht mehr deine besuchten Seiten.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1]. Die Vorschläge stammen aus Daten auf diesem PC; es ändert sich also, was andere am Bildschirm sehen, nicht was den PC verlässt. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Nachteile & Risiken
Der schnelle Weg zu früher besuchten Seiten fällt weg. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn du Seiten öffnest, indem du ein paar Buchstaben ihres Namens tippst.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/localprovidersenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
