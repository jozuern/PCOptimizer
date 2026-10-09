# Virtualisierungsbasierte Sicherheit und Speicherintegrität aus

## Zusammenfassung
Experte, Abwägung bei der Sicherheit: schaltet VBS und Speicherintegrität (HVCI) ab. Gemessen etwa 5 % in CPU-lastigen Spielen, mehr auf CPUs ohne MBEC/GMET. Manche Anti-Cheats können sie verlangen.

## So funktioniert es
VBS nutzt den Windows-Hypervisor, um Teile von Windows in einer abgeschotteten Umgebung auszuführen. Die Speicherintegrität prüft dort den Kernel-Code, bevor er läuft [1]. Die App setzt EnableVirtualizationBasedSecurity = 0 unter DeviceGuard und Enabled = 0 unter Scenarios\HypervisorEnforcedCodeIntegrity. Mit diesem Wert schaltet auch Microsoft die Speicherintegrität ab [1]. Das wirkt nach einem Neustart. VBS bleibt an, wenn eine Gruppenrichtlinie oder Intune es einschaltet, wenn es mit UEFI-Sperre eingeschaltet wurde [1] oder wenn Credential Guard läuft. Das schaltet Windows seit 22H2 auf Enterprise- und Education-PCs in einer Domäne standardmäßig ein [2].

## Warum es helfen kann
Der Hypervisor macht manche Kernel-Vorgänge aufwendiger. Auf CPUs mit MBEC (Intel ab der 7. Generation) oder GMET (AMD ab Zen 2) kostet die Speicherintegrität weniger. Ältere CPUs emulieren diese Funktionen und verlieren mehr [1].

## Belege
Tom's Hardware hat 2021 auf einer frühen Windows-11-Version mit vier CPUs mit MBEC oder GMET, einer RTX 3090 und 1080p etwa 3 bis 6 % weniger durchschnittliche FPS gemessen, wenn VBS oder Speicherintegrität an war [3]. Der Unterschied hängt von Auflösung und Grafikkarte ab [3]. FACEIT beschreibt die Kosten als in manchen Fällen gering, vor allem auf älteren Systemen [4].

## Nachteile & Risiken
Schwächt den Schutz gegen Schadsoftware, die den Windows-Kernel angreift. Windows-Sicherheit zeigt eine Warnung, solange die Speicherintegrität aus ist [1]. Riot Vanguard fordert dich womöglich auf, Schutzfunktionen vor dem Spielstart wieder einzuschalten [5]. FACEIT verlangt die Speicherintegrität von manchen Spielern und braucht VBS für seine IOMMU-Prüfung, die schrittweise eingeführt wird [4][6]. Rückgängig machen schaltet die Speicherintegrität wieder ein: Ein inzwischen installierter, nicht kompatibler Treiber lädt dann eventuell nicht oder verhindert in seltenen Fällen den Start von Windows [1]. Deshalb gilt die Änderung als startkritisch. Microsofts Wiederherstellungsschritte schalten die Speicherintegrität aus der Wiederherstellungsumgebung wieder ab [1].

## Wann du es nicht nutzen solltest
Nicht, wenn du Spiele mit Riot Vanguard oder FACEIT spielst, nicht auf Arbeits- oder Schul-PCs und nicht, wenn du die Abwägung bei der Sicherheit nicht einschätzen kannst. Kein Gewinn, wenn VBS schon aus ist (Systeminformationen > Systemübersicht > Virtualisierungsbasierte Sicherheit).

## Quellen
1. https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity
2. https://learn.microsoft.com/en-us/windows/security/identity-protection/credential-guard/
3. https://www.tomshardware.com/news/windows-11-gaming-benchmarks-performance-vbs-hvci-security
4. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
5. https://support.riotgames.com/riot/performance/vanguard-security-requirements
6. https://support.faceit.com/hc/en-us/articles/8546882512284-Enabling-Memory-Integrity-HVCI
