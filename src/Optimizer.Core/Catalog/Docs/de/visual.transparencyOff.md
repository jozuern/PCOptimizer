# Transparenzeffekte aus

## Zusammenfassung
Schaltet die unscharfen, durchscheinenden Hintergründe (Acrylic und Mica) in Startmenü, Taskleiste und Apps ab.

## So funktioniert es
Acrylic zeichnet den Inhalt hinter Menüs, Flyouts und Bereichen unscharf, was laut Microsoft die GPU stark beansprucht [1]. Mica tönt Fensterhintergründe mit dem Hintergrundbild, das es nur einmal abtastet [2]. EnableTransparency = 0 ersetzt beides durch einfarbige Flächen, wie Einstellungen > Personalisierung > Farben > Transparenzeffekte [1][2].

## Warum es helfen kann
Etwas weniger GPU-Arbeit für Acrylic-Flächen auf dem Desktop. Am ehesten relevant bei integrierter Grafik und im Akkubetrieb.

## Belege
Mit eigener Grafikkarte kein messbarer Effekt auf Spiele.

## Nachteile & Risiken
Rein optisch: einfarbige statt durchscheinender Hintergründe.

## Wann du es nicht nutzen solltest
Lass sie an, wenn dir der Look gefällt und du eine eigene Grafikkarte hast.

## Quellen
1. https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic
2. https://learn.microsoft.com/en-us/windows/apps/design/style/mica
