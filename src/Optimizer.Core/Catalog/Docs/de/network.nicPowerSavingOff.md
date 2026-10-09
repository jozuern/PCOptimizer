# Energiesparfunktionen des Netzwerkadapters aus

## Zusammenfassung
Schaltet die Energiesparfunktionen kabelgebundener Netzwerkadapter ab (Energy Efficient Ethernet, Green Ethernet und ähnliche). Startet den Adapter neu.

## So funktioniert es
Viele Ethernet-Treiber versetzen die Verbindung zwischen Paketen in einen Energiesparzustand (Energy Efficient Ethernet, IEEE 802.3az) oder senken bei kurzen Kabeln die Leistung [1]. Die App schaltet jede dieser Einstellungen ab, aber nur die, die der Treiber deines Adapters tatsächlich anbietet, und nur mit Werten, die der Treiber auflistet. Der Adapter startet neu, damit der Treiber die neuen Werte liest; die Verbindung bricht für einige Sekunden ab. Rückgängig machen stellt jeden Wert wieder her.

## Warum es helfen kann
Das Aufwecken der Verbindung aus dem Energiesparzustand dauert pro Aufwachen einige Mikrosekunden. Wichtiger in der Praxis: Manche Kombinationen aus Adapter und Switch verlieren mit Energy Efficient Ethernet die Verbindung oder Pakete.

## Belege
Die Verzögerung selbst ist in Spielen zu klein, um sie zu bemerken. Der Nutzen sind weniger Verbindungsabbrüche bei Hardware mit solchen Problemen.

## Nachteile & Risiken
Etwas höherer Stromverbrauch (deutlich unter einem Watt pro Adapter). Der Adapter startet beim Anwenden und beim Rückgängigmachen je einmal neu.

## Wann du es nicht nutzen solltest
Wenn deine Verbindung stabil ist und der Stromverbrauch zählt, etwa auf einem Laptop.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
2. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-power-management
