# Brave: ohne Rewards, Wallet und VPN

## Zusammenfassung
Entfernt Brave Rewards mit seiner Werbung, die eingebaute Krypto-Wallet mit Web3-Funktionen und das kostenpflichtige Brave VPN aus dem Browser.

## So funktioniert es
Drei Brave-Richtlinien unter HKLM\SOFTWARE\Policies\BraveSoftware\Brave [1]: BraveRewardsDisabled blendet Rewards aus, also keine Brave-Werbung und keine Token; BraveWalletDisabled entfernt Wallet, Web3 und dezentrales DNS; BraveVPNDisabled entfernt die VPN-Schaltfläche und Abo-Angebote [1].

## Warum es helfen kann
Ein Browser ohne Krypto- und Abo-Angebote. Bildrate und Latenz ändern sich nicht.

## Belege
Von Brave beschriebene Richtlinien [1].

## Nachteile & Risiken
Wenn du Wallet, Rewards oder das VPN nutzt, funktionieren sie in Brave nicht mehr. Weil es Richtlinien sind, zeigt Brave im Menü "Von deiner Organisation verwaltet", und brave://policy listet sie auf. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du Brave Rewards, die Wallet oder Brave VPN nutzt.

## Quellen
1. https://support.brave.app/hc/en-us/articles/360039248271-Group-Policy
