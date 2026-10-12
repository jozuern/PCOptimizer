// English copy. Same voice as the app: plain words, real examples, no dashes; strong claims only where they are true (DESIGN.md).
import type { Copy } from './types';

const en: Copy = {
  lang: 'en',
  meta: {
    title: 'Tunero: PC optimizer for Windows 11',
    description:
      'PC optimizer for Windows 11, built to be the only one you need. Tunero finds what costs your games frame rate, explains every change with sources and can undo everything it changes. Free and open source.',
  },
  nav: { download: 'Download', source: 'Source code', other: 'Deutsch', otherHref: '/de/', skip: 'Skip to content' },
  hero: {
    title: 'Built to be the only PC optimizer you need.',
    lead: 'Tunero finds what costs your games frame rate, fixes what it can, and puts the useful settings of the popular tweak tools into one app. Every change is explained with sources, backed up first and can be undone.',
    download: 'Download Tunero.exe',
    winget: 'or with winget',
    copy: 'Copy',
    copied: 'Copied',
    spec: {
      version: 'Version',
      changes: 'What changed',
      size: 'Download',
      needs: 'Runs on',
      needsValue: 'Windows 11 24H2 or newer, x64',
      languages: 'Languages',
      languagesValue: 'English, German',
      license: 'License',
      licenseValue: 'Free software, GPL-3.0',
    },
    shotCaption: 'The Overview after a scan, here on a test PC in a virtual machine.',
    preview: 'The exe is not code signed yet, so Windows SmartScreen may warn on the first start.',
    previewLink: 'Why',
    shotAlt:
      'The Overview page of Tunero: the readiness score, what the scan found, and the recommended changes as a checklist with Apply recommended.',
  },
  scan: {
    label: 'Scan',
    title: 'It finds what really costs you frame rate',
    body: (n) => [
      `${n} read-only checks look for problems that cost frame rate or smoothness: a monitor running at 60 Hz, memory without its XMP or EXPO profile, the Power saver plan on a desktop, a game library on a hard disk. A check that cannot read a value says unknown and gives no advice.`,
      'A profile decides what is recommended and which impact counts: Gaming, Laptop gaming, Battery, Office, Quiet and cool, or Older PC. It changes nothing by itself.',
    ],
    list: (n) => `All ${n} checks`,
  },
  confirm: {
    label: 'Confirm',
    title: 'You see every value before it changes',
    intro:
      'The confirmation lists each registry value, service, task or setting with its current and its new value. This is what turning off mouse acceleration writes:',
    columns: ['Key', 'Value', 'Now', 'New'],
    caption: 'The Now column shows the Windows defaults.',
    after:
      'Changes that turn off security features or touch the boot configuration stay behind Expert mode, and none of them is ever recommended automatically.',
  },
  undo: {
    label: 'Undo',
    title: 'Everything it changes can be undone',
    steps: [
      { name: 'Restore point', text: 'Created before the first change, when System Protection is on.' },
      { name: 'Backup', text: 'Each value is saved before it changes.' },
      { name: 'Check', text: 'After applying, each change is checked.' },
      { name: 'Undo', text: 'The Changes page lists every change with Undo, also after a restart or an update of the app.' },
    ],
    body: [
      'After a Windows update, it shows which changes Windows reset and offers to apply them again.',
      'Removed apps and deleted files are the exception, and the app says so before it removes them.',
    ],
  },
  impact: {
    label: 'Impact',
    title: 'Every tweak says what it does for games',
    body: (n, matter) => [
      `Every tweak rates its effect on games from 0 to 5, and the rating can depend on the PC: Restore processor turbo rates 4 where the power plan holds the processor at its base clock, and 0 everywhere else. ${matter} of the ${n} tweaks can matter for games. The others are about privacy, the browser and Windows settings, and their pages say so instead of promising frame rate.`,
    ],
    legend: { measured: 'Measured', situational: 'Situational', disputed: 'Disputed', higher: 'Possible on some PCs' },
    row: (level, count) => `Rating ${level}: ${count} ${count === 1 ? 'tweak' : 'tweaks'}`,
    higher: (title, level, reason) => `${title}: ${level}. ${reason}`,
    caption: 'Each square is one tweak. Hover over a square for its name.',
    after: 'The largest gains usually come from problems the scan finds, such as a monitor at 60 Hz or memory below its rated speed.',
  },
  sources: {
    label: 'Sources',
    title: 'Every claim has a source',
    intro:
      'Each check and each tweak has a page in English and German: what it does, why it can help, how solid the evidence is, the trade-offs, and the sources. Impact goes from 0 to 5 and names its basis: measured, situational or disputed. This is the page for a popular tweak the evidence does not support:',
    docLabel: 'Explanation page in the app',
    show: ['Summary', 'Evidence', 'Trade-offs & risks'],
    after:
      'A tweak that is not yet proven on real hardware carries the badge "Not yet tested on a real PC" and is never part of Apply recommended.',
    shotAlt:
      'The Tweaks page of Tunero: groups of tweaks with switches, and the explanation of the selected tweak in the details pane.',
  },
  left: {
    label: 'Left out',
    title: 'What it leaves out, and why',
    intro: 'Popular tweaks without evidence are not offered. Search for one in the app, and it tells you why.',
    columns: ['Tweak', 'Why it is not included'],
    rows: [
      {
        tweak: 'Realtime priority for games',
        why: 'Microsoft warns that a realtime process preempts operating system threads and can make the mouse unresponsive.',
        source: 'https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-setpriorityclass',
      },
      {
        tweak: 'Spectre and Meltdown mitigations off',
        why: 'Microsoft measured single-digit slowdowns that most users do not notice on the processors Windows 11 supports. A real security loss for a gain too small to notice.',
        source: 'https://www.microsoft.com/en-us/security/blog/2018/01/09/understanding-the-performance-impact-of-spectre-and-meltdown-mitigations-on-windows-systems/',
      },
      {
        tweak: 'Win32PrioritySeparation = 0x26',
        why: 'It is the value Windows already writes for "Adjust for best performance of: Programs". The tweak changes nothing on Windows 11.',
        source: 'https://learn.microsoft.com/en-us/previous-versions/cc976120(v=technet.10)',
      },
      {
        tweak: 'Turning off IPv6 completely',
        why: 'Microsoft advises against it because some Windows components might stop working. The app offers "Prefer IPv4 over IPv6" instead.',
        source: 'https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/configure-ipv6-in-windows',
      },
      {
        tweak: 'netsh TCP global tweaks',
        why: 'They change every connection of the PC, and no measurement shows a gain in games. Ping in online games depends mostly on the route to the server.',
      },
      {
        tweak: 'Registry cleaning',
        why: 'Keys an uninstaller left behind take no measurable space, and no measurement shows that they slow Windows down. A wrong guess breaks programs that share them.',
      },
    ],
    sourceLink: 'Source',
    all: 'The full list, with sources',
  },
  facts: {
    label: 'One app',
    title: 'Instead of a folder full of tools',
    intro:
      'Tunero brings together what people otherwise collect from several tweak tools, debloat scripts and system utilities, and leaves out what does nothing.',
    areas: [
      ['Scan and fix', 'Checks that find what costs frame rate, with a fix or the steps for the BIOS and drivers.'],
      ['Tweaks', 'Documented settings for games, privacy, the browser and Windows, each with its evidence.'],
      ['Remove apps', 'A reviewed list of inbox and manufacturer apps, and programs with their own uninstaller.'],
      ['Cleanup', 'Temporary files, caches and leftovers, without following links.'],
      ['Startup', 'Everything that starts with Windows, with signature checks and snapshots to compare.'],
      ['Services & tasks', 'Start types, with what each service does.'],
      ['Apps & drivers', 'Install and update apps with winget, and see how old the drivers are.'],
      ['Windows Update', 'No restarts in the evening, restarts when you choose, monthly updates a week later (Pro and higher).'],
      ['Health', 'A frame time benchmark with PresentMon, a throttle check and drive health.'],
      ['Tools', 'Storage analyzer, Windows features, network fixes, Windows Update repair, SFC and DISM.'],
      ['Changes', 'Every change with Undo, also after a restart or an update of the app.'],
      ['Export and command line', 'Move your changes to another PC or a fresh install.'],
    ],
    numbers: (c) => [
      ['Read-only checks', c.checks],
      ['Tweaks', c.tweaks],
      ['Reviewed removable apps', c.apps],
      ['Cleanup categories', c.cleanup],
    ],
    rows: [
      ['Background processes', 'None. It runs only when you start it.'],
      ['Telemetry', 'None'],
      ['Installation', 'None needed. One exe.'],
    ],
  },
  privacy: {
    label: 'Privacy',
    title: 'It goes online only when you ask it to',
    body: [
      'No telemetry, no account, no ads. The update check is off until you turn it on. A VirusTotal lookup needs your own key and sends file hashes, never files. The DNS benchmark and app installs run when you start them.',
    ],
    link: 'Everything the app sends, and to whom',
  },
  install: {
    label: 'Install',
    title: 'Get Tunero',
    steps: [
      { name: 'Download', text: 'The exe of the latest release.' },
      { name: 'Check', text: 'Compare its SHA-256 with the .sha256 file on the release page.' },
      { name: 'Start', text: 'It needs no installation. On the first start it offers an entry in the Start menu.' },
    ],
    managers: 'With a package manager',
    releases: 'All releases',
  },
  faq: {
    label: 'Questions',
    title: 'Questions',
    items: [
      {
        id: 'faster',
        q: 'Will it make my games faster?',
        a: 'When the scan finds a real problem, often noticeably: a monitor stuck at 60 Hz, memory without its XMP or EXPO profile, or a processor held at its base clock costs a lot of frame rate, and Tunero fixes it or tells you how. Most tweaks are about privacy, the browser and Windows and are rated 0 for games; the ones that can matter are rated for your PC, and each page says how solid the evidence is. The Health page has a frame time benchmark, so you can measure the difference in your own games.',
      },
      {
        id: 'smartscreen',
        q: 'Why does Windows warn when I start it?',
        a: 'The exe is not code signed yet. Signing by the SignPath Foundation is applied for. Until then, compare the SHA-256 of the download with the .sha256 file on the release page.',
      },
      {
        id: 'admin',
        q: 'Why does it need administrator rights?',
        a: 'Most settings it changes apply to the whole PC. Its data folder is locked to administrators.',
      },
      {
        id: 'home',
        q: 'Does it work on Windows 11 Home?',
        a: 'Yes. Tweaks that rely on Group Policy are offered only on Pro, Enterprise and Education.',
      },
      {
        id: 'remove',
        q: 'Can I remove it completely?',
        a: 'Settings > Uninstall offers to undo every change first. Then it removes the exe, its data folder and its Start menu entry.',
      },
    ],
  },
  footer: {
    license: (v) => `Tunero ${v}. Free software under the GPL-3.0.`,
    links: { releases: 'Releases', changelog: 'Changelog', source: 'Source code', privacy: 'Privacy', imprint: 'Legal notice', kofi: 'Support on Ko-fi' },
    privacyHref: '/privacy/',
    imprintHref: '/legal/',
  },
};

export default en;
