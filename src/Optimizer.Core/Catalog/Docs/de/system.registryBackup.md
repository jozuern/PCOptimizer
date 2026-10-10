# Registrierungssicherung im Ordner RegBack

## Zusammenfassung
Windows kopiert die Systemregistrierung wieder nach jedem Neustart und nach Zeitplan in den Ordner RegBack, wie vor Windows 10 1803. Braucht etwas Speicherplatz, keine Wirkung auf Spiele.

## So funktioniert es
Seit Windows 10 Version 1803 sichert Windows die Registrierungsstrukturen nicht mehr nach \Windows\System32\config\RegBack, die Dateien dort sind 0 KB groß. Microsoft dokumentiert, wie du das alte Verhalten zurückholst: EnablePeriodicBackup = 1 unter Session Manager\Configuration Manager, danach ein Neustart [1]. Windows sichert die Registrierung dann beim Neustart und legt eine Aufgabe RegIdleBackup für spätere Sicherungen an [1].

## Warum es helfen kann
Eine Kopie der Registrierungsstrukturen ist ein weiterer Weg zurück, wenn die Registrierung beschädigt wird, neben Wiederherstellungspunkten und den eigenen Sicherungen dieser App.

## Belege
Microsoft dokumentiert die Einstellung und die Sicherungsaufgabe [1]. Der Artikel gilt für Windows 10. Dass es unter Windows 11 genauso funktioniert, bestätigt Microsoft nicht, es ist Teil des VM-Testplans der App. Für die Wiederherstellung einer beschädigten Registrierung empfiehlt Microsoft Wiederherstellungspunkte [1].

## Nachteile & Risiken
Die Kopien brauchen Speicherplatz. Microsoft hat die Sicherung abgeschaltet, um den Platzbedarf von Windows zu verringern [1]. Das Zurückspielen aus RegBack ist ein manueller Schritt in der Windows-Wiederherstellungsumgebung. Rückgängig machen entfernt die Einstellung. Die Aufgabe RegIdleBackup und schon angelegte Kopien bleiben eventuell erhalten.

## Wann du es nicht nutzen solltest
Wenn der Speicherplatz knapp ist, oder wenn du ohnehin Wiederherstellungspunkte und Systemabbilder nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/system-registry-no-backed-up-regback-folder
