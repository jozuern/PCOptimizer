# Intel Core Ultra 200S: BIOS-Update mit Microcode {{min}}

## Zusammenfassung
::: status Problem
Dein BIOS liefert einen Microcode älter als {{min}}. Intels spätere Updates für Core Ultra 200S beheben Leistungsprobleme in Spielen.
:::
::: status Ok
Dein BIOS liefert Microcode {{min}} oder neuer mit Intels Spieleleistungs-Korrekturen für Core Ultra 200S.
:::
::: status Unknown,Info,Unsupported
Prüft, ob Core-Ultra-200S-Desktop-CPUs einen BIOS-Microcode mit Intels Korrekturen für die Spieleleistung nutzen.
:::

## Warum das wichtig ist
Zum Start blieben Intel-Core-Ultra-200S-Prozessoren (Arrow Lake) in vielen Spielen hinter den Erwartungen zurück. Intel führte einen Teil davon auf die Firmware zurück und veröffentlichte Microcode {{min}} zusammen mit einem neuen CSME-Firmware-Kit (19.0.0.1854v2.2 oder neuer) und Windows-Updates. Mainboards mit älterem BIOS fehlen diese Korrekturen. Der Gewinn hängt vom Spiel ab und liegt meist bei wenigen Prozent.

## Wie wir es erkennen
Wir lesen das Prozessormodell und die vom BIOS geladene Microcode-Revision. Modellliste und Mindestrevision stehen im Katalog.

## So behebst du es
1. Öffne die Support-Seite deines Mainboards ({{board}}).
2. Spiele nach Anleitung des Herstellers das neueste BIOS ein, das Microcode {{min}} und CSME-Firmware 19.0.0.1854v2.2 oder neuer nennt.
3. Installiere die aktuellen Windows-Updates sowie die Intel-Chipsatz- und PPM-Treiber vom Mainboard-Hersteller.

## So prüfst du die Behebung
Starte den Scan erneut. Der Microcode aus dem BIOS sollte {{min}} oder neuer sein.

## Quellen
1. https://www.elevenforum.com/t/field-update-1-of-2-intel-core-ultra-200s-series-performance-status.31640/latest
