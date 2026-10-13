import type { Lang } from '../lib/data';

export interface Counts {
  checks: number;
  tweaks: number;
  apps: number;
  cleanup: number;
}

export interface Copy {
  lang: Lang;
  meta: { title: string; description: string };
  nav: { download: string; source: string; other: string; otherHref: string; skip: string };
  hero: {
    title: string;
    lead: string;
    download: string;
    winget: string;
    copy: string;
    copied: string;
    /** One line under the button: version, system, languages, license. */
    facts: (version: string) => string;
    changes: string;
    preview: string;
    previewLink: string;
    shotCaption: string;
    shotAlt: string;
  };
  impact: {
    title: string;
    body: (tweaks: number, matter: number) => string[];
    /** Over the checks in the top row. */
    top: string;
    topRow: (count: number) => string;
    measured: string;
    disputed: string;
    /** A tweak that rates higher on some PCs, with its rating elsewhere. */
    some: (base: number) => string;
    zeroNamed: string;
    zeroRest: (count: number) => string;
    row: (level: number, count: number) => string;
  };
  scan: { title: string; body: (checks: number) => string[]; list: (checks: number) => string };
  change: {
    title: string;
    intro: string;
    columns: [string, string, string];
    caption: string;
    expert: string;
    stepsTitle: string;
    steps: { name: string; text: string }[];
    body: string[];
  };
  sources: { title: string; intro: string; docLabel: string; sourcesLabel: string; show: string[]; after: string; shotAlt: string };
  left: {
    title: string;
    intro: string;
    rows: { tweak: string; why: string; source?: string }[];
    sourceLink: string;
    all: string;
  };
  areas: { title: string; intro: string; groups: (c: Counts) => { name: string; items: [string, string][] }[] };
  privacy: { title: string; body: string[]; link: string };
  install: { title: string; steps: { name: string; text: string }[]; managers: string; releases: string };
  faq: { title: string; items: { id: string; q: string; a: string }[] };
  footer: {
    license: (version: string) => string;
    links: { releases: string; changelog: string; source: string; privacy: string; imprint: string; kofi: string };
    privacyHref: string;
    imprintHref: string;
  };
}
