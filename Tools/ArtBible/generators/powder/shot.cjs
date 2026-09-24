const { chromium } = require('playwright');
const fs = require('fs');
(async () => {
  const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: 1200, height: 800 }, deviceScaleFactor: 1 });
  for (const slug of process.argv.slice(2)) {
    const svg = fs.readFileSync('/home/user/PlunderSpell/docs/art/concept/powder/' + slug + '.svg', 'utf8');
    await p.setContent('<html><body style="margin:0;background:#14120E">' + svg + '</body></html>');
    await p.locator('svg').first().screenshot({ path: __dirname + '/prev/' + slug + '.png' });
  }
  await b.close();
})();
