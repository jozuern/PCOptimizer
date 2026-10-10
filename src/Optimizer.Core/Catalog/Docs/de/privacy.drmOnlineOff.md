# Windows Media DRM: kein Internetzugriff

## Zusammenfassung
Das alte Windows Media Digital Rights Management kann nicht mehr online gehen, um Lizenzen und Sicherheitsupdates zu holen. Streamingdienste und Spiele sind nicht betroffen.

## So funktioniert es
Die Richtlinie DisableOnline = 1 verhindert, dass Windows Media DRM für Lizenzen und Sicherheitsupdates auf das Internet oder Intranet zugreift [1].

## Warum es helfen kann
Eine Komponente weniger, die sich von selbst online verbindet. Das betrifft nur alte DRM-geschützte Windows-Media-Dateien.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Eine Datenschutzeinstellung ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Geschützte WMA- und WMV-Dateien, die eine neue Lizenz brauchen, spielen nicht mehr.

## Wann du es nicht nutzen solltest
Wenn du alte geschützte Windows-Media-Dateien abspielst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-windowsmediadrm
