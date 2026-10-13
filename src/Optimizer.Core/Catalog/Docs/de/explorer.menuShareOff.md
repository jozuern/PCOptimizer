# Kein „Teilen“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Teilen“ aus dem klassischen Rechtsklickmenü von Dateien und die Schaltfläche Teilen aus dem Windows-11-Menü. Nicht dokumentiert; ein VM-Test hat es bestätigt.

## So funktioniert es
„Teilen“ öffnet das Teilen-Fenster von Windows, das eine Datei an eine App schickt, die Teilen unterstützt [1]. Der Eintrag stammt aus einer Shell-Erweiterung: Sie fügt ihn dem klassischen Rechtsklickmenü (Weitere Optionen anzeigen) hinzu und die Schaltfläche Teilen dem Windows-11-Menü. Der Explorer lädt keine Shell-Erweiterungen, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {e2bf9676-5f8f-435c-97eb-11607a5bedf7}, die Erweiterung hinter „Teilen“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Teilen“ fehlte im klassischen Menü einer Videodatei (ebenso die Schaltfläche Teilen in deren Windows-11-Menü) und war nach dem Entfernen des Werts wieder da. Im VM-Test fehlte der Eintrag schon ohne Neustart des Explorers in jedem danach geöffneten Menü. Zeigt ein Menü ihn noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Wenn du nie Dateien über das Teilen-Fenster verschickst, sind Eintrag und Schaltfläche in jedem Dateimenü nur Ballast.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Das Teilen-Fenster lässt sich nicht mehr über das Menü einer Datei öffnen. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du Dateien über Teilen an andere Apps schickst.

## Quellen
1. https://winaero.com/remove-share-context-menu-windows-10/
