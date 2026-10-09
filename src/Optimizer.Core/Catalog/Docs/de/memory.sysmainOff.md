# SysMain (Superfetch) aus

## Zusammenfassung
Deaktiviert den Dienst SysMain, der häufig genutzte Programme vorab in den Speicher lädt. Nur auf PCs mit ausschließlich SSDs angeboten. Wirkung umstritten.

## So funktioniert es
SysMain (früher Superfetch) ist ein Windows-Dienst, der laut seiner Beschreibung die Systemleistung mit der Zeit erhält und verbessert [2]. Er lädt Daten häufig genutzter Programme vorab in freien Speicher. Microsoft schrieb 2009, dass Superfetch und verwandte Vorladefunktionen für Festplatten entwickelt wurden und Windows 7 sie auf schnellen SSDs abschaltet [1]. Die App setzt den Starttyp des Dienstes auf „Deaktiviert“. Der Dienst endet beim nächsten Neustart.

## Warum es helfen kann
Die Hintergrundaktivität von Laufwerk und CPU durch das Vorladen entfällt.

## Belege
Wir haben keine Messung von Microsoft oder einem Hardwarehersteller gefunden, die auf reinen SSD-PCs einen Unterschied in Spielen zeigt, weder in die eine noch in die andere Richtung.

## Nachteile & Risiken
Programme starten nach dem Hochfahren beim ersten Mal womöglich etwas langsamer. Microsoft dokumentiert nicht, welche anderen Speicherfunktionen SysMain brauchen.

## Wann du es nicht nutzen solltest
Nicht auf PCs mit Festplatte, für die das Vorladen gedacht ist [1].

## Quellen
1. https://learn.microsoft.com/en-us/archive/blogs/e7/support-and-qa-for-solid-state-drives
2. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
