# Dateiübertragungen im Netzwerk: keine SMB-Drosselung

## Zusammenfassung
Lässt Windows Dateien von Netzwerkfreigaben auch über Verbindungen mit hoher Latenz mit voller Geschwindigkeit kopieren. Kein Einfluss auf Spiele oder Downloads aus dem Internet.

## So funktioniert es
Der SMB-Client drosselt den Durchsatz auf Verbindungen mit hoher Latenz, um Zeitüberschreitungen zu vermeiden; DisableBandwidthThrottling = 1 schaltet das ab und erlaubt mehr Durchsatz [1]. Microsoft nennt den Wert in seiner Anleitung zum Tuning des SMB-Clients und weist darauf hin, dass solche Einstellungen nicht für jeden Computer passen [1].

## Warum es helfen kann
Kopieren zu oder von einem NAS oder einem anderen PC über VPN oder eine langsame Verbindung kann schneller werden.

## Belege
Von Microsoft beschrieben [1]. Es betrifft nur SMB-Dateiübertragungen; lokale Netze mit niedriger Latenz erreichen die Drosselung selten.

## Nachteile & Risiken
Auf unzuverlässigen Verbindungen können Übertragungen in Zeitüberschreitungen laufen, die die Drosselung verhindern sollte.

## Wann du es nicht nutzen solltest
Wenn du keine Dateien übers Netzwerk kopierst oder Übertragungen danach abbrechen.

## Quellen
1. https://learn.microsoft.com/en-US/windows-server/administration/performance-tuning/role/file-server
