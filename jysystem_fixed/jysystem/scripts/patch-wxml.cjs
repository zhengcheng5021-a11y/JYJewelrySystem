const fs = require('fs')
const pages = ['pages/home/home.wxml','pages/inbound/inbound.wxml','pages/inbound/edit.wxml','pages/members/members.wxml','pages/stock/stock.wxml','pages/sale/sale.wxml','pages/tasks/tasks.wxml','pages/profile/profile.wxml']
for (const p of pages) {
  let s = fs.readFileSync(p, 'utf8')
  if (s.includes('theme-switch')) { console.log('skip', p); continue }
  s = s.replace(/<view class="page([^"]*)">/, '<view class="page$1 scene" style="{{theme.style}}">\n  <theme-switch bind:change="onTheme"></theme-switch>')
  fs.writeFileSync(p, s)
  console.log('wxml', p, s.includes('theme.style'))
}
