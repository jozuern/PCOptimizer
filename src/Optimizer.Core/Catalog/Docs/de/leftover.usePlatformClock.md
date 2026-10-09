# Erzwungenes HPET entfernen (useplatformclock)

## Zusammenfassung
Experte, Startkonfiguration: entfernt useplatformclock, das ältere Anleitungen setzen und das den langsamen HPET-Timer erzwingt. Danach nutzt Windows wieder seinen Standard-Timer.

## So funktioniert es
Die Startoption useplatformclock zwingt Windows, die Plattformuhr (HPET) als Zeitquelle zu nutzen [1]. HPET auszulesen dauert viel länger als der CPU-eigene Timer. Programme, die oft die Zeit abfragen (Spiele tun das), verbringen deshalb mehr Zeit in Timer-Aufrufen. Die App löscht die Option, nachdem sie die Startkonfiguration exportiert hat.

## Warum es helfen kann
Spiele, die in jedem Frame die Zeit abfragen, laufen mit weniger Overhead. Die Frametimes können gleichmäßiger werden.

## Belege
Die höheren Kosten von HPET-Abfragen sind gut dokumentiert. Wie stark sich das auf die FPS auswirkt, hängt davon ab, wie oft ein Spiel die Zeit abfragt.

## Nachteile & Risiken
Eine Änderung der Startkonfiguration. Startet der PC nicht richtig, mache sie über die Wiederherstellungsumgebung rückgängig (Umschalt + Neu starten > Problembehandlung > Systemwiederherstellung). Der BCD-Export liegt im Datenordner der App.

## Wann du es nicht nutzen solltest
Nichts zu tun, wenn die Option nicht gesetzt ist. Startoptionen nur mit direktem Zugriff auf den PC ändern.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/bcdedit--set
