const session = require('../../utils/session')
const themeUtil = require('../../utils/theme')
const palette = require('../../utils/palette')
const auth = require('../../utils/auth')

const REMEMBER_KEY = 'jingyue_login_remember'

// The login page shares the exact palette of the inner pages.
const THEMES = palette.THEMES

const AGREEMENTS = {
  user: {
    title: '用户服务协议',
    body: '欢迎来到静悦（JY手作珠宝）平台。\n\n1. 手工甄选与孤品特性：平台所有沙金、纯银、古法金、老檀木及晶石手串均为独立匠人手作打造，天然材质微小纹理与雕刻细节均属正常东方手工美学范畴。\n\n2. 真品保证与证书：所有贵金属及高价值宝石配饰均经过国家级检测机构认证。\n\n3. 定制与养护：部分手围或手工编绳可享受终身免费养护与换线服务。'
  },
  privacy: {
    title: '隐私保护政策',
    body: '我们深知个人信息对您的重要性。\n\n1. 我们仅在您使用账号登录时，收集必要的联系方式与收货地址，用于专属珠宝订单鉴赏与安全发货服务。\n\n2. 严格采用银行级高强度加密技术守护您的个人数据与专属定制记录，绝不向任何第三方透露。'
  }
}

function currentLoginTheme() {
  let stored = ''
  try { stored = wx.getStorageSync('jingyue_theme') || '' } catch (error) {}
  return palette.decorate(palette.get(stored || palette.DEFAULT_ID))
}

function getLoginBarHeights() {
  let info = {}
  try { info = wx.getSystemInfoSync() } catch (error) {}
  const statusBarHeight = Number(info.statusBarHeight || 20)
  let menu = null
  try { menu = wx.getMenuButtonBoundingClientRect ? wx.getMenuButtonBoundingClientRect() : null } catch (error) {}
  let navHeight = 44
  let capsuleSpace = 88
  if (menu && menu.height && menu.top >= statusBarHeight) {
    navHeight = Math.max(40, Math.min(60, (menu.top - statusBarHeight) * 2 + menu.height))
    if (menu.left && info.windowWidth) capsuleSpace = Math.max(76, info.windowWidth - menu.left + 8)
  }
  // Align the theme capsule horizontally on the same bar as the native WeChat capsule.
  const navTop = statusBarHeight
  const topBarHeight = navTop + navHeight
  return { statusBarHeight, navHeight, capsuleSpace, navTop, topBarHeight }
}

Page({
  data: {
    theme: palette.decorate(palette.THEMES[palette.DEFAULT_ID]),
    themes: palette.list().map((item) => palette.decorate(item)),
    statusBarHeight: 20,
    navHeight: 44,
    capsuleSpace: 92,
    menuTop: 70,
    toastTop: 76,
    screenTier: 'regular',
    navTop: 20,
    username: '',
    password: '',
    passwordVisible: false,
    remember: true,
    agree: true,
    loading: false,
    menuOpen: false,
    modal: false,
    modalTitle: '',
    modalBody: '',
    toastShow: false,
    toastMessage: '',
    toastSuccess: true,
    petals: [],
    dust: [],
    ripples: [],
    burst: []
  },

  onLoad() {
    const config = session.getConfig()
    const user = session.getUser() || {}
    const remembered = wx.getStorageSync(REMEMBER_KEY)
    const hasExplicitRemember = remembered && typeof remembered === 'object' && remembered.remember !== undefined
    const remember = hasExplicitRemember ? Boolean(remembered.remember) : true
    const username = remember
      ? String((remembered && remembered.userName) || (!hasExplicitRemember ? user.userName : '') || '')
      : ''
    const theme = currentLoginTheme()
    this.config = config
    this.applyWindowMetrics()
    this.setData({
      theme,
      themes: palette.list().map((item) => palette.decorate(item)),
      username,
      remember
    })
    this.setNavigation(theme)
  },

  onReady() {
    this.seedAtmosphere()
  },

  onShow() {
    if (!this.petals || !this.petals.length) this.seedAtmosphere()
  },

  onHide() {},

  onUnload() {
    if (this._toastTimer) clearTimeout(this._toastTimer)
    if (this._loginTimer) clearTimeout(this._loginTimer)
    if (this._burstTimer) clearTimeout(this._burstTimer)
    if (this._rippleTimers) this._rippleTimers.forEach((timer) => clearTimeout(timer))
  },

  applyWindowMetrics() {
    const bars = getLoginBarHeights()
    let windowHeight = 812
    let windowWidth = 375
    try {
      const info = wx.getSystemInfoSync()
      windowHeight = Number(info.windowHeight) || windowHeight
      windowWidth = Number(info.windowWidth) || windowWidth
    } catch (error) {}
    const screenTier = windowWidth <= 340 || windowHeight <= 650
      ? 'compact'
      : (windowHeight >= 800 ? 'tall' : 'regular')
    this.setData({
      statusBarHeight: bars.statusBarHeight,
      navTop: bars.navTop,
      navHeight: bars.navHeight,
      capsuleSpace: bars.capsuleSpace,
      topBarHeight: bars.topBarHeight,
      screenTier,
      menuTop: bars.topBarHeight + 6,
      toastTop: bars.topBarHeight + 10
    })
  },

  setNavigation(theme) {
    try {
      wx.setNavigationBarColor({
        frontColor: theme.navFront,
        backgroundColor: theme.navBg,
        animation: { duration: 300, timingFunc: 'easeIn' }
      })
    } catch (error) {}
  },

  showToast(message, success) {
    if (this._toastTimer) clearTimeout(this._toastTimer)
    this.setData({
      toastShow: true,
      toastMessage: String(message || ''),
      toastSuccess: success !== false
    })
    this._toastTimer = setTimeout(() => {
      this.setData({ toastShow: false })
    }, 2400)
  },

  // Petals, stardust and ripples are real views so they always paint behind
  // the glass card (a native canvas sits above the page in some base libraries).
  seedAtmosphere() {
    const paletteColors = this.data.theme.petalColors || ['#F4A6B8']
    const petals = []
    for (let index = 0; index < 24; index += 1) {
      petals.push({
        id: 'p' + index,
        left: (Math.random() * 104 - 2).toFixed(2),
        size: (10 + Math.random() * 12).toFixed(1),
        duration: (9 + Math.random() * 10).toFixed(2),
        delay: (-Math.random() * 18).toFixed(2),
        sway: (18 + Math.random() * 34).toFixed(0),
        swayDuration: (2.6 + Math.random() * 2.6).toFixed(2),
        spin: (Math.random() > 0.5 ? 1 : -1),
        rotate: (Math.random() * 360).toFixed(0),
        alpha: (0.5 + Math.random() * 0.4).toFixed(2),
        color: paletteColors[Math.floor(Math.random() * paletteColors.length)]
      })
    }

    const dust = []
    for (let index = 0; index < 26; index += 1) {
      dust.push({
        id: 'd' + index,
        left: (Math.random() * 100).toFixed(2),
        top: (Math.random() * 100).toFixed(2),
        size: (2 + Math.random() * 3.4).toFixed(1),
        duration: (7 + Math.random() * 9).toFixed(2),
        delay: (-Math.random() * 12).toFixed(2),
        drift: (Math.random() * 26 - 13).toFixed(0),
        alpha: (0.25 + Math.random() * 0.5).toFixed(2)
      })
    }

    this.petals = petals
    this.setData({ petals, dust })
  },

  onPageTouch(e) {
    if (this.data.modal || this.data.menuOpen) return
    const touch = (e.touches && e.touches[0]) || (e.changedTouches && e.changedTouches[0])
    if (!touch) return
    const x = touch.clientX != null ? touch.clientX : (touch.pageX != null ? touch.pageX : touch.x)
    const y = touch.clientY != null ? touch.clientY : (touch.pageY != null ? touch.pageY : touch.y)
    if (typeof x !== 'number' || typeof y !== 'number') return
    let info = null
    try { info = wx.getSystemInfoSync() } catch (error) {}
    const winW = (info && info.windowWidth) || 375
    const winH = (info && info.windowHeight) || 667
    if (x < 0 || y < 0 || x > winW || y > winH) return
    this.addRipple((x / winW * 100).toFixed(2), (y / winH * 100).toFixed(2))
  },

  addRipple(left, top) {
    if (!this._rippleTimers) this._rippleTimers = []
    const id = 'r' + Date.now() + Math.round(Math.random() * 1000)
    const ripples = (this.data.ripples || []).concat([{ id, left, top }])
    this.setData({ ripples })
    const timer = setTimeout(() => {
      this.setData({ ripples: (this.data.ripples || []).filter((item) => item.id !== id) })
    }, 1100)
    this._rippleTimers.push(timer)
  },

  pulseSparkles() {
    const paletteColors = this.data.theme.petalColors || ['#F4A6B8']
    const burst = []
    for (let index = 0; index < 8; index += 1) {
      burst.push({
        id: 's' + Date.now() + index,
        angle: (index * 45 + Math.random() * 22).toFixed(0),
        distance: (36 + Math.random() * 60).toFixed(0),
        size: (8 + Math.random() * 8).toFixed(1),
        color: paletteColors[Math.floor(Math.random() * paletteColors.length)]
      })
    }
    this.setData({ burst })
    if (this._burstTimer) clearTimeout(this._burstTimer)
    this._burstTimer = setTimeout(() => this.setData({ burst: [] }), 1000)
    this.showToast('触碰东方灵韵 · 晶石生辉')
  },

  toggleMenu() {
    this.setData({ menuOpen: !this.data.menuOpen })
  },

  closeMenu() {
    this.setData({ menuOpen: false })
  },

  noop() {},

  chooseTheme(e) {
    const id = e.currentTarget.dataset.id
    if (!THEMES[id]) return
    const theme = palette.decorate(THEMES[id])
    themeUtil.use(theme.id)
    this.setData({ theme, menuOpen: false })
    this.setNavigation(theme)
    this.seedAtmosphere()
    this.showToast('已切换至：' + theme.label)
  },

  onUser(e) {
    this.setData({ username: e.detail.value })
  },

  onPass(e) {
    this.setData({ password: e.detail.value })
  },

  clearUser() {
    this.setData({ username: '' })
  },

  togglePassword() {
    this.setData({ passwordVisible: !this.data.passwordVisible })
  },

  toggleRemember() {
    const remember = !this.data.remember
    this.setData({ remember })
    if (!remember) {
      wx.setStorageSync(REMEMBER_KEY, { userName: '', remember: false })
    }
  },

  toggleAgree() {
    this.setData({ agree: !this.data.agree })
  },

  openModal(e) {
    const type = e.currentTarget.dataset.type
    const item = AGREEMENTS[type] || AGREEMENTS.user
    this.setData({ modal: true, modalTitle: item.title, modalBody: item.body })
  },

  closeModal() {
    this.setData({ modal: false })
  },

  forgot() {
    this.showToast('请联系客服微信或重置密码')
  },

  register() {
    this.showToast('新用户注册请联系门店管理员开设账号')
  },

  guest() {
    this.showToast('请先登录后再进入工作台')
  },

  async submitLogin() {
    if (this.data.loading) return
    if (!this.data.agree) {
      this.showToast('请先勾选同意用户与隐私政策', false)
      return
    }
    const user = String(this.data.username || '').trim()
    const pass = String(this.data.password || '')
    if (!user) {
      this.showToast('请输入您的账号或手机号', false)
      return
    }
    if (!pass) {
      this.showToast('请输入您的登录密码', false)
      return
    }
    this.setData({ loading: true })
    try {
      await auth.login(user, pass, this.config)
      if (this.data.remember) wx.setStorageSync(REMEMBER_KEY, { userName: user, remember: true })
      else wx.setStorageSync(REMEMBER_KEY, { userName: '', remember: false })
      this.showToast('欢迎归来，' + user + '！登录成功')
      this._loginTimer = setTimeout(() => {
        this.setData({ loading: false })
        wx.reLaunch({ url: '/pages/home/home' })
      }, 420)
    } catch (error) {
      this.setData({ loading: false })
      this.showToast(error.message || '登录失败', false)
    }
  }
})
