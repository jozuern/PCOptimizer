# Klassisches Rechtsklickmenü

## Zusammenfassung
Holt das vollständige Rechtsklickmenü im Stil von Windows 10 in den Datei-Explorer zurück, ohne den Zwischenschritt „Weitere Optionen anzeigen“.

## So funktioniert es
Ein leerer InprocServer32-Eintrag für die CLSID des neuen Explorer-Menüs in deinem Benutzerprofil überschreibt die Systemregistrierung, sodass der Explorer auf das klassische Menü zurückfällt. Microsoft dokumentiert das nicht; es ist aus Community-Tools bekannt [1]. Rückgängig machen entfernt die Einträge, die die App angelegt hat, und lässt vorher vorhandene Schlüssel stehen. Melde dich ab und wieder an (oder starte den Explorer neu), damit es wirkt.

## Warum es helfen kann
Schneller Zugriff auf alle Menüeinträge (zum Beispiel Packprogramme).

## Belege
Kein Effekt auf die Leistung.

## Nachteile & Risiken
Eine reine Bedienänderung. Weil sie undokumentiert ist, ignoriert ein künftiges Windows-Update sie womöglich.

## Wann du es nicht nutzen solltest
Behalte das neue Menü, wenn es dir gefällt.

## Quellen
1. https://github.com/ChrisTitusTech/winutil
