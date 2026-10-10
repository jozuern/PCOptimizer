# Windows-Apps: keine ungekoppelten oder vertrauenswürdigen Geräte

## Zusammenfassung
Windows-Apps (Apps aus dem Store und andere App-Pakete) können nicht mehr ungekoppelte oder vertrauenswürdige Geräte nutzen. Desktopprogramme sind nicht betroffen. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinien LetAppsSyncWithDevices und LetAppsAccessTrustedDevices auf Verweigern erzwingen (2) hindern Windows-Apps daran, mit ungekoppelten Funkgeräten zu kommunizieren und auf vertrauenswürdige Geräte zuzugreifen, und der passende Schalter unter Einstellungen > Datenschutz und Sicherheit ist gesperrt [1][2]. Desktopprogramme wie die meisten Spiele, Launcher und Browser fallen nicht unter diese Richtlinie. Eine App, die beim Ändern geöffnet ist, bemerkt es erst nach ihrem Neustart [1].

## Warum es helfen kann
Apps, die diese Funktion nicht nutzen sollen, können es nicht, egal was sie bei der Installation anfragen.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]; Microsoft führt sie für Pro, Enterprise und Education [1] und nennt sie in seiner Anleitung zu Windows-Verbindungen [2]. Eine Datenschutzeinstellung ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Store-Apps, die Funkgeräte einrichten, etwa manche Smart-Home- oder Fitness-Apps, erreichen sie nicht mehr. Der Schalter in den Einstellungen bleibt gesperrt, bis du die Änderung rückgängig machst.

## Wann du es nicht nutzen solltest
Wenn eine Windows-App, die du nutzt, ungekoppelte oder vertrauenswürdige Geräte nutzen muss.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-privacy
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
