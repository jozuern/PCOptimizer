# Microsoft Edge: kein Startboost, kein Hintergrundmodus

## Zusammenfassung
Verhindert, dass Microsoft Edge bei der Anmeldung startet und nach dem Schließen des letzten Fensters weiterläuft. Gibt Arbeitsspeicher und Prozessorzeit frei, wenn du Edge nicht nutzt.

## So funktioniert es
Zwei dokumentierte Edge-Richtlinien [1][2]: Der Startboost (StartupBoostEnabled) startet Edge-Prozesse bei der Anmeldung und nach dem Schließen des letzten Fensters im Hintergrund neu; der Hintergrundmodus (BackgroundModeEnabled) lässt Edge nach dem Schließen weiterlaufen. Die App schaltet beide aus. Edge übernimmt Richtlinienänderungen im laufenden Betrieb.

## Warum es helfen kann
Edge-Prozesse ohne offenes Fenster belegen Arbeitsspeicher und etwas Prozessorzeit, auch beim Spielen und im Akkubetrieb. Auf PCs mit wenig Arbeitsspeicher bleibt so mehr für die Apps, die du nutzt.

## Belege
Beide Richtlinien sind von Microsoft dokumentiert [1][2]. Ohne die Richtlinien kann der Startboost je nach Einrichtung an oder aus sein, der Hintergrundmodus ist aus, solange du ihn nicht eingeschaltet hast [1][2]. Wie viel Speicher die ruhenden Prozesse belegen, hängt von Erweiterungen und offenen Sitzungen ab.

## Nachteile & Risiken
Das erste Edge-Fenster nach der Anmeldung öffnet etwas langsamer. Erweiterungen und Web-Apps laufen bei geschlossenem Edge nicht mehr. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird; Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du Edge direkt nach jeder Anmeldung öffnest und es so schnell wie möglich willst, oder Erweiterungen nutzt, die bei geschlossenem Edge arbeiten.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/startupboostenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/backgroundmodeenabled
