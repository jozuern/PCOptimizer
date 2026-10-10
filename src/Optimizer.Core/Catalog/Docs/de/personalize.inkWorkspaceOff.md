# Windows Ink-Arbeitsbereich aus

## Zusammenfassung
Schaltet den Windows Ink-Arbeitsbereich aus, das Stiftfenster für Notizen und Bildschirmskizzen. Pro, Enterprise und Education.

## So funktioniert es
Die Richtlinie AllowWindowsInkWorkspace hat drei Werte: 0 Zugriff auf den Ink-Arbeitsbereich gesperrt, 1 an, aber nicht über dem Sperrbildschirm, 2 überall an (Standard) [1]. Die App setzt 0 [1].

## Warum es helfen kann
Auf PCs ohne Stift braucht man die Funktion nicht; auf Stift-PCs öffnet die Stifttaste sie nicht mehr aus Versehen.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Stift-Kurzbefehle, die den Ink-Arbeitsbereich öffnen, funktionieren nicht mehr.

## Wann du es nicht nutzen solltest
Wenn du einen Stift mit dem Ink-Arbeitsbereich nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowsinkworkspace
