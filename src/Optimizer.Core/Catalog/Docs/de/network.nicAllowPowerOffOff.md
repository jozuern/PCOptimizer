# Windows darf den Netzwerkadapter nicht abschalten

## Zusammenfassung
Entfernt den Haken bei „Computer kann das Gerät ausschalten, um Energie zu sparen“ für kabelgebundene Netzwerkadapter. Verhindert Verbindungsverluste nach Leerlauf.

## So funktioniert es
Die Registerkarte Energieverwaltung des Adapters im Geräte-Manager wird als Wert PnPCapabilities gespeichert. Der Wert 24 entfernt die Option, sodass Windows den Adapter nicht abschaltet [1]. Die App schreibt ihn für physische Ethernet-Adapter und startet sie neu. Rückgängig machen stellt den vorherigen Zustand wieder her.

## Warum es helfen kann
Manche Adapter kommen nicht zuverlässig zurück, nachdem Windows sie abgeschaltet hat. Das zeigt sich als verlorene Verbindung, nachdem der PC im Leerlauf war.

## Belege
Microsoft dokumentiert diesen Wert für Adapter, die durch die Energieverwaltung die Verbindung verlieren [1]. Ohne ein solches Problem ändert sich die Leistung nicht.

## Nachteile & Risiken
Etwas höherer Verbrauch im Leerlauf. Wake-on-LAN kann bei manchen Adaptern nicht mehr funktionieren.

## Wann du es nicht nutzen solltest
Auf Laptops oder wenn du Wake-on-LAN nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/networking/power-management-on-network-adapter
