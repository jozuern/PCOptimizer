# Benachrichtigungen ohne Ton

## Zusammenfassung
Benachrichtigungen erscheinen weiter, spielen aber für alle Apps keinen Ton mehr.

## So funktioniert es
In den Einstellungen lässt sich der Ton für Benachrichtigungen ein- oder ausschalten [1]. Die App setzt den globalen Wert NOC_GLOBAL_SETTING_ALLOW_NOTIFICATION_SOUND auf 0, der für alle Apps gilt. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt denselben Wert, den Windows beim Umlegen des Schalters speichert. Auf einer echten Windows-11-Installation (26H2) haben wir geprüft: Die Änderung wirkt, und Rückgängig machen stellt den alten Zustand wieder her.

## Warum es helfen kann
Keine Benachrichtigungstöne über dem Spielsound oder im Sprachchat.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Du verpasst eventuell Benachrichtigungen, wenn du nicht auf den Bildschirm schaust.

## Wann du es nicht nutzen solltest
Wenn du dich bei Erinnerungen oder Nachrichten auf Töne verlässt.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/notifications-and-do-not-disturb-in-windows
