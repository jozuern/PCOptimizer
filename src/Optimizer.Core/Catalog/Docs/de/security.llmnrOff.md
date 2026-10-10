# LLMNR aus

## Zusammenfassung
Schaltet die Link-Local-Multicast-Namensauflösung aus, die alle Geräte im lokalen Netz nach einem Namen fragt, den DNS nicht fand. Kann Namensauflösung in kleinen Netzen ohne DNS stören.

## So funktioniert es
LLMNR schickt Namensanfragen per Multicast an andere Geräte im selben Subnetz und funktioniert ohne DNS-Server [1]. Mit der Richtlinie "Multicastnamensauflösung deaktivieren" (EnableMulticast = 0) ist LLMNR auf allen Netzwerkadaptern aus [1].

## Warum es helfen kann
Ein Angreifer im selben Netz kann diese Rundrufe nicht beantworten, um Anmeldeversuche abzufangen, etwa im öffentlichen WLAN.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Geräte, die nur über LLMNR gefunden werden, etwa manche Drucker oder NAS im Heimnetz ohne DNS-Namen, brauchen eventuell ihre IP-Adresse.

## Wann du es nicht nutzen solltest
Wenn du Geräte im Heimnetz über ihren Namen erreichst und das nicht mehr klappt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-dnsclient
