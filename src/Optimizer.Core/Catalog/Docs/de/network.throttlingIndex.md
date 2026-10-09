# Netzwerk-Drosselung aus (NetworkThrottlingIndex)

## Zusammenfassung
Schaltet eine Paketgrenze ab, die MMCSS während Audio- oder Videowiedergabe für Netzwerkverkehr setzte (für Windows Vista dokumentiert). Unter Windows 11 unbelegt. Wirkung umstritten.

## So funktioniert es
In Windows Vista wies der Multimedia Class Scheduler Service (MMCSS) den Netzwerkstapel an, während einer Multimedia-Wiedergabe höchstens 10 empfangene Pakete pro Millisekunde weiterzugeben, damit die Netzwerkverarbeitung den Ton nicht unterbricht [1]. NetworkThrottlingIndex legt diese Grenze fest, 0xFFFFFFFF schaltet sie ab [2]. Die aktuelle Microsoft-Dokumentation zu MMCSS erwähnt den Wert nicht [3]. Ob Windows 11 die Grenze noch anwendet, ist daher unbelegt. Wirkt nach einem Neustart.

## Warum es helfen kann
Ist die Grenze aktiv, bremst sie schnelle Übertragungen mit vielen Paketen pro Sekunde, etwa das Kopieren großer Dateien im lokalen Netz, während Musik läuft.

## Belege
Die einzige Beschreibung von Microsoft ist ein Artikel von 2007 über Windows Vista. Danach blieb Internetverkehr selbst mit den schnellsten Anschlüssen dieser Zeit unter der Grenze [1]. Messungen unter Windows 11 oder Messungen mit Wirkung in Spielen sind uns nicht bekannt.

## Nachteile & Risiken
Microsoft hat die Grenze eingeführt, weil starker Netzwerkverkehr in Tests auf PCs mit einem Prozessorkern Aussetzer bei Audio und Video verursachte [1]. Ohne sie kann das auf langsamer Hardware wieder passieren.

## Wann du es nicht nutzen solltest
Für Online-Spiele nicht nötig. Knackt der Ton nach dem Anwenden bei großen Downloads, mach es rückgängig.

## Quellen
1. https://learn.microsoft.com/en-us/archive/blogs/markrussinovich/vista-multimedia-playback-and-network-throughput
2. https://support.tibco.com/s/article/Tibco-KnowledgeArticle-Article-38041
3. https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
