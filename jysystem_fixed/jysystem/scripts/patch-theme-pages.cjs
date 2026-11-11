const fs = require('fs')
const pages = ['pages/inbound/inbound.js','pages/inbound/edit.js','pages/members/members.js','pages/stock/stock.js','pages/sale/sale.js','pages/tasks/tasks.js','pages/profile/profile.js','pages/home/home.js']
for (const p of pages) {
  let s = fs.readFileSync(p, 'utf8')
  if (!s.includes('utils/theme')) s = "const themeUtil = require('../../utils/theme')\n" + s
  if (!s.includes('onTheme(')) s = s.replace(/\n\}\)\s*$/, "\n  onTheme() { themeUtil.apply(this) }\n})\n")
  if (s.includes('onShow()')) s = s.replace('onShow() {', 'onShow() { themeUtil.apply(this); ')
  else s = s.replace(/onLoad\(([^)]*)\) \{/, 'onShow() { themeUtil.apply(this) },\n  onLoad($1) { themeUtil.apply(this); ')
  fs.writeFileSync(p, s)
}
const wxmls = pages.map(p => p.replace('.js', '.wxml'))
for (const p of wxmls) {
  let s = fs.readFileSync(p, 'utf8')
  if (!s.includes('theme-switch')) {
    s = s.replace('<view class="page', '<view class="page scene" style="{{theme.style}}"><theme-switch bind:change="onTheme"></theme-switch><view class="page')
    // above is wrong. do precise
  }
  fs.writeFileSync(p, s)
}
console.log('js patched')
