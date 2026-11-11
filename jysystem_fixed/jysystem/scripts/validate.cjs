const fs = require('fs')
const path = require('path')
const cp = require('child_process')
const root = path.resolve(__dirname, '..')
const errors = []

function walk(dir, files = []) {
  if (!fs.existsSync(dir)) return files
  for (const item of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, item.name)
    if (item.name === 'node_modules') continue
    if (item.isDirectory()) walk(full, files)
    else files.push(full)
  }
  return files
}

for (const file of walk(root).filter((file) => file.endsWith('.json'))) {
  try { JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, '')) }
  catch (error) { errors.push(file + ': invalid JSON - ' + error.message) }
}
for (const file of walk(root).filter((file) => file.endsWith('.js') || file.endsWith('.cjs'))) {
  const result = cp.spawnSync(process.execPath, ['--check', file], { encoding: 'utf8' })
  if (result.status !== 0) errors.push(file + ': invalid JS - ' + (result.stderr || result.stdout))
}
const app = JSON.parse(fs.readFileSync(path.join(root, 'app.json'), 'utf8').replace(/^\uFEFF/, ''))
for (const page of app.pages || []) {
  for (const ext of ['.js', '.json', '.wxml', '.wxss']) {
    if (!fs.existsSync(path.join(root, page + ext))) errors.push('Missing page file: ' + page + ext)
  }
}
for (const file of walk(path.join(root, 'components')).filter((file) => file.endsWith('.json'))) {
  const config = JSON.parse(fs.readFileSync(file, 'utf8').replace(/^\uFEFF/, ''))
  if (config.usingComponents) {
    for (const [tag, ref] of Object.entries(config.usingComponents)) {
      const base = path.resolve(path.dirname(file), ref)
      if (!fs.existsSync(base + '.js')) errors.push(file + ': missing component ' + tag)
    }
  }
}

const appPages = new Set(app.pages || [])
for (const file of walk(root).filter((file) => file.endsWith('.wxml'))) {
  const source = fs.readFileSync(file, 'utf8')
  const scriptFile = file.replace(/\.wxml$/, '.js')
  const script = fs.existsSync(scriptFile) ? fs.readFileSync(scriptFile, 'utf8') : ''

  for (const match of source.matchAll(/\b(?:bind|catch):?[a-zA-Z]+\s*=\s*"([a-zA-Z_$][\w$]*)"/g)) {
    const handler = match[1]
    if (!new RegExp('(?:^|[\\s,{])' + handler + '\\s*\\(', 'm').test(script)) {
      errors.push(file + ': missing event handler ' + handler)
    }
  }

  for (const match of source.matchAll(/data-(?:url|tab)\s*=\s*"(\/pages\/[^"?]+)/g)) {
    const route = match[1].replace(/^\//, '')
    if (!appPages.has(route)) errors.push(file + ': unknown page route /' + route)
  }
}
if (errors.length) {
  console.error(errors.join('\n'))
  process.exit(1)
}
console.log('PASS: JSON, JavaScript syntax, app pages, and component references')
