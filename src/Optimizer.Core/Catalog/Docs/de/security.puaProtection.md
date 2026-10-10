# Microsoft Defender: potenziell unerwünschte Apps blockieren

## Zusammenfassung
Defender blockiert potenziell unerwünschte Software, etwa mitgelieferte Adware und Symbolleisten, beim Download oder der Installation. Pro, Enterprise und Education.

## So funktioniert es
Die Defender-Richtlinie PUAProtection legt fest, ob die Erkennung potenziell unerwünschter Anwendungen sie blockiert, nur protokolliert oder zulässt; nicht konfiguriert entspricht deaktiviert [1]. Die App setzt Blockieren (1) [1]. Sie wirkt, wenn Microsoft Defender das aktive Virenschutzprogramm ist.

## Warum es helfen kann
Installer, die Adware oder Browseränderungen mitbringen, werden gestoppt, bevor sie installieren.

## Belege
Eine von Microsoft beschriebene Defender-Richtlinie [1]. Die Prüfung kostet nur beim Download oder der Installation etwas Zeit.

## Nachteile & Risiken
Defender kann einen gewollten Download blockieren, etwa manche kostenlosen Werkzeuge mit Zusatzangeboten; du lässt ihn dann unter Windows-Sicherheit > Schutzverlauf zu.

## Wann du es nicht nutzen solltest
Wenn du ein anderes Virenschutzprogramm nutzt oder oft Werkzeuge installierst, die Defender als potenziell unerwünscht einstuft.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-defender
