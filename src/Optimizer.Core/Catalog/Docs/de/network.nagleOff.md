# Nagle-Algorithmus aus (TCP)

## Zusammenfassung
Schaltet verzögerte ACKs und das Bündeln von Paketen nach Nagle auf deinen aktiven Netzwerkadaptern ab. Betrifft nur TCP, der meiste Spieleverkehr nutzt UDP.

## So funktioniert es
Der Nagle-Algorithmus sammelt kleine TCP-Pakete zu größeren, bevor er sie sendet [1]. TcpAckFrequency = 1 und TCPNoDelay = 1 unter jeder aktiven Schnittstelle lassen Windows Bestätigungen und kleine Pakete sofort senden.

## Warum es helfen kann
Ältere Spiele, die kleine, häufige TCP-Pakete senden, haben womöglich weniger Verzögerung.

## Belege
Spiele, bei denen es darauf ankommt, setzen TCP_NODELAY selbst, und der meiste Echtzeit-Spieleverkehr läuft über UDP, das Nagle nicht betrifft. Messungen zeigen selten einen Unterschied.

## Nachteile & Risiken
Bei großen Übertragungen etwas mehr Pakete im Netzwerk.

## Wann du es nicht nutzen solltest
Für aktuelle Spiele nicht nötig. Die Werte gelten pro Schnittstelle. Ein neuer Adapter bekommt sie nicht.

## Quellen
1. https://www.rfc-editor.org/rfc/rfc896
