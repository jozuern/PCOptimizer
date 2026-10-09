# Alten MPO-Wert entfernen (OverlayTestMode)

## Zusammenfassung
Entfernt OverlayTestMode aus den DWM-Einstellungen. Ältere Anleitungen setzen ihn auf 5, um MPO abzuschalten. Ab 24H2 wirkt das nicht mehr und wird mit schwarzen Blitzen in Verbindung gebracht.

## So funktioniert es
Der Wert OverlayTestMode = 5 unter HKLM\SOFTWARE\Microsoft\Windows\Dwm schaltete früher Multiplane Overlay ab. Nutzer berichten, dass er seit 24H2 nicht mehr wirkt und schwarze Blitze verursachen kann [1]. Das Löschen stellt den Windows-Standard wieder her. Wirkt nach einem Neustart.

## Warum es helfen kann
Entfernt einen Wert, der seine eigentliche Wirkung verloren hat, aber Anzeigefehler verursachen kann.

## Belege
Beruht auf Berichten aus der Community zu 24H2 und neuer. Eine Microsoft-Dokumentation des Werts gibt es nicht.

## Nachteile & Risiken
Brauchst du MPO weiterhin aus, nutze stattdessen den Tweak „Multiplane Overlay (MPO) aus“.

## Wann du es nicht nutzen solltest
Nichts zu beachten, wenn der Wert existiert. Existiert er nicht, ist dieser Punkt bereits sauber.

## Quellen
1. https://forums.guru3d.com/goto/post?id=6305178
