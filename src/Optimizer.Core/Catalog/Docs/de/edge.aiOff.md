# Microsoft Edge: generative KI-Funktionen aus

## Zusammenfassung
Schaltet die KI-Verlaufssuche, die eingebauten KI-Schnittstellen für Websites und KI-Designs aus und verhindert den Download des lokalen KI-Modells; ein geladenes Modell wird gelöscht.

## So funktioniert es
EdgeHistoryAISearchEnabled auf aus beschränkt die Verlaufssuche auf genaue Treffer [1]. BuiltInAIAPIsEnabled auf aus sperrt die Schnittstellen LanguageModel, Summarization, Writer und Rewriter für Websites [2]. AIGenThemesEnabled auf aus schaltet mit DALL-E erzeugte Designs aus [3]. GenAILocalFoundationalModelSettings auf 1 verhindert den Download des KI-Grundmodells und löscht ein bereits geladenes Modell [4]. Die Modell-Richtlinie gilt auch für Profile mit Microsoft-Konto; die anderen drei wendet Edge laut Microsoft seit Edge 116 in solchen Profilen nicht an [5], dort bleibt deine eigene Einstellung.

## Warum es helfen kann
Websites können die eingebaute KI von Edge nicht auf deinem PC ausführen, und das lokale Modell belegt keinen Speicherplatz mehr.

## Belege
Von Microsoft beschriebene Edge-Richtlinien [1][2][3][4]. Wie groß das lokale Modell ist, nennt Microsoft nicht; Bildrate und Latenz ändern sich nicht.

## Nachteile & Risiken
Die Verlaufssuche findet nur noch genaue Wörter, und Seiten, die die eingebauten KI-Schnittstellen nutzen, bekommen einen Fehler. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du die KI-Verlaufssuche oder Seiten nutzt, die auf den KI-Schnittstellen von Edge aufbauen.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/edgehistoryaisearchenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/builtinaiapisenabled
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/aigenthemesenabled
4. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/genailocalfoundationalmodelsettings
5. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
