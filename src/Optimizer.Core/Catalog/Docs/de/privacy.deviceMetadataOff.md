# Kein automatischer Download von Geräte-Apps

## Zusammenfassung
Windows lädt die Hersteller-Apps zu angeschlossenen Geräten, etwa Maus- oder Headset-Software, nicht mehr automatisch. Treiber werden weiter installiert. Keine Wirkung auf die Leistung.

## So funktioniert es
Wenn du ein Gerät anschließt, kann Windows Anwendungen herunterladen, die zu den Metadaten des Geräts gehören. Die App schaltet die dokumentierte Richtlinie „Automatischen Download von Anwendungen verhindern, die Gerätemetadaten zugeordnet sind“ ein (Computerkonfiguration > System > Geräteinstallation): PreventDeviceMetadataFromNetwork = 1 [1]. Sie hat Vorrang vor der Einstellung im Dialog Geräteinstallationseinstellungen.

## Warum es helfen kann
Hersteller-Apps, die du nicht angefordert hast, werden nicht im Hintergrund installiert. Die Werkzeuge, die du willst, installierst du selbst.

## Belege
Microsoft dokumentiert, dass Windows diese Anwendungen dann nicht herunterlädt [1]. Es gibt keine Messung einer Wirkung auf Spiele, und es ist auch keine zu erwarten.

## Nachteile & Risiken
Eine neue Maus, Tastatur oder ein Headset bekommt die Hersteller-App nicht automatisch. Installiere sie selbst, wenn du ihre Funktionen brauchst (Beleuchtung, Tastenbelegung, Equalizer). Microsoft führt die Richtlinie für Pro, Enterprise und Education, deshalb bietet die App sie unter Home nicht an.

## Wann du es nicht nutzen solltest
Wenn sich Hersteller-Apps beim Anschließen eines Geräts selbst installieren sollen.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deviceinstallation
