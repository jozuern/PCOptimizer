# Virtualisierungsbasierte Sicherheit und Speicherintegrität aus

## Zusammenfassung
Experte, Abwägung bei der Sicherheit: schaltet VBS und Speicherintegrität (HVCI) ab. Gesperrt, solange Riot Vanguard oder FACEIT installiert ist.

## So funktioniert es
VBS führt Teile von Windows in einer vom Hypervisor geschützten Umgebung aus. Die Speicherintegrität nutzt sie, um Kernel-Code vor der Ausführung zu prüfen [1]. Die App setzt die DeviceGuard-Werte, die beides abschalten. Das wirkt nach einem Neustart. Verlangen auch Hyper-V, WSL oder eine UEFI-Sperre VBS, bleibt es womöglich an.

## Warum es helfen kann
Der Hypervisor macht manche Kernel-Vorgänge aufwendiger. Auf CPUs ohne MBEC/GMET (Intel vor der 7. Generation, AMD vor Zen 2) wird die Speicherintegrität emuliert und kostet mehr.

## Belege
Auf aktuellen CPUs liegen die gemessenen Unterschiede in manchen Spielen bei wenigen Prozent, in anderen bei null. Auf alten CPUs ohne MBEC sind sie größer.

## Nachteile & Risiken
Schwächt den Schutz gegen Schadsoftware auf Kernel-Ebene. Mehrere Anti-Cheats (Vanguard, FACEIT) verlangen Speicherintegrität und starten Spiele dann nicht. Startkritisch: Die App exportiert vorher die Startkonfiguration.

## Wann du es nicht nutzen solltest
Nicht nutzen, wenn du Spiele mit Vanguard oder FACEIT spielst, auf Arbeits-PCs oder wenn dir die Abwägung bei der Sicherheit nicht behagt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
