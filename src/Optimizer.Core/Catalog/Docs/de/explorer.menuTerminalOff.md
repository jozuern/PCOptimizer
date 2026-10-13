# Kein „Im Terminal öffnen“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Im Terminal öffnen“ aus dem Windows-11-Menü und dem klassischen Rechtsklickmenü von Ordnern. Terminal bleibt installiert. Nicht dokumentiert; ein VM-Test hat es bestätigt.

## So funktioniert es
Windows Terminal, das bei Windows 11 vorinstalliert ist, fügt dem Rechtsklickmenü von Ordnern über eine Shell-Erweiterung „Im Terminal öffnen“ hinzu [1]. Der Explorer lädt keine Shell-Erweiterungen, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {9F156763-7844-4DC4-B2B1-901F640F5155}, die Erweiterung hinter „Im Terminal öffnen“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Im Terminal öffnen“ fehlte im Windows-11-Menü und im klassischen Menü eines Ordners und war nach dem Entfernen des Werts wieder da. Im VM-Test fehlte der Eintrag schon ohne Neustart des Explorers in jedem danach geöffneten Menü. Zeigt ein Menü ihn noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Wenn du selten eine Kommandozeile brauchst, belegt der Eintrag in jedem Ordnermenü eine Zeile.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Terminal bleibt installiert und lässt sich wie gewohnt über das Startmenü öffnen. Windows Terminal Preview fügt über eine eigene Erweiterung einen eigenen Eintrag hinzu [1]; den lässt diese Optimierung stehen. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du über diesen Eintrag eine Kommandozeile in einem Ordner öffnest.

## Quellen
1. https://winaero.com/how-to-remove-open-in-windows-terminal-context-menu-in-windows-11/
