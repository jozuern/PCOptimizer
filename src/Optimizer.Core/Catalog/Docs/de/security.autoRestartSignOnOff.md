# Keine automatische Anmeldung nach Neustarts

## Zusammenfassung
Nach einem Neustart oder Update meldet Windows dich nicht mehr automatisch an und sperrt die Sitzung; du meldest dich selbst an. Pro, Enterprise und Education.

## So funktioniert es
Standardmäßig meldet Windows nach einem Neustart oder Herunterfahren den letzten Benutzer automatisch an und sperrt die Sitzung, wenn er sich nicht abgemeldet hatte [1]. Auf PCs ohne Domäne gilt das für Update-Neustarts und eigene Neustarts [1]. Die App schaltet die Richtlinie aus (DisableAutomaticRestartSignOn = 1) [1].

## Warum es helfen kann
Apps deines Kontos starten nicht im Hintergrund, bevor du am PC bist.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Nach einem Update-Neustart warten Apps, die bei der Anmeldung starten, bis du dich selbst anmeldest.

## Wann du es nicht nutzen solltest
Wenn deine Apps nach einem Update-Neustart bereit sein sollen.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-windowslogon
