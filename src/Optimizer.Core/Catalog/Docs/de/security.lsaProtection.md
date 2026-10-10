# LSA-Schutz an

## Zusammenfassung
Führt die lokale Sicherheitsautorität, die Anmeldegeheimnisse verwaltet, als geschützten Prozess aus, damit andere Programme ihren Speicher nicht lesen können. Wirkt nach einem Neustart.

## So funktioniert es
Mit RunAsPPL = 2 führen Windows 11 22H2 und neuer LSASS als geschützten Prozess ohne UEFI-Variable aus, die Einstellung lässt sich also wieder abschalten [1]. Im geschützten Modus laden nur von Microsoft signierte Erweiterungen in LSA; Smartcard-Treiber und andere LSA-Erweiterungen müssen Microsofts Signaturregeln erfüllen [1]. Ein Neustart ist nötig [1].

## Warum es helfen kann
Werkzeuge, die Kennwörter und Anmeldedaten aus dem LSA-Speicher stehlen, werden blockiert.

## Belege
Von Microsoft beschrieben [1]. Microsoft beschreibt einen Überwachungsmodus, um Erweiterungen zu finden, die nicht mehr laden würden [1]. Bildrate und Latenz ändern sich nicht.

## Nachteile & Risiken
Nicht signierte LSA-Erweiterungen, etwa manche Smartcard-, Kennwortfilter- oder VPN-Komponenten, laden nicht mehr. Rückgängig und ein Neustart schalten den Schutz wieder aus.

## Wann du es nicht nutzen solltest
Wenn du Smartcard-Leser, Kennwortfilter oder andere Sicherheitssoftware nutzt, die in LSA lädt und nicht von Microsoft signiert ist.

## Quellen
1. https://learn.microsoft.com/en-us/windows-server/security/credentials-protection-and-management/configuring-additional-lsa-protection
