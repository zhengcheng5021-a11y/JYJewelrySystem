const fs = require('fs')
let s = fs.readFileSync('pages/login/login.wxss', 'utf8')
s = s.replace(
  '.zen-canvas { position: absolute; left: 0; top: 0; width: 100%; height: 100%; z-index: 0; }',
  '.zen-canvas { position: absolute; left: 0; top: 0; width: 100vw; height: 100vh; z-index: 1; }\n@keyframes drift { 50% { transform: scale(1.08); opacity: .72; } }\n.orb { animation: drift 8s ease-in-out infinite; }'
)
fs.writeFileSync('pages/login/login.wxss', s)
const j = fs.readFileSync('pages/login/login.js', 'utf8')
console.log('snow', j.indexOf("kind: 'snow'") > 0, 'draw', j.indexOf('drawFrame') > 0)
