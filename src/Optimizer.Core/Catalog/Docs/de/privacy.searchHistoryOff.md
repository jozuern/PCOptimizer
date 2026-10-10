# Suche: kein Verlauf auf diesem Gerät

## Zusammenfassung
Die Windows-Suche speichert deine Suchen nicht mehr auf diesem PC, wie der Schalter "Suchverlauf auf diesem Gerät". Vorhandener Verlauf bleibt.

## So funktioniert es
Die Windows-Suche speichert deinen Suchverlauf auf dem Gerät, um Dinge schneller zu finden, etwa indem eine App weiter oben steht, nach der du schon gesucht hast [1]. Der Schalter liegt unter Einstellungen > Datenschutz und Sicherheit > Suchberechtigungen [1]. Die App setzt IsDeviceSearchHistoryEnabled auf 0. Microsoft dokumentiert den Registry-Wert hinter dem Schalter nicht; die Anleitung [2] zeigt den Wert, den der Schalter schreibt. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Andere, die dieses Konto nutzen, sehen nicht, wonach du gesucht hast.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Die Suche sortiert Ergebnisse nicht mehr nach früheren Suchen. Gespeicherter Verlauf bleibt, bis du "Geräte-Suchverlauf löschen" wählst [1].

## Wann du es nicht nutzen solltest
Wenn die Suche sich merken soll, was du oft öffnest.

## Quellen
1. https://support.microsoft.com/en-us/windows/privacy/windows-search-and-privacy
2. https://www.elevenforum.com/t/enable-or-disable-recent-search-history-in-windows-11.5395/
