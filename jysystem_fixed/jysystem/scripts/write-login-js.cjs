const fs=require('fs')
const svg=fs.readFileSync('scripts/logo-svg.min.txt','utf8').replace(/"/g,'\\"').replace(/\n/g,'')
const js=`const session = require('../../utils/session')
const auth = require('../../utils/auth')

const LOGO = '${svg}'

const THEMES = {
  blush: {
    id: 'blush',
    label: '画境 · 绯粉',
    name: '绯粉春霖 · 桃夭',
    dot: '#E5A8A2',
    accent: '#99554D',
    screenBg: 'background:linear-gradient(180deg,#FDF5F2,#F8E8E4,#F1DAD4);color:#3A2624;',
    textColor: '#3A2624',
    subTextColor: '#553634',
    cardBg: 'rgba(255,255,255,.55)',
    cardBorder: 'rgba(255,255,255,.7)',
    cardGlow: 'radial-gradient(circle, rgba(244,166,184,.35), transparent 70%)',
    headBorder: 'rgba(0,0,0,.06)',
    seal: '#99554D',
    inputBg: 'rgba(255,255,255,.85)',
    inputBorder: 'rgba(216,156,146,.3)',
    inputColor: '#2A2022',
    inputPlaceholder: '#A89495',
    buttonGradient: 'linear-gradient(90deg,#3C2827,#5A3835,#3C2827)',
    buttonTextColor: '#FDF7F2',
    buttonColor: '#523533',
    linkColor: '#5A3835',
    capsuleBg: 'rgba(0,0,0,.1)',
    capsuleBorder: 'rgba(0,0,0,.15)',
    capsuleColor: '#332324',
    orbGradient: 'radial-gradient(circle, rgba(244,166,184,.4) 0%, rgba(223,176,122,.2) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(244,166,184,.55), rgba(223,176,122,.18), transparent 70%)',
    indicator: 'rgba(60,40,39,.3)',
    mountainColor: 'rgba(216,156,146,.35)',
    petalColors: ['#F4A6B8','#FBC1CD','#E88B9E','#FBECEB'],
    dustColor: '#DFB07A',
    rippleColor: '#D9A392'
  },
  dark: {
    id: 'dark',
    label: '画境 · 玄黛',
    name: '玄黛金辉 · 沉香',
    dot: '#2B2326',
    accent: '#DFB07A',
    screenBg: 'background:linear-gradient(180deg,#1D1618,#2A1F22,#181113);color:#FBE9DC;',
    textColor: '#FBE9DC',
    subTextColor: '#D8BEAA',
    cardBg: 'rgba(35,27,30,.65)',
    cardBorder: 'rgba(223,176,122,.25)',
    cardGlow: 'radial-gradient(circle, rgba(223,176,122,.28), transparent 70%)',
    headBorder: 'rgba(223,176,122,.12)',
    seal: '#DFB07A',
    inputBg: 'rgba(20,15,17,.7)',
    inputBorder: 'rgba(223,176,122,.25)',
    inputColor: '#FDF5F2',
    inputPlaceholder: '#877074',
    buttonGradient: 'linear-gradient(90deg,#DFB07A,#C79558,#DFB07A)',
    buttonTextColor: '#2B1B15',
    buttonColor: '#DFB07A',
    linkColor: '#DFB07A',
    capsuleBg: 'rgba(255,255,255,.12)',
    capsuleBorder: 'rgba(255,255,255,.2)',
    capsuleColor: '#F5E4CE',
    orbGradient: 'radial-gradient(circle, rgba(223,176,122,.35) 0%, rgba(200,120,100,.15) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(223,176,122,.42), rgba(200,120,100,.16), transparent 70%)',
    indicator: 'rgba(255,255,255,.4)',
    mountainColor: 'rgba(223,176,122,.15)',
    petalColors: ['#DFB07A','#FBE6D0','#D9A392','#EED5C8'],
    dustColor: '#FFE3B5',
    rippleColor: '#DFB07A'
  },
  celadon: {
    id: 'celadon',
    label: '画境 · 天青',
    name: '烟雨天青 · 润玉',
    dot: '#7CA296',
    accent: '#243831',
    screenBg: 'background:linear-gradient(180deg,#EFF6F3,#E3EEE9,#D4E4DC);color:#21352E;',
    textColor: '#21352E',
    subTextColor: '#3E564E',
    cardBg: 'rgba(255,255,255,.6)',
    cardBorder: 'rgba(164,196,184,.5)',
    cardGlow: 'radial-gradient(circle, rgba(168,212,198,.38), transparent 70%)',
    headBorder: 'rgba(0,0,0,.05)',
    seal: '#243831',
    inputBg: 'rgba(255,255,255,.85)',
    inputBorder: 'rgba(144,180,168,.35)',
    inputColor: '#182B24',
    inputPlaceholder: '#7A968D',
    buttonGradient: 'linear-gradient(90deg,#243831,#3B564C,#243831)',
    buttonTextColor: '#F0F7F4',
    buttonColor: '#243831',
    linkColor: '#243831',
    capsuleBg: 'rgba(0,0,0,.08)',
    capsuleBorder: 'rgba(0,0,0,.12)',
    capsuleColor: '#21352E',
    orbGradient: 'radial-gradient(circle, rgba(168,212,198,.4) 0%, rgba(216,190,140,.2) 60%, transparent 100%)',
    logoGlow: 'radial-gradient(circle, rgba(168,212,198,.5), rgba(216,190,140,.18), transparent 70%)',
    indicator: 'rgba(33,53,46,.3)',
    mountainColor: 'rgba(124,162,150,.35)',
    petalColors: ['#C8E3D8','#F5ECEB','#9BBCAE','#E4F0EB'],
    dustColor: '#A7C7B9',
    rippleColor: '#7CA296'
  }
}

const AGREEMENTS = {
  user: {
    title: '静悦手作珠宝 · 用户服务协议',
    body: '欢迎来到静悦（JY手作珠宝）平台。\\n\\n1. 手工甄选与孤品特性：平台所有沙金、纯银、古法金、老檀木及晶石手串均为独立匠人手作打造，天然材质微小纹理与雕刻细节均属正常东方手工美学范畴。\\n\\n2. 真品保证与证书：所有贵金属及高价值宝石配饰均经过国家级检测机构认证。\\n\\n3. 定制与养护：部分手围或手工编绳可享受终身免费养护与换线服务。'
  },
  privacy: {
    title: '静悦手作珠宝 · 隐私保护政策',
    body: '我们深知个人信息对您的重要性。\\n\\n1. 我们仅在您使用账号登录时，收集必要的联系方式与收货地址，用于专属珠宝订单鉴赏与安全发货服务。\\n\\n2. 严格采用银行级高强度加密技术守护您的个人数据与专属定制记录，绝不向任何第三方透露。'
  }
}

Page({
  data: {
    theme: THEMES.blush,
    themes: Object.values(THEMES),
    logoNode: [{ name: 'div', attrs: { class: 'logo-wrap' }, children: [{ type: 'rich-text', attrs: { nodes: LOGO } }] }],
    username: '',
    password: '',
    passwordVisible: false,
    remember: true,
    agree: true,
    loading: false,
    menuOpen: false,
    modal: false,
    modalTitle: '',
    modalBody: ''
  },
  onLoad() {
    const config = session.getConfig()
    const user = session.getUser() || {}
    this.setData({
      username: user.userName || '',
      remember: !!user.userName
    })
    this.config = config
    this.initCanvas()
  },
  onUnload() { this.stopped = true },
  toggleMenu() { this.setData({ menuOpen: !this.data.menuOpen }) },
  closeMenu() { this.setData({ menuOpen: false }) },
  chooseTheme(e) {
    const id = e.currentTarget.dataset.id
    const theme = THEMES[id]
    if (!theme) return
    this.setData({ theme, menuOpen: false })
    wx.setNavigationBarColor({ frontColor: id === 'dark' ? '#ffffff' : '#000000', backgroundColor: id === 'dark' ? '#1D1618' : (id === 'celadon' ? '#EFF6F3' : '#FDF5F2'), animation: { duration: 300, timingFunc: 'easeIn' } })
    wx.showToast({ title: '已切换至：' + theme.label, icon: 'none' })
    this.resetParticles()
  },
  onUser(e) { this.setData({ username: e.detail.value }) },
  onPass(e) { this.setData({ password: e.detail.value }) },
  clearUser() { this.setData({ username: '' }) },
  togglePassword() { this.setData({ passwordVisible: !this.data.passwordVisible }) },
  toggleRemember() { this.setData({ remember: !this.data.remember }) },
  toggleAgree() { this.setData({ agree: !this.data.agree }) },
  openModal(e) {
    const type = e.currentTarget.dataset.type
    const item = AGREEMENTS[type]
    this.setData({ modal: true, modalTitle: item.title, modalBody: item.body })
  },
  closeModal() { this.setData({ modal: false }) },
  forgot() { wx.showToast({ title: '请联系客服微信或重置密码', icon: 'none' }) },
  register() { wx.showToast({ title: '请联系门店管理员开设账号', icon: 'none' }) },
  guest() { wx.showToast({ title: '请先登录后再进入工作台', icon: 'none' }) },
  async submitLogin() {
    if (this.data.loading) return
    if (!this.data.agree) return wx.showToast({ title: '请先勾选同意用户与隐私政策', icon: 'none' })
    const user = String(this.data.username || '').trim()
    const pass = String(this.data.password || '').trim()
    if (!user) return wx.showToast({ title: '请输入您的账号或手机号', icon: 'none' })
    if (!pass) return wx.showToast({ title: '请输入您的登录密码', icon: 'none' })
    this.setData({ loading: true })
    try {
      await auth.login(user, pass, this.config)
      wx.switchTab({ url: '/pages/home/home' })
    } catch (error) {
      wx.showToast({ title: error.message || '登录失败', icon: 'none' })
    } finally {
      this.setData({ loading: false })
    }
  }
})
`
fs.writeFileSync('pages/login/login.js', js)
console.log('login js', js.length)
