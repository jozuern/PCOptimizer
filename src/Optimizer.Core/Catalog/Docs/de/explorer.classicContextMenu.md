# Klassisches Rechtsklickmenü

## Zusammenfassung
Holt das vollständige Rechtsklickmenü im Stil von Windows 10 in den Datei-Explorer zurück, ohne den Zwischenschritt „Weitere Optionen anzeigen“.

## So funktioniert es
Ein leerer InprocServer32-Schlüssel für eine bestimmte CLSID in deinem Benutzerprofil lässt den Explorer das neue, kompakte Menü überspringen [1]. Rückgängig machen entfernt den Schlüssel wieder. Melde dich ab und wieder an (oder starte den Explorer neu), damit es wirkt.

## Warum es helfen kann
Schneller Zugriff auf alle Menüeinträge (zum Beispiel Packprogramme).

## Belege
Kein Effekt auf die Leistung.

## Nachteile & Risiken
Eine reine Bedienänderung. Ein künftiges Windows-Update ignoriert sie womöglich.

## Wann du es nicht nutzen solltest
Behalte das neue Menü, wenn es dir gefällt.

## Quellen
1. https://github.com/ChrisTitusTech/winutil
