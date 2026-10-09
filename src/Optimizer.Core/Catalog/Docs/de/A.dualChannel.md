# Arbeitsspeicher im Single-Channel

## Zusammenfassung
::: variant single
Es steckt nur ein Speichermodul. Der Arbeitsspeicher läuft deshalb im Single-Channel mit halber Bandbreite.
:::
::: variant sameChannel
Alle Speichermodule stecken im selben Kanal. Der Arbeitsspeicher läuft deshalb im Single-Channel mit halber Bandbreite.
:::
::: variant default
Prüft, ob der Arbeitsspeicher im Dual-Channel läuft. Dafür müssen Module in beiden Kanälen stecken.
:::

## Warum das wichtig ist
Desktop-Prozessoren lesen den Speicher über zwei Kanäle gleichzeitig. Stecken nur in einem Kanal Module, halbiert sich die Speicherbandbreite. Spiele werden langsamer, in CPU-lastigen Szenen oft um 10 bis 25 %, integrierte Grafik noch stärker. Am meisten leiden die 1-%-Lows. Zwei Module in den falschen Steckplätzen (beide in Kanal A) sind so langsam wie ein einzelnes.

## Wie wir es erkennen
Wir lesen die Steckplatznamen jedes Moduls (`DeviceLocator` und `BankLabel`) aus Windows und ordnen sie mit den Mustern aus dem Katalog den Kanälen zu („ChannelA-DIMM2“ bedeutet zum Beispiel Kanal A). Verraten die Namen den Kanal nicht, lautet das Ergebnis „Unbekannt“. Auf dem Mainboard verlöteter Speicher kann intern im Dual-Channel laufen und wird nicht als Problem gemeldet.

## So behebst du es
::: variant single
1. Ergänze ein zweites Modul mit gleichem Typ, gleicher Größe und Geschwindigkeit (am besten ein passendes Zweier-Kit kaufen).
2. Setze es in den Steckplatz, den das Handbuch für zwei Module angibt.
:::
::: variant sameChannel
1. Fahre den PC herunter und zieh den Netzstecker.
2. Sieh im Handbuch des Mainboards ({{board}}) nach, welche Steckplätze für zwei Module gedacht sind. Bei den meisten Boards mit vier Steckplätzen sind das der zweite und vierte von der CPU aus (A2 und B2).
3. Setze ein Modul in den anderen Kanal um.
:::
::: variant default
Nutze bei zwei Modulen die Steckplätze, die das Handbuch empfiehlt (meist A2 und B2).
:::

## So prüfst du die Behebung
Starte den Scan erneut. Die Steckplätze sollten zwei verschiedene Kanäle zeigen. Tools wie CPU-Z zeigen „Dual“ als Kanalanzahl.
