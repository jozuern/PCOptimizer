# Hintergrundbild ohne erneute JPEG-Komprimierung

## Zusammenfassung
Windows behält JPEG-Hintergrundbilder in voller Qualität, statt sie auf 85 % neu zu komprimieren. Melde dich neu an und setz das Bild erneut. Nicht dokumentierter Wert, getestet.

## So funktioniert es
Wenn du ein JPEG-Bild als Hintergrund festlegst, speichert Windows eine neu komprimierte Kopie mit 85 % Qualität [1]. JPEGImportQuality = 100 unter Control Panel\Desktop behält die volle Qualität [1]. Das gilt für das nächste Hintergrundbild, das du nach einer neuen Anmeldung festlegst [1]. Ein Hintergrundbild, das Windows schon umgewandelt hat, bleibt so, bis du ein neues Bild festlegst. Microsoft dokumentiert den Wert nicht.

## Warum es helfen kann
Schärfere Hintergrundbilder ohne JPEG-Artefakte, sichtbar auf großen oder hochauflösenden Bildschirmen.

## Belege
In einem VM-Test mit Windows 11 Build 26300 wurde ein JPEG, das nach einer neuen Anmeldung festgelegt wurde, mit Qualität 100 gespeichert; ohne den Wert ein neues mit etwa 85. Kein Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Die gespeicherte Kopie des Hintergrundbilds wird größer. PNG-Bilder sind nicht betroffen [1].

## Wann du es nicht nutzen solltest
Wenn du kein JPEG als Hintergrundbild nutzt.

## Quellen
1. https://winaero.com/how-to-disable-wallpaper-jpeg-quality-reduction-in-windows-10
