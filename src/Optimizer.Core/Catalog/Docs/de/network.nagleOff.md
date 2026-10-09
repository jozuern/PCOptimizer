# Verzögerte TCP-Bestätigungen aus (TcpAckFrequency)

## Zusammenfassung
Lässt Windows jedes empfangene TCP-Segment sofort bestätigen, statt zu warten. Betrifft nur TCP-Verbindungen. Wirkung in Spielen umstritten.

## So funktioniert es
Standardmäßig bestätigt Windows jedes zweite TCP-Segment oder ein einzelnes Segment, sobald ein 200-ms-Timer abläuft, um weniger Pakete zu senden [1]. TcpAckFrequency = 1 unter jeder aktiven Netzwerkschnittstelle lässt Windows jedes Segment sofort bestätigen [1]. Der Nagle-Algorithmus ist ein anderer Mechanismus auf der Senderseite: Er hält kleine Pakete zurück, solange frühere Daten unbestätigt sind [2]. Programme schalten ihn für ihre eigenen Verbindungen mit der Option TCP_NODELAY ab [3]. Einen Registrierungswert, der Nagle für das ganze System abschaltet, dokumentiert Microsoft nicht. Die App setzt deshalb keinen.

## Warum es helfen kann
Sendet ein Spielserver kleine TCP-Nachrichten und wartet auf Bestätigungen, bevor er weitersendet, können schnellere Bestätigungen diese Wartezeit verkürzen.

## Belege
Microsoft rät davon ab, den Standardwert ohne sorgfältige Prüfung der Umgebung zu ändern [1]. Ob ein Spiel seinen Echtzeitverkehr über TCP oder UDP schickt, hängt vom Spiel ab. UDP kennt keine Bestätigungen, darauf wirkt die Einstellung nicht. Messungen mit einem Gewinn in aktuellen Spielen sind uns nicht bekannt.

## Nachteile & Risiken
Mehr Bestätigungspakete im Netzwerk, vor allem bei großen Downloads.

## Wann du es nicht nutzen solltest
Nicht nötig, außer ein bestimmtes TCP-basiertes Spiel zeigt einen messbaren Vorteil. Der Wert gilt pro Schnittstelle. Ein später verbundener Adapter bekommt ihn nicht.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/registry-entry-control-tcp-acknowledgment-behavior
2. https://www.rfc-editor.org/rfc/rfc896
3. https://learn.microsoft.com/en-us/windows/win32/winsock/ipproto-tcp-socket-options
