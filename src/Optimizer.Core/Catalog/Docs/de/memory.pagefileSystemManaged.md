# Auslagerungsdatei von Windows verwalten lassen

## Zusammenfassung
Stellt die Auslagerungsdatei wieder auf „Automatisch verwalten“. Behebt Abstürze und Speicherfehler durch eine abgeschaltete oder zu kleine Auslagerungsdatei.

## So funktioniert es
Die Auslagerungsdatei erweitert den Arbeitsspeicher und wird für Absturzabbilder gebraucht. Alte Anleitungen empfehlen, sie abzuschalten oder eine kleine feste Größe zu setzen. Der Wert PagingFiles = ?:\pagefile.sys bedeutet, dass Windows Größe und Ort auf allen Laufwerken verwaltet [1]. Wirkt nach einem Neustart.

## Warum es helfen kann
Spiele mit hohem Speicherbedarf können abstürzen oder ruckeln, wenn das Zusagelimit erreicht ist. Eine verwaltete Auslagerungsdatei wächst bei Bedarf mit.

## Belege
Microsoft empfiehlt für die meisten Systeme eine vom System verwaltete Auslagerungsdatei [1]. Mit genug RAM wird sie selten genutzt und kostet nichts.

## Nachteile & Risiken
Belegt etwas Platz auf dem Systemlaufwerk.

## Wann du es nicht nutzen solltest
Behalte eine eigene Größe nur, wenn du sie bewusst gesetzt hast und den Spitzenbedarf deiner Anwendungen kennst.

## Quellen
1. https://learn.microsoft.com/en-us/troubleshoot/windows-client/performance/introduction-to-the-page-file
