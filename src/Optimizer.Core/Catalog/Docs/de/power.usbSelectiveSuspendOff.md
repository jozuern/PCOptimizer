# Selektives USB-Energiesparen aus

## Zusammenfassung
Verhindert im Netzbetrieb, dass Windows ungenutzte USB-Geräte schlafen legt. Kann helfen, wenn Maus, Headset oder Controller kurz aussetzen.

## So funktioniert es
Beim selektiven Energiesparen versetzt Windows USB-Geräte im Leerlauf in den Ruhezustand und weckt sie bei Aktivität [1]. Manche Geräte oder Hubs wachen spät oder gar nicht auf. Die App setzt den Planwert für den Netzbetrieb auf „Deaktiviert“.

## Warum es helfen kann
Eingabegeräte, die spät aufwachen, verpassen die erste Bewegung oder trennen kurz die Verbindung. Ohne Energiesparen bleiben sie aktiv.

## Belege
Auf die FPS hat das keinen Einfluss. Es behebt Stabilitätsprobleme mit bestimmten Geräten und bewirkt auf Systemen ohne solche Probleme nichts.

## Nachteile & Risiken
Höherer Leerlaufverbrauch: Ein USB-Gerät, das nie in den Ruhezustand geht, kann den USB-Controller beschäftigt halten und verhindern, dass der Prozessor tiefere Schlafzustände erreicht [1]. Microsoft empfiehlt, das selektive Energiesparen eingeschaltet zu lassen [1]; nutze das also nur gegen ein Gerät mit Aussetzern. Der Akkubetrieb von Laptops bleibt unverändert.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn deine USB-Geräte ohne Aussetzer funktionieren.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/usb-selective-suspend
