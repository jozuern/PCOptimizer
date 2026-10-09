# PCIe-Energiesparen im Akkubetrieb: maximal

## Zusammenfassung
Setzt die PCIe-Energieverwaltung im Akkubetrieb auf „Maximale Energieeinsparungen“, den Standard im Plan „Ausbalanciert“. Ändert nur etwas, wenn ein Tool oder der Hersteller ihn gesenkt hat.

## So funktioniert es
Active State Power Management (ASPM) lässt PCIe-Verbindungen (SSD, WLAN-Karte, Grafik) im Leerlauf in Stromsparzustände wechseln [1]. Der Energieplan hat getrennte Werte für Netz- und Akkubetrieb; die App setzt den Akkuwert auf 2 (maximale Energieeinsparungen).

## Warum es helfen kann
Verbindungen, die im Leerlauf nicht schlafen dürfen, verbrauchen weiter Strom. Laptops verbringen im Akkubetrieb die meiste Zeit mit ungenutzten Verbindungen, daher senkt maximales Sparen den Leerlaufverbrauch.

## Belege
Windows selbst nutzt im Plan „Ausbalanciert“ im Akkubetrieb maximale Energieeinsparungen. Die App empfiehlt das nur, wenn dein aktueller Wert davon abweicht.

## Nachteile & Risiken
Das Aufwecken einer Verbindung kostet eine kurze Verzögerung. Manche ältere Geräte hatten Stabilitätsprobleme mit ASPM; verhält sich ein Gerät im Akkubetrieb danach seltsam, nutze Rückgängig.

## Wann du es nicht nutzen solltest
Wenn ein PCIe-Gerät (zum Beispiel ein externes Dock oder eine Capture-Karte) mit dieser Einstellung im Akkubetrieb ausfällt.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/pci-express-settings-link-state-power-management
