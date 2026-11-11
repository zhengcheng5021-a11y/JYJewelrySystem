const api = require('./api')
const session = require('./session')
const md5 = require('./md5')
const { toast } = require('./ui')

function toSafeText(value) { return value === undefined || value === null ? '' : String(value) }

async function finishLogin(token, config) {
  if (!token) throw new Error('登录成功但未返回访问凭证')
  session.setConfig(config)
  session.setToken(token)
  const info = await api.getInfo()
  const user = (info && info.user) || {}
  user.userId = toSafeText(user.userId || user.id)
  user.userName = toSafeText(user.userName)
  user.nickName = toSafeText(user.nickName || user.userName)
  session.setUser(user)
  session.setPermissions((info && info.permissions) || [])
  return info
}

async function login(username, password, config) {
  const usernameText = String(username || '').trim()
  if (!usernameText) throw new Error('请输入账号')
  if (!password) throw new Error('请输入密码')
  const body = {
    username: usernameText,
    password: md5(password),
    code: '',
    uuid: '',
    clientId: 'jingyue-mini-program',
    tenantId: String((config && config.tenantId) || '0')
  }
  session.setConfig(config)
  const token = await api.login(body)
  return finishLogin(token, config)
}

async function restore() {
  if (!session.getToken()) return false
  try {
    const info = await api.getInfo()
    const user = (info && info.user) || session.getUser() || {}
    user.userId = toSafeText(user.userId || user.id)
    user.userName = toSafeText(user.userName)
    user.nickName = toSafeText(user.nickName || user.userName)
    session.setUser(user)
    session.setPermissions((info && info.permissions) || [])
    return true
  } catch (error) {
    if (session.getToken()) toast(error.message || '无法刷新账号信息')
    return false
  }
}

const LOGIN_ROUTE = 'pages/login/login'

function currentRoute() {
  const pages = getCurrentPages()
  return pages.length ? pages[pages.length - 1].route : ''
}

function goLogin(onDone) {
  if (currentRoute() === LOGIN_ROUTE) {
    if (typeof onDone === 'function') onDone(true)
    return
  }

  // Let the native confirmation modal close before replacing the page stack.
  setTimeout(() => {
    wx.reLaunch({
      url: '/' + LOGIN_ROUTE,
      success() { if (typeof onDone === 'function') onDone(true) },
      fail() {
        wx.redirectTo({
          url: '/' + LOGIN_ROUTE,
          success() { if (typeof onDone === 'function') onDone(true) },
          fail() { if (typeof onDone === 'function') onDone(false) }
        })
      }
    })
  }, 200)
}

function logout(onDone) {
  // Do not call the network logout endpoint here. Its 401/error redirect can
  // race this navigation, especially when the request goes through cloud proxy.
  // Removing the local token is sufficient to invalidate this device session.
  session.clearSession()
  goLogin(onDone)
}

module.exports = { login, restore, logout, goLogin }
