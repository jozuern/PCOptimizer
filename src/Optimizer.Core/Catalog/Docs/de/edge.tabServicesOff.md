# Microsoft Edge: kein Dienst zum Ordnen von Tabs

## Zusammenfassung
Edge schickt keine URLs und Titel deiner Tabs mehr an seinen Dienst, der Tabgruppen und Gruppennamen vorschlägt. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Wenn du eine Tabgruppe anlegst oder bestimmte Funktionen zum Gruppieren ähnlicher Tabs nutzt, schickt Edge URLs, Seitentitel und bestehende Gruppen an seinen Dienst zum Ordnen von Tabs [1]. Mit der Richtlinie TabServicesEnabled auf aus werden keine Daten gesendet, und diese Vorschläge fallen weg [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Deine offenen Tabs bleiben auf dem PC. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1].

## Nachteile & Risiken
Keine automatischen Gruppennamen und keine Vorschläge zum Gruppieren ähnlicher Tabs. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn du Edge deine Tabs gruppieren lässt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/tabservicesenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
