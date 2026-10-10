# Bildschirm auf höchste Bildwiederholrate stellen

## Zusammenfassung
Stellt den Monitor auf die höchste Bildwiederholrate, die Windows bei der aktuellen Auflösung anbietet, wie die Auswahl unter Einstellungen > Bildschirm.

## So funktioniert es
Die App ändert den Anzeigemodus über die Windows-Anzeige-API: gleiche Auflösung, höchste angebotene Bildwiederholrate [1]. Der neue Modus gilt sofort und wird in deinem Benutzerprofil gespeichert [1]. Rückgängig machen stellt die vorherige Rate wieder ein.

## Warum es helfen kann
Mit 144 Hz statt 60 Hz zeigt der Bildschirm alle 6,9 ms statt alle 16,7 ms ein neues Bild: flüssigere Bewegung und weniger Verzögerung.

## Belege
Die Bildwiederholrate begrenzt direkt, wie viele Bilder du siehst. Der Gewinn ist groß, wann immer der Bildschirm unter seinem Maximum lief.

## Nachteile & Risiken
Bleibt der Bildschirm schwarz oder flackert er, schaffen das Kabel oder ein Anschluss diese Rate nicht. Windows fragt nicht nach einer Bestätigung und stellt nach einer Änderung durch eine App nicht von selbst zurück. Nutze Rückgängig auf der Seite **Änderungen** von einem anderen Bildschirm aus, oder stelle unter Einstellungen > System > Bildschirm > Erweiterte Anzeige eine niedrigere Rate ein.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn der Bildschirm schon mit seinem Maximum läuft.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-changedisplaysettingsexw
