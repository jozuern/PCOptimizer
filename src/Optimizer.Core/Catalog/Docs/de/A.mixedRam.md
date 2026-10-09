# Speichermodule aus verschiedenen Kits

## Zusammenfassung
::: status Info
Die Speichermodule haben unterschiedliche Teilenummern oder Größen und sind damit kein zusammengehöriges Kit. Gemischte Module laufen womöglich nicht stabil mit ihrem XMP- oder EXPO-Takt.
:::
::: status Ok,Unknown,Unsupported,Problem
Prüft, ob alle Speichermodule dasselbe Modell und dieselbe Größe haben, wie in einem zusammengehörigen Kit.
:::

## Warum das wichtig ist
Speicherkits sind nur mit den Modulen aus genau diesem Kit für ihren Nenntakt geprüft. Wer Kits kombiniert, auch mit gleichem Takt, riskiert Instabilität, ein XMP- oder EXPO-Profil, das sich nicht einschalten lässt, oder einen PC, der nicht startet [1]. Mainboard-Hersteller empfehlen für zwei oder vier Module ein zusammengehöriges Set [2]. Instabiler Speicher zeigt sich als Abstürze und Fehler; Speicher, der unter seinem Profil laufen muss, kostet FPS.

## Wie wir es erkennen
Wir lesen Teilenummer und Größe jedes Moduls aus Windows (`Win32_PhysicalMemory`). Unterschiedliche Teilenummern oder Größen bedeuten verschiedene Kits. Zwei Kits desselben Modells sehen hier gleich aus, diese Prüfung erkennt sie also nicht.

## So behebst du es
1. Läuft der PC stabil und der Speicher mit seinem Nenntakt (siehe die Prüfung zum Speichertakt), musst du nichts ändern.
2. Stürzt der PC ab oder hält das XMP- oder EXPO-Profil nicht, teste zuerst ohne Profil und erhöhe den Takt dann schrittweise wieder.
3. Dauerhaft hilft ein zusammengehöriges Kit in der Gesamtgröße, die du brauchst, am besten aus der Speicherliste des Mainboards (QVL) [2].

## So prüfst du die Behebung
Starte den Scan erneut. Alle Module sollten dieselbe Teilenummer und Größe zeigen.

## Quellen
1. https://www.corsair.com/us/en/explorer/diy-builder/memory/can-i-mix-corsair-memory-kits/
2. https://www.asus.com/support/faq/1042256/
