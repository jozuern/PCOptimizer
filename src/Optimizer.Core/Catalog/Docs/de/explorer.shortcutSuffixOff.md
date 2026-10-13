# Kein „- Verknüpfung“ in neuen Verknüpfungen

## Zusammenfassung
Neue Verknüpfungen heißen „Spiel.lnk“ statt „Spiel - Verknüpfung.lnk“. Der Wert ist nicht dokumentiert; ein VM-Test hat ihn bestätigt.

## So funktioniert es
Beim Erstellen einer Verknüpfung benennt Windows sie nach einer Vorlage, standardmäßig „%s - Verknüpfung.lnk“, wobei %s der Name des Ziels ist [1]. Der Wert ShortcutNameTemplate unter HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates ersetzt die Vorlage; die App setzt ihn auf „%s.lnk“ [1]. Microsoft hat diesen Wert nicht dokumentiert. Ein Test in einer virtuellen Maschine mit Windows 11 hat ihn bestätigt: „Verknüpfung erstellen“ der Shell ergab mit dem Wert „Doc.lnk“ und ohne ihn „Doc - Shortcut.lnk“. Er gilt für Verknüpfungen, die du danach erstellst, ohne Abmelden [1].

## Warum es helfen kann
Verknüpfungen auf dem Desktop oder in einem Ordner bekommen einen sauberen Namen, ohne dass du jede umbenennen musst.

## Belege
Keine Auswirkung auf die Leistung; nur der Name neuer Verknüpfungen ändert sich. Vorhandene Verknüpfungen behalten ihren Namen.

## Nachteile & Risiken
Eine neue Verknüpfung ist am Namen nicht mehr als solche zu erkennen (der Pfeil am Symbol zeigt es weiterhin). Manche berichten, dass ein Update den Wert ignorierte [1]; Rückgängig entfernt ihn in jedem Fall.

## Wann du es nicht nutzen solltest
Wenn du „Verknüpfung“ im Namen sehen willst.

## Quellen
1. https://winaero.com/disable-the-shortcut-text-addition-for-shortcuts-in-windows-11
