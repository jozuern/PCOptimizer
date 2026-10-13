# Start ohne Empfohlen

## Zusammenfassung
Entfernt den Bereich Empfohlen (zuletzt geöffnete Dateien, neue Apps, Tipps) aus dem Startmenü; es zeigt dann nur angeheftete Apps und die App-Liste. Dokumentierte Richtlinie ab Pro.

## So funktioniert es
Das Startmenü zeigt unter den angehefteten Apps zuletzt geöffnete Dateien und neu installierte Apps. Die Richtlinie HideRecommendedSection auf 1 entfernt diesen Bereich [1]. Microsoft führt sie für Pro, Enterprise und Education ab Windows 11 22H2 [1]. Ein Test in einer virtuellen Maschine mit Windows 11 Pro hat das bestätigt: Mit der Richtlinie zeigte das Startmenü keinen Bereich Zuletzt verwendet, ohne sie war er wieder da.

## Warum es helfen kann
Das Startmenü ist aufgeräumter, und zuletzt geöffnete Dateien erscheinen dort nicht mehr für jeden, der auf deinen Bildschirm schaut.

## Belege
Eine dokumentierte Windows-Richtlinie [1], in einer VM unter Windows 11 Pro bestätigt. Keine Auswirkung auf die Leistung.

## Nachteile & Risiken
Zuletzt geöffnete Dateien und neue Apps sind im Startmenü nicht mehr mit einem Klick erreichbar. Das Startmenü kann einen Hinweis zeigen, dass einige Einstellungen von deiner Organisation verwaltet werden.

## Wann du es nicht nutzen solltest
Wenn du zuletzt verwendete Dateien über das Startmenü öffnest.

## Quellen
1. https://learn.microsoft.com/de-de/windows/client-management/mdm/policy-csp-start
