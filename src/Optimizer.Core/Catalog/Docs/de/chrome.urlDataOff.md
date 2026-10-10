# Google Chrome: keine URL-bezogene Datenerfassung

## Zusammenfassung
Chrome schickt die Adressen besuchter Seiten nicht mehr an Google, um Suche und Surfen zu verbessern.

## So funktioniert es
Mit der Richtlinie UrlKeyedAnonymizedDataCollectionEnabled auf aus erfasst Chrome keine anonymisierten URL-bezogenen Daten mehr, bei denen sonst Adressen besuchter Seiten an Google gehen [1]. Chrome liest die Richtlinie aus HKLM\SOFTWARE\Policies\Google\Chrome, auch auf PCs ohne Domäne [2].

## Warum es helfen kann
Die Seiten, die du öffnest, werden Google dafür nicht gemeldet. Bildrate und Latenz ändern sich nicht.

## Belege
Eine von Google beschriebene Chrome-Richtlinie [1].

## Nachteile & Risiken
Die Chrome-Einstellung "Suchanfragen und Surfen verbessern" ist aus und gesperrt. Weil es Richtlinien sind, zeigt Chrome an, dass es von deiner Organisation verwaltet wird, und die passenden Einstellungen sind gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du diese Daten mit Google teilen willst.

## Quellen
1. https://chromeenterprise.google/policies/#UrlKeyedAnonymizedDataCollectionEnabled
2. https://support.google.com/chrome/a/answer/9131254?hl=en
