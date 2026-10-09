# SysMain (Superfetch) aus

## Zusammenfassung
Deaktiviert den Dienst SysMain, der häufig genutzte Programme vorab in den Speicher lädt. Nur auf PCs mit ausschließlich SSDs angeboten. Wirkung umstritten.

## So funktioniert es
SysMain beobachtet, welche Programme du nutzt, und lädt sie vorab in freien Speicher. Entwickelt wurde es für Festplatten [1]. Auf SSDs laden Programme auch ohne SysMain schnell. Die App setzt den Starttyp des Dienstes auf „Deaktiviert“.

## Warum es helfen kann
Die Hintergrundaktivität von Laufwerk und CPU durch das Vorladen entfällt.

## Belege
Auf reinen SSD-Systemen zeigen Messungen in keine Richtung einen einheitlichen Unterschied.

## Nachteile & Risiken
Programme starten nach dem Hochfahren beim ersten Mal womöglich etwas langsamer.

## Wann du es nicht nutzen solltest
Nicht auf PCs mit Festplatte, wo SysMain weiterhin hilft.

## Quellen
1. https://learn.microsoft.com/en-us/powershell/module/mmagent/disable-mmagent
