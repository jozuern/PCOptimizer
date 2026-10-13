// Deutsche Texte. Gleiche Stimme wie die App: du, einfache Wörter, echte Beispiele, keine Gedankenstriche; starke Aussagen nur, wo sie stimmen (DESIGN.md).
import type { Copy } from './types';

const de: Copy = {
  lang: 'de',
  meta: {
    title: 'Tunero: PC-Optimierer für Windows 11',
    description:
      'PC-Optimierer für Windows 11, gebaut, damit du nur noch einen brauchst. Tunero findet, was deine Spiele FPS kostet, erklärt jede Änderung mit Quellen und kann alles rückgängig machen, was es ändert. Kostenlos und quelloffen.',
  },
  nav: { download: 'Download', source: 'Quellcode', other: 'English', otherHref: '/', skip: 'Zum Inhalt' },
  hero: {
    title: 'Gebaut, damit du nur noch einen PC-Optimierer brauchst.',
    lead: 'Tunero sucht auf deinem PC, was deine Spiele FPS kostet, und behebt, was es kann. Es ist gebaut, um die Tweak-Tools, Debloat-Skripte, Autostart-Manager und Aufräumprogramme zu ersetzen, die du sonst zusammensuchst. Jede Änderung wird mit Quellen erklärt, vorher gesichert und lässt sich rückgängig machen.',
    download: 'Tunero.exe herunterladen',
    winget: 'oder mit winget',
    copy: 'Kopieren',
    copied: 'Kopiert',
    facts: (v) => `Version ${v} für Windows 11 24H2 oder neuer, x64. Auf Deutsch und Englisch. Freie Software unter der GPL-3.0.`,
    changes: 'Was neu ist',
    preview: 'Die exe ist noch nicht signiert, deshalb kann Windows SmartScreen beim ersten Start warnen.',
    previewLink: 'Warum',
    shotCaption: 'Die Übersicht nach einem Scan, auf einem Test-PC in einer virtuellen Maschine.',
    shotAlt:
      'Die Übersicht von Tunero: die Eignung, was der Scan gefunden hat, und die empfohlenen Änderungen als Checkliste mit „Empfohlene anwenden“.',
  },
  impact: {
    title: 'Eine 5 gibt es nur für Probleme, die der Scan findet. Für keinen Tweak.',
    body: (n, matter) => [
      'Prüfungen und Tweaks teilen sich eine Skala für Spiele, von 0 bis 5. Den größten Unterschied machen Probleme, die der Scan findet, etwa ein Monitor unter seiner Bildwiederholrate oder eine CPU, die beim Basistakt festhängt.',
      `Von den ${n} Tweaks können ${matter} für Spiele zählen, manche nur auf manchen PCs: „CPU-Turbo wiederherstellen“ hat eine 4, wo der Energiesparplan die CPU beim Basistakt hält, und überall sonst eine 0. Die App bewertet jeden Tweak für den PC, auf dem sie läuft.`,
    ],
    top: 'Probleme, die der Scan findet. Er behebt sie oder sagt dir, wie.',
    topRow: (n) => `Wirkung 5: ${n} Probleme, die der Scan findet`,
    measured: 'gemessen',
    disputed: 'umstritten',
    some: (base) => `auf manchen PCs, sonst ${base}`,
    zeroNamed: 'Beliebte Gaming-Tweaks mit umstrittenen Belegen. Angeboten, mit 0 bewertet, und ihre Seiten erklären den Streit.',
    zeroRest: (n) => `Dazu ${n} weitere, die meisten für Datenschutz, den Browser und Windows. Sie versprechen keine FPS.`,
    row: (level, count) => `Wirkung ${level}: ${count} ${count === 1 ? 'Tweak' : 'Tweaks'}`,
  },
  scan: {
    title: 'Der Scan sucht, was dich wirklich FPS kostet',
    body: (n) => [
      `${n} Prüfungen, die nur lesen, suchen nach Problemen, die FPS oder flüssiges Spielen kosten. Kann eine Prüfung einen Wert nicht lesen, sagt sie „unbekannt“ und gibt keinen Rat.`,
      'Ein Profil legt fest, was empfohlen wird und welche Wirkung zählt: Gaming, Laptop-Gaming, Akku, Büro, Leise und kühl oder Älterer PC. Von sich aus ändert es nichts.',
    ],
    list: (n) => `Die ${n} Prüfungen`,
  },
  change: {
    title: 'Du siehst jeden Wert, bevor er sich ändert, und kannst ihn zurückholen',
    intro:
      'Die Bestätigung listet jeden Registry-Wert, Dienst, jede Aufgabe und Einstellung mit dem aktuellen und dem neuen Wert. So sieht das aus, wenn du die Mausbeschleunigung ausschaltest:',
    columns: ['Wert', 'Jetzt', 'Neu'],
    caption: '„Jetzt“ zeigt die Standardwerte von Windows.',
    expert:
      'Änderungen, die Sicherheitsfunktionen ausschalten oder die Startkonfiguration ändern, bleiben im Expertenmodus, und keine davon wird je automatisch empfohlen.',
    stepsTitle: 'Was beim Anwenden passiert',
    steps: [
      { name: 'Wiederherstellungspunkt', text: 'Vor der ersten Änderung, wenn der Computerschutz eingeschaltet ist.' },
      { name: 'Sicherung', text: 'Jeder Wert wird gesichert, bevor er sich ändert.' },
      { name: 'Prüfung', text: 'Nach dem Anwenden wird jede Änderung geprüft.' },
      { name: 'Rückgängig', text: 'Die Seite Änderungen listet jede Änderung mit „Rückgängig“, auch nach einem Neustart oder einem Update der App.' },
    ],
    body: [
      'Nach einem Windows-Update zeigt sie, welche Änderungen Windows zurückgesetzt hat, und bietet an, sie erneut anzuwenden. Entfernte Apps und gelöschte Dateien sind die Ausnahme, und das sagt die App, bevor sie etwas entfernt.',
    ],
  },
  sources: {
    title: 'Jede Aussage hat eine Quelle',
    intro:
      'Jede Prüfung und jeder Tweak hat eine Seite auf Deutsch und Englisch: was er tut, warum er helfen kann, wie belastbar die Belege sind, die Nachteile und die Quellen. Das ist die Seite eines beliebten Tweaks, den die Belege nicht stützen:',
    docLabel: 'Erklärseite in der App',
    sourcesLabel: 'Quellen',
    show: ['Zusammenfassung', 'Belege', 'Nachteile & Risiken'],
    after:
      'Ein Tweak, der auf echter Hardware noch nicht erprobt ist, trägt das Abzeichen „Noch nicht auf einem echten PC getestet“ und ist nie Teil von „Empfohlene anwenden“.',
    shotAlt:
      'Die Seite Tweaks von Tunero: Gruppen von Tweaks mit Schaltern und die Erklärung des gewählten Tweaks im Detailbereich.',
  },
  left: {
    title: 'Was es weglässt, und warum',
    intro: 'Manche beliebten Tweaks gibt es gar nicht, weil sie schaden oder nichts ändern. Suchst du in der App danach, sagt sie dir, warum.',
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
  areas: {
    title: 'Gebaut, um einen Ordner voller Tools zu ersetzen',
    intro:
      'Tunero vereint, was man sonst aus Tweak-Tools, Debloat-Skripten, Autostart-Managern, Aufräumprogrammen und Systemprogrammen zusammensucht, und lässt weg, was nichts bringt. Das sind seine wichtigsten Seiten, gruppiert wie in der App.',
    groups: (c) => [
      {
        name: '',
        items: [['Übersicht', `${c.checks} Prüfungen, die finden, was FPS kostet, mit Behebung oder den Schritten für BIOS und Treiber.`]],
      },
      {
        name: 'Optimieren',
        items: [
          ['Tweaks', `${c.tweaks} dokumentierte Einstellungen für Spiele, Datenschutz, den Browser, Windows und Windows Update, jede mit ihren Belegen.`],
          ['Autostart', 'Alles, was mit Windows startet, mit Signaturprüfung und Schnappschüssen zum Vergleichen.'],
          ['Dienste & Aufgaben', 'Starttypen, mit dem, was jeder Dienst tut.'],
        ],
      },
      {
        name: 'Aufräumen',
        items: [
          ['Apps entfernen', `${c.apps} geprüfte vorinstallierte Apps und Hersteller-Apps, dazu Programme mit eigenem Deinstallationsprogramm.`],
          ['Bereinigen', `${c.cleanup} Kategorien temporärer Dateien, Caches und Reste, ohne Verknüpfungen zu folgen.`],
        ],
      },
      {
        name: 'Pflegen und prüfen',
        items: [
          ['Apps & Treiber', 'Apps mit winget installieren und aktualisieren, und sehen, wie alt die Treiber sind.'],
          ['Tools', 'Speicheranalyse, Windows-Features, Netzwerkreparaturen, Windows-Update-Reparatur, SFC und DISM.'],
          ['Zustand', 'Ein Frametime-Benchmark mit PresentMon, eine Drosselungsprüfung und der Zustand der Laufwerke.'],
        ],
      },
      {
        name: '',
        items: [['Änderungen', 'Jede Änderung mit „Rückgängig“, dazu ein Export, um deine Änderungen auf einen anderen PC oder eine Neuinstallation zu übertragen.']],
      },
    ],
  },
  privacy: {
    title: 'Ins Internet geht es nur, wenn du es willst',
    body: [
      'Keine Telemetrie, kein Konto, keine Werbung und nichts, was im Hintergrund läuft: Es läuft nur, wenn du es startest, als eine exe ohne Installation.',
      'Die Update-Prüfung ist aus, bis du sie einschaltest. Eine VirusTotal-Abfrage braucht deinen eigenen Schlüssel und sendet Hashes von Dateien, nie die Dateien. Der DNS-Benchmark und App-Installationen laufen, wenn du sie startest.',
    ],
    link: 'Alles, was die App sendet, und an wen',
  },
  install: {
    title: 'Hol dir Tunero',
    steps: [
      { name: 'Herunterladen', text: 'Die exe der neuesten Version.' },
      { name: 'Prüfen', text: 'Vergleiche ihren SHA-256 mit der .sha256-Datei auf der Release-Seite.' },
      { name: 'Starten', text: 'Sie braucht keine Installation. Beim ersten Start bietet sie einen Eintrag im Startmenü an.' },
    ],
    managers: 'Mit einem Paketmanager',
    releases: 'Alle Versionen',
  },
  faq: {
    title: 'Fragen',
    items: [
      {
        id: 'schneller',
        q: 'Laufen meine Spiele damit schneller?',
        a: 'Wenn der Scan ein echtes Problem findet, oft deutlich: Ein Monitor auf 60 Hz, Arbeitsspeicher ohne sein XMP- oder EXPO-Profil oder ein Prozessor, der auf seinem Basistakt festhängt, kostet viel FPS, und Tunero behebt das oder sagt dir, wie. Die meisten Tweaks betreffen Datenschutz, den Browser und Windows und sind für Spiele mit 0 bewertet; die, die etwas bringen können, werden für deinen PC bewertet, und jede Seite sagt, wie belastbar die Belege sind. Auf der Seite Zustand gibt es einen Frametime-Benchmark, damit du den Unterschied in deinen eigenen Spielen messen kannst.',
      },
      {
        id: 'smartscreen',
        q: 'Warum warnt Windows beim Start?',
        a: 'Die exe ist noch nicht signiert, deshalb kennt SmartScreen sie nicht. Klicke auf „Weitere Informationen“ und dann auf „Trotzdem ausführen“. Die SignPath Foundation, die Open-Source-Projekte kostenlos signiert, signiert nur Projekte, die schon bekannt sind; Tunero bewirbt sich wieder, sobald es so weit ist. Bis dahin vergleiche den SHA-256 des Downloads mit der .sha256-Datei auf der Release-Seite.',
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
