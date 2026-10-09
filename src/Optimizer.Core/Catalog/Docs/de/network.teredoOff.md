# Teredo-Tunnel aus

## Zusammenfassung
Schaltet den Teredo-IPv6-Tunnel ab. Gesperrt, wenn Xbox oder Game Pass genutzt wird, weil die Xbox-Netzwerkfunktionen Teredo brauchen.

## So funktioniert es
Teredo tunnelt IPv6-Verkehr über IPv4, wenn kein natives IPv6 vorhanden ist. Bit 0x08 von DisabledComponents schaltet ihn ab [1]. Die App setzt nur dieses Bit. Wirkt nach einem Neustart.

## Warum es helfen kann
Entfernt einen Tunneladapter, mit dem manche Spiele und VPNs schlecht umgehen.

## Belege
Kein Effekt auf die FPS. Es ist nur wichtig, wenn Teredo Verbindungsprobleme verursacht.

## Nachteile & Risiken
Xbox-Partychat und manche Xbox-Mehrspielerfunktionen funktionieren ohne Teredo nicht mehr.

## Wann du es nicht nutzen solltest
Nicht nutzen, wenn du Xbox- oder Game-Pass-Spiele online spielst.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows
