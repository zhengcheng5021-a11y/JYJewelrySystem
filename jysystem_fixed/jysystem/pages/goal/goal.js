const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const format = require('../../utils/format')

Page({
  data: {
    theme: {},
    loading: false,
    error: '',
    month: '',
    targetText: '0.00',
    doneText: '0.00',
    percent: 0,
    progressWidth: '0%',
    todayText: '0.00',
    leftText: '0.00',
    needPerDayText: '0.00',
    daysLeft: 0,
    categories: []
  },
  onShow() {
    themeUtil.apply(this)
    if (session.requireLogin()) this.load()
  },
  async onPullDownRefresh() {
    await this.load()
    wx.stopPullDownRefresh()
  },
  async load() {
    this.setData({ loading: true, error: '' })
    try {
      const data = await api.salesGoals({}) || {}
      const percent = Number(data.percent || 0)
      const categories = (data.categories || []).map((item) => {
        const categoryPercent = Number(item.percent || 0)
        return Object.assign({}, item, {
          targetText: format.money(item.targetAmount),
          doneText: format.money(item.doneAmount),
          percent: categoryPercent,
          progressWidth: Math.max(0, Math.min(100, categoryPercent)) + '%',
          needText: format.money(item.needAmount)
        })
      })
      this.setData({
        month: data.month || '',
        targetText: format.money(data.monthTargetAmount),
        doneText: format.money(data.doneAmount),
        percent,
        progressWidth: Math.max(0, Math.min(100, percent)) + '%',
        todayText: format.money(data.todayAmount),
        leftText: format.money(data.remainingAmount),
        needPerDayText: format.money(data.needPerDay),
        daysLeft: Number(data.daysLeft || 0),
        categories
      })
    } catch (error) {
      this.setData({ error: error.message || '目标加载失败' })
    } finally {
      this.setData({ loading: false })
    }
  },
  onTheme() { themeUtil.apply(this) }
})
