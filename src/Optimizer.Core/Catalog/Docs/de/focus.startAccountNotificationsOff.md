# Start: keine Kontobenachrichtigungen

## Zusammenfassung
Beendet die Konto-Hinweise am Benutzerbild im Start, etwa zu Gerätesicherung, Cloudspeicher oder Abos. Ab Pro.

## So funktioniert es
Die Richtlinie "Kontobenachrichtigungen im Startmenü deaktivieren" verhindert, dass Windows Benachrichtigungen für Microsoft-Konten und lokale Benutzer an der Benutzerkachel im Start zeigt [1]. Dazu gehören Aufforderungen, sich erneut anzumelden, das Gerät zu sichern, Cloudspeicher zu verwalten oder ein Microsoft 365- oder Xbox-Abo zu verwalten [1]. Die App setzt DisableAccountNotifications im Richtlinienschlüssel des Benutzers auf 1.

## Warum es helfen kann
Weniger Hinweise im Start, die Konten, Sicherung oder Abos bewerben.

## Belege
Microsoft dokumentiert die Richtlinie für Pro, Enterprise und Education ab Windows 11 24H2 [1]. Auf Bildrate oder Latenz wirkt sie nicht.

## Nachteile & Risiken
Du verpasst eventuell einen wichtigen Hinweis, etwa eine nötige Anmeldung. Solche Hinweise bekommst du weiter über die Einstellungen und die Apps selbst.

## Wann du es nicht nutzen solltest
Wenn du dich auf diese Erinnerungen verlässt, etwa für die Sicherung.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-notifications
