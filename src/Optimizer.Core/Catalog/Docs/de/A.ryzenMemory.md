# Speichergeschwindigkeit bei Ryzen

## Zusammenfassung
::: variant aboveSync
Der Speicher läuft mit {{speed}} MT/s, über dem Bereich, in dem der Ryzen-Speichercontroller meist synchron läuft. Prüfe, dass er nicht auf halben Takt gewechselt hat.
:::
::: variant slowKit
Der Speicher läuft mit {{speed}} MT/s, unter dem üblichen Bereich für Ryzen ({{low}} bis {{high}} MT/s). Ein schnelleres Kit verbessert die minimalen Bildraten.
:::
::: variant default
Prüft, ob die Speichergeschwindigkeit zum Ryzen-Speichercontroller passt.
:::

## Warum das wichtig ist
Bei Ryzen laufen der Speichercontroller und die Verbindung zwischen den Chips des Prozessors mit Takten, die an den Speichertakt gekoppelt sind. Spiele reagieren stark auf Speicherlatenz, vor allem bei den minimalen Bildraten.
::: if am4
Bei AM4 kann der Infinity-Fabric-Takt (FCLK) dem Speichertakt meist bis etwa DDR4-3600 im Verhältnis 1:1 folgen, bei vielen Prozessoren bis DDR4-3800. Darüber wechselt er auf 2:1, und die zusätzliche Speichergeschwindigkeit kostet dann oft mehr Latenz, als sie bringt.
:::
::: if am5
Bei AM5 läuft der Takt des Speichercontrollers (UCLK) standardmäßig bis etwa DDR5-6000 mit dem Speichertakt. Darüber stellt das BIOS ihn meist auf halben Takt (1:2), was Latenz kostet, außer du stellst 1:1 von Hand ein und der Prozessor schafft das.
:::

## Wie wir es erkennen
Wir lesen die eingestellte Speichergeschwindigkeit und die Nenngeschwindigkeit aus den Teilenummern der Module. Das tatsächliche Verhältnis von FCLK oder UCLK kann Windows nicht lesen. Das hier ist also eine Empfehlung, keine Messung.

## So behebst du es
::: variant aboveSync
::: if am4
1. Prüfe im BIOS, dass **FCLK** auf die Hälfte der Speichergeschwindigkeit steht (bei DDR4-3600: 1800 MHz). Schafft er das nicht, ist DDR4-3600 mit 1:1 meist genauso schnell oder schneller.
:::
::: if am5
1. Suche im BIOS nach **UCLK DIV1 MODE** (Bezeichnung je nach Hersteller) und stelle **UCLK = MEMCLK** ein. Läuft das System so nicht stabil, ist DDR5-6000 mit 1:1 meist genauso schnell oder schneller.
:::
:::
::: variant slowKit
1. Für ein Upgrade passt zu dieser Plattform ein Kit im Bereich {{low}} bis {{high}} MT/s mit niedrigen CL-Timings. Nutze die gleiche Anzahl Module in den Steckplätzen, die das Handbuch für zwei Module empfiehlt.
:::
::: variant default
Nichts zu tun.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Für das Taktverhältnis zeigen Tools wie ZenTimings oder das BIOS FCLK, UCLK und MCLK.
