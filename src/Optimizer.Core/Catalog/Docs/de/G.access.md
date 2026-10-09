# Spielzugang: Anforderungen der Anti-Cheats

## Zusammenfassung
::: status Problem
Ein installierter Anti-Cheat ({{antiCheats}}) braucht eine Sicherheitsfunktion, die auf diesem PC aus ist. Seine Spiele starten erst, wenn sie an ist.
:::
::: status Info
::: variant sometimes
Alle Pflichtfunktionen sind an. {{antiCheats}} kann in manchen Spielen oder Modi mehr verlangen (Speicherintegrität oder IOMMU).
:::
::: variant baseline
UEFI-Start, Secure Boot oder TPM 2.0 fehlt. Spiele mit strengen Anti-Cheats (Valorant, FACEIT, Battlefield 6) starten nicht.
:::
:::
::: status Ok
Die Sicherheitsfunktionen, die die installierten Anti-Cheats verlangen, sind an.
:::
::: status Unknown,Unsupported
Zeigt die Sicherheitsfunktionen, die Kernel-Anti-Cheats vor dem Spielstart prüfen. Es geht um den Zugang zu Spielen, nicht um FPS.
:::

## Warum das wichtig ist
Mehrere aktuelle Anti-Cheats erlauben das Spielen nur auf PCs mit einer geprüften Startkette. Fehlt eine geforderte Funktion, zeigt das Spiel einen Fehler und startet nicht. Deshalb empfiehlt diese App nie, diese Funktionen für mehr Leistung abzuschalten, und sperrt solche Änderungen, solange ein strenger Anti-Cheat installiert ist.
::: if vanguard
**Riot Vanguard** (Valorant, League of Legends) verlangt unter Windows 11 TPM 2.0 und Secure Boot. Auf bestimmten PCs, etwa nach einer Erkennung, kann es zusätzlich Speicherintegrität (HVCI) und IOMMU verlangen [1].
:::
::: if faceit
**FACEIT** verlangt TPM 2.0 und Secure Boot und setzt Speicherintegrität und IOMMU in Wellen für immer mehr Spieler durch [2].
:::
::: if javelin
**EA Javelin** (Battlefield 6) verlangt Secure Boot. TPM 2.0 ist für einige Spiele Pflicht, etwa für Battlefield 6 [3].
::: if unverified_javelin
Dieser Eintrag stützt sich auf Presseberichte [3], nicht auf eine Seite von EA, und ist noch nicht bestätigt.
:::
:::

## Wie wir es erkennen
Wir lesen den Firmware-Typ (UEFI oder Legacy), den Secure-Boot-Status, die TPM-Version, den Status der virtualisierungsbasierten Sicherheit und der Speicherintegrität, die IOMMU-Unterstützung (über den Kernel-DMA-Schutz) und den Partitionsstil des Systemdatenträgers. Installierte Anti-Cheats erkennen wir an ihrer Dienst- und Treiberregistrierung, daher finden wir Vanguard auch im Modus „bei Bedarf“. IOMMU kann auch ohne Kernel-DMA-Schutz an sein, „unbekannt“ ist dort also kein Problem.

## So behebst du es
::: if mbr
1. **UEFI und GPT:** Der Systemdatenträger nutzt MBR. Wandle ihn mit `mbr2gpt /validate` und danach `mbr2gpt /convert /allowFullOS` um, bevor du das BIOS auf reines UEFI stellst (CSM aus). Sichere vorher deine Daten.
:::
::: ifnot mbr
1. **UEFI:** Schalte im BIOS **CSM** (Legacy-Start) ab, damit der PC im reinen UEFI-Modus startet.
:::
2. **Secure Boot:** Stelle im BIOS auf der Seite für Start oder Sicherheit Secure Boot auf **Enabled** (OS-Typ: Windows UEFI mode). Manche Mainboards brauchen vorher „Install default Secure Boot keys“.
3. **TPM 2.0:** Schalte im BIOS **Intel PTT** oder **AMD fTPM** ein.
4. **Speicherintegrität:** Windows-Sicherheit > Gerätesicherheit > Kernisolierung > Speicherintegrität. Danach neu starten.
5. **IOMMU:** Schalte im BIOS **VT-d** (Intel) bzw. **IOMMU** / **AMD-Vi** (AMD) ein.

## So prüfst du die Behebung
Starte den Scan erneut. Jede geforderte Funktion sollte „Ja“ oder „Läuft“ zeigen. Die Fehlercode-Seite des Anti-Cheats nennt die genaue Anforderung, die er prüft.

## Quellen
1. https://support.riotgames.com/riot/performance/vanguard-security-requirements
2. https://support.faceit.com/hc/en-us/articles/23117181142556-Windows-Security-Requirements-FAQ
3. https://www.thesixthaxis.com/2025/08/07/battlefield-6-and-black-ops-7-both-need-secure-boot-and-tpm-2-0-for-pc-anti-cheat/
