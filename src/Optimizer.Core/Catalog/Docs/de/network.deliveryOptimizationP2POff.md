# Übermittlungsoptimierung (Peer-to-Peer) aus

## Zusammenfassung
Verhindert, dass Windows Update-Daten mit anderen PCs teilt. Updates kommen dann nur von Microsofts Servern. Standardmäßig teilt Windows nur im lokalen Netz.

## So funktioniert es
Die Übermittlungsoptimierung kann Updates für Windows und Store von anderen PCs laden und an andere PCs hochladen. Im Standardmodus teilt sie nur mit PCs in deinem lokalen Netz [1]. Die Richtlinie DODownloadMode = 0 schaltet Peer-to-Peer ab und lässt nur normale HTTP-Downloads von Microsoft zu [1][2]. Weil es ein Richtlinienwert ist, zeigen die Windows-Einstellungen auf dieser Seite einen Hinweis, dass Einstellungen von deiner Organisation verwaltet werden.

## Warum es helfen kann
Keine Uploads an andere PCs im Hintergrund. Für Online-Spiele zählt das vor allem, wenn du Uploads an PCs im Internet erlaubt hattest oder andere PCs im Heimnetz über ein langsames WLAN von deinem PC laden.

## Belege
Im Standardmodus bleiben Uploads im lokalen Netz und belasten deinen Internet-Upload nicht [1]. Uploads an PCs im Internet gibt es nur, wenn du diese Option eingeschaltet hast, und sie sind standardmäßig auf 20 GB pro Monat begrenzt [1].

## Nachteile & Risiken
Mehrere PCs in deinem Heimnetz laden Updates einzeln herunter.

## Wann du es nicht nutzen solltest
Bei schnellen Leitungen nicht nötig, ebenso wenn du Peer-to-Peer im lokalen Netz nutzen willst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deliveryoptimization
