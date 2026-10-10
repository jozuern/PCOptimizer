# Keine Tastenkürzel für Einrastfunktion, Anschlagverzögerung und Statustasten

## Zusammenfassung
Fünfmal Umschalt, rechte Umschalttaste halten oder Num-Taste halten schaltet Einrastfunktion, Anschlagverzögerung oder Statustasten-Töne nicht mehr ein, und kein Dialog reißt dich aus dem Spiel.

## So funktioniert es
Jede dieser Tastatur-Hilfen hat ein Kennzeichen für ihr Tastenkürzel [1][2][3]: Die Einrastfunktion startet nach fünfmal Umschalt, die Anschlagverzögerung nach acht Sekunden rechter Umschalttaste, die Statustasten-Töne nach acht Sekunden Num-Taste. Die App löscht nur diese Kürzel-Kennzeichen über SystemParametersInfo und speichert sie in deinem Benutzerprofil [4]; ob eine Funktion selbst an ist, bleibt wie es war. Die App ändert das nur, wenn sie unter deinem eigenen Konto läuft, dem üblichen Fall mit der UAC-Abfrage.

## Warum es helfen kann
In Spielen, die viel Umschalt nutzen, öffnen fünf schnelle Anschläge den Dialog der Einrastfunktion und können dich aus dem Spiel holen.

## Belege
Von Microsoft beschriebene Windows-Schnittstellen [1][2][3][4]. Es verhindert eine Unterbrechung, Bildrate und Latenz ändern sich nicht.

## Nachteile & Risiken
Wer diese Funktionen braucht, schaltet sie unter Einstellungen > Barrierefreiheit > Tastatur ein statt per Tastenkürzel.

## Wann du es nicht nutzen solltest
Wenn du oder jemand an diesem PC diese Funktionen per Tastenkürzel einschaltet.

## Quellen
1. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-stickykeys
2. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-filterkeys
3. https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-togglekeys
4. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow
