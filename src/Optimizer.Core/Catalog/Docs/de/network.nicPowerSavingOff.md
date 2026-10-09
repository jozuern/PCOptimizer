# Energiesparfunktionen des Netzwerkadapters aus

## Zusammenfassung
Schaltet Energy Efficient Ethernet und Selective Suspend bei kabelgebundenen Netzwerkadaptern ab. Kann Verbindungsabbrüche bei manchen Adaptern beheben. Startet den Adapter neu.

## So funktioniert es
Mit Energy Efficient Ethernet (IEEE 802.3az) versetzt der Adapter die Verbindung zwischen Datenpaketen in einen Energiesparzustand, und beide Seiten wachen auf, sobald Daten gesendet werden müssen [1][2]. Selective Suspend lässt einen untätigen Adapter in einen Stromsparzustand wechseln [3]. Für beide definiert Windows eine Standard-Treibereinstellung [1][3]. Die App schaltet beide ab, aber nur, wenn der Treiber deines Adapters sie mit einem Wert für „aus“ anbietet. Energiesparoptionen, die nur der Treiber eines bestimmten Herstellers hat, ändert sie nicht. Der Adapter startet neu, damit der Treiber die neuen Werte liest; die Verbindung bricht für einige Sekunden ab. Rückgängig machen stellt jeden Wert wieder her.

## Warum es helfen kann
Das Aufwachen der Verbindung kostet etwas Latenz [2]. Wichtiger in der Praxis: Manche Adapter verlieren mit Energy Efficient Ethernet die Verbindung, und das Abschalten ist die Umgehung des Herstellers [4].

## Belege
Die Aufwachverzögerung ist in Spielen zu klein, um sie zu bemerken [2]. Der Nutzen sind weniger Verbindungsabbrüche bei Hardware mit solchen Problemen [4].

## Nachteile & Risiken
Etwas höherer Stromverbrauch. Der Adapter startet beim Anwenden und beim Rückgängigmachen je einmal neu.

## Wann du es nicht nutzen solltest
Wenn deine Verbindung stabil ist und der Stromverbrauch zählt, etwa auf einem Laptop.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-power-management
2. https://edc.intel.com/content/www/us/en/design/products/ethernet/adapters-and-devices-user-guide/other-power-options
3. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/standardized-inf-keywords-for-ndis-selective-suspend
4. https://www.asus.com/support/faq/1052466
