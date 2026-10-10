# Keine Konfigurationsdownloads von OneSettings

## Zusammenfassung
Windows lädt keine Konfigurationseinstellungen mehr vom Dienst OneSettings. Microsoft warnt, dass Apps, die diesen Dienst nutzen, nicht mehr funktionieren könnten. Pro, Enterprise und Education.

## So funktioniert es
Ohne die Richtlinie verbindet sich Windows regelmäßig mit dem Dienst OneSettings, um Konfigurationseinstellungen zu laden [1]. Mit DisableOneSettingsDownloads = 1 nicht mehr [1]. Windows-Komponenten und Apps wie der Telemetriedienst nutzen diesen Dienst, um ihre Konfiguration anzupassen [2].

## Warum es helfen kann
Eine regelmäßige Verbindung von Windows zu Microsoft weniger.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1][2]. Keine gemessene Wirkung auf die Leistung.

## Nachteile & Risiken
Microsoft warnt, dass Apps, die diesen Dienst nutzen, nicht mehr funktionieren könnten [2], und Korrekturen, die Microsoft als Konfigurationsänderung verteilt, erreichen diesen PC eventuell nicht.

## Wann du es nicht nutzen solltest
Auf einem PC, der mit möglichst wenig Überraschungen laufen soll; die Option ist für alle, die Verbindungen so weit begrenzen wollen, wie Microsoft es erlaubt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
2. https://learn.microsoft.com/en-us/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services
