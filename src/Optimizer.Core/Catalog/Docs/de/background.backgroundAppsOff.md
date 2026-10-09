# Hintergrund-Apps aus

## Zusammenfassung
Verhindert, dass Store-Apps im Hintergrund laufen. Wirkung auf aktuellen Windows-11-Builds umstritten.

## So funktioniert es
Der Wert GlobalUserDisabled = 1 blockiert für deinen Benutzer die Hintergrundaktivität von Paket-Apps (Store) [1]. Windows 11 verwaltet Hintergrundberechtigungen pro App, und nicht jeder Build beachtet den globalen Wert.

## Warum es helfen kann
Weniger Store-Apps, die im Hintergrund aufwachen.

## Belege
Desktop-Programme (Steam, Discord, Launcher) sind nicht betroffen. Der messbare Effekt auf Spiele ist meist null.

## Nachteile & Risiken
Store-Apps wie Mail, Kalender oder Smartphone-Link senden keine Benachrichtigungen mehr und synchronisieren nicht im Hintergrund.

## Wann du es nicht nutzen solltest
Behalte Hintergrund-Apps, wenn du auf Benachrichtigungen von Store-Apps angewiesen bist.

## Quellen
1. https://github.com/ChrisTitusTech/winutil
