# Windows-Apps: keine Anwesenheitserkennung

## Zusammenfassung
Windows-Apps (aus dem Store und andere App-Pakete) können die Anwesenheitserkennung nicht mehr nutzen, den Sensor, mit dem manche Notebooks und Monitore merken, ob du davor sitzt. Ab Pro.

## So funktioniert es
Manche PCs haben einen Anwesenheitssensor, der erkennt, ob jemand vor dem Bildschirm sitzt, und Apps können Zugriff darauf anfordern. Die Richtlinie LetAppsAccessHumanPresence legt fest, ob Windows-Apps auf die Anwesenheitserkennung zugreifen dürfen [1]. Die App setzt sie auf „Verweigern erzwingen“ (2): Windows-Apps können den Sensor dann nicht nutzen, und der passende Schalter unter Einstellungen > Datenschutz und Sicherheit ist gesperrt [1]. Eine App, die beim Ändern geöffnet ist, bemerkt es erst nach ihrem Neustart [1]. Auf einem PC ohne solchen Sensor ändert sich nichts, was du merken würdest.

## Warum es helfen kann
Apps, die nicht auf deine Anwesenheit achten sollen, kommen nicht an den Sensor heran, egal welche Berechtigungen sie bei der Installation anfordern.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]; Microsoft führt sie für Windows 11 Pro, Enterprise und Education [1]. Eine Datenschutzeinstellung ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Windows-Apps, die die Anwesenheitserkennung nutzen, etwa um zu pausieren, wenn du weggehst, tun das nicht mehr. Der Schalter in den Einstellungen bleibt gesperrt, bis du die Änderung rückgängig machst.

## Wann du es nicht nutzen solltest
Wenn eine Windows-App, die du nutzt, die Anwesenheitserkennung braucht.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
