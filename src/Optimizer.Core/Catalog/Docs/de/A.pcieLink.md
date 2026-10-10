# PCIe-Anbindung der Grafikkarte

## Zusammenfassung
::: variant slotLimited
{{gpu}} läuft mit weniger PCIe-Lanes, als sie unterstützt, weil der Steckplatz weniger bietet. Meist steckt sie im falschen Slot oder teilt sich Lanes.
:::
::: variant trainedDown
{{gpu}} läuft mit weniger PCIe-Lanes, als sie unterstützt. Typische Ursachen: Steckplatz, Riser-Kabel, nicht ganz eingerastete Karte oder geteilte Lanes.
:::
::: variant designLimited
{{gpu}} läuft mit weniger PCIe-Lanes als ihr Maximum. In Laptops kann der Grafikchip bauartbedingt mit weniger Lanes angebunden sein.
:::
::: variant default
Prüft, ob die Grafikkarte alle PCIe-Lanes nutzt, die sie unterstützt, zum Beispiel x16.
:::

## Warum das wichtig ist
Die Grafikkarte tauscht Daten über PCIe-Lanes mit dem Prozessor aus. Eine x16-Karte, die mit x8 oder x4 läuft, hat nur die Hälfte oder ein Viertel der Bandbreite. Wie viel das kostet, hängt vom Spiel, von der PCIe-Generation und davon ab, wie viele Daten das Spiel bewegt; diese App misst das nicht. Karten, die mit weniger Lanes gebaut sind, zum Beispiel x8, vergleichen wir mit ihrem eigenen Maximum, sie werden also nicht gemeldet.
::: variant designLimited
In einem Laptop legt der Hersteller die Lanes fest, du kannst hier also nichts ändern.
:::

## Wie wir es erkennen
Wir lesen die aktuelle (ausgehandelte) und die maximale PCIe-Breite der Grafikkarte und das Maximum des Steckplatzes (Ports) darüber aus Windows. Die Breite ist die Anzahl der Lanes, die die Verbindung nutzt [1]. Manche Karten (AMD Radeon ab RX 5000) enthalten einen PCIe-Switch. Den überspringen wir, um die Verbindung zwischen Karte und Steckplatz zu erreichen. Bewertet wird nur die Lane-Anzahl, weil sich die PCIe-Generation je nach Last ändern kann. Manche Mainboards melden das Maximum des Steckplatzes nicht. Dann lässt sich die Ursache nicht eingrenzen.

## So behebst du es
::: variant slotLimited
1. Sieh im Handbuch des Mainboards nach, welcher Steckplatz mit x16-Lanes an der CPU hängt, meist der oberste lange Slot.
2. Setze die Karte dort ein.
3. Prüfe im Handbuch auch, ob Lanes geteilt werden: Bei manchen Mainboards halbiert die Nutzung bestimmter M.2-Steckplätze den Grafikkarten-Slot auf x8.
:::
::: variant trainedDown
1. Herunterfahren, Netzstecker ziehen und die Karte fest neu einsetzen. Die Verriegelung am Slot muss einrasten.
2. Nutzt du ein Riser-Kabel (vertikaler Einbau), teste ohne oder mit einem Riser für die passende PCIe-Generation.
3. Prüfe im Handbuch, ob Lanes geteilt werden: Manche M.2-Steckplätze nehmen dem Grafikkarten-Slot Lanes weg.
:::
::: variant designLimited,default
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Die aktuelle Breite sollte dem Maximum der Karte entsprechen. Auch GPU-Z zeigt die Busanbindung (mit einem Lasttest für die Generation unter Last).

## Quellen
1. https://learn.microsoft.com/de-de/windows-hardware/drivers/ddi/ntddk/ns-ntddk-_pci_express_link_status_register
