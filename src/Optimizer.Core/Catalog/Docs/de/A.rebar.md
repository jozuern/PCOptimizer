# Resizable BAR

## Zusammenfassung
::: variant off
{{gpu}} unterstützt Resizable BAR, es ist aber aus. Schalte im BIOS „Above 4G Decoding“ und „Resizable BAR“ ein.
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
Ohne Resizable BAR sieht der Prozessor den Grafikspeicher nur durch ein 256-MB-Fenster und muss dieses Fenster verschieben, um an den Rest zu kommen. Mit Resizable BAR ist der ganze Grafikspeicher auf einmal eingeblendet. In vielen Spielen ändert das wenig, in manchen bringt es ein paar Prozent. Für Intel-Arc-Karten ist es unverzichtbar: Ohne verlieren sie einen großen Teil ihrer Leistung. Nötig sind eine neuere Grafikkarte (NVIDIA ab RTX 30, AMD ab RX 6000, Intel Arc), UEFI-Start und eine BIOS-Einstellung.
::: variant unsupported
Ältere Karten wie die GeForce-RTX-20-Serie unterstützen es nicht, die Einstellung hätte also keine Wirkung.
:::

## Wie wir es erkennen
Wir lesen die Größe der Speicherfenster der Grafikkarte (PCI-Speicherressourcen) über den Windows-Konfigurationsmanager. Ein Fenster größer als 256 MB bedeutet: Resizable BAR ist aktiv. Ob die Karte es unterstützt, steht in der GPU-Tabelle des Katalogs. Außerdem prüfen wir UEFI-Start und Partitionsstil des Systemlaufwerks.

## So behebst du es
::: variant off
::: if mbr
0. Dein Systemlaufwerk nutzt MBR. Schaltest du CSM (Legacy-Start) ab, startet Windows nicht mehr. Wandle das Laufwerk zuerst mit `mbr2gpt /validate` und `mbr2gpt /convert` in GPT um (vorher sichern).
:::
1. Aktualisiere das BIOS auf eine Version mit Resizable-BAR-Unterstützung.
::: if menuPath
2. Auf deinem {{board}}: **{{menuPath}}**.
:::
::: ifnot menuPath
2. Schalte **Above 4G Decoding** und **Re-Size BAR Support** ein (bei ASRock: C.A.M.).
:::
3. Stelle sicher, dass **CSM** aus ist (reiner UEFI-Start).
4. Aktualisiere den Grafiktreiber.
:::
::: variant active,unsupported,unknownGpu,unknownState
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Das größte Speicherfenster sollte der Größe des Grafikspeichers entsprechen. Auch die NVIDIA-Systemsteuerung zeigt unter Systeminformationen „Resizable BAR: Ja“.

## Quellen
1. https://www.nvidia.com/en-us/geforce/news/geforce-rtx-30-series-resizable-bar-support/
2. https://www.intel.com/content/www/us/en/support/articles/000090831/graphics.html
