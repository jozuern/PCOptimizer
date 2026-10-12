// Deutsche Texte. Gleiche Stimme wie die App: du, einfache Wörter, echte Beispiele, keine Gedankenstriche (DESIGN.md).
import type { Copy } from './types';

const de: Copy = {
  lang: 'de',
  meta: {
    title: 'Tunero: PC-Optimierer für Windows 11',
    description:
      'PC-Optimierer für Windows 11. Tunero prüft deinen PC, erklärt jeden Fund und jede Änderung mit Quellen und kann alles rückgängig machen, was es ändert. Kostenlos und quelloffen.',
  },
  nav: { download: 'Download', source: 'Quellcode', other: 'English', otherHref: '/', skip: 'Zum Inhalt' },
  hero: {
    title: 'Ein PC-Optimierer für Windows 11, der seine Arbeit zeigt.',
    lead: 'Tunero prüft deinen PC, erklärt seine Funde mit Quellen und ändert nur, was du bestätigst. Jede Änderung wird vorher gesichert und lässt sich rückgängig machen.',
    download: 'Tunero.exe herunterladen',
    winget: 'oder mit winget',
    copy: 'Kopieren',
    copied: 'Kopiert',
    spec: {
      version: 'Version',
      changes: 'Was neu ist',
      size: 'Download',
      needs: 'Läuft auf',
      needsValue: 'Windows 11 24H2 oder neuer, x64',
      languages: 'Sprachen',
      languagesValue: 'Deutsch, Englisch',
      license: 'Lizenz',
      licenseValue: 'Freie Software, GPL-3.0',
    },
    shotCaption: 'Die Übersicht nach einem Scan, hier auf einem Test-PC in einer virtuellen Maschine.',
    preview: 'Die exe ist noch nicht signiert, deshalb kann Windows SmartScreen beim ersten Start warnen.',
    previewLink: 'Warum',
    shotAlt:
      'Die Übersicht von Tunero: die Eignung, was der Scan gefunden hat, und die empfohlenen Änderungen als Checkliste mit „Empfohlene anwenden“.',
  },
  scan: {
    label: 'Scan',
    title: 'Erst schaut es sich diesen PC an',
    body: (n) => [
      `${n} Prüfungen, die nur lesen, suchen nach Problemen, die FPS oder flüssiges Spielen kosten: ein Monitor mit 60 Hz, Arbeitsspeicher ohne XMP- oder EXPO-Profil, der Energiesparmodus auf einem Desktop, eine Spielebibliothek auf einer Festplatte. Kann eine Prüfung einen Wert nicht lesen, sagt sie „unbekannt“ und gibt keinen Rat.`,
      'Ein Profil legt fest, was empfohlen wird und welche Wirkung zählt: Gaming, Laptop-Gaming, Akku, Büro, Leise und kühl oder Älterer PC. Von sich aus ändert es nichts.',
    ],
    list: (n) => `Alle ${n} Prüfungen`,
  },
  confirm: {
    label: 'Bestätigen',
    title: 'Du siehst jeden Wert, bevor er sich ändert',
    intro:
      'Die Bestätigung listet jeden Registry-Wert, Dienst, jede Aufgabe und Einstellung mit dem aktuellen und dem neuen Wert. So sieht das aus, wenn du die Mausbeschleunigung ausschaltest:',
    columns: ['Schlüssel', 'Wert', 'Jetzt', 'Neu'],
    caption: 'Die Spalte „Jetzt“ zeigt die Standardwerte von Windows.',
    after:
      'Änderungen, die Sicherheitsfunktionen ausschalten oder die Startkonfiguration ändern, bleiben im Expertenmodus, und keine davon wird je automatisch empfohlen.',
  },
  undo: {
    label: 'Rückgängig',
    title: 'Alles, was es ändert, lässt sich rückgängig machen',
    steps: [
      { name: 'Wiederherstellungspunkt', text: 'Vor der ersten Änderung, wenn der Computerschutz eingeschaltet ist.' },
      { name: 'Sicherung', text: 'Jeder Wert wird gesichert, bevor er sich ändert.' },
      { name: 'Prüfung', text: 'Nach dem Anwenden wird jede Änderung geprüft.' },
      { name: 'Rückgängig', text: 'Die Seite Änderungen listet jede Änderung mit „Rückgängig“, auch nach einem Neustart oder einem Update der App.' },
    ],
    body: [
      'Nach einem Windows-Update zeigt sie, welche Änderungen Windows zurückgesetzt hat, und bietet an, sie erneut anzuwenden.',
      'Entfernte Apps und gelöschte Dateien sind die Ausnahme, und das sagt die App, bevor sie etwas entfernt.',
    ],
  },
  impact: {
    label: 'Wirkung',
    title: 'Die meisten Tweaks bringen keine FPS',
    body: (n, zero) => [
      `Jeder Tweak bewertet seine Wirkung auf Spiele von 0 bis 5, und die Bewertung kann vom PC abhängen: „CPU-Turbo wiederherstellen“ hat eine 4, wo der Energiesparplan die CPU beim Basistakt hält, und überall sonst eine 0. ${zero} der ${n} Tweaks haben auf den meisten PCs eine 0. Der größte Teil des Katalogs dreht sich um Datenschutz, den Browser und darum, wie Windows aussieht und sich verhält, und ihre Seiten sagen das.`,
    ],
    legend: { situational: 'Situationsabhängig', disputed: 'Umstritten', higher: 'Auf manchen PCs möglich' },
    row: (level, count) => `Wirkung ${level}: ${count} ${count === 1 ? 'Tweak' : 'Tweaks'}`,
    higher: (title, level, reason) => `${title}: ${level}. ${reason}`,
    caption: 'Jedes Quadrat ist ein Tweak. Fahr mit der Maus darüber, um seinen Namen zu sehen.',
    after: 'Den größten Unterschied macht meist, was der Scan findet, etwa ein Monitor mit 60 Hz oder Arbeitsspeicher unter seinem Nenntakt.',
  },
  sources: {
    label: 'Quellen',
    title: 'Jede Aussage hat eine Quelle',
    intro:
      'Jede Prüfung und jeder Tweak hat eine Seite auf Deutsch und Englisch: was er tut, warum er helfen kann, wie belastbar die Belege sind, die Nachteile und die Quellen. Die Wirkung reicht von 0 bis 5 und nennt ihre Grundlage: gemessen, situationsabhängig oder umstritten. Das ist die Seite eines beliebten Tweaks, den die Belege nicht stützen:',
    docLabel: 'Erklärseite in der App',
    show: ['Zusammenfassung', 'Belege', 'Nachteile & Risiken'],
    after:
      'Ein Tweak, der auf echter Hardware noch nicht erprobt ist, trägt das Abzeichen „Noch nicht auf einem echten PC getestet“ und ist nie Teil von „Empfohlene anwenden“.',
    shotAlt:
      'Die Seite Tweaks von Tunero: Gruppen von Tweaks mit Schaltern und die Erklärung des gewählten Tweaks im Detailbereich.',
  },
  left: {
    label: 'Weggelassen',
    title: 'Was es weglässt, und warum',
    intro: 'Beliebte Tweaks ohne Belege gibt es nicht. Suchst du in der App danach, sagt sie dir, warum.',
    columns: ['Tweak', 'Warum er fehlt'],
    rows: [
      {
        tweak: 'Echtzeitpriorität für Spiele',
        why: 'Microsoft warnt, dass ein Prozess mit Echtzeitpriorität Threads des Betriebssystems verdrängt und die Maus nicht mehr reagieren kann.',
        source: 'https://learn.microsoft.com/de-de/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass',
      },
      {
        tweak: 'Schutz vor Spectre und Meltdown aus',
        why: 'Microsoft hat auf den Prozessoren, die Windows 11 unterstützt, Verluste im einstelligen Prozentbereich gemessen, die die meisten nicht bemerken. Ein echter Sicherheitsverlust für einen Gewinn, den man nicht spürt.',
        source: 'https://www.microsoft.com/en-us/security/blog/2018/01/09/understanding-the-performance-impact-of-spectre-and-meltdown-mitigations-on-windows-systems/',
      },
      {
        tweak: 'Win32PrioritySeparation = 0x26',
        why: 'Das ist genau der Wert, den Windows für „Optimale Leistung anpassen für: Programme“ schon schreibt. Der Tweak ändert unter Windows 11 nichts.',
        source: 'https://learn.microsoft.com/en-us/previous-versions/cc976120(v=technet.10)',
      },
      {
        tweak: 'IPv6 ganz ausschalten',
        why: 'Microsoft rät davon ab, weil einige Windows-Komponenten dann nicht mehr funktionieren können. Die App bietet stattdessen „IPv4 vor IPv6 bevorzugen“.',
        source: 'https://learn.microsoft.com/de-de/troubleshoot/windows-server/networking/configure-ipv6-in-windows',
      },
      {
        tweak: 'Globale TCP-Tweaks mit netsh',
        why: 'Sie ändern jede Verbindung des PCs, und keine Messung zeigt einen Gewinn in Spielen. Der Ping in Onlinespielen hängt vor allem vom Weg zum Server ab.',
      },
      {
        tweak: 'Registry reinigen',
        why: 'Schlüssel, die ein Deinstallationsprogramm zurücklässt, belegen keinen messbaren Platz, und keine Messung zeigt, dass sie Windows bremsen. Ein falscher Treffer macht Programme kaputt, die sie mitnutzen.',
      },
    ],
    sourceLink: 'Quelle',
    all: 'Die ganze Liste, mit Quellen (Englisch)',
  },
  facts: {
    label: 'Fakten',
    title: 'In Zahlen',
    numbers: (c) => [
      ['Prüfungen, die nur lesen', c.checks],
      ['Tweaks', c.tweaks],
      ['Geprüfte entfernbare Apps', c.apps],
      ['Kategorien zum Bereinigen', c.cleanup],
    ],
    rows: [
      ['Hintergrundprozesse', 'Keine. Es läuft nur, wenn du es startest.'],
      ['Telemetrie', 'Keine'],
      ['Installation', 'Nicht nötig. Eine exe.'],
    ],
  },
  privacy: {
    label: 'Datenschutz',
    title: 'Ins Internet geht es nur, wenn du es willst',
    body: [
      'Keine Telemetrie, kein Konto, keine Werbung. Die Update-Prüfung ist aus, bis du sie einschaltest. Eine VirusTotal-Abfrage braucht deinen eigenen Schlüssel und sendet Hashes von Dateien, nie die Dateien. Der DNS-Benchmark und App-Installationen laufen, wenn du sie startest.',
    ],
    link: 'Alles, was die App sendet, und an wen',
  },
  install: {
    label: 'Installieren',
    title: 'So bekommst du es',
    steps: [
      { name: 'Herunterladen', text: 'Die exe der neuesten Version.' },
      { name: 'Prüfen', text: 'Vergleiche ihren SHA-256 mit der .sha256-Datei auf der Release-Seite.' },
      { name: 'Starten', text: 'Sie braucht keine Installation. Beim ersten Start bietet sie einen Eintrag im Startmenü an.' },
    ],
    managers: 'Mit einem Paketmanager',
    releases: 'Alle Versionen',
  },
  faq: {
    label: 'Fragen',
    title: 'Fragen',
    items: [
      {
        id: 'schneller',
        q: 'Laufen meine Spiele damit schneller?',
        a: 'Manchmal. Am meisten bringt es, echte Probleme zu beheben, die der Scan findet, etwa einen Monitor mit 60 Hz oder Arbeitsspeicher ohne XMP. Die meisten Tweaks ändern wenig, und jede Seite sagt, wie belastbar die Belege sind. Auf der Seite Zustand gibt es einen Frametime-Benchmark, damit du deine eigenen Spiele messen kannst.',
      },
      {
        id: 'smartscreen',
        q: 'Warum warnt Windows beim Start?',
        a: 'Die exe ist noch nicht signiert. Die Signatur durch die SignPath Foundation ist beantragt. Bis dahin vergleiche den SHA-256 des Downloads mit der .sha256-Datei auf der Release-Seite.',
      },
      {
        id: 'admin',
        q: 'Warum braucht es Administratorrechte?',
        a: 'Die meisten Einstellungen, die es ändert, gelten für den ganzen PC. Sein Datenordner ist für Administratoren gesperrt.',
      },
      {
        id: 'home',
        q: 'Läuft es unter Windows 11 Home?',
        a: 'Ja. Tweaks, die Gruppenrichtlinien brauchen, gibt es nur unter Pro, Enterprise und Education.',
      },
      {
        id: 'entfernen',
        q: 'Kann ich es vollständig entfernen?',
        a: '„Deinstallieren“ in den Einstellungen bietet an, vorher jede Änderung rückgängig zu machen. Dann entfernt es die exe, seinen Datenordner und seinen Eintrag im Startmenü.',
      },
    ],
  },
  footer: {
    license: (v) => `Tunero ${v}. Freie Software unter der GPL-3.0.`,
    links: { releases: 'Versionen', changelog: 'Änderungen', source: 'Quellcode', privacy: 'Datenschutz', imprint: 'Impressum', kofi: 'Unterstützen auf Ko-fi' },
    privacyHref: '/de/datenschutz/',
    imprintHref: '/de/impressum/',
  },
};

export default de;
