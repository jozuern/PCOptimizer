# Microsoft Edge: strikte Tracking-Verhinderung

## Zusammenfassung
Setzt die Tracking-Verhinderung auf Streng: blockiert die meisten Tracker aller Seiten, manche Seiten funktionieren dann nicht richtig. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Die Richtlinie TrackingPrevention hat vier Stufen: 0 aus, 1 einfach, 2 ausgewogen und 3 streng [1]. Ausgewogen blockiert schädliche Tracker und Tracker von Seiten, die du nicht besucht hast; streng blockiert schädliche Tracker und die meisten Tracker aller Seiten, und laut Microsoft funktionieren dann Teile mancher Seiten nicht [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Weniger Tracker können dir von Seite zu Seite folgen. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1].

## Nachteile & Risiken
Eingebettete Videos, Anmeldeknöpfe oder Kommentarbereiche anderer Seiten können ausfallen; die Stufe lässt sich in den Edge-Einstellungen nicht mehr senken. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn Seiten, die du brauchst, mit strenger Blockierung ausfallen; ausgewogen ist die sicherere Wahl.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/trackingprevention
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
