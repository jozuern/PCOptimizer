# Kein Bildschirm "Einrichtung abschließen"

## Zusammenfassung
Schaltet "Vorschläge, wie ich Windows optimal nutzen und die Einrichtung dieses Geräts abschließen kann" ab, den Einrichtungsbildschirm bei der Anmeldung.

## So funktioniert es
Der Bildschirm "Lass uns die Einrichtung deines Geräts abschließen" kann bei der Anmeldung erscheinen und Vorschläge machen, wie du Windows optimal nutzt [1]. Er gehört zur Option "Vorschläge, wie ich Windows optimal nutzen und die Einrichtung dieses Geräts abschließen kann" unter Einstellungen > System > Benachrichtigungen > Weitere Einstellungen [1]. Die App setzt ScoobeSystemSettingEnabled auf 0. Microsoft dokumentiert den Registry-Wert hinter dem Schalter nicht; die Anleitung [1] zeigt den Wert, den der Schalter schreibt. Ein Test unter echtem Windows 11 26H2 hat die Wirkung bestätigt, und Rückgängig machen hat sie wieder entfernt.

## Warum es helfen kann
Kein Einrichtungsbildschirm zwischen Anmeldung und Desktop.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Keine; diese Dienste kannst du jederzeit in den Einstellungen einrichten.

## Wann du es nicht nutzen solltest
Wenn Windows dich an Einrichtungsschritte erinnern soll.

## Quellen
1. https://www.elevenforum.com/t/enable-or-disable-lets-finish-setting-up-your-device-in-windows-11.5205/
