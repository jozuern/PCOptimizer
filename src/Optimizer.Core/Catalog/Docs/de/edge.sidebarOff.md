# Microsoft Edge: keine Seitenleiste

## Zusammenfassung
Blendet die Seitenleiste, die Startleiste am rechten Rand von Edge, dauerhaft aus. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Die Richtlinie HubsSidebarEnabled auf aus bedeutet, dass die Seitenleiste nie angezeigt wird [1]. Seit Edge 141 hat die Copilot-Schaltfläche in der Symbolleiste eine eigene Richtlinie für Arbeitskonten, diese hier steuert sie nicht [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Mehr Platz für Webseiten und ein Bereich weniger, der im Hintergrund Webinhalte lädt. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1]; Edge übernimmt sie ohne Neustart [1].

## Nachteile & Risiken
Apps und Werkzeuge, die du an die Seitenleiste angeheftet hast, sind dort nicht mehr erreichbar. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn du Apps oder Werkzeuge in der Edge-Seitenleiste nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/hubssidebarenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
