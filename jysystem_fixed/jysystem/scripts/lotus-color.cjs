const fs = require('fs')
const p = 'pages/login/login.js'
let s = fs.readFileSync(p, 'utf8')
s = s.replace("petals: ['rgba(255,255,255,.75)','rgba(210,245,230,.8)','rgba(255,255,255,.45)']", "petals: ['#F7C5D4','#FFF6F8','#E989A4','#F3B7C8']")
fs.writeFileSync(p, s)
console.log('colors', s.includes('#E989A4'))
