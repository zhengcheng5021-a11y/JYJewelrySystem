const session = require('./session')
const { REQUEST_TIMEOUT, UPLOAD_TIMEOUT } = require('./config')

function appendQuery(rawPath, params) {
  const base = String(rawPath || '').charAt(0) === '/' ? String(rawPath) : '/' + String(rawPath || '')
  if (!params || typeof params !== 'object' || Array.isArray(params)) return base
  const pairs = Object.keys(params)
    .filter((key) => params[key] !== undefined && params[key] !== null && params[key] !== '')
    .map((key) => encodeURIComponent(key) + '=' + encodeURIComponent(String(params[key])))
  if (!pairs.length) return base
  return base + (base.indexOf('?') >= 0 ? '&' : '?') + pairs.join('&')
}

function buildUrl(path) {
  const config = session.getConfig()
  if (!config.apiBase) throw new Error('请先配置服务端地址')
  return config.apiBase + (String(path).charAt(0) === '/' ? path : '/' + path)
}

function responseMessage(body, fallback) {
  if (!body) return fallback
  if (typeof body === 'string') return body.slice(0, 120) || fallback
  return body.msg || body.message || body.error || fallback
}

function formatNetError(err, fallback) {
  const msg = (err && err.errMsg) || ''
  if (msg.indexOf('url not in domain list') >= 0) {
    return '域名未加入白名单，请在开发工具开启「不校验合法域名」或配置服务器域名'
  }
  if (msg.indexOf('timeout') >= 0) {
    return '连接后台服务器超时，请检查网络或服务端状态'
  }
  return msg || fallback || '网络连接失败'
}

function handleResponse(res, resolve, reject) {
  let body = res.data
  if (typeof body === 'string') {
    try { body = JSON.parse(body) } catch (e) {}
  }
  if (res.statusCode === 401 || (body && body.code === 401)) {
    session.clearSession()
    wx.reLaunch({ url: '/pages/login/login' })
    reject(new Error('登录已过期，请重新登录'))
    return
  }
  if (res.statusCode === 403 || (body && body.code === 403)) {
    reject(new Error('当前账号没有这项权限'))
    return
  }
  if (res.statusCode < 200 || res.statusCode >= 300) {
    reject(new Error(responseMessage(body, '服务异常（' + res.statusCode + '）')))
    return
  }
  if (body && body.code !== undefined && body.code !== 200) {
    reject(new Error(responseMessage(body, '请求未完成')))
    return
  }
  resolve(body && body.data !== undefined ? body.data : body)
}

function request(options) {
  const config = session.getConfig()
  const token = session.getToken()
  const user = session.getUser() || {}
  const method = String((options && options.method) || 'GET').toUpperCase()
  const isQueryMethod = method === 'GET' || method === 'DELETE' || method === 'HEAD'
  const resolvedPath = isQueryMethod
    ? appendQuery(options.url, method === 'DELETE' ? null : options.data)
    : (String(options.url).charAt(0) === '/' ? options.url : '/' + options.url)

  const headers = Object.assign({
    'Content-Type': 'application/json;charset=utf-8',
    tenantId: config.tenantId
  }, options.header || {})

  if (token) {
    headers.Authorization = 'Bearer ' + token
    headers.userid = user.userId || ''
    headers.userName = encodeURIComponent(user.userName || '')
  }

  return new Promise((resolve, reject) => {
    wx.request({
      url: buildUrl(resolvedPath),
      method,
      data: isQueryMethod ? undefined : options.data,
      header: headers,
      timeout: options.timeout || REQUEST_TIMEOUT || 20000,
      success: (res) => handleResponse(res, resolve, reject),
      fail: (err) => reject(new Error(formatNetError(err, '网络连接失败')))
    })
  })
}

function uploadFile(filePath, options) {
  const config = session.getConfig()
  const token = session.getToken()
  const user = session.getUser() || {}
  const extra = (options && options.formData) || {}
  const headers = {
    Authorization: token ? 'Bearer ' + token : '',
    tenantId: config.tenantId,
    userid: user.userId || '',
    userName: encodeURIComponent(user.userName || '')
  }
  const formData = Object.assign({
    FileNameType: '3',
    FileDir: 'jingyue/mobile',
    Quality: '80',
    ClassifyType: (options && options.classifyType) || 'jingyue'
  }, extra)

  return new Promise((resolve, reject) => {
    wx.uploadFile({
      url: buildUrl('/Common/UploadFile'),
      filePath,
      name: 'file',
      timeout: (options && options.timeout) || UPLOAD_TIMEOUT || 60000,
      header: headers,
      formData,
      success(res) {
        let body = res.data
        try { body = JSON.parse(body) } catch (e) {}
        if (res.statusCode === 401 || (body && body.code === 401)) {
          session.clearSession()
          wx.reLaunch({ url: '/pages/login/login' })
          reject(new Error('登录已过期，请重新登录'))
          return
        }
        if (res.statusCode < 200 || res.statusCode >= 300 || (body && body.code !== undefined && body.code !== 200)) {
          reject(new Error(responseMessage(body, '上传失败')))
          return
        }
        resolve(body && body.data !== undefined ? body.data : body)
      },
      fail(err) {
        reject(new Error(formatNetError(err, '图片上传失败')))
      }
    })
  })
}

module.exports = { request, uploadFile, buildUrl, appendQuery }
