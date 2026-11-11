const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
s = s.replace("kind === 'snow' ? 7 + Math.random() * 8", "kind === 'snow' ? 12 + Math.random() * 8")
s = s.replace("else if (kind === 'snow') this.flake(ctx, p.s * 0.7)", "else if (kind === 'snow') this.flake(ctx, p.s * 0.95)")
fs.writeFileSync(p, s)
console.log('ok')
