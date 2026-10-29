// Records the Fatum prototype in headless Chromium.
//
//   node docs/plans/doom-spell/capture.js stills          five PNGs, one per phase, into stills/
//   node docs/plans/doom-spell/capture.js video [fps]     every frame into frames/, then ffmpeg joins them
//                                                          with doom_spell_full.ogg into fatum_cast.mp4,
//                                                          and makes fatum_cast.gif for the proposal page
//
// Runs from any folder. Needs Playwright with its Chromium, and ffmpeg for "video".
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

const HERE = __dirname;
const STILLS = [
  [1.6, '1-incantation'],
  [4.2, '2-omen-rises'],
  [9.5, '3-maw-opens'],
  [14.75, '4-inhale'],
  [15.62, '5-doom'],
  [18.0, '6-aftermath'],
];

async function openPrototype(width, height) {
  const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const page = await browser.newPage({ viewport: { width: 1200, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));
  page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') errors.push(m.text()); });
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body><div id="root"></div></body></html>');
  await page.addScriptTag({ path: path.join(HERE, 'prototype.js') });
  await page.evaluate(([w, h]) => window.Fatum.start(document.getElementById('root'), { width: w, height: h, audioSrc: '' }), [width, height]);
  if (errors.length) throw new Error('The prototype reported errors:\n' + errors.join('\n'));
  return { browser, page, errors };
}

function writeDataUrl(dataUrl, file) {
  fs.writeFileSync(file, Buffer.from(dataUrl.split(',')[1], 'base64'));
}

async function stills() {
  const { browser, page, errors } = await openPrototype(1280, 720);
  const dir = path.join(HERE, 'stills');
  fs.mkdirSync(dir, { recursive: true });
  for (const [t, name] of STILLS) {
    const started = Date.now();
    writeDataUrl(await page.evaluate(t => window.frameAt(t), t), path.join(dir, `${name}.png`));
    console.log(`still ${name}.png at ${t}s (${Date.now() - started} ms)`);
  }
  await browser.close();
  if (errors.length) throw new Error(errors.join('\n'));
}

async function video(fps) {
  const { browser, page, errors } = await openPrototype(960, 540);
  const end = await page.evaluate(() => window.FATUM_TIMELINE.END);
  const dir = path.join(HERE, 'frames');
  fs.rmSync(dir, { recursive: true, force: true });
  fs.mkdirSync(dir, { recursive: true });
  const count = Math.round(end * fps);
  const started = Date.now();
  for (let i = 0; i < count; i++) {
    const url = await page.evaluate(t => window.frameAt(t, 'image/jpeg', 0.92), i / fps);
    writeDataUrl(url, path.join(dir, `f${String(i).padStart(4, '0')}.jpg`));
    if (i % 48 === 0) console.log(`frame ${i}/${count} (${((Date.now() - started) / 1000).toFixed(0)} s so far)`);
  }
  await browser.close();
  if (errors.length) throw new Error(errors.join('\n'));
  console.log(`rendered ${count} frames in ${((Date.now() - started) / 1000).toFixed(0)} s`);

  const mp4 = path.join(HERE, 'fatum_cast.mp4');
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-framerate', String(fps), '-i', path.join(dir, 'f%04d.jpg'),
    '-i', path.join(HERE, 'doom_spell_full.ogg'), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20',
    '-c:a', 'aac', '-b:a', '192k', '-shortest', mp4], { stdio: 'inherit' });
  console.log('wrote ' + mp4);

  const gif = path.join(HERE, 'fatum_cast.gif');
  execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-framerate', String(fps), '-i', path.join(dir, 'f%04d.jpg'),
    '-vf', 'fps=12,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=160[p];[b][p]paletteuse=dither=bayer:bayer_scale=4',
    gif], { stdio: 'inherit' });
  console.log('wrote ' + gif);
}

const [mode, fpsArg] = process.argv.slice(2);
const job = mode === 'video' ? video(parseInt(fpsArg || '24', 10)) : mode === 'stills' ? stills() : null;
if (!job) { console.error('usage: node capture.js stills | video [fps]'); process.exit(2); }
job.catch(e => { console.error(e.message); process.exit(1); });
