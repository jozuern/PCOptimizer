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
    spec: { version: string; changes: string; size: string; needs: string; needsValue: string; languages: string; languagesValue: string; license: string; licenseValue: string };
    shotCaption: string;
    preview: string;
    previewLink: string;
    shotAlt: string;
  };
  scan: { label: string; title: string; body: (checks: number) => string[]; list: (checks: number) => string };
  confirm: { label: string; title: string; intro: string; columns: string[]; caption: string; after: string };
  undo: { label: string; title: string; steps: { name: string; text: string }[]; body: string[] };
  impact: {
    label: string;
    title: string;
    body: (tweaks: number, zero: number) => string[];
    legend: { situational: string; disputed: string; higher: string };
    row: (level: number, count: number) => string;
    higher: (title: string, level: number, reason: string) => string;
    caption: string;
    after: string;
  };
  sources: { label: string; title: string; intro: string; docLabel: string; show: string[]; after: string; shotAlt: string };
  left: {
    label: string;
    title: string;
    intro: string;
    columns: string[];
    rows: { tweak: string; why: string; source?: string }[];
    sourceLink: string;
    all: string;
  };
  facts: { label: string; title: string; numbers: (c: Counts) => [string, number][]; rows: [string, string][] };
  privacy: { label: string; title: string; body: string[]; link: string };
  install: { label: string; title: string; steps: { name: string; text: string }[]; managers: string; releases: string };
  faq: { label: string; title: string; items: { id: string; q: string; a: string }[] };
  footer: {
    license: (version: string) => string;
    links: { releases: string; changelog: string; source: string; privacy: string; imprint: string; kofi: string };
    privacyHref: string;
    imprintHref: string;
  };
}
