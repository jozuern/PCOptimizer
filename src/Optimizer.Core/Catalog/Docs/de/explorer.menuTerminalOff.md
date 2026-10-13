# Kein „Im Terminal öffnen“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Im Terminal öffnen“ aus dem Rechtsklickmenü von Ordnern, im Windows-11-Menü und im klassischen. Terminal bleibt installiert. Der Wert ist nicht dokumentiert; ein VM-Test hat ihn bestätigt.

## So funktioniert es
Windows Terminal, das mit Windows 11 kommt, fügt dem Rechtsklickmenü von Ordnern über eine Shell-Erweiterung „Im Terminal öffnen“ hinzu [1]. Der Explorer lädt keine Shell-Erweiterung, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {9F156763-7844-4DC4-B2B1-901F640F5155}, die Erweiterung hinter „Im Terminal öffnen“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Im Terminal öffnen“ fehlte im Windows-11-Menü und im klassischen Menü eines Ordners und war nach dem Entfernen des Werts wieder da. Menüs, die nach der Änderung geöffnet wurden, zeigten die Änderung schon ohne Neustart des Explorers; zeigt ein Menü den Eintrag noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Wenn du selten eine Kommandozeile brauchst, belegt der Eintrag in jedem Ordnermenü eine Zeile.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Terminal bleibt installiert und startet wie gewohnt über Start. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du über diesen Eintrag eine Kommandozeile in einem Ordner öffnest.

## Quellen
1. https://winaero.com/how-to-remove-open-in-windows-terminal-context-menu-in-windows-11/
