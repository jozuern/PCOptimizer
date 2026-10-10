# Microsoft Edge: keine Browser-Anmeldung

## Zusammenfassung
Edge meldet sich nicht mehr bei einem Microsoft-Konto an, weder von selbst mit deinem Windows-Konto noch von Hand. Die Synchronisierung von Favoriten und Kennwörtern endet.

## So funktioniert es
BrowserSignin auf 0 sperrt die Anmeldung im Browser, Kontodienste wie Synchronisierung und einmaliges Anmelden sind dann nicht verfügbar [1]. ImplicitSignInEnabled auf aus verhindert, dass Edge dich anhand deiner Windows-Anmeldung automatisch anmeldet [2]. Beide gelten nach einem Neustart von Edge für alle Profile [1][2].

## Warum es helfen kann
Edge bleibt ein lokaler Browser ohne Microsoft-Konto, auch wenn Windows eines nutzt.

## Belege
Von Microsoft beschriebene Edge-Richtlinien [1][2]. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Nachteile & Risiken
Favoriten, Kennwörter, Verlauf und offene Tabs werden nicht mehr zwischen deinen Geräten synchronisiert, und Seiten mit Anmeldung über Edge fragen selbst nach deinen Daten. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du Edge zwischen Geräten synchronisierst oder dein Arbeitskonto in Edge nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/browsersignin
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/implicitsigninenabled
