// Static site for tunero.dev, built by .github/workflows/pages.yml and served by GitHub Pages.
import { defineConfig } from 'astro/config';

export default defineConfig({
  site: 'https://tunero.dev',
  trailingSlash: 'ignore',
  build: { format: 'directory' },
  i18n: {
    locales: ['en', 'de'],
    defaultLocale: 'en',
    routing: { prefixDefaultLocale: false },
  },
});
