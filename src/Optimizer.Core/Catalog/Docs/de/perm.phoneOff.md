# Windows-Apps: keine Telefonanrufe

## Zusammenfassung
Windows-Apps (Apps aus dem Store und andere App-Pakete) können nicht mehr Telefonanrufe tätigen. Desktopprogramme sind nicht betroffen. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie LetAppsAccessPhone auf Verweigern erzwingen (2) hindert Windows-Apps daran, Telefonanrufe zu tätigen, und der passende Schalter unter Einstellungen > Datenschutz und Sicherheit ist gesperrt [1][2]. Desktopprogramme wie die meisten Spiele, Launcher und Browser fallen nicht unter diese Richtlinie. Eine App, die beim Ändern geöffnet ist, bemerkt es erst nach ihrem Neustart [1].

## Warum es helfen kann
Apps, die diese Funktion nicht nutzen sollen, können es nicht, egal was sie bei der Installation anfragen.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]; Microsoft führt sie für Pro, Enterprise und Education [1] und nennt sie in seiner Anleitung zu Windows-Verbindungen [2]. Eine Datenschutzeinstellung ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Store-Apps können keine Anrufe mehr über ein verbundenes Telefon oder ein Mobilfunkmodem führen. Der Schalter in den Einstellungen bleibt gesperrt, bis du die Änderung rückgängig machst.

## Wann du es nicht nutzen solltest
Wenn eine Windows-App, die du nutzt, Telefonanrufe tätigen muss.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
