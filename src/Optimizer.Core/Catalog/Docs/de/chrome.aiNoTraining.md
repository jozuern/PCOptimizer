# Google Chrome: KI-Funktionen ohne Training für Googles Modelle

## Zusammenfassung
Behält die generativen KI-Funktionen von Chrome, erlaubt Google aber standardmäßig nicht, deine Eingaben und Ergebnisse zum Verbessern der KI-Modelle zu nutzen.

## So funktioniert es
GenAiDefaultSettings legt den Standard für die abgedeckten generativen KI-Funktionen fest [1]. Mit 0, dem üblichen Standard, darf Google relevante Daten wie Eingaben, Ergebnisse und Feedback zum Verbessern seiner Modelle nutzen, auch mit Prüfung durch Menschen [1]. Die App setzt 1: Die Funktionen bleiben, ohne diese Nutzung [1]. Chrome liest die Richtlinie aus HKLM\SOFTWARE\Policies\Google\Chrome, auch auf PCs ohne Domäne [2].

## Warum es helfen kann
Du behältst die Funktionen und gibst weniger Daten für das Training. Bildrate und Latenz ändern sich nicht.

## Belege
Eine von Google beschriebene Chrome-Richtlinie [1]. Sie setzt einen Standard: Eine Richtlinie für eine einzelne Funktion hat Vorrang [1].

## Nachteile & Risiken
Weil es Richtlinien sind, zeigt Chrome an, dass es von deiner Organisation verwaltet wird, und die passenden Einstellungen sind gesperrt. Rückgängig entfernt sie.

## Wann du es nicht nutzen solltest
Wenn du die KI-Funktionen ganz ausschalten willst; dafür gibt es die andere Chrome-KI-Option.

## Quellen
1. https://chromeenterprise.google/policies/#GenAiDefaultSettings
2. https://support.google.com/chrome/a/answer/9131254?hl=en
