# Ortungsdienst für alles aus

## Zusammenfassung
Schaltet den Windows-Ortungsdienst für alle Apps, die Suche und Windows selbst aus und sperrt die Standorteinstellungen. Strenger als nur Apps den Zugriff zu verweigern. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie "Standort deaktivieren" (DisableLocation = 1) entspricht Standort erzwungen aus: Alle Standort-Datenschutzeinstellungen sind aus und ausgegraut, und keine App darf den Ortungsdienst nutzen, auch die Suche nicht [1][2]. Zurück auf Benutzersteuerung bekommt jede App ihre frühere Einstellung zurück [1].

## Warum es helfen kann
Keine App und keine Windows-Funktion kann lesen, wo der PC steht. Die Option "Standortzugriff für Apps aus" ändert nur den Standard für Apps; diese Richtlinie gilt auch für Windows-Funktionen und lässt sich in den Einstellungen nicht übergehen.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1][2]. Eine Datenschutzeinstellung ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Apps und Windows-Funktionen, die deinen Standort nutzen, etwa Karten, Wetter und Mein Gerät suchen, bekommen ihn nicht mehr. Rückgängig gibt die Steuerung an die Einstellungen zurück [1].

## Wann du es nicht nutzen solltest
Wenn du den Standort in Karten, Wetter oder Mein Gerät suchen nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
