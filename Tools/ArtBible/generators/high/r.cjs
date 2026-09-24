const { chromium } = require('playwright');
const fs = require('node:fs');
const fonts = 'https://fonts.googleapis.com/css2?family=Eczar:wght@500;600;700;800&family=Overpass+Mono:wght@400;600&display=swap';
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1200, height: 800 }, deviceScaleFactor: 1 });
  for (const f of process.argv.slice(2)) {
    const svg = fs.readFileSync(f, 'utf8');
    await p.setContent(`<!doctype html><html><head><link rel="stylesheet" href="${fonts}"><style>html,body{margin:0;background:#14120E}svg{display:block;width:1200px;height:800px}</style></head><body>${svg}</body></html>`, { waitUntil: 'load', timeout: 20000 }).catch(()=>{});
    await p.evaluate(() => document.fonts.ready);
    const out = __dirname + '/png/' + f.split('/').pop().replace('.svg', '.png');
    await p.locator('svg').first().screenshot({ path: out });
    console.log(out);
  }
  await b.close();
})();
