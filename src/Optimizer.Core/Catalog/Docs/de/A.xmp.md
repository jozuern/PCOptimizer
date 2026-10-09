# Arbeitsspeicher unter Nenngeschwindigkeit (XMP / EXPO)

## Zusammenfassung
::: status Problem
Dein Arbeitsspeicher ist für {{rated}} MT/s ausgelegt, läuft aber mit {{configured}} MT/s. Schalte {{profileName}} im BIOS ein, dann läuft er mit voller Geschwindigkeit.
:::
::: status Ok
Dein Arbeitsspeicher läuft mit seiner Nenngeschwindigkeit von {{rated}} MT/s. {{profileName}} (oder eine gleichwertige Einstellung) ist aktiv.
:::
::: status Unknown,Info,Unsupported
Prüft, ob der Arbeitsspeicher mit der Geschwindigkeit aus seiner Teilenummer läuft. Dafür ist meist XMP oder EXPO im BIOS nötig.
:::

## Warum das wichtig ist
Speichermodule starten mit einer sicheren Standardgeschwindigkeit (JEDEC, zum Beispiel DDR4-2133 oder DDR5-4800). Die höhere Geschwindigkeit von der Verpackung ist als Profil gespeichert (Intel XMP, AMD EXPO, bei ASUS auf AM4 „D.O.C.P“) und muss im BIOS eingeschaltet werden. Sonst läuft der Speicher deutlich langsamer, als du bezahlt hast. Spiele, die von Speicherbandbreite und -latenz abhängen, verlieren FPS und vor allem 1-%-Lows. Bei Ryzen-Prozessoren hängt außerdem der interne Fabric-Takt am Speichertakt.
::: variant laptop
Die meisten Laptops bieten kein XMP an. Ihre Speichergeschwindigkeit legt der Hersteller fest.
:::

## Wie wir es erkennen
Wir lesen jedes Speichermodul aus Windows (WMI): die Teilenummer und die Geschwindigkeit, mit der es gerade läuft (`ConfiguredClockSpeed`). Die Nenngeschwindigkeit ermitteln wir aus der Teilenummer, mit den Katalogtabellen für Corsair, G.Skill, Kingston, Crucial, TeamGroup und Patriot. `Win32_PhysicalMemory.Speed` nutzen wir dafür nicht, weil es meist nur die Standardgeschwindigkeit meldet. Unbekannte Teilenummern ergeben „Unbekannt“ und keinen Rat.

## So behebst du es
1. Starte neu ins BIOS (beim Start **Entf** oder **F2** drücken).
::: if menuPath
2. Auf deinem {{board}}: **{{menuPath}}**.
::: if menuUnverified
   Dieser Pfad ist noch nicht mit dem Handbuch deines Mainboards abgeglichen. Menünamen unterscheiden sich je nach Board und BIOS-Version. Passt der Pfad nicht, suche die Einstellung über ihren Namen.
:::
:::
::: ifnot menuPath
2. Suche die Einstellung für das Speicherprofil: **XMP**, **EXPO**, **D.O.C.P** oder **A-XMP**, meist auf der Übertaktungs- oder „Tweaker“-Seite.
:::
3. Wähle das erste Profil, speichere und beende (meist **F10**).
4. Der erste Start kann länger dauern, weil das Mainboard den Speicher einmisst. Startet der PC nicht oder stürzt er ab, aktualisiere zuerst das BIOS oder probiere das zweite Profil.

## So prüfst du die Behebung
Starte den Scan erneut. Die aktuelle Geschwindigkeit sollte der Nenngeschwindigkeit entsprechen. Im Task-Manager unter Leistung > Arbeitsspeicher > **Geschwindigkeit** steht derselbe Wert.

## Quellen
1. https://www.intel.com/content/www/us/en/gaming/extreme-memory-profile-xmp.html
2. https://www.amd.com/en/products/processors/technologies/expo.html
