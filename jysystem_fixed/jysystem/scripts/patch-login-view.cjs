const fs = require('fs')
let w = fs.readFileSync('pages/login/login.wxml','utf8')
w = w.replace(/<view class="logo-shell"[\s\S]*?<\/view>/, '<view class="logo-shell" style="background:{{theme.logoGlow}};"><image class="logo-art" src="/images/logo.svg" mode="aspectFit"></image></view>')
w = w.replace('<view class="mountain"></view>', '<view class="mountain" style="background:{{theme.mountainColor}};"></view>')
fs.writeFileSync('pages/login/login.wxml', w)
let j = fs.readFileSync('pages/login/login.js','utf8')
if (!j.includes('initCanvas()')) {}
j = j.replace('onUnload() { this.stopped = true },', `onUnload() { this.stopped = true },
  initCanvas() {},
  resetParticles() {},
  onTouch() {},
  pulseSparkles() { wx.showToast({ title: '触碰东方灵韵 · 晶石生辉', icon: 'none' }) },
  noop() {},`)
fs.writeFileSync('pages/login/login.js', j)
fs.writeFileSync('pages/login/login.json', JSON.stringify({
  navigationStyle: 'custom',
  navigationBarTitleText: '静悦',
  navigationBarTextStyle: 'black',
  backgroundColor: '#F8E8E4',
  usingComponents: {}
}, null, 2))
console.log('patched', w.includes('logo.svg'))
