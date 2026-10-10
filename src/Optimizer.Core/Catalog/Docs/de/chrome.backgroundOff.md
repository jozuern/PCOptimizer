# Google Chrome: kein Hintergrundmodus

## Zusammenfassung
Chrome startet nicht mehr bei der Anmeldung und beendet sich, wenn du das letzte Fenster schließt. Gibt Arbeitsspeicher frei, wenn du Chrome nicht nutzt.

## So funktioniert es
Im Hintergrundmodus startet ein Chrome-Prozess bei der Windows-Anmeldung und läuft nach dem Schließen des letzten Fensters weiter, damit Hintergrund-Apps und die Sitzung aktiv bleiben [1]. Mit der Richtlinie BackgroundModeEnabled auf aus ist der Hintergrundmodus aus und lässt sich nicht einschalten [1]. Chrome liest die Richtlinie aus HKLM\SOFTWARE\Policies\Google\Chrome, auch auf PCs ohne Domäne [2].

## Warum es helfen kann
Ein Chrome-Prozess ohne Fenster belegt Arbeitsspeicher und etwas Prozessorzeit, auch beim Spielen und im Akkubetrieb.

## Belege
Eine von Google beschriebene Chrome-Richtlinie [1]. Ohne die Richtlinie ist der Hintergrundmodus zunächst aus, lässt sich aber in Chrome einschalten [1]; wie viel Speicher der Prozess belegt, hängt von Apps und Erweiterungen ab.

## Nachteile & Risiken
Chrome-Apps und Erweiterungen laufen bei geschlossenem Chrome nicht. Weil es Richtlinien sind, zeigt Chrome an, dass es von deiner Organisation verwaltet wird, und die passenden Einstellungen sind gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du Chrome-Apps oder Benachrichtigungen bei geschlossenem Chrome brauchst.

## Quellen
1. https://chromeenterprise.google/policies/#BackgroundModeEnabled
2. https://support.google.com/chrome/a/answer/9131254?hl=en
