const { chromium } = require(process.env.PWPATH);
(async () => {
  const b = await chromium.launch();
  const p = await b.newPage();
  p.on('pageerror', e => console.log('PAGEERR', e.message));
  await p.goto(process.env.URL);
  await p.waitForFunction(() => document.querySelector('#log')?.textContent.includes('DONE'), null, { timeout: 300000 });
  console.log(await p.textContent('#log'));
  await b.close();
})();
