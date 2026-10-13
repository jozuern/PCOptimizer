# Startpriorität für ein Programm

## Zusammenfassung
Windows startet dieses Programm jedes Mal mit der gewählten CPU-Priorität und auf Wunsch niedriger Datenträger- und Speicherpriorität, ohne Programm im Hintergrund. Echtzeit gibt es nie.

## So funktioniert es
Jeder Prozess hat eine Prioritätsklasse; Windows verteilt CPU-Zeit zuerst an höhere Klassen [1]. Image File Execution Options sind Einstellungen pro Programm, die Windows beim Start des Programms liest [2]. Die App schreibt CpuPriorityClass (IoPriority für niedrige Datenträgerpriorität, PagePriority für niedrige Speicherpriorität) unter Image File Execution Options\<Programm>\PerfOptions. Die Speicherpriorität ist ein Hinweis an die Speicherverwaltung: Holt sie Seiten von Programmen zurück, nimmt sie sie zuerst denen mit niedrigerer Priorität [3]. Microsoft beschreibt die Prioritätsklassen, die Speicherpriorität und die Image File Execution Options, die PerfOptions-Werte sind aber nicht dokumentiert. Tests in einer virtuellen Maschine mit Windows 11 haben bestätigt, dass Windows sie anwendet: Ein Programm, das mit einer Regel für „Niedriger als normal“ und niedrige Datenträgerpriorität startete, lief mit beidem, und nach dem Rückgängigmachen wieder mit „Normal“; mit PagePriority 1, 2 oder 3 startete es mit dieser Speicherpriorität und ohne den Wert wieder mit normal (5).

## Warum es helfen kann
Ein Spiel auf Höher als normal behält seinen Anteil am Prozessor, wenn Hintergrundprogramme viel zu tun haben; ein Backup- oder Download-Tool auf Niedriger als normal oder Niedrig hält sich zurück. Niedrige Speicherpriorität für einen Launcher oder ein Chatprogramm, das neben einem Spiel offen bleibt, lässt Windows bei knappem Arbeitsspeicher zuerst diesem Programm Speicher wegnehmen.

## Belege
Eine höhere Priorität für ein Spiel hilft nur, wenn gleichzeitig andere Programme um den Prozessor konkurrieren; auf einem weitgehend ruhigen PC ändert sich nichts. Niedrige Speicherpriorität wirkt nur, wenn der Arbeitsspeicher knapp wird. Keine veröffentlichte Messung zeigt für eines von beidem einen allgemeinen FPS-Gewinn, deshalb ist die Wirkung als umstritten bewertet.

## Nachteile & Risiken
Hohe Priorität kann den Rest von Windows träge machen, solange das Programm stark arbeitet [1]. Manche Anti-Cheat-Systeme prüfen die Image File Execution Options; startet ein Spiel nicht, entferne die Regel. Die Regel gilt für jedes Programm mit diesem Dateinamen. Ein Programm mit niedriger Speicherpriorität kann träge reagieren, wenn du es nach einer Zeit im Hintergrund bei knappem Speicher wieder öffnest.

## Wann du es nicht nutzen solltest
Für Programme, die schon gut laufen, und mit Hoch für alles, was den Prozessor lange voll auslastet.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-overview
3. https://learn.microsoft.com/de-de/windows/win32/api/processthreadsapi/ns-processthreadsapi-memory_priority_information
