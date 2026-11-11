const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const bridge = require('../../utils/bridge')

const PAGE_SIZE = 20
const PRIORITY = { 4: '紧急', 3: '重要', 2: '普通', 1: '稍后' }

function isTaskDone(item) {
  return Number(item && item.status) === 1 || Number(item && item.isCompleted) === 1 || Boolean(item && item.completed)
}

function decorateTask(item) {
  const done = isTaskDone(item)
  return Object.assign({}, item, {
    done,
    priorityText: PRIORITY[Number(item.priority)] || item.priorityName || '普通',
    dueText: item.dueTime ? format.dateText(item.dueTime) : (item.isOverdue ? '已逾期' : '今日内')
  })
}

Page({
  data: {
    theme: {},
    view: 'today',
    rows: [],
    pageNum: 1,
    totalCount: 0,
    hasMore: false,
    loadingMore: false,
    title: '',
    priority: 3,
    openCount: 0,
    overdueCount: 0,
    doneCount: 0,
    loading: false,
    saving: false,
    error: ''
  },
  onShow() {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    this.setData({ view: bridge.preference('taskView', 'today'), priority: Number(bridge.preference('taskPriority', 3)) })
    this.load()
    this.consumeFollowTask()
  },
  async onPullDownRefresh() {
    await this.load()
    wx.stopPullDownRefresh()
  },
  onReachBottom() {
    if (this.data.hasMore && !this.data.loading && !this.data.loadingMore) {
      this.loadMore()
    }
  },
  scrollTop() { wx.pageScrollTo({ scrollTop: 0, duration: 260 }) },
  onTitle(e) { this.setData({ title: e.detail.value }) },
  setView(e) {
    const view = e.currentTarget.dataset.view
    bridge.setPreference('taskView', view)
    this.setData({ view })
    this.load()
  },
  setPriority(e) {
    const priority = Number(e.currentTarget.dataset.priority)
    bridge.setPreference('taskPriority', priority)
    this.setData({ priority })
  },
  async load() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const [stats, data] = await Promise.all([
        api.workStats().catch(() => null),
        api.workItems({ pageNum: 1, pageSize: PAGE_SIZE, view: this.data.view })
      ])
      const rows = list.rows(data).map(decorateTask)
      const total = list.total(data) || rows.length
      const openCount = stats && stats.todayCount !== undefined ? Number(stats.todayCount) : rows.filter((item) => !item.done).length
      const overdueCount = stats && stats.overdueCount !== undefined ? Number(stats.overdueCount) : 0
      const doneCount = rows.filter((item) => item.done).length
      this.setData({
        rows,
        pageNum: 1,
        totalCount: total,
        hasMore: rows.length < total && rows.length >= PAGE_SIZE,
        openCount,
        overdueCount,
        doneCount
      })
    } catch (error) { this.setData({ error: error.message || '加载失败' }) }
    finally { this.setData({ loading: false }) }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.workItems({ pageNum: nextPage, pageSize: PAGE_SIZE, view: this.data.view })
      const nextRows = list.rows(data).map(decorateTask)
      const merged = this.data.rows.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        rows: merged,
        pageNum: nextPage,
        totalCount: total,
        doneCount: merged.filter((item) => item.done).length,
        hasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ loadingMore: false }) }
  },
  async add() {
    if (!this.data.title.trim()) return ui.toast('请填写待办')
    this.setData({ saving: true, error: '' })
    try {
      await api.saveWorkItem({ id: 0, title: this.data.title.trim(), itemKind: 1, periodType: 3, priority: this.data.priority, dueTime: format.todayText() + ' 23:59:59' })
      this.setData({ title: '' })
      this.load()
      ui.toast('已加入今日待办', 'success')
    } catch (error) { this.setData({ error: error.message || '保存失败' }) }
    finally { this.setData({ saving: false }) }
  },
  async toggle(e) {
    const done = Number(e.currentTarget.dataset.done) === 1
    try { await api.completeWorkItem(e.currentTarget.dataset.id, !done); this.load() }
    catch (error) { ui.toast(error.message || '更新失败') }
  },
  async consumeFollowTask() {
    const item = bridge.takeFollowTask()
    if (!item || !item.title) return
    try {
      await api.saveWorkItem({ id: 0, title: item.title, itemKind: 1, periodType: 3, priority: 3, dueTime: format.todayText() + ' 23:59:59', remark: item.remark || '' })
      ui.toast('客户跟进已转入今日待办', 'success')
      this.load()
    } catch (error) { ui.toast(error.message || '转入待办失败') }
  },
  onTheme() { themeUtil.apply(this) }
})
