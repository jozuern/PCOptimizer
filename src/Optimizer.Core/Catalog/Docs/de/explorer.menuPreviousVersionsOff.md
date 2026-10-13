# Kein „Vorgängerversionen wiederherstellen“ im Rechtsklickmenü

## Zusammenfassung
Entfernt „Vorgängerversionen wiederherstellen“ aus dem klassischen Rechtsklickmenü und die Registerkarte Vorgängerversionen aus den Eigenschaften. Nicht dokumentiert; ein VM-Test hat es bestätigt.

## So funktioniert es
Der Eintrag öffnet ältere Kopien einer Datei oder eines Ordners, die der Dateiversionsverlauf, Sicherungen oder Wiederherstellungspunkte aufbewahrt haben [1]. Er stammt aus einer Shell-Erweiterung, einer kleinen Komponente, die dem klassischen Rechtsklickmenü (Weitere Optionen anzeigen) eigene Einträge hinzufügt. Der Explorer lädt keine Shell-Erweiterung, deren CLSID als Wert unter HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked steht. Die App trägt {596AB062-B4D2-4215-9F74-E9109B0A8153}, die Erweiterung hinter „Vorgängerversionen wiederherstellen“, in diese Liste ein [1]. Diese Liste ist nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat es bestätigt: „Vorgängerversionen wiederherstellen“ fehlte im klassischen Menü eines Ordners und einer Videodatei und war nach dem Entfernen des Werts wieder da. Menüs, die nach der Änderung geöffnet wurden, zeigten die Änderung schon ohne Neustart des Explorers; zeigt ein Menü den Eintrag noch, melde dich ab und wieder an [1].

## Warum es helfen kann
Wenn du ältere Kopien nie auf diesem Weg zurückholst, ist der Eintrag in jedem Menü nur Ballast.

## Belege
Keine Auswirkung auf die Leistung; nur das Menü ändert sich. Sicherungen, Dateiversionsverlauf und Wiederherstellungspunkte bleiben, wie sie sind.

## Nachteile & Risiken
Dieselbe Erweiterung zeichnet die Registerkarte Vorgängerversionen in den Eigenschaften (registriert für Dateien, Ordner und Laufwerke): Im VM-Test fehlte sie auch in den Eigenschaften eines Ordners und war nach Rückgängig wieder da. Ältere Kopien holst du dann nur noch über den Dateiversionsverlauf oder dein Sicherungsprogramm selbst zurück. Weil der Wert nicht dokumentiert ist, könnte eine künftige Windows-Version ihn ignorieren; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du den Dateiversionsverlauf oder Sicherungen nutzt und ältere Kopien über dieses Menü zurückholst.

## Quellen
1. https://winaero.com/remove-previous-versions-windows-10/
