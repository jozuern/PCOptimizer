# Für neue Konten übernehmen

## Zusammenfassung
Schreibt die Tweaks für dein Konto, die du eingeschaltet hast, ins Standardprofil, damit später angelegte Konten mit ihnen starten. Bestehende Konten behalten ihre Einstellungen.

## So funktioniert es
Windows nutzt das Standardprofil als Vorlage für jedes neue Konto [1]. Tunero lädt seinen Registrierungs-Hive (C:\Users\Default\NTUSER.DAT) unter HKEY_USERS, wie es Microsofts eigene Schritte mit Struktur laden tun [1][3], schreibt die Werte deiner Tweaks dorthin und entlädt ihn wieder. Nur Tweaks, die ganz aus Registrierungswerten deines Kontos bestehen, kommen mit: Ein Tweak, der zusätzlich Windows aufruft oder den ganzen PC ändert, käme nur halb an. Die ursprünglichen Werte werden gesichert, und Rückgängig auf der Seite Änderungen stellt sie wieder her.

## Warum es helfen kann
Nach einer Neuinstallation oder auf einem Familien-PC bekommt ein neues Konto dieselben Einstellungen für Explorer, Taskleiste und Datenschutz, ohne dass du jeden Tweak in diesem Konto erneut einschalten musst.

## Belege
Einen Geschwindigkeitseffekt gibt es nicht: Es spart Einrichtungsarbeit. Ein VM-Test zeigte ein neues lokales Konto, das mit den übernommenen Werten startete.

## Nachteile & Risiken
Microsoft nennt Sysprep mit CopyProfile als unterstützten Weg, das Standardprofil für Images vorzubereiten [2]; einzelne Werte in den Hive zu schreiben ist der Schritt, den seine CopyProfile-Seite von Hand zeigt [1]. Manche Einstellungen setzt Windows bei der ersten Anmeldung eines neuen Kontos zurück, und Einstellungen mit einem Hash pro Benutzer (etwa Standard-Apps) behandelt es als ungültig und setzt sie zurück [1]. Machst du einen Tweak in deinem Konto später rückgängig, ändert das das Standardprofil nicht: Mache dafür diese Änderung rückgängig.

## Wann du es nicht nutzen solltest
Auf einem PC, an dem nur du dich anmeldest, oder wenn neue Konten mit den Windows-Standards starten sollen. In einer Organisation wird das Standardprofil meist über das Image oder Gruppenrichtlinien verwaltet.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/customize-the-default-user-profile-by-using-copyprofile
2. https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/customize-default-local-user-profile
3. https://learn.microsoft.com/en-us/windows/win32/api/winreg/nf-winreg-regloadkeyw
