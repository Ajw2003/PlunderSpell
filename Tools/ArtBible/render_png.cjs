// Render every concept SVG in docs/art/concept/<age>/ to a 2400×1600 PNG beside it.
// Run from anywhere:  NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs [age ...]
// Needs Playwright with a Chromium it can find (PLAYWRIGHT_BROWSERS_PATH, or /opt/pw-browsers).

// CommonJS on purpose: require() honours NODE_PATH, so a globally installed Playwright is found.
const { chromium } = require('playwright');
const { readdirSync, readFileSync, statSync } = require('node:fs');
const { join, resolve } = require('node:path');

const repo = resolve(__dirname, '..', '..');
const conceptRoot = join(repo, 'docs', 'art', 'concept');
const onlyAges = process.argv.slice(2);

const svgs = readdirSync(conceptRoot)
  .filter((age) => statSync(join(conceptRoot, age)).isDirectory())
  .filter((age) => onlyAges.length === 0 || onlyAges.includes(age))
  .flatMap((age) => readdirSync(join(conceptRoot, age))
    .filter((name) => name.endsWith('.svg'))
    .map((name) => join(conceptRoot, age, name)));

if (svgs.length === 0) {
  console.error(`No SVGs found under ${conceptRoot}${onlyAges.length ? ` for ${onlyAges.join(', ')}` : ''}.`);
  process.exit(1);
}

const fonts = 'https://fonts.googleapis.com/css2?family=Eczar:wght@500;600;700;800&family=Overpass+Mono:wght@400;600&family=Spectral:ital,wght@0,400;1,400&display=swap';
(async () => {
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1200, height: 800 }, deviceScaleFactor: 2 });
let fontsLoaded = true;
let failures = 0;

for (const svgPath of svgs) {
  const svg = readFileSync(svgPath, 'utf8').replace(/<\?xml[^>]*\?>/, '');
  await page.setContent(
    `<!doctype html><html><head><link rel="stylesheet" href="${fonts}">` +
    `<style>html,body{margin:0;background:#14120E}svg{display:block;width:1200px;height:800px}</style></head>` +
    `<body>${svg}</body></html>`,
    { waitUntil: 'load', timeout: 20000 },
  ).catch(() => { fontsLoaded = false; });
  await page.evaluate(() => document.fonts.ready);
  const pngPath = svgPath.replace(/\.svg$/, '.png');
  try {
    await page.locator('svg').first().screenshot({ path: pngPath });
    console.log(`rendered ${pngPath.slice(repo.length + 1)}`);
  } catch (error) {
    failures += 1;
    console.error(`FAILED ${svgPath.slice(repo.length + 1)}: ${error.message}`);
  }
}

await browser.close();
if (!fontsLoaded) console.warn('Warning: web fonts did not load for at least one sheet; fallback fonts were used.');
console.log(`${svgs.length - failures} of ${svgs.length} sheets rendered.`);
process.exit(failures ? 1 : 0);
})().catch((error) => { console.error(error); process.exit(1); });
