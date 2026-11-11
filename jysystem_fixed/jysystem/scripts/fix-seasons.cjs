const fs = require('fs')
const p = 'scripts/seasons.cjs'
let s = fs.readFileSync(p, 'utf8')
const i = s.lastIndexOf('console.log')
s = s.slice(0, i) + "console.log('written')\n"
fs.writeFileSync(p, s)
