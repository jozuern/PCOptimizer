# Startpriorität für ein Programm

## Zusammenfassung
Windows startet dieses Programm jedes Mal mit der gewählten CPU-Priorität und auf Wunsch niedriger Datenträgerpriorität, ohne dass ein Programm im Hintergrund läuft. Echtzeit wird nie angeboten.

## So funktioniert es
Jeder Prozess hat eine Prioritätsklasse; Windows verteilt Prozessorzeit zuerst an höhere Klassen [1]. Image File Execution Options sind Einstellungen pro Programm, die Windows beim Start des Programms liest [2]. Die App schreibt CpuPriorityClass (und IoPriority für niedrige Datenträgerpriorität) unter Image File Execution Options\<Programm>\PerfOptions. Microsoft beschreibt die Prioritätsklassen und die Image File Execution Options, die PerfOptions-Werte sind aber nicht dokumentiert; bis ein Test sie bestätigt, ist die Regel eine Vorschau.

## Warum es helfen kann
Ein Spiel auf Höher als normal behält seinen Anteil am Prozessor, wenn Hintergrundprogramme viel zu tun haben; ein Sicherungs- oder Download-Werkzeug auf Niedriger als normal oder Niedrig hält sich zurück.

## Belege
Eine höhere Priorität für ein Spiel hilft nur, wenn gleichzeitig andere Programme um den Prozessor konkurrieren; auf einem weitgehend ruhigen PC ändert sich nichts. Keine veröffentlichte Messung zeigt einen allgemeinen Gewinn an Bildrate, deshalb ist die Wirkung als umstritten bewertet.

## Nachteile & Risiken
Hohe Priorität kann den Rest von Windows träge machen, solange das Programm stark arbeitet [1]. Manche Anti-Cheat-Systeme prüfen die Image File Execution Options; startet ein Spiel nicht, entferne die Regel. Die Regel gilt für jedes Programm mit diesem Dateinamen.

## Wann du es nicht nutzen solltest
Für Programme, die schon gut laufen, und mit Hoch für alles, was den Prozessor lange voll auslastet.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/gflags-overview
