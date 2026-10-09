# Öffentliche DNS-Server

## Zusammenfassung
Stellt die IPv4-DNS-Server der aktiven Netzwerkadapter auf einen öffentlichen Anbieter. Ändert, wie schnell Namen aufgelöst werden, nicht deinen Ping in Spielen.

## So funktioniert es
Jede Verbindung beginnt mit dem Auflösen eines Namens (zum Beispiel des Login-Servers eines Spiels). Meist antwortet der DNS-Server des Routers oder Providers. Die App stellt die DNS-Server jedes aktiven Adapters über die Windows-Netzwerkkonfiguration auf den gewählten Anbieter [1]. IPv6-DNS-Einstellungen bleiben unverändert. Rückgängig machen stellt die vorherige Einstellung wieder her, auch „automatisch“ (DHCP).

## Warum es helfen kann
Ein langsamer oder unzuverlässiger DNS-Server verzögert jede neue Verbindung: Launcher, Shopseiten, Matchmaking. Der DNS-Test auf der Netzwerkseite misst, welcher Server von deinem Anschluss aus am schnellsten antwortet.

## Belege
DNS dient nur dazu, einen Server zu finden. Ist das Spiel verbunden, spielt DNS keine Rolle mehr, Ping und Bildrate ändern sich nicht. Deshalb ist die Wirkung mit 0 bewertet.

## Nachteile & Risiken
Die DNS-Server mancher Provider finden nahe Download-Server besser. Firmen- oder Schulnetze verlangen eventuell ihren eigenen DNS. Die öffentlichen Anbieter haben unterschiedliche Datenschutzregeln [2][3][4].

## Wann du es nicht nutzen solltest
In Firmen- oder Schulnetzen oder wenn der DNS deines Providers im Test schnell ist.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/setdnsserversearchorder-method-in-class-win32-networkadapterconfiguration
2. https://developers.cloudflare.com/1.1.1.1/ip-addresses/
3. https://developers.google.com/speed/public-dns/docs/using
4. https://www.quad9.net/service/service-addresses-and-features/
