# Zurückhaltende Dienst-Voreinstellung

## Zusammenfassung
Stellt drei Dienste, die die meisten PCs nicht brauchen, auf Manuell: Verwaltung heruntergeladener Karten, Programmkompatibilitäts-Assistent und Überwachung verteilter Verknüpfungen.

## So funktioniert es
Die drei Dienste starten bei Bedarf (Manuell) statt mit Windows [2]. Sie werden nicht deaktiviert: Eine App oder ein Windows-Auslöser kann sie weiterhin starten. Der Programmkompatibilitäts-Assistent hat solche Auslöser und läuft deshalb weiter, wenn er gebraucht wird.

## Warum es helfen kann
Weniger Dienste starten mit Windows, es gibt also etwas weniger Hintergrundaktivität.

## Belege
Untätige Dienste nutzen fast keine Prozessorzeit; einen messbaren Bildratengewinn gibt es nicht, deshalb ist die Wirkung mit 0 bewertet. Microsofts Dienst-Empfehlungen für Windows Server 2016 stufen den Karten- und den Kompatibilitätsdienst als deaktivierbar ein [1]; für Windows 11 gibt es keine solche Liste.

## Nachteile & Risiken
Die Karten-App ist abgekündigt, der Kartendienst hat daher wenig zu tun [3]. Die Überwachung verteilter Verknüpfungen läuft nicht mehr, Verknüpfungen zu Dateien, die du auf ein anderes Laufwerk oder einen anderen PC verschoben hast, werden nicht mehr automatisch repariert. Der Kompatibilitäts-Assistent arbeitet über seine Auslöser weiter.

## Wann du es nicht nutzen solltest
In Netzwerken, in denen Verknüpfungen zu Dateien auf anderen PCs auch nach dem Verschieben funktionieren müssen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/security/windows-services/security-guidelines-for-disabling-system-services-in-windows-server
2. https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfigw
3. https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features
