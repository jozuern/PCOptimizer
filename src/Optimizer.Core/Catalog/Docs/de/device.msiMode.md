# MSI-Modus für dieses Gerät (Experte)

## Zusammenfassung
Lässt das Gerät nachrichtenbasierte statt leitungsbasierte Interrupts nutzen. Viele aktuelle Treiber tun das bereits. Umstritten, braucht einen Neustart.

## So funktioniert es
Geräte melden sich beim Prozessor mit Interrupts. Ältere leitungsbasierte Interrupts können sich mehrere Geräte teilen; nachrichtenbasierte Interrupts (MSI) werden in den Speicher geschrieben und nie geteilt. Windows nutzt MSI, wenn der Treiber es in der Registry anfordert; die App setzt diesen dokumentierten Wert (MSISupported = 1) für dieses Gerät [1]. Die Änderung wirkt nach einem Neustart.

## Warum es helfen kann
Nutzte das Gerät geteilte leitungsbasierte Interrupts, vermeidet MSI, dass zuerst die Interrupts anderer Geräte geprüft werden. Das kann Verzögerungsspitzen verringern.

## Belege
Die meisten aktuellen Grafik- und Netzwerktreiber fordern MSI bereits selbst an; dann ändert sich nichts. Von Nutzern berichtete Verbesserungen sind nicht einheitlich messbar.

## Nachteile & Risiken
Ein Treiber, der MSI nicht richtig unterstützt, startet eventuell nicht (Fehler im Geräte-Manager) oder verursacht selten einen Bluescreen. Deshalb ist das eine startkritische Expertenänderung: Lege vorher einen Wiederherstellungspunkt an. Bei einer Grafikkarte startet Windows weiterhin mit dem Basis-Anzeigetreiber, und von dort funktioniert das Rückgängigmachen.

## Wann du es nicht nutzen solltest
Wenn das Gerät MSI bereits nutzt, oder für Speichercontroller (hier nicht angeboten).

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/enabling-message-signaled-interrupts-in-the-registry
