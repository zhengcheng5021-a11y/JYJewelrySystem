const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
s = s.replace("const count = this.data.theme.kind === 'snow' ? 28 : 14", "const count = this.data.theme.kind === 'snow' ? 9 : 14")
fs.writeFileSync(p, s)
console.log('ok')
