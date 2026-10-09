# AMD-Software-Einstellungen fürs Spielen

## Zusammenfassung
::: status Info
Einstellungen in AMD Software: Adrenalin Edition, die fürs Spielen auf der {{gpu}} zählen. Die App erklärt sie; geändert werden sie in AMD Software.
:::
::: status Ok,Unknown,Problem,Unsupported
Empfohlene Einstellungen in AMD Software fürs Spielen.
:::

## Warum das wichtig ist
Manche Radeon-Funktionen tauschen Bildqualität oder Bildrate gegen Energieersparnis oder Laufruhe, und ihre Standardwerte hängen von der Treiberversion und dem bei der Installation gewählten Profil ab. AMDs Einstellungsschnittstelle (ADLX) ist ein C++-SDK ohne unterstützten Weg, über den diese App die Werte sicher schreiben kann. Deshalb zeigt die App die Einstellungen und ihre Wirkung, statt sie zu ändern.

## Wie wir es erkennen
Wir haben eine Radeon-Grafikkarte mit AMD-Treiber gefunden.

## So behebst du es
1. Öffne **AMD Software: Adrenalin Edition > Gaming > Grafik** (global oder pro Spiel).
2. **Radeon Anti-Lag:** Ein. Senkt die Eingabeverzögerung in GPU-limitierten Spielen; Spiele mit Anti-Lag-2-Unterstützung nutzen die stärkere Version im Spiel.
3. **Radeon Chill:** Aus, außer du willst die Bildrate für weniger Wärme und Lautstärke begrenzen.
4. **Radeon Boost:** Aus, wenn dir die niedrigere Auflösung bei schnellen Bewegungen auffällt; es erhöht die Bildrate, indem es während der Bewegung mit geringerer Auflösung rendert.
5. **Auf vertikale Aktualisierung warten:** „Aus, außer von der Anwendung festgelegt“, und **Enhanced Sync** aus, wenn du mit FreeSync Ruckler siehst.
6. **Gaming > Anzeige:** Schalte **AMD FreeSync** für einen FreeSync-Monitor ein.
7. **Aufnahme und Streaming:** Schalte die Sofortwiederholung ab, wenn du sie nicht nutzt; sie nimmt im Hintergrund auf.

## So prüfst du die Behebung
AMD Software zeigt die aktiven Werte pro Spiel. Der Benchmark auf der Seite Zustand misst die Wirkung.
