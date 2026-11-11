const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const auth = require('../../utils/auth')
const ui = require('../../utils/ui')

Page({
  data: {
    theme: {},
    nickName: '静悦主理人',
    userName: '',
    tenantId: '0',
    apiBase: '',
    editApiBase: '',
    editTenantId: '0',
    apiModalVisible: false,
    permissionText: '门店顾问权限',
    initial: '静',
    seasons: [],
    loggingOut: false,
    logoutConfirmVisible: false
  },
  onShow() {
    themeUtil.apply(this)
    this.setData({ loggingOut: false, logoutConfirmVisible: false, apiModalVisible: false })
    this.fill()
  },
  fill() {
    if (!session.requireLogin()) return
    const user = session.getUser() || {}
    const config = session.getConfig()
    const permissions = session.getPermissions()
    const nickName = user.nickName || user.userName || '静悦主理人'
    this.setData({
      theme: themeUtil.current(),
      seasons: themeUtil.list(),
      nickName,
      initial: String(nickName).slice(0, 1),
      userName: user.userName || '',
      tenantId: config.tenantId,
      apiBase: config.apiBase,
      permissionText: permissions.indexOf('*:*:*') >= 0 ? '全域掌柜权限 (*:*:*)' : ('已授权 ' + permissions.length + ' 项门店权限')
    })
  },
  chooseSeason(e) {
    const theme = themeUtil.use(e.currentTarget.dataset.id, this)
    this.setData({ theme })
    ui.toast('已切换至 ' + theme.short, 'success')
  },
  copyBase() {
    if (!this.data.apiBase) return
    wx.setClipboardData({ data: this.data.apiBase, success: () => ui.toast('服务端地址已复制', 'success') })
  },
  openApiModal() {
    this.setData({
      apiModalVisible: true,
      editApiBase: this.data.apiBase,
      editTenantId: this.data.tenantId || '0'
    })
  },
  closeApiModal() {
    this.setData({ apiModalVisible: false })
  },
  onEditApiBase(e) {
    this.setData({ editApiBase: e.detail.value })
  },
  onEditTenantId(e) {
    this.setData({ editTenantId: e.detail.value })
  },
  saveApiConfig() {
    const next = session.setConfig({
      apiBase: this.data.editApiBase,
      tenantId: this.data.editTenantId
    })
    const app = getApp && getApp()
    if (app && app.globalData) app.globalData.apiBase = next.apiBase
    this.setData({
      apiBase: next.apiBase,
      tenantId: next.tenantId,
      apiModalVisible: false
    })
    ui.toast('已更新直连服务器地址', 'success')
  },
  resetApiConfig() {
    const next = session.resetConfig()
    const app = getApp && getApp()
    if (app && app.globalData) app.globalData.apiBase = next.apiBase
    this.setData({
      apiBase: next.apiBase,
      tenantId: next.tenantId,
      apiModalVisible: false
    })
    ui.toast('已恢复默认服务器地址', 'success')
  },
  noop() {},
  logout() {
    if (this.data.loggingOut) return
    this.setData({ logoutConfirmVisible: true })
  },
  cancelLogout() {
    if (this.data.loggingOut) return
    this.setData({ logoutConfirmVisible: false })
  },
  confirmLogout() {
    if (this.data.loggingOut) return
    this.setData({ loggingOut: true, logoutConfirmVisible: false })
    auth.logout((ok) => {
      if (ok) return
      this.setData({ loggingOut: false })
      ui.toast('登录页未能打开，请重试', 'none')
    })
  },
  onTheme() { themeUtil.apply(this); this.fill() }
})
