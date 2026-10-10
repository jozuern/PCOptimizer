# Editor: KI-Funktionen aus

## Zusammenfassung
Schaltet die KI-Funktionen im Windows-Editor (Notepad) aus, etwa das Umschreiben und Zusammenfassen von Text.

## So funktioniert es
Die Editor-Richtlinie DisableAIFeaturesInNotepad schreibt DisableAIFeatures = 1 unter SOFTWARE\Policies\WindowsNotepad; damit sind die KI-Funktionen im Editor nicht mehr zugänglich [1]. Sie braucht Windows 11 22H2 oder neuer und Editor 11.2503.16.0 oder neuer [1].

## Warum es helfen kann
Der Editor bleibt ein einfaches Textprogramm, und kein Text geht durch einen Fehlklick an einen KI-Dienst.

## Belege
Eine von Microsoft beschriebene Editor-Richtlinie [1]. Kein Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Die KI-Schaltflächen im Editor fallen weg.

## Wann du es nicht nutzen solltest
Wenn du Text mit den KI-Funktionen des Editors umschreibst oder zusammenfasst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/manage-notepad
