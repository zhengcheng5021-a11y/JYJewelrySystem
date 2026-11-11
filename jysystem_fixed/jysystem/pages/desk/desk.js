const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const navigation = require('../../utils/navigation')

const EMPTY_BADGES = {
  saleOrders: '0',
  inboundDrafts: '0',
  stockQty: '0',
  customCount: '0',
  memberCount: '0',
  followCount: '0',
  taskCount: '0',
  dailyDone: '0/0'
}

const TAB_ROUTES = new Set([
  '/pages/home/home',
  '/pages/inbound/inbound',
  '/pages/members/members',
  '/pages/tasks/tasks',
  '/pages/profile/profile'
])

function makeGroups(badges) {
  return [
    { name: 'Ⅰ · 柜面交易与货品流转', items: [
      { name: '现场开单', icon: '✦', badge: badges.saleOrders + ' 今日单', desc: '识客挑货与预览收银', path: '/pages/sale/sale' },
      { name: '拍照入库', icon: '📷', badge: badges.inboundDrafts + ' 草稿', desc: '连拍建件、智能命名', path: '/pages/inbound/edit' },
      { name: '库存台账', icon: '❖', badge: badges.stockQty + ' 件在库', desc: '成本显隐与出入库流水', path: '/pages/stock/stock' },
      { name: '定制预定', icon: '◈', badge: badges.customCount + ' 定制', desc: '工坊定金与到货签收', path: '/pages/custom/custom' }
    ] },
    { name: 'Ⅱ · 客户私域与顾问赋能', items: [
      { name: '客户档案', icon: '♛', badge: badges.memberCount + ' 位客户', desc: '抽屉建档与一键开单', path: '/pages/members/members', tab: true },
      { name: '客户跟进', icon: '❀', badge: badges.followCount + ' 条记录', desc: '时间轴与一键转待办', path: '/pages/follow/follow' },
      { name: '销冠话术', icon: '❝', badge: '销售锦囊', desc: '多场景话术一键复制', path: '/pages/script/script' },
      { name: '月度目标', icon: '◎', badge: '目标罗盘', desc: '达成率与日均冲刺测算', path: '/pages/goal/goal' }
    ] },
    { name: 'Ⅲ · 店务执行与目标罗盘', items: [
      { name: '今日待办', icon: '⚡', badge: badges.taskCount + ' 项待办', desc: '要事优先与随手销项', path: '/pages/tasks/tasks', tab: true },
      { name: '每日必做', icon: '✓', badge: badges.dailyDone + ' 已打卡', desc: '门店标准动作逐项打卡', path: '/pages/daily/daily' },
      { name: '采买入库', icon: '❖', badge: badges.inboundDrafts + ' 待审', desc: '入库草稿审核与确认', path: '/pages/inbound/inbound', tab: true },
      { name: '顾问身份', icon: '❀', badge: '账号中心', desc: '主题、服务地址与登录状态', path: '/pages/profile/profile', tab: true }
    ] }
  ]
}

Page({
  data: { theme: {}, badges: EMPTY_BADGES, groups: makeGroups(EMPTY_BADGES), loading: false, error: '' },
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
    const errors = []
    const badges = Object.assign({}, EMPTY_BADGES)
    const jobs = [
      api.saleDashboard().then((data) => { badges.saleOrders = String((data && data.todayOrders) || 0) }).catch((error) => errors.push(error.message)),
      api.inboundDashboard().then((data) => { badges.inboundDrafts = String((data && data.draftCount) || 0) }).catch((error) => errors.push(error.message)),
      api.stockSummary().then((data) => { badges.stockQty = String((data && data.totalQty) || 0) }).catch((error) => errors.push(error.message)),
      api.customList({ pageNum: 1, pageSize: 1 }).then((data) => { badges.customCount = String(list.total(data)) }).catch((error) => errors.push(error.message)),
      api.memberList({ pageNum: 1, pageSize: 1, status: 0 }).then((data) => { badges.memberCount = String(list.total(data)) }).catch((error) => errors.push(error.message)),
      api.followList({ pageNum: 1, pageSize: 1 }).then((data) => { badges.followCount = String(list.total(data)) }).catch((error) => errors.push(error.message)),
      api.workStats().then((data) => { badges.taskCount = String((data && data.todayCount) || 0) }).catch((error) => errors.push(error.message)),
      api.dailyList({}).then((data) => {
        const summary = (data && data.summary) || {}
        const items = list.rows(data)
        const done = summary.done === undefined ? items.filter((item) => Number(item.isCompleted) === 1).length : summary.done
        const total = summary.total === undefined ? items.length : summary.total
        badges.dailyDone = String(done) + '/' + String(total)
      }).catch((error) => errors.push(error.message))
    ]
    await Promise.all(jobs)
    this.setData({ badges, groups: makeGroups(badges), error: errors.filter(Boolean).slice(0, 2).join('；'), loading: false })
  },
  open(e) {
    const path = e.currentTarget.dataset.path
    if (!path) return
    if (TAB_ROUTES.has(path)) navigation.tab(path)
    else navigation.open(path)
  },
  onTheme() { themeUtil.apply(this) }
})
