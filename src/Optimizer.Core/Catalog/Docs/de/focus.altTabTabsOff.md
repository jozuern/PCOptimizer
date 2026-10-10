# Alt+Tab: nur geöffnete Fenster

## Zusammenfassung
Alt+Tab zeigt nur geöffnete Fenster, ohne Browser-Tabs aus Microsoft Edge. Ab Pro; Microsoft führt die Richtlinie als Vorschau.

## So funktioniert es
Die Richtlinie BrowserAltTabBlowout steuert, ob App-Tabs in Alt+Tab erscheinen [1]. Der Wert "Nur geöffnete Fenster" schaltet die Funktion ab [1]. Die App setzt MultiTaskingAltTabFilter im Richtlinienschlüssel des Benutzers auf 4. Microsoft weist darauf hin, dass die Richtlinie eine Vorschau für Tests ist [1], daher ist auch die Option eine Vorschau.

## Warum es helfen kann
Der Wechsel zwischen Spiel und anderen Fenstern mit Alt+Tab geht schneller, wenn keine Edge-Tabs die Liste füllen.

## Belege
Dokumentiert für Pro, Enterprise und Education ab Windows 11 21H2 [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Edge-Tabs erscheinen nicht mehr einzeln in Alt+Tab; Tabs wechselst du in Edge selbst.

## Wann du es nicht nutzen solltest
Wenn du mit Alt+Tab zwischen Edge-Tabs springst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-multitasking
