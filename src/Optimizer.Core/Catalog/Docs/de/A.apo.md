# Intel Application Optimization (APO)

## Zusammenfassung
::: status Info
Der {{cpu}} unterstützt Intel APO, das in einer Liste unterstützter Spiele die Leistung verbessert. Dafür braucht es den Dynamic-Tuning-Treiber des Mainboards und die APO-App.
:::
::: status Ok,Unknown,Problem,Unsupported
Prüft, ob der Prozessor Intel Application Optimization unterstützt.
:::

## Warum das wichtig ist
Intel APO passt an, wie die Threads eines unterstützten Spiels auf Performance- und Effizienzkerne verteilt werden. Intel listet die unterstützten Spiele; in diesen steigt die Bildrate, in anderen ändert sich nichts [1]. Es funktioniert nur mit bestimmten K-Prozessoren und braucht Unterstützung durch das Mainboard (Treiber für Intel Dynamic Tuning Technology).
::: variant dttFound
Der Dynamic-Tuning-Treiber ist auf diesem PC installiert.
:::
::: variant dttNotFound
Den Dynamic-Tuning-Treiber haben wir nicht gefunden. Er fehlt vielleicht oder nutzt einen Namen, den wir nicht kennen.
:::

## Wie wir es erkennen
Wir gleichen den Prozessornamen mit der Liste unterstützter Modelle ab und suchen nach den Diensten von Dynamic Tuning. Ob die APO-App installiert und aktiv ist, lässt sich nicht zuverlässig lesen.

## So behebst du es
1. Installiere den Treiber **Intel Dynamic Tuning Technology** von der Supportseite deines {{board}} (nicht direkt von Intel, er ist mainboardspezifisch).
2. Installiere **Intel Application Optimization** aus dem Microsoft Store.
3. Bei manchen Mainboards muss APO im BIOS eingeschaltet werden (oft unter Intel Dynamic Tuning oder „Application Optimization“).
4. Öffne die APO-App und prüfe, ob sie für deine Spiele eingeschaltet ist.

## So prüfst du die Behebung
Die APO-App listet die unterstützten Spiele und zeigt, ob sie aktiv ist.

## Quellen
1. https://www.kitguru.net/components/cpu/joao-silva/intel-apo-receives-12-new-game-profiles-core-ultra-200k-series-now-supported/
