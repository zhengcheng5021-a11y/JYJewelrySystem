const themeUtil = require('../../utils/theme')
const navigation = require('../../utils/navigation')

Component({
  data: {
    theme: {},
    statusBarHeight: 20,
    navHeight: 44,
    capsuleSpace: 88,
    goingBack: false
  },
  properties: {
    title: { type: String, value: '今日' },
    back: { type: Boolean, value: false },
    action: { type: String, value: '' },
    fallback: { type: String, value: '/pages/home/home' }
  },
  lifetimes: {
    attached() {
      this.refresh()
      this.unsubscribe = themeUtil.subscribe((theme) => this.setData({ theme }))
    },
    detached() {
      if (this.unsubscribe) this.unsubscribe()
      if (this._backTimer) clearTimeout(this._backTimer)
    }
  },
  pageLifetimes: {
    show() { this.refresh() }
  },
  methods: {
    refresh() {
      const system = wx.getSystemInfoSync()
      const menu = wx.getMenuButtonBoundingClientRect ? wx.getMenuButtonBoundingClientRect() : null
      const statusBarHeight = Number(system.statusBarHeight || 20)
      const navHeight = menu && menu.height ? Math.max(42, (menu.top - statusBarHeight) * 2 + menu.height) : 44
      const capsuleSpace = menu && menu.left ? Math.max(76, system.windowWidth - menu.left + 8) : 88
      this.setData({ theme: themeUtil.current(), statusBarHeight, navHeight, capsuleSpace, goingBack: false })
    },
    goBack() {
      if (this.data.goingBack) return
      this.setData({ goingBack: true })
      if (this._backTimer) clearTimeout(this._backTimer)
      this._backTimer = setTimeout(() => this.setData({ goingBack: false }), 600)
      navigation.back(this.data.fallback)
    },
    onAction() {
      this.triggerEvent('action')
    }
  }
})
