# Lange Dateipfade

## Zusammenfassung
Erlaubt Apps, die es unterstützen, Dateipfade mit mehr als 260 Zeichen. Hilft bei tief verschachtelten Projektordnern, etwa in Entwicklerwerkzeugen und Mod-Managern.

## So funktioniert es
Windows begrenzt Pfade auf 260 Zeichen, außer LongPathsEnabled = 1 ist gesetzt und die App meldet in ihrem Manifest Unterstützung für lange Pfade [1][2]. Jeder Prozess liest den Wert einmal, ein Neustart sorgt dafür, dass alle Apps ihn sehen [1].

## Warum es helfen kann
Kopieren oder Entpacken tief verschachtelter Ordner scheitert in Apps mit Unterstützung nicht mehr an zu langen Pfaden.

## Belege
Von Microsoft beschrieben [1][2]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Apps ohne Unterstützung für lange Pfade können solche Pfade weiter nicht nutzen [1].

## Wann du es nicht nutzen solltest
Wenn du nie mit tief verschachtelten Ordnern arbeitest.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-filesys
