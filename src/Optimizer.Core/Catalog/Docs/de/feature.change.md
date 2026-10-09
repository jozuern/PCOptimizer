# Windows-Feature ein- oder ausschalten

## Zusammenfassung
Schaltet dieses optionale Windows-Feature mit DISM ein oder aus, wie unter Einstellungen > System > Optionale Features. Braucht meist einen Neustart.

## So funktioniert es
Die App führt DISM aus, um das Feature ein- oder auszuschalten [1]. Beim Ausschalten bleiben die Dateien erhalten, das Wiedereinschalten klappt daher meist offline. Rückgängig machen stellt den vorherigen Zustand wieder her.

## Warum es helfen kann
Features, die du nicht nutzt (zum Beispiel SMB 1.0), bieten eine Angriffsfläche weniger. Schaltest du Hypervisor-Features ab (Hyper-V, VM-Plattform, Sandbox), startet der Hypervisor nicht mehr, außer Speicherintegrität oder ein anderes Feature braucht ihn weiterhin.

## Belege
Die meisten Features haben keinen Einfluss auf Spiele. Die Hypervisor-Features können je nach Prozessor und Speicherintegrität einen haben.

## Nachteile & Risiken
Programme, die das Feature brauchen, funktionieren nicht mehr (zum Beispiel WSL, virtuelle Maschinen, alte Spiele ohne DirectPlay).

## Wann du es nicht nutzen solltest
Wenn ein Programm, das du nutzt, das Feature braucht.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/enable-or-disable-windows-features-using-dism
