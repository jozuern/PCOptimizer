# Netzwerk-Drosselung aus (NetworkThrottlingIndex)

## Zusammenfassung
Hebt die Grenze auf, die MMCSS während Multimedia-Wiedergabe für die Netzwerkverarbeitung setzt. Oft für Spiele empfohlen, Wirkung umstritten.

## So funktioniert es
Solange Multimedia-Threads laufen, begrenzt MMCSS die übrige Netzwerkverarbeitung, um Audio- und Videowiedergabe zu schützen [1]. NetworkThrottlingIndex = 0xFFFFFFFF schaltet diese Grenze ab.

## Warum es helfen kann
Streamt ein Spiel Musik oder Sprache über bei MMCSS angemeldete Threads, werden Netzwerkpakete nicht mehr zurückgehalten.

## Belege
Die Grenze liegt bei etwa 10 Paketen pro Millisekunde, weit mehr als ein Spiel sendet. Messungen zeigen beim Spieleverkehr keinen Unterschied.

## Nachteile & Risiken
Auf alter Hardware könnte Audio bei extremer Netzwerklast stocken.

## Wann du es nicht nutzen solltest
Nicht nötig. Behalte es nur, wenn ein bestimmtes Spiel bei dir davon profitiert.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
