# Spiele auf der Grafikkarte ausführen

## Zusammenfassung
Setzt für jedes erkannte Spiel die Grafikeinstellung „Hohe Leistung“, damit Windows es auf der eigenen GPU statt der integrierten ausführt.

## So funktioniert es
Windows speichert in deinem Benutzerprofil eine GPU-Vorgabe pro App (UserGpuPreferences, dieselbe Liste wie Einstellungen > Bildschirm > Grafik). GpuPreference=2 bedeutet hohe Leistung [1]. Die App legt für jede erkannte Spiel-exe einen Eintrag an und behält andere Werte.

## Warum es helfen kann
Auf PCs mit zwei GPUs läuft ein Spiel auf der integrierten GPU nur mit einem Bruchteil der FPS.

## Belege
Der Unterschied zwischen integrierter und eigener Grafik ist in jedem 3D-Spiel groß.

## Nachteile & Risiken
Auf Laptops etwas höherer Verbrauch, solange diese Spiele laufen. Ist die erkannte exe falsch (etwa ein Launcher), füge die richtige unter Einstellungen > Bildschirm > Grafik hinzu.

## Wann du es nicht nutzen solltest
Auf PCs mit nur einer GPU nicht nötig.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_6/nf-dxgi1_6-idxgifactory6-enumadapterbygpupreference
