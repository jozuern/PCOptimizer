# Microsoft Edge: keine Netzwerkvorhersage

## Zusammenfassung
Edge verbindet und lädt Seiten, die du als Nächstes öffnen könntest, nicht mehr vorab. Seiten öffnen evtl. etwas langsamer. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Die Richtlinie NetworkPredictionOptions steuert DNS-Vorabauflösung, TCP- und SSL-Vorverbindungen und das Vorladen von Webseiten [1]. Die App setzt 2, keine Netzwerkvorhersage auf keiner Verbindung [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Edge baut keine Verbindungen zu Seiten auf, nur weil ein Link darauf auf der Seite steht. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1].

## Nachteile & Risiken
Seiten aus Links oder Suchergebnissen können etwas später zu laden beginnen. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn dir das Ladetempo wichtiger ist als diese zusätzlichen Verbindungen.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/networkpredictionoptions
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
