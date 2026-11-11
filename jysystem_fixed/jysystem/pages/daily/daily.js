const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const ui = require('../../utils/ui')

Page({
  data: {
    theme: {},
    filter: 'all',
    rows: [],
    allRows: [],
    done: 0,
    total: 0,
    percent: 0,
    progressWidth: '0%',
    dateText: '',
    loading: false,
    error: '',
    updatingId: 0
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
      const data = await api.dailyList({}) || {}
      const summary = data.summary || {}
      const allRows = list.rows(data).map((item, index) => Object.assign({}, item, {
        number: index + 1,
        completed: Number(item.isCompleted) === 1,
        contentText: item.content || '今日门店标准动作',
        completedText: item.completedTime ? format.dateTimeText(item.completedTime) : ''
      }))
      const done = Number(summary.done === undefined ? allRows.filter((item) => item.completed).length : summary.done)
      const total = Number(summary.total === undefined ? allRows.length : summary.total)
      const percent = Number(summary.percent === undefined ? (total ? Math.round(done * 100 / total) : 0) : summary.percent)
      this.setData({
        allRows,
        rows: this.filterRows(allRows, this.data.filter),
        done,
        total,
        percent,
        progressWidth: Math.max(0, Math.min(100, percent)) + '%',
        dateText: format.dateText(data.date)
      })
    } catch (error) {
      this.setData({ error: error.message || '加载失败' })
    } finally {
      this.setData({ loading: false })
    }
  },
  filterRows(rows, filter) {
    if (filter === 'todo') return rows.filter((item) => !item.completed)
    if (filter === 'done') return rows.filter((item) => item.completed)
    return rows
  },
  setFilter(e) {
    const filter = e.currentTarget.dataset.filter
    this.setData({ filter, rows: this.filterRows(this.data.allRows, filter) })
  },
  async toggle(e) {
    const id = e.currentTarget.dataset.id
    if (this.data.updatingId) return
    const row = this.data.allRows.find((item) => String(item.id) === String(id))
    if (!row) return
    this.setData({ updatingId: id })
    try {
      await api.dailyComplete(id, !row.completed)
      ui.toast(row.completed ? '已取消打卡' : '今日打卡完成', 'success')
      await this.load()
    } catch (error) {
      ui.toast(error.message || '更新失败')
    } finally {
      this.setData({ updatingId: 0 })
    }
  },
  onTheme() { themeUtil.apply(this) }
})
