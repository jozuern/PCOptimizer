# Kein „Zugriff gewähren auf“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Zugriff gewähren auf“ aus dem klassischen Rechtsklickmenü und die Registerkarte Freigabe aus den Eigenschaften von Ordnern und Laufwerken. Nicht dokumentiert; ein VM-Test hat es bestätigt.

## So funktioniert es
Der Explorer baut das klassische Rechtsklickmenü (Weitere Optionen anzeigen) zum Teil aus Shell-Erweiterungen auf, kleinen Komponenten, die eigene Einträge hinzufügen. Der Explorer lädt keine Shell-Erweiterungen, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}, die Erweiterung hinter „Zugriff gewähren auf“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Zugriff gewähren auf“ fehlte im klassischen Menü eines Ordners und war nach dem Entfernen des Werts wieder da. Im VM-Test fehlte der Eintrag schon ohne Neustart des Explorers in jedem danach geöffneten Menü. Zeigt ein Menü ihn noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Auf einem PC, der nie Ordner im Netzwerk freigibt, ist der Eintrag in jedem Ordnermenü nur Ballast.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Dieselbe Erweiterung liefert die Registerkarte Freigabe in den Eigenschaften von Ordnern und Laufwerken: Im VM-Test fehlte auch sie und war nach Rückgängig wieder da. Willst du dann einen Ordner im Explorer freigeben, nutze zuerst Rückgängig. Bereits freigegebene Ordner bleiben freigegeben. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du über dieses Menü oder die Registerkarte Freigabe Ordner für andere in deinem Netzwerk freigibst.

## Quellen
1. https://www.majorgeeks.com/content/page/remove_the_give_access_to_context_menu_in_windows_10.html
