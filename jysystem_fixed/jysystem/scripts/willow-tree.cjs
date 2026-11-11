const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
const fn = `  drawWillowTree(ctx, w, h) {
    this._wind = (this._wind || 0) + 0.018
    const t = this._wind
    const x = w * 0.18
    const ground = h * 0.9
    const crown = h * 0.3
    ctx.save()
    ctx.lineCap = 'round'
    ctx.strokeStyle = '#6A4A32'
    ctx.lineWidth = Math.max(8, w * 0.018)
    ctx.beginPath()
    ctx.moveTo(x, ground)
    ctx.quadraticCurveTo(x - w * 0.03, h * 0.62, x + w * 0.01, crown)
    ctx.stroke()
    ctx.fillStyle = '#5E9148'
    ctx.globalAlpha = 0.92
    ctx.beginPath()
    ctx.ellipse(x + w * 0.02, crown + 6, w * 0.16, h * 0.045, -0.15, 0, Math.PI * 2)
    ctx.fill()
    ctx.fillStyle = '#7AAA5C'
    ctx.beginPath()
    ctx.ellipse(x - w * 0.04, crown + h * 0.02, w * 0.11, h * 0.032, 0.2, 0, Math.PI * 2)
    ctx.fill()
    for (let i = 0; i < 14; i++) {
      const sway = Math.sin(t * 1.3 + i * 0.55) * w * 0.035
      const x0 = x - w * 0.12 + i * w * 0.018
      const len = h * (0.22 + (i % 4) * 0.045)
      ctx.globalAlpha = 0.9
      ctx.strokeStyle = i % 2 ? '#6B9A4E' : '#3F7A3A'
      ctx.lineWidth = 1.7
      ctx.beginPath()
      ctx.moveTo(x0, crown + 8)
      ctx.bezierCurveTo(x0 + sway * 0.25, crown + len * 0.35, x0 + sway * 0.8, crown + len * 0.68, x0 + sway, crown + len)
      ctx.stroke()
      ctx.fillStyle = '#93C56E'
      ctx.beginPath()
      ctx.ellipse(x0 + sway, crown + len, 3.5, 8, sway * 0.01, 0, Math.PI * 2)
      ctx.fill()
    }
    ctx.restore()
  },
`
if (!s.includes('drawWillowTree')) s = s.replace('  drawFrame() {', fn + '  drawFrame() {')
s = s.replace(`    ctx.clearRect(0, 0, box.w, box.h)
    this.particles.forEach((p) => {`, `    ctx.clearRect(0, 0, box.w, box.h)
    if (kind === 'willow') { this.drawWillowTree(ctx, box.w, box.h); return }
    this.particles.forEach((p) => {`)
s = s.replace("      if (kind === 'willow') this.willow(ctx, p.s * 0.85)\n      else if (kind === 'leaf')", "      if (kind === 'leaf')")
fs.writeFileSync(p, s)
console.log(s.includes('drawWillowTree'), s.includes("kind === 'willow') { this.drawWillowTree"))
