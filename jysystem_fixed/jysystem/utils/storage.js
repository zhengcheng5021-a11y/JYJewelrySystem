function get(key, fallback) {
  try {
    const value = wx.getStorageSync(key)
    return value === '' || value === undefined || value === null ? fallback : value
  } catch (error) {
    return fallback
  }
}

function set(key, value) {
  try { wx.setStorageSync(key, value); return true } catch (error) { return false }
}

function remove(key) {
  try { wx.removeStorageSync(key); return true } catch (error) { return false }
}

function peek(key, fallback) { return get(key, fallback) }

function take(key, fallback) {
  const value = get(key, fallback)
  remove(key)
  return value
}

function clear(prefix) {
  try {
    const info = wx.getStorageInfoSync()
    ;(info.keys || []).filter((key) => !prefix || String(key).indexOf(prefix) === 0).forEach((key) => wx.removeStorageSync(key))
  } catch (error) {}
}

module.exports = { get, set, remove, peek, take, clear }
