# Google Chrome: generative KI-Funktionen aus

## Zusammenfassung
Schaltet die generativen KI-Funktionen aus, die Google über einen gemeinsamen Standard steuert, dazu Gemini in Chrome und den KI-Modus der Adressleiste, und stoppt den Download des lokalen KI-Modells.

## So funktioniert es
GenAiDefaultSettings auf 2 macht "Nicht zulassen" zum Standard für die generativen KI-Funktionen, die es abdeckt; eine eigens gesetzte Richtlinie für eine Funktion hat Vorrang [1]. GeminiSettings und AIModeSettings kennen nur zulassen und nicht zulassen, die App setzt beide auf nicht zulassen [2][3]. GenAILocalFoundationalModelSettings auf 1 stoppt den Download des lokalen KI-Modells und löscht ein bereits geladenes [4]. Chrome liest diese Richtlinien aus HKLM\SOFTWARE\Policies\Google\Chrome, auch auf PCs ohne Domäne [5].

## Warum es helfen kann
Chrome schickt keine Seiteninhalte oder Eingaben an Googles KI-Dienste, und das lokale Modell belegt keinen Speicherplatz.

## Belege
Von Google beschriebene Chrome-Richtlinien [1][2][3][4]. Bildrate und Latenz ändern sich nicht.

## Nachteile & Risiken
Gemini, der KI-Modus und die übrigen abgedeckten KI-Funktionen fallen weg. Weil es Richtlinien sind, zeigt Chrome an, dass es von deiner Organisation verwaltet wird, und die passenden Einstellungen sind gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du Gemini oder andere KI-Funktionen in Chrome nutzt; die Variante ohne Modelltraining behält sie.

## Quellen
1. https://chromeenterprise.google/policies/#GenAiDefaultSettings
2. https://chromeenterprise.google/policies/#GeminiSettings
3. https://chromeenterprise.google/policies/#AIModeSettings
4. https://chromeenterprise.google/policies/#GenAILocalFoundationalModelSettings
5. https://support.google.com/chrome/a/answer/9131254?hl=en
