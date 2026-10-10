# Druck-Taste kopiert wieder den Bildschirm

## Zusammenfassung
Die Druck-Taste kopiert den ganzen Bildschirm in die Zwischenablage, statt das Snipping Tool zu öffnen, wie vor Windows 11 Build 22621.1928.

## So funktioniert es
Seit Windows 11 Build 22621.1928 öffnet die Druck-Taste standardmäßig das Snipping Tool [1]. Ist der Schalter "Druck-Taste zum Öffnen der Bildschirmaufnahme verwenden" aus, kopiert die Taste den Bildschirm in die Zwischenablage [1]. Die App setzt PrintScreenKeyForSnippingEnabled auf 0. Je nach anderen Apps braucht die Änderung eine Abmeldung oder einen Neustart [1]. Microsoft dokumentiert den Registry-Wert hinter dem Schalter nicht; die Anleitung [1] zeigt den Wert, den der Schalter schreibt. Bis ein Test unter echtem Windows die Wirkung bestätigt, ist die Option eine Vorschau.

## Warum es helfen kann
Ein Druck kopiert den Bildschirm ohne Auswahlschritt, etwa um ihn direkt in einen Chat einzufügen.

## Belege
Eine persönliche Vorliebe ohne Einfluss auf Bildrate oder Latenz.

## Nachteile & Risiken
Einen Bildschirmausschnitt nimmst du mit Windows + Umschalt + S auf.

## Wann du es nicht nutzen solltest
Wenn du mit der Druck-Taste einen Bereich auswählst.

## Quellen
1. https://www.elevenforum.com/t/enable-or-disable-use-print-screen-key-to-open-screen-snipping-in-windows-11.520/
