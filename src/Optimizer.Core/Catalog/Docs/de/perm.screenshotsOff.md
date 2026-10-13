# Windows-Apps: keine Screenshots anderer Fenster

## Zusammenfassung
Windows-Apps (Store und andere App-Pakete) können andere Fenster oder den Bildschirm nicht mehr selbst aufnehmen und den Aufnahmerahmen nicht ausblenden. Desktopprogramme betrifft das nicht. Ab Pro.

## So funktioniert es
Windows erlaubt Apps, das Bild anderer Fenster oder ganzer Bildschirme aufzunehmen, und eine App kann den Rahmen ausblenden, der eine laufende Aufnahme zeigt. Zwei Richtlinien regeln das für Windows-Apps: LetAppsAccessGraphicsCaptureProgrammatic legt fest, ob Windows-Apps Screenshots verschiedener Fenster oder Bildschirme machen dürfen, und LetAppsAccessGraphicsCaptureWithoutBorder, ob sie den Screenshot-Rahmen ausschalten dürfen [1]. Die App setzt beide auf „Verweigern erzwingen“ (2): Windows-Apps können dann beides nicht, und die passenden Schalter unter Einstellungen > Datenschutz und Sicherheit sind gesperrt [1]. Eine App, die beim Ändern geöffnet ist, bemerkt es erst nach ihrem Neustart [1]. Desktopprogramme wie die meisten Spiele, Launcher, OBS und Browser fallen nicht unter diese Richtlinien.

## Warum es helfen kann
Eine installierte Windows-App kann nicht mehr aufnehmen, was andere Fenster zeigen, ohne dass du es startest, und eine Aufnahme läuft nie mehr ohne ihren Rahmen.

## Belege
Zwei von Microsoft beschriebene Windows-Richtlinien [1]; Microsoft führt sie für Windows 11 Pro, Enterprise und Education [1]. Eine Datenschutzeinstellung ohne Einfluss auf Framerate oder Latenz. Sie bleibt eine Vorschau, bis ein Test gezeigt hat, dass die eigenen Aufnahmewerkzeuge von Windows wie das Snipping Tool und die Xbox Game Bar weiter funktionieren.

## Nachteile & Risiken
Windows-Apps, die für dich den Bildschirm aufnehmen, etwa Screenshot- oder Meeting-Apps aus dem Store, können das nicht mehr. Die Schalter in den Einstellungen bleiben gesperrt, bis du die Änderung rückgängig machst.

## Wann du es nicht nutzen solltest
Wenn du mit einer Windows-App aus dem Store Screenshots machst oder deinen Bildschirm teilst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
