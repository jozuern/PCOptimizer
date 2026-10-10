# Spielsound bleibt bei Sprachanrufen laut

## Zusammenfassung
Windows senkt andere Töne, etwa dein Spiel, nicht mehr um 80 Prozent, wenn es einen Anruf oder Sprachchat erkennt.

## So funktioniert es
Auf der Registerkarte Kommunikation der Sound-Systemsteuerung wählst du, was bei Kommunikation passiert: andere Töne senken (standardmäßig um 80 Prozent), stummschalten oder nichts tun [1]. Die App wählt "Nichts unternehmen", indem sie UserDuckingPreference auf 3 setzt. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Bis ein Test unter echtem Windows die Wirkung bestätigt, ist die Option eine Vorschau.

## Warum es helfen kann
Der Spielsound wird nicht leiser, wenn Discord oder eine andere Sprach-App das Mikrofon öffnet.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Gespräche sind über lautem Sound schwerer zu hören; die Lautstärke regelst du selbst.

## Wann du es nicht nutzen solltest
Wenn Windows andere Töne bei Anrufen leiser machen soll.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/coreaudio/stream-attenuation
