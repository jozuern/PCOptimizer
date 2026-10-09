# Hardware-Virtualisierung im BIOS aus

## Zusammenfassung
::: variant antiCheat
Die Hardware-Virtualisierung ist im BIOS aus. {{antiCheats}} kann Speicherintegrität verlangen, die sie braucht. Spiele starten dann womöglich nicht.
:::
::: variant off
Die Hardware-Virtualisierung (Intel VT-x oder AMD-V) ist im BIOS aus. Speicherintegrität sowie Linux- oder Android-Apps unter Windows brauchen sie.
:::
::: variant default
Prüft, ob die Hardware-Virtualisierung (Intel VT-x oder AMD-V) im BIOS an ist. Speicherintegrität braucht sie.
:::

## Warum das wichtig ist
Speicherintegrität (Kernisolierung) arbeitet in einer abgeschotteten Umgebung, die mit Hardware-Virtualisierung entsteht. Dafür muss die Virtualisierung im UEFI oder BIOS eingeschaltet sein [1]. Manche Anti-Cheats prüfen die Speicherintegrität: Riot Vanguard kann sie auf bestimmten PCs verlangen [2], FACEIT setzt sie schrittweise für immer mehr Spieler durch [3]. Außerdem kann Windows mit Virtualisierung andere Betriebssysteme ausführen, etwa Linux- oder Android-Apps [4].

Die Virtualisierung im BIOS einzuschalten ändert die Spieleleistung für sich allein nicht. Ob du danach die Speicherintegrität einschaltest, ist eine eigene Entscheidung.

## Wie wir es erkennen
Wir lesen aus Windows (WMI), ob der Prozessor Virtualisierung unterstützt (`Win32_Processor.VMMonitorModeExtensions`), ob die Firmware sie eingeschaltet hat (`VirtualizationFirmwareEnabled`) [5] und ob Windows seinen Hypervisor ausführt (`Win32_ComputerSystem.HypervisorPresent`) [6]. Läuft der Windows-Hypervisor bereits, können die Prozessor-Werte als aus erscheinen. Ein laufender Hypervisor zählt deshalb als „an“.

## So behebst du es
1. Starte neu ins BIOS (beim Start **Entf** oder **F2** drücken).
::: if intel
2. Schalte **Intel Virtualization Technology (VT-x)** ein, meist auf der CPU- oder Advanced-Seite [4].
:::
::: ifnot intel
2. Schalte **AMD Virtualization (AMD-V)** ein, meist auf der CPU- oder Advanced-Seite [4]. Der Menüname unterscheidet sich je nach Board; suche nach „Virtualization“.
:::
3. Speichere und beende (meist **F10**).

## So prüfst du die Behebung
Starte den Scan erneut. „Virtualisierung (Intel VT-x oder AMD-V) an“ sollte „Ja“ zeigen.

## Quellen
1. https://support.microsoft.com/en-us/windows/security/windows-security/device-security-in-the-windows-security-app
2. https://support.riotgames.com/riot/performance/vanguard-security-requirements
3. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
4. https://support.microsoft.com/en-us/windows/experience/enable-virtualization-on-windows
5. https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-processor
6. https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-computersystem
