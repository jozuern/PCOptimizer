# Game-Bar-Aufnahmen aus

## Zusammenfassung
Schaltet die Aufnahmen der Xbox Game Bar ab, auch „Aufzeichnen, was passiert ist“, das die letzten Spielminuten ständig im Hintergrund aufnimmt.

## So funktioniert es
Ist die Hintergrundaufnahme an, kodiert Windows die letzten Spielminuten laufend mit dem Video-Encoder der GPU in einen Puffer. Die App schaltet Game DVR, App-Aufnahme und Hintergrundaufnahme für dein Benutzerkonto ab, mit denselben Werten wie WinUtil [2]. Microsoft dokumentiert diese Werte pro Benutzer nicht; die dokumentierte Richtlinie „AllowGameDVR“ schaltet die Aufnahme für den ganzen PC ab [1]. Die Game Bar selbst bleibt installiert.

## Warum es helfen kann
Die Daueraufnahme belegt Encoder-Zeit der GPU, Arbeitsspeicher und Schreibzugriffe. Ohne sie fällt diese Last beim Spielen weg.

## Belege
Ist die Hintergrundaufnahme aus, wie meist üblich, ändert sich nichts Messbares. Ist sie an, arbeitet der Video-Encoder der GPU während des Spielens ständig; eine veröffentlichte Messung der Kosten haben wir nicht gefunden.

## Nachteile & Risiken
Du verlierst Game-Bar-Clips und die Tastenkombination „Das aufzeichnen“. Aufnahmen mit NVIDIA ShadowPlay oder OBS sind nicht betroffen.

## Wann du es nicht nutzen solltest
Behalte die Funktion, wenn du Game-Bar-Clips nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-applicationmanagement
2. https://github.com/ChrisTitusTech/winutil
