# Diagnoseprotokolle und Speicherabbilder begrenzen

## Zusammenfassung
Verhindert zusätzliche Diagnoseprotokolle und vollständige Speicherabbilder. Wirkt nur, wenn du optionale Diagnosedaten sendest.

## So funktioniert es
Die Richtlinien „Diagnoseprotokollsammlung einschränken“ und „Absturzabbildsammlung einschränken“ werden aktiviert [1]. Die Windows-Fehlerberichterstattung sendet dann nur Kernel-Minidumps und Triage-Abbilder im Benutzermodus. Beide Richtlinien wirken nur, wenn der PC optionale Diagnosedaten sendet.

## Warum es helfen kann
Wenn du optionale Diagnosedaten sendest, verlassen weniger Daten diesen PC. Kein messbarer Einfluss auf die Leistung.

## Belege
Datenschutzeinstellung; sie ändert weder Bildrate noch Latenz.

## Nachteile & Risiken
Microsoft erhält weniger Details, um Abstürze auf deinem PC zu analysieren.

## Wann du es nicht nutzen solltest
Wenn der Microsoft-Support dich um vollständige Diagnosedaten bittet. Sendest du nur erforderliche Diagnosedaten, ändert sich nichts.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-system
