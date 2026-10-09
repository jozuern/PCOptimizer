# IPv4 vor IPv6 bevorzugen

## Zusammenfassung
Lässt Windows IPv4 vor IPv6 versuchen, wenn ein Server beides anbietet. IPv6 bleibt eingeschaltet.

## So funktioniert es
Der Registrierungswert DisabledComponents steuert das IPv6-Verhalten. Bit 0x20 ändert die Adressreihenfolge, sodass IPv4 zuerst genutzt wird [1]. Die App setzt nur dieses Bit und behält die übrigen. Wirkt nach einem Neustart.

## Warum es helfen kann
Hilft in Netzen oder bei Spielservern, deren IPv6-Routen langsamer oder unzuverlässig sind.

## Belege
Funktioniert IPv6 gut, ändert sich nichts. Microsoft empfiehlt diesen Weg statt IPv6 abzuschalten, was Windows-Funktionen stören kann [1].

## Nachteile & Risiken
Dienste, die dich nur über IPv6 erreichen, funktionieren weiter. Nur die Reihenfolge ändert sich.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn deine Verbindung keine IPv6-Probleme hat.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows
