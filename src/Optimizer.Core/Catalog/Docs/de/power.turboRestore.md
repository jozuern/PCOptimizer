# Prozessor-Turbo wiederherstellen

## Zusammenfassung
Setzt im aktiven Plan den maximalen Leistungszustand auf 100 % und den Boost-Modus auf „Aggressiv“. So erreicht die CPU wieder ihren Turbotakt.

## So funktioniert es
Zwei Werte des aktiven Energiesparplans steuern den Turbo: der maximale Leistungszustand (PROCTHROTTLEMAX) und der Boost-Modus (PERFBOOSTMODE). Unter 100 % oder mit abgeschaltetem Boost fordert Windows keine Turbofrequenzen an [1]. Die App setzt beide Werte für den Netzbetrieb.

## Warum es helfen kann
Aktuelle CPUs laufen in Spielen weit über ihrem Basistakt. Ist der Turbo gesperrt, verlieren CPU-lastige Spiele einen großen Teil ihrer FPS.

## Belege
Der Verlust ohne Turbo lässt sich leicht messen: Der CPU-Takt im Task-Manager bleibt unter Last beim Basistakt oder darunter.

## Nachteile & Risiken
Unter Last höherer Verbrauch und höhere Temperatur. Das ist das normale Verhalten der CPU. Die Akku-Einstellung eines Laptops bleibt unverändert.

## Wann du es nicht nutzen solltest
Nur nötig, wenn der Scan den Turbo als abgeschaltet meldet. Hast du den Turbo absichtlich begrenzt (etwa wegen der Lautstärke), behalte deine Einstellung.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
