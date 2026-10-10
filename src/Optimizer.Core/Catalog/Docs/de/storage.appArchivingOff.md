# Keine automatische Archivierung selten genutzter Apps

## Zusammenfassung
Windows archiviert selten genutzte Store-Apps nicht mehr; sie bleiben vollständig installiert. Pro, Enterprise und Education.

## So funktioniert es
Standardmäßig sucht Windows regelmäßig nach selten genutzten Apps und archiviert sie, und du kannst das selbst ändern [1]. AllowAutomaticAppArchiving = 0 verbietet es ausdrücklich: Windows archiviert keine Apps [1].

## Warum es helfen kann
Selten genutzte Store-Apps und -Spiele bleiben startbereit, auch ohne Internetverbindung.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Ungenutzte Apps belegen weiter Speicherplatz.

## Wann du es nicht nutzen solltest
Auf einem PC mit wenig freiem Speicher, wo das Archivieren hilft.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
