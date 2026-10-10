# Microsoft Edge: keine Textvorhersage und Rechtschreibprüfung aus der Cloud

## Zusammenfassung
Text in Webformularen geht nicht mehr für Vorhersagen und erweiterte Prüfung an Microsoft-Dienste; die lokale Rechtschreibprüfung bleibt. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
TextPredictionEnabled auf aus beendet die Vorhersagen, die der Microsoft-Dienst Turing für lange Textfelder erzeugt [1]. MicrosoftEditorProofingEnabled auf aus überlässt die Rechtschreibprüfung lokalen Prüfern statt dem Dienst Microsoft Editor [2]. Seit Edge 116 wendet Edge diese Richtlinien laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [3]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Text, den du in Webformularen schreibst, bleibt auf dem PC. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Von Microsoft beschriebene Edge-Richtlinien [1][2]. Laut Microsoft liefert die lokale Prüfung weniger ausführliche Ergebnisse [2].

## Nachteile & Risiken
Keine Wortvorhersagen und einfachere Rechtschreibvorschläge. Weil es Richtlinien sind, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du die Textvorhersage oder die Grammatikvorschläge von Edge nutzt.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/textpredictionenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/microsofteditorproofingenabled
3. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
