# Spiele auf der Grafikkarte ausführen

## Zusammenfassung
Setzt für jedes erkannte Spiel die Windows-Grafikeinstellung „Hohe Leistung“, damit Windows die eigene GPU statt der integrierten anfordert.

## So funktioniert es
Windows speichert in deinem Benutzerprofil eine GPU-Vorgabe pro App (UserGpuPreferences, dieselbe Liste wie Einstellungen > System > Bildschirm > Grafik). Die App legt für jede erkannte Spiel-exe den Eintrag GpuPreference=2 an und behält andere Werte. Der Wert 2 entspricht der DirectX-Vorgabe „hohe Leistung“, die die leistungsstärkste GPU anfordert, etwa eine eigene Grafikkarte [1]. Microsoft dokumentiert die DirectX-Vorgabe, aber nicht diese Liste in der Registrierung; die Einträge entsprechen denen, die die Einstellungsseite schreibt.

## Warum es helfen kann
Auf PCs mit zwei GPUs läuft ein Spiel, das auf der integrierten GPU landet, mit deutlich weniger FPS. Ohne Vorgabe nennt DirectX zuerst den Adapter, der die Hauptanzeige ansteuert [2], und das ist bei Laptops mit Hybridgrafik meist die integrierte GPU.

## Belege
Der Grafiktreiber kann Spiele schon selbst auf die Grafikkarte legen: NVIDIAs Treiberprofile teilen dem System mit, welche Spiele die GeForce-GPU brauchen [3]. Die Windows-Vorgabe hilft, wenn ein Spiel nicht von einem solchen Profil erfasst ist. Ob ein Spiel vorher betroffen war, siehst du nur, wenn du prüfst, welche GPU es genutzt hat (Task-Manager > Leistung > GPU).

## Nachteile & Risiken
Auf Laptops etwas höherer Verbrauch, solange diese Spiele laufen. Ist die erkannte exe falsch (etwa ein Launcher), füge die richtige unter Einstellungen > System > Bildschirm > Grafik hinzu.

## Wann du es nicht nutzen solltest
Auf PCs mit nur einer GPU nicht nötig, ebenso wenn der Task-Manager das Spiel schon auf der Grafikkarte zeigt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_6/ne-dxgi1_6-dxgi_gpu_preference
2. https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgifactory-enumadapters
3. https://www.nvidia.com/en-us/geforce/news/rtx-laptops-advanced-optimus/
