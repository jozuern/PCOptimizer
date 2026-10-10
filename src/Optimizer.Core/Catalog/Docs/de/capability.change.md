# Windows-Funktion hinzufügen oder entfernen

## Zusammenfassung
Fügt diese optionale Windows-Funktion (Feature on Demand) mit DISM hinzu oder entfernt sie, wie unter Einstellungen > System > Optionale Features. Beim Hinzufügen lädt Windows sie über Windows Update.

## So funktioniert es
Optionale Funktionen sind Teile von Windows, die getrennt installiert werden, etwa PowerShell ISE oder der OpenSSH-Client [1]. Die App führt DISM mit /Remove-Capability oder /Add-Capability aus; beim Hinzufügen sucht DISM das Paket über Windows Update [2]. Rückgängig fügt sie wieder hinzu oder entfernt sie wieder.

## Warum es helfen kann
Werkzeuge zu entfernen, die du nicht nutzt, gibt ein paar Megabyte frei und nimmt sie aus Start und der Befehlszeile. Eines wieder hinzuzufügen (etwa WMIC für ein altes Skript) braucht keinen Installer aus anderer Quelle.

## Belege
Von Microsoft beschrieben [1][2]. Optionale Funktionen laufen nicht im Hintergrund, Entfernen ändert also weder Bildrate noch Latenz.

## Nachteile & Risiken
Programme oder Skripte, die ein entferntes Werkzeug aufrufen, funktionieren nicht mehr, bis du es wieder hinzufügst. Zum Hinzufügen braucht Windows Update eine Internetverbindung.

## Wann du es nicht nutzen solltest
Wenn du das Werkzeug nutzt oder ein altes Skript oder Installer es aufruft.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/features-on-demand-non-language-fod
2. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/dism-capabilities-package-servicing-command-line-options
