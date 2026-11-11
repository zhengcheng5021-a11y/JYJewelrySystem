const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
const mapleStart = s.indexOf('  maple(ctx, s) {')
const mapleEnd = s.indexOf('  flake(ctx, s) {')
if (mapleStart < 0 || mapleEnd < 0) throw new Error('missing maple')
const maple = `  maple(ctx, s) {
    ctx.beginPath()
    for (let i = 0; i < 5; i++) {
      const a = -Math.PI / 2 + (i * Math.PI * 2) / 5
      const tip = i === 0 || i === 2 || i === 3 ? s : s * 0.86
      ctx.lineTo(Math.cos(a - 0.52) * s * 0.18, Math.sin(a - 0.52) * s * 0.18)
      ctx.quadraticCurveTo(Math.cos(a - 0.28) * tip * 0.72, Math.sin(a - 0.28) * tip * 0.72, Math.cos(a) * tip, Math.sin(a) * tip)
      ctx.quadraticCurveTo(Math.cos(a + 0.28) * tip * 0.72, Math.sin(a + 0.28) * tip * 0.72, Math.cos(a + 0.52) * s * 0.18, Math.sin(a + 0.52) * s * 0.18)
    }
    ctx.closePath()
    ctx.fill()
    ctx.save()
    ctx.strokeStyle = 'rgba(110,36,12,.4)'
    ctx.lineWidth = 1
    ctx.beginPath()
    for (let i = 0; i < 5; i++) {
      const a = -Math.PI / 2 + (i * Math.PI * 2) / 5
      ctx.moveTo(0, s * 0.05)
      ctx.lineTo(Math.cos(a) * s * 0.62, Math.sin(a) * s * 0.62)
    }
    ctx.moveTo(0, s * 0.08)
    ctx.lineTo(0, s * 1.08)
    ctx.stroke()
    ctx.restore()
  },
  blossom(ctx, s) {
    for (let i = 0; i < 5; i++) {
      ctx.save()
      ctx.rotate((Math.PI * 2 * i) / 5 - Math.PI / 2)
      ctx.beginPath()
      ctx.moveTo(0, s * 0.08)
      ctx.bezierCurveTo(s * 0.42, -s * 0.05, s * 0.48, -s * 0.78, 0, -s)
      ctx.bezierCurveTo(-s * 0.48, -s * 0.78, -s * 0.42, -s * 0.05, 0, s * 0.08)
      ctx.fill()
      ctx.restore()
    }
    ctx.beginPath()
    ctx.arc(0, 0, s * 0.16, 0, Math.PI * 2)
    ctx.fillStyle = '#F3C85A'
    ctx.fill()
    ctx.strokeStyle = '#C9842C'
    ctx.lineWidth = 0.8
    for (let i = 0; i < 7; i++) {
      const a = (Math.PI * 2 * i) / 7
      ctx.beginPath()
      ctx.moveTo(Math.cos(a) * s * 0.05, Math.sin(a) * s * 0.05)
      ctx.lineTo(Math.cos(a) * s * 0.24, Math.sin(a) * s * 0.24)
      ctx.stroke()
    }
  },
`
s = s.slice(0, mapleStart) + maple + s.slice(mapleEnd)
s = s.replace(
  's: kind === \'willow\' ? 18 + Math.random() * 16 : kind === \'snow\' ? 12 + Math.random() * 8 : kind === \'lotus\' ? 16 + Math.random() * 18 : 12 + Math.random() * 14',
  's: kind === \'willow\' ? 18 + Math.random() * 16 : kind === \'snow\' ? 12 + Math.random() * 8 : kind === \'leaf\' ? 26 + Math.random() * 10 : kind === \'petal\' ? 20 + Math.random() * 8 : 12 + Math.random() * 14'
)
s = s.replace("else if (kind === 'leaf') this.maple(ctx, p.s * 0.55)", "else if (kind === 'leaf') this.maple(ctx, p.s * 0.8)\n      else if (kind === 'petal') this.blossom(ctx, p.s * 0.62)")
fs.writeFileSync(p, s)
console.log('maple', s.includes('blossom(ctx'), s.includes("kind === 'leaf' ? 26"))
