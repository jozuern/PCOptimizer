# Dynamic Tick aus (BCD)

## Zusammenfassung
Experte, Startkonfiguration: lässt den Systemtimer in festem Takt laufen, statt ihn im Leerlauf anzuhalten. Wirkung umstritten.

## So funktioniert es
Mit Dynamic Tick stoppt Windows den periodischen Timer-Interrupt, solange die CPU ruht, um Strom zu sparen. Die Startoption disabledynamictick yes lässt den Timer dauerhaft laufen [1]. Die App exportiert vorher die Startkonfiguration.

## Warum es helfen kann
Manche Nutzer berichten mit festem Takt von gleichmäßigeren Frametimes oder niedrigerer DPC-Latenz.

## Belege
Kontrollierte Tests zeigen keinen einheitlichen Vorteil in Spielen. Der Nutzen ist umstritten.

## Nachteile & Risiken
Höherer Leerlaufverbrauch. Es ist eine Änderung der Startkonfiguration: Startet der PC nicht richtig, mache sie über die Wiederherstellungsumgebung rückgängig (Umschalt + Neu starten > Problembehandlung > Systemwiederherstellung). Der BCD-Export liegt im Datenordner der App.

## Wann du es nicht nutzen solltest
Nicht auf Laptops. Nur ausprobieren, wenn du direkten Zugriff auf den PC hast.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
