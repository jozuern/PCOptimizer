# Resizable BAR

## Zusammenfassung
::: variant off
{{gpu}} unterstützt Resizable BAR, es ist aber aus. Schalte im BIOS „Above 4G Decoding“ und „Resizable BAR“ ein.
:::
::: variant laptop
Resizable BAR ist für {{gpu}} aus. Bei Laptops hängt die Unterstützung vom Modell ab und kommt mit der Firmware des Herstellers; es gibt keine Einstellung dafür.
:::
::: variant active
Resizable BAR ist für {{gpu}} aktiv: Der Prozessor kann auf den gesamten Grafikspeicher auf einmal zugreifen.
:::
::: variant unsupported
{{gpu}} unterstützt Resizable BAR nicht. Hier gibt es nichts zu ändern, keine BIOS-Einstellung kann es nachrüsten.
:::
::: variant unknownGpu,unknownState
Prüft, ob Resizable BAR (bei AMD: Smart Access Memory) für die Grafikkarte aktiv ist.
:::

## Warum das wichtig ist
Ohne Resizable BAR sieht der Prozessor den Grafikspeicher durch ein Fenster, das meist 256 MB groß ist [3]. Mit Resizable BAR vergrößert Windows dieses Fenster beim Start auf den gesamten Grafikspeicher [3]. NVIDIA schaltet es nur in Spielen ein, in denen die eigenen Tests einen Gewinn zeigen, von wenigen Prozent bis 12 %, und manche Spiele laufen damit langsamer [1]. Laut Intel brauchen Arc-Karten es für ein gutes Spielerlebnis: Ohne werden Ausreißer bei den Frametimes größer, die Karten funktionieren aber trotzdem [4][5]. Nötig sind eine neuere Grafikkarte (NVIDIA ab RTX 30 [1], AMD Radeon ab RX 5000 [7], Intel Arc), UEFI-Start und eine BIOS-Einstellung [6]. Für Smart Access Memory nennt AMD einen Ryzen ab Serie 3000 auf einem Mainboard ab der 500er-Serie [7].
::: variant unsupported
Ältere Karten wie die GeForce-RTX-20-Serie unterstützen es nicht, die Einstellung hätte also keine Wirkung.
:::
::: variant laptop
Bei Laptops hängt die Unterstützung für Resizable BAR vom Modell ab; nur der Hersteller kann sie per Firmware-Update nachrüsten [1][2].
:::

## Wie wir es erkennen
Wir lesen die Größe der Speicherfenster der Grafikkarte (PCI-Speicherressourcen) über den Windows-Konfigurationsmanager. Ein Fenster größer als 256 MB bedeutet: Resizable BAR ist aktiv. Ob die Karte es unterstützt, steht in der GPU-Tabelle des Katalogs. Außerdem prüfen wir UEFI-Start und Partitionsstil des Systemlaufwerks.

## So behebst du es
::: variant off
1. **BitLocker:** Ist BitLocker oder die Geräteverschlüsselung an, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [9][10].
::: if mbr
2. Dein Systemlaufwerk nutzt MBR. Schaltest du CSM (Legacy-Start) ab, startet Windows nicht mehr. Prüfe das Laufwerk in einer als Administrator geöffneten Eingabeaufforderung mit `mbr2gpt /validate /allowFullOS`, wandle es mit `mbr2gpt /convert /allowFullOS` um und stelle die Firmware danach auf UEFI [8]. Sichere vorher deine Daten; der BitLocker-Schutz muss für die Umwandlung angehalten sein [8].
:::
3. Aktualisiere das BIOS auf eine Version mit Resizable-BAR-Unterstützung. Bietet der Mainboard-Hersteller keine an, unterstützt die Plattform es nicht.
4. GeForce RTX 3060 Ti, 3070, 3080 und 3090 brauchen eventuell zuerst ein Firmware-Update der Grafikkarte (VBIOS) [1]. Es gibt es auf NVIDIAs Seite zum Update-Tool und beim Kartenhersteller [2].
::: if menuPath
5. Auf deinem {{board}}: **{{menuPath}}**.
::: if menuUnverified
   Dieser Pfad ist noch nicht mit dem Handbuch deines Mainboards abgeglichen. Menünamen unterscheiden sich je nach Board und BIOS-Version. Passt der Pfad nicht, suche die Einstellung über ihren Namen.
:::
:::
::: ifnot menuPath
5. Schalte **Above 4G Decoding** und **Re-Size BAR Support** ein. Manche Mainboards nennen es Smart Access Memory oder Clever Access Memory (ASRock: C.A.M.) [6].
:::
6. Stelle sicher, dass **CSM** aus ist (reiner UEFI-Start) und das Windows-Laufwerk GPT nutzt [2][6].
7. Aktualisiere den Grafiktreiber.
:::
::: variant laptop,active,unsupported,unknownGpu,unknownState
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Das größte Speicherfenster sollte der Größe des Grafikspeichers entsprechen. Auch die NVIDIA-Systemsteuerung zeigt unter Systeminformationen „Resizable BAR“ mit „Ja“ an [2].

## Quellen
1. https://www.nvidia.com/en-us/geforce/news/geforce-rtx-30-series-resizable-bar-support/
2. https://nvidia.custhelp.com/app/answers/detail/a_id/5165/~/nvidia-resizable-bar-firmware-update-tool
3. https://learn.microsoft.com/de-de/windows-hardware/drivers/display/resizable-bar-support
4. https://game.intel.com/stories/intel-arc-graphics-resizable-bar/
5. https://www.intel.com/content/www/us/en/support/articles/000092416/graphics.html
6. https://www.intel.com/content/www/us/en/support/articles/000090831/graphics.html
7. https://www.amd.com/en/legal/claims/gaming-details.html
8. https://learn.microsoft.com/de-de/windows/deployment/mbr-to-gpt
9. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
10. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
