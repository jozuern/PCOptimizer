# Microsoft Edge: Seiten dürfen nicht nach gespeicherten Zahlungsmethoden fragen

## Zusammenfassung
Websites können Edge nicht mehr fragen, ob du Zahlungsmethoden gespeichert hast. Gilt nicht in Edge-Profilen mit privatem Microsoft-Konto.

## So funktioniert es
Mit der Richtlinie PaymentMethodQueryEnabled auf aus erfahren Seiten, die PaymentRequest.canMakePayment oder hasEnrolledInstrument nutzen, dass keine Zahlungsmethoden verfügbar sind [1]. Seit Edge 116 wendet Edge diese Richtlinie laut Microsoft in einem Profil, das mit einem privaten Microsoft-Konto angemeldet ist, nicht an [2]: dort bleibt deine eigene Einstellung, auch wenn die App die Änderung als erledigt zeigt.

## Warum es helfen kann
Ein Detail weniger, das Seiten über dich erfahren. Eine Einstellung für Datenschutz oder Bedienung; sie ändert weder Bildrate noch Latenz.

## Belege
Eine von Microsoft beschriebene Edge-Richtlinie [1].

## Nachteile & Risiken
Manche Kassenseiten bieten keine schnelle Zahlung mit einer gespeicherten Karte mehr an. Weil es eine Richtlinie ist, zeigt Edge in Menü und Einstellungen an, dass es von deiner Organisation verwaltet wird, und der passende Schalter in den Edge-Einstellungen ist gesperrt. Rückgängig entfernt sie wieder.

## Wann du es nicht nutzen solltest
Wenn du mit in Edge gespeicherten Karten bezahlst.

## Quellen
1. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/paymentmethodqueryenabled
2. https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies
