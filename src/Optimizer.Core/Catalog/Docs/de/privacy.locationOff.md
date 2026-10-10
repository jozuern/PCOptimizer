# Standortzugriff aus

## Zusammenfassung
Schaltet die Standortdienste für den ganzen PC ab, wie der Schalter „Standortdienste“ in den Einstellungen.

## So funktioniert es
Der geräteweite Einwilligungswert für den Standort wird auf „Deny“ gesetzt. Das ist der Wert hinter Einstellungen > Datenschutz und Sicherheit > Standort > Standortdienste, den nur Administratoren ändern können [1]. Windows und Apps bekommen dann keinen Gerätestandort mehr [1]. Microsofts Anleitung zum Verwalten der Verbindungen von Windows nennt denselben Schalter, um den Standort für ein Gerät abzuschalten [2].

## Warum es helfen kann
Keine Standortabfragen im Hintergrund. Kein Effekt auf die Leistung.

## Belege
Eine Datenschutzeinstellung ohne Effekt auf die FPS.

## Nachteile & Risiken
Apps, die automatische Zeitzone und „Mein Gerät suchen“ verlieren den Gerätestandort [1]. Manche Funktionen, etwa das Wetter in der Taskleiste, können weiter deine IP-Adresse nutzen, und ein Notruf übermittelt deinen Standort trotzdem [1]. Die eigene WLAN-Band-Prüfung der App zeigt danach „unbekannt“: Windows gibt WLAN-Details nur an Apps weiter, die den Standort nutzen dürfen [3].

## Wann du es nicht nutzen solltest
Lass den Standort an, wenn du Apps oder Funktionen nutzt, die ihn brauchen.

## Quellen
1. https://support.microsoft.com/en-us/windows/windows-location-service-and-privacy-3a8eee0a-5b0b-dc07-eede-2a5ca1c49088
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
3. https://learn.microsoft.com/en-us/windows/win32/nativewifi/wi-fi-access-location-changes
