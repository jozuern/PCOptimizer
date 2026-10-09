# Speicheroptimierung an

## Zusammenfassung
Schaltet die Speicheroptimierung ein. Wird der Platz knapp, löscht sie temporäre Dateien und Dateien, die länger als 30 Tage im Papierkorb liegen.

## So funktioniert es
Die Speicheroptimierung ist eine Windows-Funktion. Sie entfernt temporäre Dateien, die nicht in Gebrauch sind, und standardmäßig Dateien, die seit mehr als 30 Tagen im Papierkorb liegen [2]. Standardmäßig läuft sie, wenn auf dem Laufwerk wenig Platz frei ist [1][2]. Downloads bleiben unberührt, solange du diese Regel nicht einschaltest [2]. Die App schaltet den Schalter unter Einstellungen > System > Speicher ein. Dort kannst du die Regeln anpassen. Der Registrierungswert, den die App schreibt, ist der Wert hinter diesem Schalter. Microsoft dokumentiert ihn nicht eigens.

## Warum es helfen kann
Hält Platz für Spiele-Updates und Shader-Caches frei, ohne dass du manuell aufräumen musst.

## Belege
Kein direkter Effekt auf die Leistung. Sie beugt Problemen durch vollen Speicher vor.

## Nachteile & Risiken
Dateien, die länger als 30 Tage im Papierkorb liegen, werden endgültig gelöscht [2]. Ab Windows 11 Version 22H2 kann die Funktion laut Microsoft OneDrive-Dateien, die 30 Tage nicht geöffnet wurden, standardmäßig auf „Nur online“ setzen [1]. Sie bleiben in OneDrive, brauchen zum Öffnen aber eine Verbindung.

## Wann du es nicht nutzen solltest
Nicht nötig, wenn du selbst aufräumst oder Dateien bewusst im Papierkorb aufbewahrst.

## Quellen
1. https://support.microsoft.com/en-us/windows/manage-drive-space-with-storage-sense-654f6ada-7bfc-45e5-966b-e24aded96ad5
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-storage
