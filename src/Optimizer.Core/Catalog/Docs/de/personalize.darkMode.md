# Dunkler Modus für Windows und Apps

## Zusammenfassung
Stellt Windows und Apps, die sich danach richten, auf den dunklen Modus, wie Einstellungen > Personalisierung > Farben > Modus auswählen > Dunkel.

## So funktioniert es
Windows hat einen hellen und einen dunklen Farbmodus für Windows und für Apps [1]. Die App setzt AppsUseLightTheme und SystemUsesLightTheme auf 0 und teilt laufenden Programmen die Änderung mit. Microsoft beschreibt den Schalter [1], der Registry-Wert dahinter ist aber nicht dokumentiert; die App schreibt den Wert, den Windows selbst für den Schalter speichert. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Angenehmer für die Augen in dunklen Räumen und passt zu dunklen Spiele-Launchern.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Manche älteren Programme bleiben hell.

## Wann du es nicht nutzen solltest
Wenn du den hellen Modus magst.

## Quellen
1. https://support.microsoft.com/en-us/windows/experience/personalization/personalize-your-colors-in-windows
