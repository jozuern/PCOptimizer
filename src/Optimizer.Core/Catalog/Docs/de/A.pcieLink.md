# PCIe-Anbindung der Grafikkarte

## Zusammenfassung
::: variant slotLimited
{{gpu}} läuft mit weniger PCIe-Lanes, als sie unterstützt, weil der Steckplatz weniger bietet. Meist steckt sie im falschen Slot.
:::
::: variant trainedDown
{{gpu}} läuft mit weniger PCIe-Lanes, als sie unterstützt. Typische Ursachen: Steckplatz, Riser-Kabel, nicht ganz eingerastete Karte oder geteilte Lanes.
:::
::: variant default
Prüft, ob die Grafikkarte alle PCIe-Lanes nutzt, die sie unterstützt, zum Beispiel x16.
:::

## Warum das wichtig ist
Die Grafikkarte tauscht Daten über PCIe-Lanes mit dem Prozessor aus. Eine x16-Karte, die mit x8 oder x4 läuft, hat nur die Hälfte oder ein Viertel der Bandbreite. Bei x8 mit PCIe 4.0 oder neuer ist der Verlust meist klein. Bei x4 oder älteren PCIe-Generationen verlieren Spiele messbar Leistung, vor allem wenn sie viele Daten nachladen oder der Grafikspeicher voll ist. Karten, die für x8 gebaut sind (zum Beispiel die RTX-4060/5060-Klasse), laufen normal mit x8.

## Wie wir es erkennen
Wir lesen die PCIe-Verbindungsdaten der Grafikkarte und des Steckplatzes (Ports) darüber aus Windows. Manche Karten (AMD Radeon ab RX 5000) enthalten einen PCIe-Switch. Den überspringen wir, um die Verbindung zwischen Karte und Steckplatz zu erreichen. Bewertet wird nur die Lane-Anzahl. Die PCIe-Generation sinkt im Leerlauf, um Strom zu sparen (Gen 1 auf dem Desktop ist normal), und ist nur unter Last aussagekräftig. Manche Mainboards melden das Maximum des Steckplatzes nicht. Dann lässt sich die Ursache nicht eingrenzen.

## So behebst du es
::: variant slotLimited
1. Sieh im Handbuch des Mainboards nach, welcher Steckplatz mit x16-Lanes an der CPU hängt, meist der oberste lange Slot.
2. Setze die Karte dort ein.
:::
::: variant trainedDown
1. Herunterfahren, Netzstecker ziehen und die Karte fest neu einsetzen. Die Verriegelung am Slot muss einrasten.
2. Nutzt du ein Riser-Kabel (vertikaler Einbau), teste ohne oder mit einem Riser für die passende PCIe-Generation.
3. Prüfe im Handbuch, ob Lanes geteilt werden: Manche M.2-Steckplätze nehmen dem Grafikkarten-Slot Lanes weg.
:::
::: variant default
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Die aktuelle Breite sollte dem Maximum der Karte entsprechen. Auch GPU-Z zeigt die Busanbindung (mit einem Lasttest für die Generation unter Last).

## Quellen
1. https://docs.nvidia.com/deploy/nvml-api/group__nvmlDeviceQueries.html
