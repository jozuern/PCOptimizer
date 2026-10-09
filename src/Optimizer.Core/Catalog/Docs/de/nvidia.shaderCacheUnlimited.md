# NVIDIA-Shader-Cache: unbegrenzt

## Zusammenfassung
Lässt den NVIDIA-Shader-Cache auf der Festplatte ohne Größenlimit des Treibers wachsen, damit kompilierte Shader nicht gelöscht und neu kompiliert werden.

## So funktioniert es
Spiele kompilieren ihre Shader für deine Grafikkarte; der Treiber speichert das Ergebnis auf dem Datenträger und nutzt es beim nächsten Mal wieder. Erreicht der Cache sein Größenlimit, löscht der Treiber ältere Einträge. Diese Einstellung setzt die Cache-Größe im globalen Profil auf unbegrenzt, wie „Shader-Cache-Größe: Unbegrenzt“ in der NVIDIA Systemsteuerung [1].

## Warum es helfen kann
Spielst du viele große Spiele, werden bei vollem Cache Shader neu kompiliert. Das zeigt sich als Ruckeln in den ersten Minuten einer Sitzung oder nach Treiberupdates.

## Belege
Der Nutzen hängt davon ab, wie viele Spiele du spielst und wie groß ihre Shader sind; bei wenigen Spielen reicht das Standardlimit oft aus.

## Nachteile & Risiken
Der Cache belegt mehr Speicherplatz (bei vielen Spielen mehrere GB). Bei Treiberupdates wird er weiterhin geleert, und die Datenträgerbereinigung kann ihn löschen.

## Wann du es nicht nutzen solltest
Auf einem kleinen, fast vollen Systemlaufwerk.

## Quellen
1. https://docs.nvidia.com/gameworks/content/gameworkslibrary/coresdk/nvapi/group__drsapi.html
