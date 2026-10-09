# Power Throttling aus

## Zusammenfassung
Verhindert, dass Windows Hintergrundprozesse im Stromsparmodus ausführt. Vor allem auf Laptops und CPUs mit Effizienzkernen relevant.

## So funktioniert es
Mit Power Throttling markiert Windows Hintergrundarbeit als niedrig priorisiert und führt sie mit sparsamem Takt oder auf Effizienzkernen aus (EcoQoS) [1]. Der Wert PowerThrottlingOff = 1 schaltet das für alle Prozesse ab.

## Warum es helfen kann
Programme, die neben einem Spiel laufen, etwa Voice-Chat, Overlays oder Streaming-Software, werden sonst womöglich gebremst, wenn sie den Fokus verlieren.

## Belege
Auf Desktops ohne Effizienzkerne ist der Effekt meist nicht messbar. Auf hybriden CPUs und Laptops behalten Hintergrund-Apps volle Geschwindigkeit.

## Nachteile & Risiken
Höherer Stromverbrauch, besonders im Akkubetrieb. Wirkt nach einem Neustart.

## Wann du es nicht nutzen solltest
Auf Desktops ohne Effizienzkerne nicht sinnvoll. Auf Laptops die Akkulaufzeit bedenken.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
