# Keine automatischen Updates der Sprachmodelle

## Zusammenfassung
Windows lädt aktualisierte Modelle für Spracherkennung und Sprachausgabe nicht mehr im Hintergrund. Sprachfunktionen arbeiten mit den Modellen weiter, die schon auf dem PC sind. Ab Pro.

## So funktioniert es
Sprachmodelle enthalten die Daten, mit denen die Sprach-Engine Audio in Text und Text in Sprache umwandelt. Windows sucht von Zeit zu Zeit nach neueren Modellen und lädt sie mit dem Intelligenten Hintergrundübertragungsdienst (BITS) von einem Microsoft-Dienst [1]. Die Richtlinie AllowSpeechModelUpdate auf 0 beendet diese Suche und die Downloads [1]. Die Modelle sind Datendateien, keine Programme [1]; es geht also um Netzwerknutzung und aktuelle Daten, nicht um Sicherheitsupdates.

## Warum es helfen kann
Weniger Downloads und Verbindungen zu Microsoft im Hintergrund, was bei einer getakteten oder langsamen Verbindung zählt.

## Belege
Eine von Microsoft beschriebene Windows-Richtlinie [1]; Microsoft führt sie für Pro, Enterprise und Education [1]. Eine Einstellung für Datenschutz und Netzwerk ohne Einfluss auf Framerate oder Latenz.

## Nachteile & Risiken
Spracherkennung und Sprachausgabe behalten ihre Modelle und bekommen die Verbesserungen bei Genauigkeit und Leistung neuerer Modelle nicht mehr [1].

## Wann du es nicht nutzen solltest
Wenn du auf diesem PC Spracherkennung oder Sprachausgabe nutzt und ihre Modelle aktuell halten willst.

## Quellen
1. https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-speech
