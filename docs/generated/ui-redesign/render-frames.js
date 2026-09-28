const { chromium } = require('playwright');
(async () => {
  const b = await chromium.launch({executablePath: '/opt/pw-browsers/chromium'}).catch(async()=>chromium.launch());
  const p = await b.newPage({viewport:{width:1280,height:900}});
  p.on('console', m => console.log('console:', m.text()));
  await p.goto('file:///home/user/PlunderSpell/docs/generated/ui-redesign/index.html'); await p.addStyleTag({path:'fonts.css'});
  await p.waitForTimeout(2500);
  const frames = await p.$$('.frame');
  for (let i=0;i<frames.length;i++) await frames[i].screenshot({path:`frame-${i}.png`});
  const ow = await p.evaluate(()=>document.documentElement.scrollWidth);
  console.log('frames',frames.length,'scrollWidth',ow);
  await p.setViewportSize({width:400,height:900}); await p.waitForTimeout(300);
  console.log('phone scrollWidth', await p.evaluate(()=>document.documentElement.scrollWidth));
  console.log('font eczar loaded', await p.evaluate(()=>document.fonts.check('700 20px Eczar')));
  await b.close();
})();
