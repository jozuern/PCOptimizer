# Starttyp eines Dienstes ändern

## Zusammenfassung
Ändert, wann dieser Dienst startet: automatisch mit Windows, bei Bedarf (Manuell) oder nie (Deaktiviert).

## So funktioniert es
Der Starttyp wird über den Dienststeuerungs-Manager geändert [1], wie in der Konsole Dienste. Ein Dienst auf Manuell kann weiterhin von einer App oder einem Windows-Auslöser gestartet werden. Rückgängig machen stellt den vorherigen Starttyp wieder her.

## Warum es helfen kann
Dienste, die mit Windows starten, belegen Arbeitsspeicher und können im Hintergrund arbeiten. Bei Bedarf laufen sie nur, wenn etwas sie braucht.

## Belege
Die meisten Dienste nutzen im Leerlauf fast keine Prozessorzeit, der Effekt auf die Bildrate ist meist nicht messbar.

## Nachteile & Risiken
Ein deaktivierter Dienst kann von nichts gestartet werden, das ihn braucht, was Funktionen beschädigen kann [1]. Manuell ist die sicherere Wahl.

## Wann du es nicht nutzen solltest
Bei Diensten, die du nicht kennst; Windows-Dienste, die die App nicht erklärt, sind schreibgeschützt. Deaktiviert wird nur für Dienste anderer Hersteller angeboten.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfigw
