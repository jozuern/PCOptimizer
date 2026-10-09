# Ethernet-Geschwindigkeit auf automatische Aushandlung stellen

## Zusammenfassung
Stellt Geschwindigkeit und Duplex des Adapters zurück auf automatische Aushandlung, damit er mit der höchsten gemeinsamen Geschwindigkeit verbindet. Hilft bei Downloads, nicht beim Ping.

## So funktioniert es
Die App schreibt das Standard-Treiberschlüsselwort für Geschwindigkeit und Duplex (Wert 0 = automatische Aushandlung) [1] in die Einstellungen des Adapters und startet ihn neu, wie beim Ändern im Geräte-Manager. Die Verbindung bricht für einige Sekunden ab. Rückgängig machen stellt den vorherigen Wert wieder her.

## Warum es helfen kann
Eine feste Geschwindigkeit unter dem Maximum des Adapters begrenzt Downloads und Updates, sobald dein Internetanschluss oder dein lokales Netz schneller ist. Mit automatischer Aushandlung einigen sich Adapter und Router oder Switch auf die schnellste Geschwindigkeit, die beide können [1].

## Belege
Automatische Aushandlung ist der Treiberstandard; eine feste, niedrigere Geschwindigkeit begrenzt den Durchsatz absichtlich.

## Nachteile & Risiken
Selten handelt ein alter Switch schlecht aus, und die feste Geschwindigkeit war eine Umgehung dafür. Rückgängig machen stellt sie wieder her.

## Wann du es nicht nutzen solltest
Wenn jemand die Geschwindigkeit wegen eines fehlerhaften Switches bewusst festgelegt hat.

## Quellen
1. https://learn.microsoft.com/en-us/windows-hardware/drivers/network/enumeration-keywords
