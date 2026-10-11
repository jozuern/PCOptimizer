# Bluescreen zeigt technische Details

## Zusammenfassung
Nach einem Absturz zeigt der Bluescreen auch die Parameter des Stopcodes, was bei der Suche nach der Ursache hilft. Braucht einen Neustart. Nicht dokumentierter Wert, getestet.

## So funktioniert es
DisplayParameters = 1 unter SYSTEM\CurrentControlSet\Control\CrashControl lässt den Bluescreen die Parameter des Stopcodes anzeigen [1]. Der Rest des Stopbildschirms bleibt, wie er ist. Microsoft dokumentiert den Wert nicht.

## Warum es helfen kann
Der Stopcode allein nennt oft nicht den schuldigen Treiber; die Parameter helfen, ihn einzugrenzen, ohne ein Speicherabbild zu öffnen.

## Belege
In einem VM-Test mit Windows 11 Build 26300 zeigte ein über Hyper-V ausgelöster Stoppfehler mit dem Wert die vier Parameter klein oben links auf dem Stopbildschirm und nach dem Rückgängigmachen keine. Kein Einfluss auf Framerate oder Latenz, und Speicherabbilder werden wie bisher geschrieben.

## Nachteile & Risiken
Keine bekannt.

## Wann du es nicht nutzen solltest
Wenn du dir Bluescreens nie ansiehst.

## Quellen
1. https://winaero.com/show-bsod-details-instead-of-the-sad-smiley-in-windows-10/
