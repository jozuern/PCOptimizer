# Microsoft Defender: Netzwerkschutz

## Zusammenfassung
Blockiert Verbindungen jeder App, nicht nur des Browsers, zu Domains, die Microsoft als gefährlich einstuft, etwa Phishing- und Exploit-Seiten. Pro, Enterprise und Education.

## So funktioniert es
Der Netzwerkschutz verhindert, dass eine Anwendung auf gefährliche Domains zugreift, die Phishing, Exploits oder andere schädliche Inhalte anbieten könnten [1]. Die Richtlinie EnableNetworkProtection hat drei Werte: 0 aus (Standard), 1 blockieren, 2 nur protokollieren [1]. Die App setzt 1. Sie wirkt, wenn Microsoft Defender das aktive Virenschutzprogramm ist.

## Warum es helfen kann
Schutz vor schädlichen Seiten auch in Apps ohne SmartScreen, etwa Chatprogrammen und Spiele-Launchern.

## Belege
Eine von Microsoft beschriebene Defender-Richtlinie [1]. Microsoft veröffentlicht keine Messung der Wirkung auf Spiele.

## Nachteile & Risiken
Eine fälschlich als gefährlich eingestufte Seite ist in jeder App gesperrt, bis Microsoft die Einstufung korrigiert.

## Wann du es nicht nutzen solltest
Wenn du ein anderes Virenschutzprogramm nutzt oder eine gesperrte Seite brauchst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-defender
