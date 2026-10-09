# Prozessor-Boost im Akkubetrieb aus

## Zusammenfassung
Im Akkubetrieb bleibt der Prozessor bei seinem Basistakt, statt darüber hinaus zu boosten. Am Netzteil ändert sich nichts.

## So funktioniert es
Boost lässt den Prozessor über seinem Nenntakt laufen, wenn Temperatur und Energie es erlauben [1]. Die App setzt den Energieplanwert „Leistungssteigerungsmodus für Prozessoren“ für den Akkubetrieb auf „Deaktiviert“ [1][2]. Die Einstellung ist in den Energieoptionen normalerweise ausgeblendet [1]. Der Wert für den Netzbetrieb bleibt, wie er ist.

## Warum es helfen kann
Boost-Takte brauchen eine höhere Spannung, daher steigt der Verbrauch stärker als die Geschwindigkeit. Ohne Boost verbrauchen kurze Lastspitzen (Apps öffnen, Webseiten laden) weniger Energie, der Laptop bleibt kühler und der Lüfter läuft seltener. Beim Schreiben, Surfen und Videoschauen bedeutet das meist mehr Laufzeit.

## Belege
Die Einstellung ist von Microsoft dokumentiert [1]. Wie viel Laufzeit sie bringt, hängt vom Prozessor und von deiner Nutzung ab: Leichte Arbeit mit vielen kurzen Lastspitzen profitiert mehr als Videowiedergabe, bei der kaum geboostet wird.

## Nachteile & Risiken
Im Akkubetrieb dauern anspruchsvolle Aufgaben (Exporte, Kompilieren, Spiele) spürbar länger, weil der Prozessor seinen Basistakt nicht überschreiten kann. Rückgängig stellt den bisherigen Akkuwert wieder her.

## Wann du es nicht nutzen solltest
Wenn du regelmäßig im Akkubetrieb aufwendig arbeitest und dort volle Leistung brauchst. Spiele laufen im Akkubetrieb ohnehin langsam; zum Spielen das Netzteil anschließen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-perfboostmode
2. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-processor-power-management-options
