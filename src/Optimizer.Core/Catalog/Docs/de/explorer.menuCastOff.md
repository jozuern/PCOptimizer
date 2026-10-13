# Kein „Auf Gerät übertragen“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Auf Gerät übertragen“ aus dem klassischen Rechtsklickmenü von Videos, Musik und Bildern. Der Wert ist nicht dokumentiert; ein VM-Test hat ihn bestätigt.

## So funktioniert es
„Auf Gerät übertragen“ schickt ein Video, ein Lied oder ein Bild per DLNA an einen Fernseher oder ein anderes Gerät im Netzwerk [1]. Der Eintrag stammt aus einer Shell-Erweiterung, einer kleinen Komponente, die dem klassischen Rechtsklickmenü (Weitere Optionen anzeigen) eigene Einträge hinzufügt. Der Explorer lädt keine Shell-Erweiterung, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {7AD84985-87B4-4a16-BE58-8B72A5B390F7}, die Erweiterung hinter „Auf Gerät übertragen“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Auf Gerät übertragen“ fehlte im klassischen Menü einer Videodatei und war nach dem Entfernen des Werts wieder da. Menüs, die nach der Änderung geöffnet wurden, zeigten die Änderung schon ohne Neustart des Explorers; zeigt ein Menü den Eintrag noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Ohne DLNA-Fernseher oder anderes DLNA-Gerät im Netzwerk ist der Eintrag im Menü jeder Mediendatei nur Ballast.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Übertragen lässt sich nicht mehr über das Rechtsklickmenü starten. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du mit diesem Eintrag Videos oder Musik an einen Fernseher schickst.

## Quellen
1. https://www.howtogeek.com/720449/how-to-remove-cast-to-device-option-from-windows-10-context-menu/
