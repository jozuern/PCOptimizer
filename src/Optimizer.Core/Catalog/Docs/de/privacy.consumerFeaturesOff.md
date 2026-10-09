# Verbraucherfunktionen aus

## Zusammenfassung
Schaltet Microsoft-Verbraucherfunktionen wie vorgeschlagene Apps im Startmenü ab. Microsoft unterstützt die Richtlinie nur auf Enterprise und Education, nicht auf Home oder Pro.

## So funktioniert es
Die Richtlinie DisableWindowsConsumerFeatures = 1 schaltet Funktionen ab, die typisch für Privatkunden sind, etwa Vorschläge im Startmenü, Mitgliedschaftshinweise, App-Installationen nach der Einrichtung und Weiterleitungskacheln [1]. Microsoft führt den Wert auch in seiner Anleitung, mit der Enterprise-Kunden die Verbindungen von Windows einschränken [2].

## Warum es helfen kann
Auf Enterprise und Education verhindert sie, dass beworbene Apps im Hintergrund installiert und aktualisiert werden.

## Belege
Kein direkter Effekt auf die FPS. Microsoft führt die Richtlinie nur für Enterprise, Education und IoT Enterprise [1].

## Nachteile & Risiken
Auf Home und Pro unterstützt Microsoft die Einstellung nicht, sie wirkt dort womöglich gar nicht. Die App zeigt den Tweak dort deshalb als nicht zutreffend an. Nutze dort die Schalter für Vorschläge in den Einstellungen.

## Wann du es nicht nutzen solltest
Auf Home oder Pro nicht sinnvoll.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-experience
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
