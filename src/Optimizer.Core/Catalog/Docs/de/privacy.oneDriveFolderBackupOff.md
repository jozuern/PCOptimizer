# OneDrive: keine Sicherung von Desktop, Dokumente und Bilder

## Zusammenfassung
OneDrive kann deine Ordner Desktop, Dokumente und Bilder nicht mehr in OneDrive verschieben und fragt nicht mehr danach. Bereits verschobene Ordner bleiben in OneDrive.

## So funktioniert es
Die OneDrive-Richtlinie "Benutzer daran hindern, ihre bekannten Windows-Ordner in OneDrive zu verschieben" (KFMBlockOptIn = 1) sperrt das Verschieben dieser Ordner in jedes OneDrive-Konto; es erscheint keine Aufforderung mehr, die Ordner zu schützen, und der Befehl zum Verwalten der Sicherung ist gesperrt [1]. Bereits verschobene Ordner bleiben in OneDrive [1].

## Warum es helfen kann
Desktop, Dokumente und Bilder bleiben auf diesem PC und werden nicht durch einen Klick auf eine Aufforderung hochgeladen.

## Belege
Eine von Microsoft beschriebene OneDrive-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Keine automatische Cloud-Sicherung dieser Ordner; mach eigene Sicherungen. Ordner, die schon in OneDrive liegen, verschiebst du in den OneDrive-Einstellungen zurück.

## Wann du es nicht nutzen solltest
Wenn OneDrive diese Ordner sichern soll.

## Quellen
1. https://learn.microsoft.com/en-us/sharepoint/use-group-policy
