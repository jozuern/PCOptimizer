# Auto HDR an

## Zusammenfassung
Ältere DirectX-11- und -12-Spiele, die nur SDR ausgeben, werden auf einem HDR-Bildschirm mit HDR dargestellt. HDR selbst muss an sein.

## So funktioniert es
Auto HDR erweitert Farbraum und Helligkeit von SDR-Spielen mit DirectX 11 oder 12 auf einem HDR-fähigen Bildschirm; es liegt unter Einstellungen > System > Anzeige > HDR [1]. Die App setzt das Token AutoHDREnable in DirectXUserGlobalSettings, neben den Optimierungen für Spiele im Fenstermodus. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Bis ein Test unter echtem Windows die Wirkung bestätigt, ist die Option eine Vorschau.

## Warum es helfen kann
Ältere Spiele sehen auf einem HDR-Monitor eher wie HDR-Spiele aus.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Das Ergebnis hängt vom Spiel ab; manche wirken blass oder zu hell. Ohne HDR-Bildschirm oder mit HDR aus ändert sich nichts.

## Wann du es nicht nutzen solltest
Wenn du keinen HDR-Bildschirm hast oder Spiele im Originallook magst.

## Quellen
1. https://support.microsoft.com/en-us/windows/hardware/display-graphics/use-auto-hdr-for-better-gaming-in-windows
