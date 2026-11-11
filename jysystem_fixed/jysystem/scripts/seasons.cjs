const fs = require('fs')
let s = fs.readFileSync('pages/login/login.js', 'utf8')
const a = s.indexOf('const THEMES')
const b = s.indexOf('const AGREEMENTS')
const themes = `const THEMES = {
  spring: {
    id: 'spring', label: '节气 · 春', name: '春分 · 桃花', dot: '#E5A8A2', accent: '#99554D', kind: 'petal',
    navBg: '#FDF5F2',
    screenBg: 'background:linear-gradient(180deg,#FDF5F2 0%,#F8E8E4 55%,#F3D5E4 100%);color:#3A2624;',
    textColor: '#3A2624', subTextColor: '#553634',
    cardBg: 'rgba(255,255,255,.55)', cardBorder: 'rgba(255,255,255,.72)',
    cardGlow: 'radial-gradient(circle, rgba(244,166,184,.4), transparent 70%)',
    headBorder: 'rgba(0,0,0,.06)', seal: '#99554D',
    inputBg: 'rgba(255,255,255,.86)', inputBorder: 'rgba(216,156,146,.35)',
    inputColor: '#2A2022', inputPlaceholder: '#A89495',
    buttonGradient: 'linear-gradient(90deg,#3C2827,#5A3835,#3C2827)', buttonTextColor: '#FDF7F2', buttonColor: '#523533', linkColor: '#5A3835',
    orbGradient: 'radial-gradient(circle, rgba(244,166,184,.55) 0%, rgba(255,214,170,.2) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(244,166,184,.55), rgba(255,214,170,.15), transparent 70%)',
    indicator: 'rgba(60,40,39,.3)', mountainColor: 'rgba(216,156,146,.38)',
    petals: ['#F4A6B8','#FBC1CD','#E88B9E','#FBECEB']
  },
  summer: {
    id: 'summer', label: '节气 · 夏', name: '夏至 · 荷风', dot: '#3E8F78', accent: '#1F6B56', kind: 'rain',
    navBg: '#E7F4EE',
    screenBg: 'background:linear-gradient(180deg,#F3FBF7 0%,#D7F0E4 48%,#8FCBB4 100%);color:#16352C;',
    textColor: '#16352C', subTextColor: '#3E564E',
    cardBg: 'rgba(255,255,255,.58)', cardBorder: 'rgba(164,214,196,.7)',
    cardGlow: 'radial-gradient(circle, rgba(120,210,180,.4), transparent 70%)',
    headBorder: 'rgba(22,53,44,.06)', seal: '#1F6B56',
    inputBg: 'rgba(255,255,255,.88)', inputBorder: 'rgba(80,150,130,.28)',
    inputColor: '#16352C', inputPlaceholder: '#7A968D',
    buttonGradient: 'linear-gradient(90deg,#145C49,#2E8F72,#145C49)', buttonTextColor: '#F3FFF9', buttonColor: '#145C49', linkColor: '#145C49',
    orbGradient: 'radial-gradient(circle, rgba(120,220,190,.5) 0%, rgba(255,214,120,.18) 62%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(140,220,190,.5), transparent 70%)',
    indicator: 'rgba(20,70,58,.28)', mountainColor: 'rgba(70,150,120,.28)',
    petals: ['rgba(255,255,255,.75)','rgba(210,245,230,.8)','rgba(255,255,255,.45)']
  },
  autumn: {
    id: 'autumn', label: '节气 · 秋', name: '秋分 · 枫落', dot: '#C46A2E', accent: '#8A3E12', kind: 'leaf',
    navBg: '#F8E7D2',
    screenBg: 'background:linear-gradient(180deg,#FFF6EA 0%,#F6D7B0 50%,#E7A15A 100%);color:#4A2A16;',
    textColor: '#4A2A16', subTextColor: '#7A4A28',
    cardBg: 'rgba(255,248,240,.6)', cardBorder: 'rgba(255,220,180,.75)',
    cardGlow: 'radial-gradient(circle, rgba(230,120,50,.35), transparent 70%)',
    headBorder: 'rgba(74,42,22,.08)', seal: '#C46A2E',
    inputBg: 'rgba(255,255,255,.86)', inputBorder: 'rgba(196,106,46,.28)',
    inputColor: '#4A2A16', inputPlaceholder: '#B08968',
    buttonGradient: 'linear-gradient(90deg,#6B3414,#C46A2E,#6B3414)', buttonTextColor: '#FFF8EF', buttonColor: '#6B3414', linkColor: '#8A3E12',
    orbGradient: 'radial-gradient(circle, rgba(240,150,60,.5) 0%, rgba(180,70,40,.15) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(240,160,70,.45), transparent 70%)',
    indicator: 'rgba(90,40,16,.3)', mountainColor: 'rgba(180,90,30,.28)',
    petals: ['#E25B2A','#F0A04B','#C4482D','#F6D7A8']
  },
  winter: {
    id: 'winter', label: '节气 · 冬', name: '冬至 · 初雪', dot: '#D7E4F5', accent: '#E8F1FF', kind: 'snow',
    navBg: '#1B2433',
    screenBg: 'background:linear-gradient(180deg,#243044 0%,#1B2433 46%,#101722 100%);color:#F4F7FB;',
    textColor: '#F4F7FB', subTextColor: '#C9D5E6',
    cardBg: 'rgba(18,26,38,.62)', cardBorder: 'rgba(210,225,245,.22)',
    cardGlow: 'radial-gradient(circle, rgba(180,210,255,.28), transparent 70%)',
    headBorder: 'rgba(255,255,255,.08)', seal: '#D7E4F5',
    inputBg: 'rgba(10,16,26,.55)', inputBorder: 'rgba(210,225,245,.22)',
    inputColor: '#F4F7FB', inputPlaceholder: '#8EA0B8',
    buttonGradient: 'linear-gradient(90deg,#E8F1FF,#B9CDE8,#E8F1FF)', buttonTextColor: '#1B2433', buttonColor: '#D7E4F5', linkColor: '#E8F1FF',
    orbGradient: 'radial-gradient(circle, rgba(190,215,255,.35) 0%, rgba(120,150,200,.08) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(200,220,255,.35), transparent 70%)',
    indicator: 'rgba(255,255,255,.35)', mountainColor: 'rgba(180,200,230,.12)',
    petals: ['#FFFFFF','#E7F0FF','#D5E4F7','#F8FBFF']
  }
}

`
s = s.slice(0, a) + themes + s.slice(b)
s = s.replace('theme: THEMES.blush', 'theme: THEMES.spring')
s = s.replace("frontColor: id === 'dark' ? '#ffffff' : '#000000', backgroundColor: id === 'dark' ? '#1D1618' : (id === 'celadon' ? '#EFF6F3' : '#FDF5F2')", "frontColor: id === 'winter' ? '#ffffff' : '#000000', backgroundColor: theme.navBg")
s = s.replace(`onUnload() { this.stopped = true },
  initCanvas() {},
  resetParticles() {},
  onTouch() {},
  pulseSparkles() { wx.showToast({ title: '触碰东方灵韵 · 晶石生辉', icon: 'none' }) },
  noop() {},`, `onUnload() { this.stopped = true; if (this._timer && this._canvas) this._canvas.canvas.cancelAnimationFrame(this._timer) },
  initCanvas() {
    wx.createSelectorQuery().in(this).select('#zenCanvas').fields({ node: true, size: true }).exec((res) => {
      const info = res && res[0]
      if (!info || !info.node) return
      const canvas = info.node
      const ctx = canvas.getContext('2d')
      const dpr = wx.getSystemInfoSync().pixelRatio || 1
      canvas.width = info.width * dpr
      canvas.height = info.height * dpr
      ctx.scale(dpr, dpr)
      this._canvas = { canvas, ctx, w: info.width, h: info.height }
      this.seedParticles()
      const tick = () => {
        if (this.stopped) return
        this.drawFrame()
        this._timer = canvas.requestAnimationFrame(tick)
      }
      tick()
    })
  },
  makeParticle(init) {
    const box = this._canvas
    const theme = this.data.theme
    const kind = theme.kind
    return {
      x: Math.random() * box.w,
      y: init ? Math.random() * box.h : -24,
      s: kind === 'rain' ? 12 + Math.random() * 18 : 5 + Math.random() * 9,
      vy: kind === 'snow' ? 0.35 + Math.random() * 0.7 : kind === 'rain' ? 7 + Math.random() * 5 : 0.55 + Math.random() * 1.15,
      vx: kind === 'rain' ? -1.4 - Math.random() : (Math.random() - 0.35) * 0.9,
      rot: Math.random() * Math.PI * 2,
      vr: (Math.random() - 0.5) * 0.03,
      color: theme.petals[Math.floor(Math.random() * theme.petals.length)],
      a: 0.35 + Math.random() * 0.55
    }
  },
  seedParticles() {
    const count = this.data.theme.kind === 'rain' ? 64 : 32
    this.particles = Array.from({ length: count }, () => this.makeParticle(true))
  },
  drawFrame() {
    const box = this._canvas
    if (!box) return
    const ctx = box.ctx
    const kind = this.data.theme.kind
    ctx.clearRect(0, 0, box.w, box.h)
    this.particles.forEach((p) => {
      p.y += p.vy
      p.x += p.vx + Math.sin(p.y * 0.012) * (kind === 'snow' ? 0.6 : 0.25)
      p.rot += p.vr
      if (p.y > box.h + 30 || p.x < -30 || p.x > box.w + 30) Object.assign(p, this.makeParticle(false))
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.rot)
      ctx.globalAlpha = p.a
      ctx.fillStyle = p.color
      ctx.beginPath()
      if (kind === 'rain') ctx.rect(-0.8, -p.s / 2, 1.6, p.s)
      else if (kind === 'snow') ctx.arc(0, 0, Math.max(1.2, p.s * 0.18), 0, Math.PI * 2)
      else if (kind === 'leaf') ctx.ellipse(0, 0, p.s * 0.32, p.s * 0.62, 0, 0, Math.PI * 2)
      else {
        ctx.moveTo(0, -p.s)
        ctx.bezierCurveTo(p.s * 0.9, -p.s * 0.8, p.s * 0.8, p.s * 0.45, 0, p.s)
        ctx.bezierCurveTo(-p.s * 0.8, p.s * 0.45, -p.s * 0.9, -p.s * 0.8, 0, -p.s)
      }
      ctx.fill()
      ctx.restore()
    })
  },
  resetParticles() { if (this._canvas) this.seedParticles() },
  onTouch() {},
  pulseSparkles() { wx.showToast({ title: '节气流转 · 晶石生辉', icon: 'none' }) },
  noop() {},`)
fs.writeFileSync('pages/login/login.js', s)
console.log('written')
