# Arbeitsspeicher unter Nenngeschwindigkeit (XMP / EXPO)

## Zusammenfassung
::: status Problem
Dein Arbeitsspeicher ist für {{rated}} MT/s ausgelegt, läuft aber mit {{configured}} MT/s. Mit {{profileName}} im BIOS läuft er meist mit seiner Nenngeschwindigkeit.
:::
::: status Ok
Dein Arbeitsspeicher läuft mit seiner Nenngeschwindigkeit von {{rated}} MT/s. {{profileName}} (oder eine gleichwertige Einstellung) ist aktiv.
:::
::: status Info
::: variant fourDimms
Vier DDR5-Module laufen mit {{configured}} MT/s, unter den {{rated}} MT/s des Kits. AMD gibt für vier Module bei Ryzen DDR5-3600 an, das ist also zu erwarten.
:::
::: variant laptop
Dein Arbeitsspeicher ist für {{rated}} MT/s ausgelegt, läuft aber mit {{configured}} MT/s. Die meisten Laptops legen den Speichertakt fest, eine Einstellung dafür fehlt dann.
:::
::: variant default
Prüft, ob der Arbeitsspeicher mit der Geschwindigkeit aus seiner Teilenummer läuft. Dafür ist meist XMP oder EXPO im BIOS nötig.
:::
:::
::: status Unknown,Unsupported
Prüft, ob der Arbeitsspeicher mit der Geschwindigkeit aus seiner Teilenummer läuft. Dafür ist meist XMP oder EXPO im BIOS nötig.
:::

## Warum das wichtig ist
Speichermodule starten zuerst mit einer sicheren Standardgeschwindigkeit (JEDEC) [1]. Die höhere Geschwindigkeit von der Verpackung ist als Profil gespeichert (Intel XMP, AMD EXPO) und muss im BIOS eingeschaltet werden [1][2]. ASUS-Mainboards zeigen **DOCP**, wenn das Kit das Profil des jeweils anderen Herstellers trägt, zum Beispiel ein XMP-Kit auf einem AMD-Mainboard [3]. Intel und AMD bezeichnen diese Profile als Speicherübertaktung, und laut Intel kann eine Änderung von Takt oder Spannung die Garantie berühren und die Stabilität verringern [1][2]. Ohne Profil läuft der Speicher langsamer als angegeben, und Spiele, die von Speicherbandbreite und -latenz abhängen, können FPS verlieren; wie viel, hängt vom Spiel und von der Plattform ab.
::: variant laptop
Die meisten Laptops bieten kein XMP an. Ihre Speichergeschwindigkeit legt der Hersteller fest.
:::
::: variant fourDimms
Mit vier DDR5-Modulen gibt AMD für Ryzen 7000 und 9000 DDR5-3600 an [4][5]. Eine niedrigere Geschwindigkeit als auf dem Kit ist also zu erwarten und keine vergessene Einstellung.
:::

## Wie wir es erkennen
Wir lesen jedes Speichermodul aus Windows (WMI): die Teilenummer und die Geschwindigkeit, mit der es gerade läuft (`ConfiguredClockSpeed`). Die Nenngeschwindigkeit ermitteln wir aus der Teilenummer, mit den Katalogtabellen für Corsair, G.Skill, Kingston, Crucial, TeamGroup und Patriot. `Win32_PhysicalMemory.Speed` nutzen wir dafür nicht, weil es meist nur die Standardgeschwindigkeit meldet. Unbekannte Teilenummern ergeben „Unbekannt“ und keinen Rat. Laufen vier DDR5-Module an einem Ryzen unter der Kit-Angabe, gibt es wegen AMDs Spezifikation eine Information statt eines Problems.

## So behebst du es
::: variant fourDimms
Nichts zu tun. Für die volle Geschwindigkeit des Kits bräuchtest du zwei statt vier Module.
:::
::: variant default,laptop
1. **BitLocker:** Prüfe, bevor du BIOS-Einstellungen änderst, ob BitLocker oder die Geräteverschlüsselung an ist. Wenn ja, setze den Schutz vorher aus (Start > **BitLocker verwalten** > **Schutz anhalten**) oder halte den Wiederherstellungsschlüssel bereit. Oft ist er in deinem Microsoft-Konto gespeichert. Nach einem BIOS-Update oder einer Änderung am TPM oder an der Startkonfiguration kann Windows beim nächsten Start danach fragen [7][8].
2. Starte neu ins BIOS: **Einstellungen > System > Wiederherstellung > Erweiterter Start > Jetzt neu starten**, dann **Problembehandlung > Erweiterte Optionen > UEFI-Firmwareeinstellungen** (je nach Version „UEFI-Einstellungen“) [6]. Bei vielen PCs öffnet es sich auch, wenn du beim Start **Entf** oder **F2** drückst.
::: if menuPath
3. Auf deinem {{board}}: **{{menuPath}}**.
::: if menuUnverified
   Dieser Pfad ist noch nicht mit dem Handbuch deines Mainboards abgeglichen. Menünamen unterscheiden sich je nach Board und BIOS-Version. Passt der Pfad nicht, suche die Einstellung über ihren Namen.
:::
:::
::: ifnot menuPath
3. Suche die Einstellung für das Speicherprofil: **XMP**, **EXPO**, **DOCP** oder **A-XMP**, meist auf der Übertaktungs- oder „Tweaker“-Seite.
:::
4. Wähle das erste Profil, speichere und beende (meist **F10**).
5. Der erste Start kann länger dauern, weil das Mainboard den Speicher einmisst. Startet der PC nicht oder stürzt er ab, aktualisiere zuerst das BIOS oder probiere das zweite Profil. Bleibt er instabil, schalte das Profil wieder aus: Ein stabiler PC mit Standardtakt ist besser als ein instabiler schneller.
:::

## So prüfst du die Behebung
Starte den Scan erneut. Die aktuelle Geschwindigkeit sollte der Nenngeschwindigkeit entsprechen. Im Task-Manager unter Leistung > Arbeitsspeicher > **Geschwindigkeit** steht derselbe Wert.

## Quellen
1. https://www.intel.com/content/www/us/en/gaming/extreme-memory-profile-xmp.html
2. https://www.amd.com/de/products/processors/technologies/expo.html
3. https://www.asus.com/de/support/faq/1042256/
4. https://www.amd.com/de/products/processors/desktops/ryzen/7000-series/amd-ryzen-7-7800x3d.html
5. https://www.amd.com/de/products/processors/desktops/ryzen/9000-series/amd-ryzen-7-9800x3d.html
6. https://support.microsoft.com/de-de/windows/experience/enable-virtualization-on-windows
7. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/recovery-overview
8. https://learn.microsoft.com/de-de/windows/security/operating-system-security/data-protection/bitlocker/operations-guide
