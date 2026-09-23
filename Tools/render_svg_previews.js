// Renders each SVG given on the command line to a PNG beside it, at the SVG's own width/height.
// Uses the globally installed Playwright with the container's pre-installed Chromium.
//
// Run:  NODE_PATH="$(npm root -g)" node Tools/render_svg_previews.js docs/generated/bestiary/*.svg

const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright");

const CHROMIUM = "/opt/pw-browsers/chromium-1194/chrome-linux/chrome";

(async () => {
  const files = process.argv.slice(2);
  if (files.length === 0) {
    console.error("usage: node Tools/render_svg_previews.js <file.svg>...");
    process.exit(1);
  }
  const browser = await chromium.launch({ executablePath: CHROMIUM });
  for (const file of files) {
    const source = fs.readFileSync(file, "utf8");
    const width = Number(/width="(\d+)"/.exec(source)[1]);
    const height = Number(/height="(\d+)"/.exec(source)[1]);
    const page = await browser.newPage({ viewport: { width, height } });
    await page.goto("file://" + path.resolve(file));
    await page.waitForTimeout(300);
    const out = file.replace(/\.svg$/, ".png");
    await page.screenshot({ path: out });
    await page.close();
    console.log(`rendered ${out} (${width}x${height})`);
  }
  await browser.close();
})();
