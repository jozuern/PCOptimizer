# Microsoft Edge: keine Webdienste für Fehlerseiten

## Zusammenfassung
Bei Seiten, die nicht laden, fragt Edge keine Microsoft-Webdienste mehr nach ähnlichen Seiten oder einer Verbindungsprüfung. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
AlternateErrorPagesEnabled auf aus verhindert, dass Edge ähnliche Seiten vorschlägt, wenn eine Webseite nicht gefunden wird [1]. ResolveNavigationErrorsUseWebService auf aus lässt Edge die Verbindung mit Windows-eigenen Schnittstellen prüfen statt mit einer datenlosen Verbindung zu einem Webdienst, etwa im WLAN von Hotels und Flughäfen [2]. Seit Edge 116 wendet Edge diese Richtlinien laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [3]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Weniger Anfragen an Microsoft zu Adressen, die nicht funktioniert haben. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Von Microsoft beschriebene Edge-Richtlinien [1][2].

## Nachteile & Risiken
Edge erkennt Anmeldeseiten im WLAN von Hotels oder Flughäfen eventuell schlechter und bietet für vertippte Adressen keine Alternative mehr an. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du oft öffentliches WLAN mit Anmeldeseite nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/alternateerrorpagesenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/resolvenavigationerrorsusewebservice
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
