# MSI-Modus für dieses Gerät (Experte)

## Zusammenfassung
Lässt das Gerät nachrichtenbasierte statt leitungsbasierte Interrupts nutzen. Umstritten: Von Nutzern berichtete Verbesserungen sind nicht einheitlich messbar. Braucht einen Neustart.

## So funktioniert es
Geräte melden sich beim Prozessor mit Interrupts. Ältere leitungsbasierte Interrupts können sich mehrere Geräte teilen. Nachrichtenbasierte Interrupts (MSI) werden in den Speicher geschrieben und nicht geteilt. Windows nutzt MSI für ein Gerät, wenn dessen Registry-Eintrag MSISupported auf 1 steht. Normalerweise setzt das Installationsprogramm des Treibers diesen Wert [1]. Die App setzt ihn für dieses Gerät. Die Änderung wirkt nach einem Neustart.

## Warum es helfen kann
Nutzte das Gerät geteilte leitungsbasierte Interrupts, vermeidet MSI, dass zuerst die Interrupts anderer Geräte geprüft werden. Das kann Verzögerungsspitzen verringern.

## Belege
Steht MSISupported für dieses Gerät schon auf 1, wird der Punkt als an angezeigt und es ändert sich nichts. Von Nutzern berichtete Verbesserungen sind nicht einheitlich messbar.

## Nachteile & Risiken
Hat das Installationsprogramm des Treibers MSISupported auf 0 gesetzt, hat der Hersteller leitungsbasierte Interrupts bewusst gewählt. Erzwungenes MSI kann dann dazu führen, dass der Treiber nicht startet (Fehler im Geräte-Manager) oder Windows selten abstürzt. Ein Treiber-Update kann außerdem wieder seinen eigenen Wert schreiben. Deshalb ist das eine startkritische Expertenänderung. Die App legt vorher einen Wiederherstellungspunkt an.

## Wann du es nicht nutzen solltest
Wenn das Gerät MSI bereits nutzt, oder für Speichercontroller (hier nicht angeboten).

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/enabling-message-signaled-interrupts-in-the-registry
