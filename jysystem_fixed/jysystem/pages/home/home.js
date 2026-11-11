const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const navigation = require('../../utils/navigation')

Page({
  data: { today: '', userName: '', saleAmount: '--', saleOrders: '0', draftCount: '--', inboundCost: '--', stockQty: '--', stockSale: '--', todoCount: '--', overdueCount: '0', error: '', goalPercent: '--', goalDone: '--', goalLeft: '--', goalWidth: '0%', dailyDone: '0', dailyTotal: '0' },
  onShow() {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    const user = session.getUser() || {}
    this.setData({ today: format.todayText(), userName: user.nickName || user.userName || '同事' })
    this.load()
  },
  async onPullDownRefresh() {
    await this.load()
    wx.stopPullDownRefresh()
  },
  go(e) { navigation.open(e.currentTarget.dataset.url) },
  tab(e) { navigation.tab(e.currentTarget.dataset.tab) },
  async load() {
    const errors = []
    const next = {}
    const jobs = [
      api.saleDashboard().then((data) => { next.saleAmount = format.money(data && data.todayAmount); next.saleOrders = String((data && data.todayOrders) || 0) }).catch((e) => errors.push(e.message)),
      api.inboundDashboard().then((data) => { next.draftCount = String((data && data.draftCount) || 0); next.inboundCost = format.money(data && data.todayCost) }).catch((e) => errors.push(e.message)),
      api.stockSummary().then((data) => { next.stockQty = String((data && data.totalQty) || 0); next.stockSale = format.money(data && data.stockSale) }).catch((e) => errors.push(e.message)),
      api.workStats().then((data) => { next.todoCount = String((data && data.todayCount) || 0); next.overdueCount = String((data && data.overdueCount) || 0) }).catch((e) => errors.push(e.message)),
      api.salesGoals({}).then((data) => {
        const percent = Number((data && data.percent) || 0)
        next.goalPercent = percent + '%'
        next.goalWidth = Math.max(0, Math.min(100, percent)) + '%'
        next.goalDone = format.money(data && data.doneAmount)
        next.goalLeft = format.money(data && data.remainingAmount)
      }).catch((e) => errors.push(e.message)),
      api.dailyList({}).then((data) => {
        const summary = (data && data.summary) || {}
        const items = list.rows(data)
        const done = summary.done === undefined ? items.filter((item) => Number(item.isCompleted) === 1).length : summary.done
        const total = summary.total === undefined ? items.length : summary.total
        next.dailyTotal = String(total)
        next.dailyDone = String(done)
      }).catch((e) => errors.push(e.message))
    ]
    await Promise.all(jobs)
    next.error = errors.filter(Boolean).slice(0, 2).join('；')
    this.setData(next)
  },
  onTheme() { themeUtil.apply(this) }
})
