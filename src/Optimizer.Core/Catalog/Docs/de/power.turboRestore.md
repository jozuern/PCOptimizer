# Prozessor-Turbo wiederherstellen

## Zusammenfassung
Setzt im aktiven Plan den maximalen Leistungszustand auf 100 % und den Leistungssteigerungsmodus auf den Windows-Standard. So erreicht die CPU wieder ihren Boost-Takt.

## So funktioniert es
Zwei Werte des aktiven Energiesparplans steuern den Boost: der maximale Leistungszustand des Prozessors (PROCTHROTTLEMAX), ein Prozentsatz der maximalen Prozessorleistung [2], und der Leistungssteigerungsmodus für Prozessoren (PERFBOOSTMODE). Steht der Modus auf „Deaktiviert“, geht der Prozessor nicht über seine Nennleistung hinaus [1]; ein maximaler Leistungszustand unter 100 % begrenzt die Leistung, die Windows anfordert. Die App setzt für den Netzbetrieb 100 % und den Modus 2, den auch die eingebauten Windows-Pläne nutzen. Je nach Prozessor heißt dieser Wert in Windows „Hoch“ oder „Aktiviert“ [1].

## Warum es helfen kann
Aktuelle CPUs laufen in Spielen über ihrem Basistakt. Ist der Boost gesperrt, verlieren Spiele, die von der CPU begrenzt werden, FPS; Spiele, die von der Grafikkarte begrenzt werden, verlieren weniger.

## Belege
Wie viel verloren geht, hängt vom Abstand zwischen Basis- und Boost-Takt ab und davon, ob das Spiel von der CPU begrenzt wird. Prüfen kannst du es selbst: Ist der Boost gesperrt, steigt der Takt im Task-Manager unter Last nicht über den Basistakt.

## Nachteile & Risiken
Unter Last höherer Verbrauch und höhere Temperatur. Das ist das normale Verhalten der CPU. Die Akku-Einstellung eines Laptops bleibt unverändert.

## Wann du es nicht nutzen solltest
Nur nötig, wenn der Scan den Turbo als abgeschaltet meldet. Hast du den Turbo absichtlich begrenzt (etwa wegen der Lautstärke), behalte deine Einstellung.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-maxperformance
