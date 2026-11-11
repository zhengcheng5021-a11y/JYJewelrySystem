const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
const a = s.indexOf('  drawWillowTree(ctx, w, h) {')
const b = s.indexOf('  drawFrame() {')
if (a < 0 || b < 0) throw new Error('missing')
const fn = `  drawWillowTree(ctx, w, h) {
    this._wind = (this._wind || 0) + 0.016
    const sway = Math.sin(this._wind) * w * 0.012
    const x = w * 0.2
    const ground = h * 0.93
    const crown = h * 0.28
    ctx.save()
    ctx.translate(sway, 0)
    ctx.lineCap = 'round'
    ctx.strokeStyle = '#5A3B28'
    ctx.lineWidth = Math.max(14, w * 0.03)
    ctx.beginPath()
    ctx.moveTo(x, ground)
    ctx.bezierCurveTo(x - w * 0.02, h * 0.72, x - w * 0.04, h * 0.5, x, crown + h * 0.04)
    ctx.stroke()
    ctx.strokeStyle = '#7A5640'
    ctx.lineWidth = Math.max(6, w * 0.012)
    ctx.beginPath()
    ctx.moveTo(x, h * 0.55)
    ctx.quadraticCurveTo(x + w * 0.06, h * 0.48, x + w * 0.1, crown + h * 0.03)
    ctx.stroke()
    ctx.fillStyle = '#3E6B34'
    ctx.beginPath()
    ctx.ellipse(x + w * 0.02, crown, w * 0.2, h * 0.055, -0.2, 0, Math.PI * 2)
    ctx.fill()
    ctx.fillStyle = '#6FA34C'
    ctx.beginPath()
    ctx.ellipse(x - w * 0.03, crown + h * 0.02, w * 0.13, h * 0.04, 0.3, 0, Math.PI * 2)
    ctx.fill()
    for (let i = 0; i < 22; i++) {
      const phase = Math.sin(this._wind * 1.4 + i * 0.45)
      const x0 = x - w * 0.16 + (i / 21) * w * 0.32
      const len = h * (0.28 + (i % 5) * 0.035)
      const bend = phase * w * 0.04
      ctx.strokeStyle = i % 2 ? '#4E8A3C' : '#2F6A32'
      ctx.lineWidth = 1.3
      ctx.beginPath()
      ctx.moveTo(x0, crown + 4)
      ctx.bezierCurveTo(x0 + bend * 0.2, crown + len * 0.35, x0 + bend, crown + len * 0.7, x0 + bend * 1.15, crown + len)
      ctx.stroke()
      ctx.fillStyle = i % 3 ? '#8FBF68' : '#6E9E48'
      for (let k = 2; k <= 7; k++) {
        const t = k / 8
        const lx = x0 + bend * t
        const ly = crown + len * t
        ctx.save()
        ctx.translate(lx, ly)
        ctx.rotate(bend * 0.01)
        ctx.beginPath()
        ctx.moveTo(0, -7)
        ctx.bezierCurveTo(5, -3, 4, 6, 0, 9)
        ctx.bezierCurveTo(-4, 6, -3, -3, 0, -7)
        ctx.fill()
        ctx.restore()
      }
    }
    ctx.restore()
  },
`
s = s.slice(0, a) + fn + s.slice(b)
fs.writeFileSync(p, s)
console.log('tree', s.includes('for (let i = 0; i < 22; i++)'))
