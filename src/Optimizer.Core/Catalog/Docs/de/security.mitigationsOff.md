# Spectre/Meltdown-Schutzmaßnahmen aus

## Zusammenfassung
Experte, Abwägung bei der Sicherheit: schaltet die Schutzmaßnahmen des Betriebssystems gegen Spectre Variante 2 und Meltdown ab. Betrifft vor allem ältere CPUs.

## So funktioniert es
FeatureSettingsOverride = 3 zusammen mit FeatureSettingsOverrideMask = 3 schaltet die Windows-Schutzmaßnahmen gegen CVE-2017-5715 und CVE-2017-5754 ab, wie von Microsoft dokumentiert [1]. Wirkt nach einem Neustart.

## Warum es helfen kann
Die Schutzmaßnahmen machen Systemaufrufe und Kontextwechsel aufwendiger. Auf CPUs ohne Hardware-Korrektur (etwa vor 2019) ist dieser Aufwand größer.

## Belege
Aktuelle CPUs haben Schutz in Hardware. Dort liegt der Gewinn meist innerhalb der Messschwankung. Der Nutzen ist umstritten.

## Nachteile & Risiken
Macht den PC angreifbar für Angriffe über spekulative Ausführung, etwa durch Schadcode im Browser.

## Wann du es nicht nutzen solltest
Nicht auf aktuellen CPUs, nicht auf PCs für Onlinebanking oder Arbeit und nicht, wenn du nicht vertrauenswürdige Seiten besuchst.

## Quellen
1. https://support.microsoft.com/en-us/topic/kb4073119-windows-client-guidance-for-it-pros-to-protect-against-silicon-based-microarchitectural-and-speculative-execution-side-channel-vulnerabilities-35820a8a-ae13-1299-88cc-357f104f5b11
