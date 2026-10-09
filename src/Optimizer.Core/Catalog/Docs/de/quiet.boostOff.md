# Prozessor-Boost aus (leiser und kühler)

## Zusammenfassung
Der Prozessor bleibt bei seinem Basistakt, am Netzteil und im Akkubetrieb. Er bleibt kühler und die Lüfter leiser, auf Kosten der Spitzenleistung.

## So funktioniert es
Boost lässt den Prozessor seinen Nenntakt überschreiten, wenn Temperatur und Energie es erlauben [1]. Die App setzt den „Leistungssteigerungsmodus für Prozessoren“ für Netz- und Akkubetrieb auf „Deaktiviert“ [1][2]. Die Einstellung ist in den Energieoptionen normalerweise ausgeblendet [1]. Rückgängig schreibt beide bisherigen Werte zurück.

## Warum es helfen kann
Boost-Takte brauchen eine höhere Spannung, daher steigt die Wärme stärker als die Geschwindigkeit. Ohne Boost erzeugt der Prozessor unter Last weniger Wärme, und die Lüfter haben weniger zu tun. In kleinen Gehäusen und dünnen Laptops ist das der direkteste Weg zu weniger Lärm.

## Belege
Die Einstellung ist von Microsoft dokumentiert [1]. Wie viel langsamer der PC wird, hängt vom Abstand zwischen Basis- und Boost-Takt deines Prozessors ab; bei vielen Prozessoren liegt der Boost-Takt weit über dem Basistakt, daher verlieren Aufgaben mit einem Thread und Spiele spürbar.

## Nachteile & Risiken
Weniger Leistung in Spielen und bei aufwendigen Aufgaben. In den Gaming-Profilen meldet der Scan den Boost als abgeschaltet. Nicht kombinierbar mit „Prozessor-Boost im Akkubetrieb aus“ oder „Prozessor-Turbo wiederherstellen“ (gleiche Einstellung).

## Wann du es nicht nutzen solltest
Zum Spielen, für Videoschnitt oder andere aufwendige Arbeit. Stört dich der Lärm nur in Spielen, ist eine Bildratenbegrenzung im Spiel das bessere Werkzeug.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
