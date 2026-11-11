const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
s = s.replace("name: '夏至 · 荷风', dot: '#3E8F78', accent: '#1F6B56', kind: 'lotus'", "name: '夏至 · 柳叶', dot: '#3E8F78', accent: '#1F6B56', kind: 'willow'")
s = s.replace("petals: ['#F7C5D4','#FFF6F8','#E989A4','#F3B7C8']", "petals: ['#7FA86A','#A9C48A','#5E8F52','#C5D6A4']")
const willow = `  willow(ctx, s) {
    ctx.beginPath()
    ctx.moveTo(0, -s)
    ctx.bezierCurveTo(s * 0.42, -s * 0.45, s * 0.28, s * 0.45, 0, s)
    ctx.bezierCurveTo(-s * 0.28, s * 0.45, -s * 0.18, -s * 0.45, 0, -s)
    ctx.fill()
    ctx.strokeStyle = 'rgba(40,70,30,.35)'
    ctx.lineWidth = 0.8
    ctx.beginPath()
    ctx.moveTo(0, -s * 0.82)
    ctx.lineTo(0, s * 0.82)
    ctx.stroke()
  },
`
if (!s.includes('willow(ctx, s)')) s = s.replace('  lotus(ctx, s) {', willow + '  lotus(ctx, s) {')
s = s.replace("s: kind === 'snow'", "s: kind === 'willow' ? 18 + Math.random() * 16 : kind === 'snow'")
s = s.replace("vy: kind === 'snow'", "vy: kind === 'willow' ? 0.22 + Math.random() * 0.35 : kind === 'snow'")
s = s.replace("if (kind === 'lotus' && p.form > 0.72) this.pad(ctx, p.s * 0.9)\n      else if (kind === 'lotus') this.lotus(ctx, p.s * 0.55)\n      else if", "if (kind === 'willow') this.willow(ctx, p.s * 0.85)\n      else if")
fs.writeFileSync(p, s)
console.log('willow', s.includes("kind: 'willow'"), s.includes('this.willow'))
