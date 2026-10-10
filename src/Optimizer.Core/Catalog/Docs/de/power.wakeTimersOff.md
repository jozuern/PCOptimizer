# Keine Zeitgeber zur Aktivierung

## Zusammenfassung
Geplante Aufgaben, Wartung und Updates können den PC nicht mehr aus dem Energiesparmodus wecken. Hilft, wenn sich ein PC nachts einschaltet. Gilt für den aktiven Energiesparplan.

## So funktioniert es
Die Energieeinstellung "Zeitgeber zur Aktivierung zulassen" entscheidet, ob das System Wecken per Zeitgeber nutzt, etwa um Updates zu installieren [1]. Die App setzt sie für Netz- und Akkubetrieb des aktiven Energiesparplans auf 0 (deaktiviert) [1]. Sonst plant Windows um 3 Uhr eine Regelwartung, die den PC über einen solchen Zeitgeber wecken kann [2].

## Warum es helfen kann
Der PC bleibt nachts im Energiesparmodus: keine Lüfter, kein Lärm, kein leerer Akku in der Tasche.

## Belege
Eine von Microsoft beschriebene Energieeinstellung [1][2]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Wartung und Updates laufen, wenn du den PC das nächste Mal nutzt; direkt nach dem Aufwachen kann etwas mehr los sein. Aufgaben, die den PC wecken sollen, etwa Aufnahmen oder Sicherungen, wecken ihn nicht mehr.

## Wann du es nicht nutzen solltest
Wenn du geplante Aufgaben nutzt, die den PC wecken, etwa für Sicherungen oder Aufnahmen.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/sleep-settings-automatically-wake-for-tasks
2. https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/desktop-wakes-up-unexpectedly-from-sleep-hibernation
