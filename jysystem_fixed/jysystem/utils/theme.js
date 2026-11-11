const palette = require('./palette')

const KEY = 'jingyue_theme'
const listeners = new Set()

// Chinese season key -> palette id (kept so old callers keep working).
const SEASON = { spring: 'blush', summer: 'celadon', autumn: 'blush', winter: 'dark' }

function current() {
  const raw = wx.getStorageSync(KEY) || palette.DEFAULT_ID
  return palette.decorate(palette.get(raw))
}

function apply(page) {
  const theme = current()
  if (page && page.setData) page.setData({ theme })
  try {
    wx.setNavigationBarColor({
      frontColor: theme.navFront,
      backgroundColor: theme.navBg,
      animation: { duration: 280, timingFunc: 'easeIn' }
    })
    wx.setBackgroundColor({ backgroundColor: theme.vars.navBg, backgroundColorTop: theme.vars.navBg, backgroundColorBottom: theme.vars.paper })
  } catch (error) {}
  return theme
}

function use(id, page) {
  const key = palette.normalize(id)
  wx.setStorageSync(KEY, key)
  const theme = apply(page)
  listeners.forEach((listener) => {
    try { listener(theme) } catch (error) { console.error('theme listener failed', error) }
  })
  return theme
}

function subscribe(listener) {
  if (typeof listener !== 'function') return function () {}
  listeners.add(listener)
  return function () { listeners.delete(listener) }
}

function seasons() { return list() }

function list() { return palette.list().map((item) => palette.decorate(item)) }

module.exports = {
  THEMES: palette.THEMES,
  SEASON,
  current,
  apply,
  use,
  subscribe,
  list,
  seasons
}