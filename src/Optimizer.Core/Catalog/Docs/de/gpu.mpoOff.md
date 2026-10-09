# Multiplane Overlay (MPO) aus

## Zusammenfassung
Nur zur Fehlerbehebung: schaltet Multiplane Overlay mit dem von NVIDIA dokumentierten Registrierungswert ab, um Flackern oder schwarze Bildschirme in manchen Apps zu beheben. Kein Leistungsgewinn.

## So funktioniert es
Mit Multiplane Overlay setzt die Anzeigehardware mehrere Ebenen, etwa ein Video und den Desktop, selbst zusammen, statt das dem Desktop-Compositor zu überlassen. NVIDIA beschreibt das als Weg zu mehr Leistung und weniger Stromverbrauch [1]. Bei manchen Kombinationen aus Treiber und Monitor führt es zu Flackern. Die App setzt OverlayTestMode = 5 unter HKLM\SOFTWARE\Microsoft\Windows\Dwm, denselben Wert wie NVIDIAs mpo_disable.reg; Rückgängig machen löscht ihn wieder, wie NVIDIAs mpo_restore.reg [1]. Wirkt nach einem Neustart.

## Warum es helfen kann
Beseitigt die Ursache MPO-bedingter Flacker- und Einfrierprobleme, wenn sie auftreten.

## Belege
NVIDIA dokumentiert diesen Wert für Windows 11 [1]. Microsoft dokumentiert ihn nicht. Ohne MPO-Probleme gibt es nichts zu gewinnen.

## Nachteile & Risiken
Der Compositor arbeitet mehr, was den Stromverbrauch erhöhen kann, etwa bei der Videowiedergabe [1]. Neustart nötig.

## Wann du es nicht nutzen solltest
Nur bei Flackern oder schwarzen Blitzen. Aktualisiere vorher Grafiktreiber und Windows: NVIDIA nennt verbesserte MPO-Unterstützung ab Treibern der Release 610 mit Windows-Build 26100.7705 oder 26200.7705 und neuer [1].

## Quellen
1. https://nvidia.custhelp.com/app/answers/detail/a_id/5157
