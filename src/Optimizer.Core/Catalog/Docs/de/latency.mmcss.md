# Multimedia-Scheduler-Prioritäten für Spiele

## Zusammenfassung
Hebt die „Games“-Prioritäten des Multimedia Class Schedulers an und senkt den CPU-Anteil für Hintergrundarbeit. Oft empfohlen, Wirkung umstritten.

## So funktioniert es
Der Dienst Multimedia Class Scheduler (MMCSS) bevorzugt Threads, die sich für eine Aufgabe wie „Games“ oder „Audio“ anmelden [1]. SystemResponsiveness legt fest, wie viel CPU-Zeit für niedriger priorisierte Arbeit reserviert bleibt (Standard 20 %, hier 10 %). Die Werte der Aufgabe „Games“ erhöhen GPU- und Planungspriorität angemeldeter Threads.

## Warum es helfen kann
Threads, die sich bei MMCSS unter „Games“ anmelden, bekommen bei voller Auslastung mehr CPU-Zeit.

## Belege
Nur wenige Spiele melden ihre Threads bei MMCSS unter „Games“ an. Die meisten Spiele sind also nicht betroffen. Benchmarks zeigen keinen einheitlichen Unterschied.

## Nachteile & Risiken
Hintergrundaufgaben bekommen etwas weniger CPU-Zeit, solange Multimedia-Threads aktiv sind.

## Wann du es nicht nutzen solltest
Nicht nötig. Es schadet nicht, aber erwarte keinen messbaren Gewinn.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
