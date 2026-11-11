const { DEFAULT_API_BASE, DEFAULT_TENANT_ID } = require('./config')

const CONFIG_KEY = 'jingyue_api_config'
const TOKEN_KEY = 'jingyue_token'
const USER_KEY = 'jingyue_user'
const PERMISSIONS_KEY = 'jingyue_permissions'

function normalizeBase(value) {
  const raw = String(value || DEFAULT_API_BASE).trim().replace(/\/+$/, '')
  if (!raw) return DEFAULT_API_BASE
  if (!/^https?:\/\//i.test(raw)) return 'http://' + raw
  return raw
}

function getConfig() {
  const stored = wx.getStorageSync(CONFIG_KEY) || {}
  return {
    apiBase: normalizeBase(stored.apiBase || DEFAULT_API_BASE),
    tenantId: String(stored.tenantId !== undefined && stored.tenantId !== '' ? stored.tenantId : (DEFAULT_TENANT_ID || '0'))
  }
}

function setConfig(config) {
  const next = {
    apiBase: normalizeBase((config && config.apiBase) || DEFAULT_API_BASE),
    tenantId: String((config && config.tenantId !== undefined && config.tenantId !== '') ? config.tenantId : (DEFAULT_TENANT_ID || '0'))
  }
  wx.setStorageSync(CONFIG_KEY, next)
  return next
}

function resetConfig() {
  wx.removeStorageSync(CONFIG_KEY)
  return getConfig()
}

function getToken() { return wx.getStorageSync(TOKEN_KEY) || '' }
function setToken(token) { wx.setStorageSync(TOKEN_KEY, token || '') }
function getUser() { return wx.getStorageSync(USER_KEY) || null }
function setUser(user) { wx.setStorageSync(USER_KEY, user || null) }
function getPermissions() { return wx.getStorageSync(PERMISSIONS_KEY) || [] }
function setPermissions(permissions) { wx.setStorageSync(PERMISSIONS_KEY, Array.isArray(permissions) ? permissions : []) }
function can(permission) {
  const permissions = getPermissions()
  return permissions.indexOf('*:*:*') >= 0 || permissions.indexOf(permission) >= 0
}
function clearSession() {
  wx.removeStorageSync(TOKEN_KEY)
  wx.removeStorageSync(USER_KEY)
  wx.removeStorageSync(PERMISSIONS_KEY)
}
function requireLogin() {
  if (getToken()) return true
  wx.reLaunch({ url: '/pages/login/login' })
  return false
}

module.exports = {
  getConfig,
  setConfig,
  resetConfig,
  getToken,
  setToken,
  getUser,
  setUser,
  getPermissions,
  setPermissions,
  can,
  clearSession,
  requireLogin
}
