# Übermittlungsoptimierung (Peer-to-Peer) aus

## Zusammenfassung
Verhindert, dass Windows Update-Daten an andere PCs hochlädt und von ihnen herunterlädt. Updates kommen dann nur von Microsofts Servern.

## So funktioniert es
Die Übermittlungsoptimierung teilt Update-Daten von Windows und Store zwischen PCs. Die Richtlinie DODownloadMode = 0 beschränkt sie auf reine HTTP-Downloads von Microsoft [1]. Weil es ein Richtlinienwert ist, zeigen die Windows-Einstellungen auf dieser Seite „Einige Einstellungen werden von Ihrer Organisation verwaltet“.

## Warum es helfen kann
Keine Uploads an andere PCs im Hintergrund, die beim Online-Spielen Upload-Bandbreite belegen können.

## Belege
Die Upload-Bandbreite ist standardmäßig schon begrenzt. Auf den Ping wirkt sich das nur bei langsamen Leitungen spürbar aus.

## Nachteile & Risiken
Mehrere PCs in deinem Heimnetz laden Updates einzeln herunter.

## Wann du es nicht nutzen solltest
Bei schnellen Leitungen nicht nötig, ebenso wenn du Peer-to-Peer im lokalen Netz nutzen willst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference
2. https://github.com/ChrisTitusTech/winutil
