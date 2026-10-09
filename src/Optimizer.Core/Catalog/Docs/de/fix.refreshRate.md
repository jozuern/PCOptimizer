# Bildschirm auf höchste Bildwiederholrate stellen

## Zusammenfassung
Stellt den Monitor auf die höchste Bildwiederholrate, die Windows bei der aktuellen Auflösung anbietet, wie die Auswahl unter Einstellungen > Bildschirm.

## So funktioniert es
Die App ändert den Anzeigemodus über die Windows-Anzeige-API: gleiche Auflösung, höchste angebotene Bildwiederholrate [1]. Die Änderung wird für den Bildschirm gespeichert. Rückgängig machen stellt die vorherige Rate wieder ein.

## Warum es helfen kann
Mit 144 Hz statt 60 Hz zeigt der Bildschirm alle 6,9 ms statt alle 16,7 ms ein neues Bild: flüssigere Bewegung und weniger Verzögerung.

## Belege
Die Bildwiederholrate begrenzt direkt, wie viele Bilder du siehst. Der Gewinn ist groß, wann immer der Bildschirm unter seinem Maximum lief.

## Nachteile & Risiken
Bleibt der Bildschirm schwarz oder flackert er, warte 15 Sekunden. Häufigste Ursache ist ein Kabel, das die Rate nicht schafft. Windows behält dann den vorherigen Modus.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn der Bildschirm schon mit seinem Maximum läuft.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-changedisplaysettingsexw
