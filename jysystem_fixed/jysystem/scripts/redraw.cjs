const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
s = s.replace("kind: 'rain'", "kind: 'lotus'")
const start = s.indexOf('  makeParticle(init) {')
const end = s.indexOf('  resetParticles()')
if (start < 0 || end < 0) throw new Error('block missing ' + start + ' ' + end)
const block = `  makeParticle(init) {
    const box = this._canvas
    const kind = this.data.theme.kind
    const petals = this.data.theme.petals
    return {
      x: Math.random() * box.w,
      y: init ? Math.random() * box.h : -40,
      s: kind === 'snow' ? 7 + Math.random() * 8 : kind === 'lotus' ? 16 + Math.random() * 18 : 12 + Math.random() * 14,
      vy: kind === 'snow' ? 0.25 + Math.random() * 0.55 : 0.35 + Math.random() * 0.7,
      vx: (Math.random() - 0.5) * 0.45,
      rot: Math.random() * Math.PI * 2,
      vr: (Math.random() - 0.5) * (kind === 'leaf' ? 0.02 : 0.008),
      color: petals[Math.floor(Math.random() * petals.length)],
      shade: petals[Math.floor(Math.random() * petals.length)],
      a: 0.72 + Math.random() * 0.28,
      form: Math.random()
    }
  },
  seedParticles() {
    const count = this.data.theme.kind === 'snow' ? 28 : 14
    this.particles = Array.from({ length: count }, () => this.makeParticle(true))
  },
  lotus(ctx, s) {
    const n = 8
    for (let i = 0; i < n; i++) {
      ctx.save()
      ctx.rotate((Math.PI * 2 * i) / n)
      ctx.beginPath()
      ctx.moveTo(0, 0)
      ctx.bezierCurveTo(s * 0.22, -s * 0.15, s * 0.18, -s * 0.72, 0, -s)
      ctx.bezierCurveTo(-s * 0.18, -s * 0.72, -s * 0.22, -s * 0.15, 0, 0)
      ctx.fill()
      ctx.restore()
    }
    ctx.beginPath()
    ctx.arc(0, 0, s * 0.16, 0, Math.PI * 2)
    ctx.fillStyle = '#F7E27A'
    ctx.fill()
  },
  pad(ctx, s) {
    ctx.beginPath()
    ctx.arc(0, 0, s * 0.72, 0.35, Math.PI * 2 - 0.15)
    ctx.lineTo(0, 0)
    ctx.closePath()
    ctx.fill()
    ctx.strokeStyle = 'rgba(20,80,50,.25)'
    ctx.lineWidth = 1
    ctx.beginPath()
    ctx.moveTo(0, 0)
    ctx.lineTo(s * 0.55, -s * 0.15)
    ctx.moveTo(0, 0)
    ctx.lineTo(-s * 0.4, s * 0.35)
    ctx.stroke()
  },
  maple(ctx, s) {
    ctx.beginPath()
    for (let i = 0; i < 5; i++) {
      const a = -Math.PI / 2 + (i * Math.PI * 2) / 5
      const r1 = i === 0 ? s : s * 0.82
      ctx.lineTo(Math.cos(a) * r1, Math.sin(a) * r1)
      const b = a + Math.PI / 5
      ctx.lineTo(Math.cos(b) * s * 0.28, Math.sin(b) * s * 0.28)
    }
    ctx.closePath()
    ctx.fill()
    ctx.strokeStyle = 'rgba(90,30,10,.35)'
    ctx.lineWidth = 1
    ctx.beginPath()
    ctx.moveTo(0, s * 0.15)
    ctx.lineTo(0, s * 1.15)
    ctx.stroke()
  },
  flake(ctx, s) {
    ctx.strokeStyle = ctx.fillStyle
    ctx.lineWidth = Math.max(0.8, s * 0.08)
    ctx.lineCap = 'round'
    for (let i = 0; i < 6; i++) {
      ctx.save()
      ctx.rotate((Math.PI / 3) * i)
      ctx.beginPath()
      ctx.moveTo(0, 0)
      ctx.lineTo(0, -s)
      ctx.moveTo(0, -s * 0.45)
      ctx.lineTo(s * 0.22, -s * 0.68)
      ctx.moveTo(0, -s * 0.45)
      ctx.lineTo(-s * 0.22, -s * 0.68)
      ctx.stroke()
      ctx.restore()
    }
    ctx.beginPath()
    ctx.arc(0, 0, s * 0.12, 0, Math.PI * 2)
    ctx.fill()
  },
  drawFrame() {
    const box = this._canvas
    if (!box) return
    const ctx = box.ctx
    const kind = this.data.theme.kind
    ctx.clearRect(0, 0, box.w, box.h)
    this.particles.forEach((p) => {
      p.y += p.vy
      p.x += p.vx + Math.sin((p.y + p.s) * 0.01) * 0.45
      p.rot += p.vr
      if (p.y > box.h + 50) Object.assign(p, this.makeParticle(false))
      ctx.save()
      ctx.translate(p.x, p.y)
      ctx.rotate(p.rot)
      ctx.globalAlpha = p.a
      ctx.fillStyle = p.form > 0.72 && kind === 'lotus' ? '#2F8F62' : p.color
      if (kind === 'lotus' && p.form > 0.72) this.pad(ctx, p.s * 0.9)
      else if (kind === 'lotus') this.lotus(ctx, p.s * 0.55)
      else if (kind === 'leaf') this.maple(ctx, p.s * 0.55)
      else if (kind === 'snow') this.flake(ctx, p.s * 0.7)
      else {
        ctx.beginPath()
        ctx.moveTo(0, -p.s)
        ctx.bezierCurveTo(p.s * 0.9, -p.s * 0.8, p.s * 0.8, p.s * 0.45, 0, p.s)
        ctx.bezierCurveTo(-p.s * 0.8, p.s * 0.45, -p.s * 0.9, -p.s * 0.8, 0, -p.s)
        ctx.fill()
      }
      ctx.restore()
    })
  },
`
s = s.slice(0, start) + block + s.slice(end)
fs.writeFileSync(p, s)
console.log('replaced', s.includes('this.maple'))
