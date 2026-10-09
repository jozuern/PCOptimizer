# Multiplane Overlay (MPO) aus

## Zusammenfassung
Nur zur Fehlerbehebung: schaltet Multiplane Overlay ab, um Flackern, schwarze Bildschirme oder eingefrorene Fenster in manchen Apps zu beheben. Kein Leistungsgewinn.

## So funktioniert es
Mit Multiplane Overlay kann die Anzeigehardware mehrere Ebenen (etwa ein Video und den Desktop) selbst zusammensetzen, statt das dem Compositor zu überlassen. Bei manchen Kombinationen aus Treiber und Monitor führt das zu Flackern. Laut Nutzerberichten schaltet der Wert DisableOverlays = 1 unter GraphicsDrivers es ab 24H2 ab [1].

## Warum es helfen kann
Beseitigt die Ursache MPO-bedingter Flacker- und Einfrierprobleme, wenn sie auftreten.

## Belege
Für diesen Wert gibt es keine Microsoft-Dokumentation, er beruht auf Berichten aus der Community [1]. dxdiag zeigt nicht zuverlässig, ob MPO genutzt wird [2]. Ohne MPO-Probleme gibt es nichts zu gewinnen.

## Nachteile & Risiken
Kann den Verbrauch bei der Videowiedergabe erhöhen, weil der Compositor mehr arbeitet. Neustart nötig.

## Wann du es nicht nutzen solltest
Nur nutzen, wenn du Flackern oder schwarze Blitze siehst. Der alte Wert OverlayTestMode = 5 wirkt ab 24H2 nicht mehr und wird stattdessen von der Prüfung auf Altlasten gemeldet.

## Quellen
1. https://www.guru3d.com/publish/comments/geforce-56603-whql-driver-download/page-17
2. https://www.techpowerup.com/forums/goto/post?id=5552960
