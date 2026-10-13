# Kein „Wiedergabe auf Gerät“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Wiedergabe auf Gerät“ aus dem klassischen Rechtsklickmenü von Videos und Bildern. Nicht dokumentiert; ein VM-Test hat es bestätigt.

## So funktioniert es
„Wiedergabe auf Gerät“ schickt ein Video oder ein Bild per DLNA an ein anderes Gerät im Netzwerk [1]. Der Eintrag stammt aus einer Shell-Erweiterung, einer kleinen Komponente, die dem klassischen Rechtsklickmenü (Weitere Optionen anzeigen) eigene Einträge hinzufügt. Der Explorer lädt keine Shell-Erweiterungen, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {7AD84985-87B4-4a16-BE58-8B72A5B390F7}, die Erweiterung hinter „Wiedergabe auf Gerät“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Wiedergabe auf Gerät“ fehlte im klassischen Menü einer Videodatei und war nach dem Entfernen des Werts wieder da. Im VM-Test fehlte der Eintrag schon ohne Neustart des Explorers in jedem danach geöffneten Menü. Zeigt ein Menü ihn noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Ohne DLNA-Gerät im Netzwerk ist der Eintrag im Menü jeder Mediendatei nur Ballast.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich.

## Nachteile & Risiken
Die Wiedergabe auf einem anderen Gerät kannst du dann nicht mehr über das Rechtsklickmenü starten. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du mit diesem Eintrag Videos oder Bilder an ein anderes Gerät schickst.

## Quellen
1. https://www.howtogeek.com/720449/how-to-remove-cast-to-device-option-from-windows-10-context-menu/
