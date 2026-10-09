# Windows Platform Binary Table aus

## Zusammenfassung
Verhindert, dass Windows beim Start Programme ausführt, die die Firmware bereitstellt (WPBT). Manche Mainboard-Hersteller installieren so automatisch ihre Apps.

## So funktioniert es
WPBT ist eine ACPI-Tabelle, in der die Firmware ein Programm hinterlegen kann, das Windows bei jedem Start ausführt. DisableWpbtExecution = 1 weist Windows an, sie zu ignorieren [1]. Wirkt nach einem Neustart.

## Warum es helfen kann
Verhindert, dass sich Herstellertools nach dem Entfernen selbst neu installieren.

## Belege
Kein Effekt auf die FPS.

## Nachteile & Risiken
OEM-Wiederherstellungs- oder Diebstahlschutz-Agenten, die WPBT nutzen, funktionieren nicht mehr.

## Wann du es nicht nutzen solltest
Behalte es, wenn du einen Diebstahlschutz- oder Wiederherstellungsdienst des Herstellers nutzt.

## Quellen
1. https://github.com/ChrisTitusTech/winutil
