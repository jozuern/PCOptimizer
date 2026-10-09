# Erzwungene Plattformuhr entfernen (useplatformclock)

## Zusammenfassung
Experte, Startkonfiguration: entfernt useplatformclock, eine Debug-Option aus älteren Anleitungen. Sie erzwingt einen langsameren Plattform-Timer. Danach wählt Windows den Timer wieder selbst.

## So funktioniert es
Die Startoption useplatformclock zwingt Windows, die Plattformuhr als Leistungszähler zu nutzen. Laut Microsoft ist sie nur zur Fehlersuche gedacht [1]. Die Plattformuhr ist der HPET oder der ACPI-PM-Timer. Normalerweise nutzt Windows den Zeitstempelzähler der CPU (TSC), wenn er geeignet ist [2]. Die App löscht die Option, nachdem sie die Startkonfiguration exportiert hat.

## Warum es helfen kann
Den TSC auszulesen dauert einige zehn bis einige hundert CPU-Takte. Ein Plattform-Timer braucht etwa 0,8 bis 1,0 Mikrosekunden und einen Systemaufruf [2]. Spiele fragen die Zeit pro Frame oft ab, das summiert sich.

## Belege
Den Kostenunterschied dokumentiert Microsoft [2]. Wie stark sich das auf die FPS auswirkt, hängt davon ab, wie oft ein Spiel die Zeit abfragt.

## Nachteile & Risiken
Eine Änderung der Startkonfiguration. Ist BitLocker aktiv, halte deinen Wiederherstellungsschlüssel bereit: Laut Microsoft muss BitLocker vor Änderungen an Startoptionen eventuell angehalten werden [1]. Startet der PC nicht richtig, mache die Änderung über die Wiederherstellungsumgebung rückgängig (Umschalt + Neu starten > Problembehandlung > Systemwiederherstellung). Der BCD-Export liegt im Datenordner der App.

## Wann du es nicht nutzen solltest
Nichts zu tun, wenn die Option nicht gesetzt ist. Startoptionen nur mit direktem Zugriff auf den PC ändern.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
2. https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps
