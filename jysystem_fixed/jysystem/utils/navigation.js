function pageUrl(path, params) {
  const query = Object.keys(params || {})
    .filter((key) => params[key] !== undefined && params[key] !== null && params[key] !== '')
    .map((key) => encodeURIComponent(key) + '=' + encodeURIComponent(String(params[key])))
    .join('&')
  return '/' + String(path).replace(/^\/+/, '') + (query ? '?' + query : '')
}

function open(path, params) {
  const url = pageUrl(path, params)
  const pages = getCurrentPages()
  const current = pages.length ? '/' + pages[pages.length - 1].route : ''
  if (url.split('?')[0] === current) return
  wx.navigateTo({
    url,
    fail() {
      wx.redirectTo({
        url,
        fail() { wx.reLaunch({ url }) }
      })
    }
  })
}

function redirect(path, params) {
  const url = pageUrl(path, params)
  wx.redirectTo({
    url,
    fail() { wx.reLaunch({ url }) }
  })
}

function tab(path, params) {
  const url = pageUrl(path, params)
  const pages = getCurrentPages()
  const current = pages.length ? '/' + pages[pages.length - 1].route : ''
  if (url.split('?')[0] === current && !queryHasParams(params)) return
  wx.reLaunch({ url })
}

function queryHasParams(params) {
  return params && Object.keys(params).some((key) => params[key] !== undefined && params[key] !== null && params[key] !== '')
}

function back(fallback) {
  const pages = getCurrentPages()
  if (pages.length > 1) {
    wx.navigateBack({
      delta: 1,
      fail() { wx.reLaunch({ url: fallback || '/pages/home/home' }) }
    })
    return
  }
  wx.reLaunch({ url: fallback || '/pages/home/home' })
}

function home() { tab('/pages/home/home') }

module.exports = { pageUrl, open, redirect, tab, back, home }
