# Microsoft Edge: keine Kennwörter speichern

## Zusammenfassung
Edge bietet nicht mehr an, Kennwörter zu speichern. Bereits gespeicherte Kennwörter bleiben nutzbar. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Mit der Richtlinie PasswordManagerEnabled auf aus lassen sich in Edge keine neuen Kennwörter speichern oder hinzufügen, gespeicherte bleiben aber nutzbar [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Sinnvoll, wenn du Kennwörter in einem eigenen Passwortmanager verwaltest und Edge nicht fragen soll.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1]. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Nachteile & Risiken
Neue Kennwörter landen nicht in Edge, du brauchst also einen anderen Passwortmanager. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn Edge dein Passwortmanager ist.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/passwordmanagerenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
