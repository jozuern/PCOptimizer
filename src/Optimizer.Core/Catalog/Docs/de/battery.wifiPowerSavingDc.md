# WLAN-Energiesparen im Akkubetrieb: maximal

## Zusammenfassung
Setzt den Energiesparmodus des WLAN-Adapters im Akkubetrieb von „Mittlerer Energiesparmodus“ (Standard im Plan „Ausbalanciert“) auf „Maximaler Energiesparmodus“. Am Netzteil ändert sich nichts.

## So funktioniert es
Der Energieplan hat die Einstellung „Drahtlosadaptereinstellungen > Energiesparmodus“ mit vier Stufen, von „Höchstleistung“ (0) bis „Maximaler Energiesparmodus“ (3) [1]. Die App setzt den Akkuwert auf 3. Windows gibt die Stufe an den WLAN-Treiber weiter, der entscheidet, wie lange das Funkmodul zwischen Übertragungen schlafen darf.

## Warum es helfen kann
Ein Funkmodul, das zwischen Übertragungen länger schläft, braucht weniger Energie. Das hilft vor allem, wenn der Laptop im WLAN meist untätig ist.

## Belege
Die Wirkung hängt vom WLAN-Treiber ab: Manche Treiber setzen die Stufe genau um, andere ignorieren sie. Messungen, die für alle Adapter gelten, haben wir nicht gefunden, daher ist die Wirkung niedrig bewertet.

## Nachteile & Risiken
Im Akkubetrieb kann die Latenz steigen und Übertragungen können langsamer werden, spürbar bei Videoanrufen und Onlinespielen. Manche Router kommen mit dem WLAN-Energiesparen nicht gut zurecht, dann kann es zu Verbindungsproblemen kommen [3]. Rückgängig stellt den bisherigen Akkuwert wieder her.

## Wann du es nicht nutzen solltest
Wenn du im Akkubetrieb Videoanrufe machst oder online spielst. Fehlt die Einstellung auf deinem Laptop, bietet der Treiber sie nicht an, und der Tweak wird als nicht unterstützt angezeigt.

## Quellen
1. https://learn.microsoft.com/en-us/archive/blogs/richardsmith/powercfg-useful-if-you-know-the-guids
2. https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options
3. https://learn.microsoft.com/en-us/previous-versions/windows/it-pro/windows-7/dd744398(v=ws.10)
