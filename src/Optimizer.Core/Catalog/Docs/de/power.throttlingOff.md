# Power Throttling aus

## Zusammenfassung
Verhindert, dass Windows Hintergrundprozesse im Stromsparmodus ausführt. Vor allem auf Laptops und CPUs mit Effizienzkernen relevant.

## So funktioniert es
Mit Power Throttling stuft Windows Hintergrundarbeit als EcoQoS ein und versucht, sie sparsamer auszuführen, etwa mit niedrigerem Takt oder auf Effizienzkernen [1]. Der Wert PowerThrottlingOff = 1 ist der Registrierungswert der Windows-Richtlinie „Power Throttling deaktivieren“ und schaltet das systemweit ab [2]. Steht der Windows-Energiemodus auf „Beste Leistung“, nimmt Windows ohnehin alle Apps vom Power Throttling aus [3].

## Warum es helfen kann
Programme, die neben einem Spiel laufen, etwa Voice-Chat, Overlays oder Streaming-Software, werden sonst womöglich gebremst, wenn sie den Fokus verlieren.

## Belege
Eine Messung der Wirkung auf Spiele haben wir nicht gefunden. Auf Desktops ohne Effizienzkerne hat Windows wenig zu drosseln; auf hybriden CPUs und Laptops zeigt sich die Änderung daran, dass Hintergrund-Apps volle Geschwindigkeit behalten, nicht an mehr FPS im laufenden Spiel.

## Nachteile & Risiken
Höherer Stromverbrauch, besonders im Akkubetrieb. Nach dem Anwenden bittet die App um einen Neustart.

## Wann du es nicht nutzen solltest
Auf Desktops ohne Effizienzkerne nicht sinnvoll. Auf Laptops die Akkulaufzeit bedenken.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setprocessinformation
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-power
3. https://learn.microsoft.com/en-us/windows-hardware/customize/desktop/customize-power-slider
