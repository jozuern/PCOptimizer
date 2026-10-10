# DNS über HTTPS für Cloudflare, Google und Quad9

## Zusammenfassung
Nutzt ein Adapter Cloudflare-, Google- oder Quad9-DNS, schickt Windows die Anfragen verschlüsselt über HTTPS. Wirkt nur mit einem dieser DNS-Server.

## So funktioniert es
Windows kennt die DNS-über-HTTPS-Adressen (DoH) dieser Anbieter. Mit AutoUpgrade nutzt Windows DoH automatisch für einen bekannten Server, den ein Adapter verwendet [1][2]. Die App schaltet AutoUpgrade für ihre IPv4-Adressen mit Set-DnsClientDohServerAddress ein und lässt die Einstellung für den Rückfall unverändert [2]. Kombiniere es mit einer der DNS-Voreinstellungen oben.

## Warum es helfen kann
Deine DNS-Anfragen sind verschlüsselt, das Netz, in dem du bist (öffentliches WLAN, dein Provider), kann nicht lesen oder ändern, welche Seiten du aufrufst.

## Belege
Von Microsoft beschrieben [1][2]. DoH baut zu jedem Server eine HTTPS-Verbindung auf; DNS findet nur Server, Ping und Bildrate in Spielen ändern sich nicht.

## Nachteile & Risiken
Sperrt ein Netz DoH und erlaubt der Server keinen Rückfall auf normales DNS, lassen sich keine Namen auflösen, bis du die Änderung rückgängig machst oder andere DNS-Server wählst. Ist keiner dieser Anbieter als DNS-Server gesetzt, ändert sich nichts.

## Wann du es nicht nutzen solltest
In Firmen- oder Schulnetzen mit eigenem DNS oder in Netzen, die verschlüsseltes DNS sperren.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/networking/dns/doh-client-support
2. https://learn.microsoft.com/en-us/powershell/module/dnsclient/set-dnsclientdohserveraddress
