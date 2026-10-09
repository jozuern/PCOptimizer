# TRIM an

## Zusammenfassung
Schaltet TRIM wieder ein, damit Windows SSDs mitteilt, welche Blöcke frei sind. Windows hat TRIM standardmäßig an. Wichtig ist das nur, wenn es abgeschaltet wurde.

## So funktioniert es
Löschst du eine Datei, markiert Windows den Platz nur als frei. TRIM gibt diese Information an die SSD weiter, die die Blöcke dann im Hintergrund löschen kann. Der Wert DisableDeleteNotification = 0 schaltet es ein (wie fsutil behavior set DisableDeleteNotify 0) [1].

## Warum es helfen kann
Mit TRIM erfährt die SSD direkt nach dem Löschen, welche Blöcke frei sind, und kann sie im Hintergrund vorbereiten. Ohne TRIM merkt sie das erst, wenn die Blöcke überschrieben werden.

## Belege
Bei NTFS ist TRIM standardmäßig an, sofern kein Administrator es abschaltet [1]. Eine Änderung braucht keinen Neustart [1].

## Nachteile & Risiken
Laut Microsoft können manche Geräte mit eingeschalteten Löschbenachrichtigungen langsamer werden [1]. Das ist der einzige dokumentierte Grund, TRIM abzuschalten. Schaltet eine Gruppenrichtlinie TRIM für alle Volumes ab [2], wirkt diese Einstellung erst, wenn die Richtlinie entfernt ist.

## Wann du es nicht nutzen solltest
Nur wenn der Hersteller deiner SSD rät, TRIM für dieses Laufwerk abzuschalten.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/fsutil-behavior
2. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-admx-filesys
