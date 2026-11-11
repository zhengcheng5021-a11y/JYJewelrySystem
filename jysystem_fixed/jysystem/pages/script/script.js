const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const ui = require('../../utils/ui')

const PAGE_SIZE = 20
const CATEGORIES = ['全部', '接待开场', '产品介绍', '异议处理', '成交推进', '售后维护', '其他']

function decorateScript(item) {
  const sortNo = Number(item.sortNo)
  return Object.assign({}, item, {
    scriptType: item.scriptType || '其他',
    sortNo: sortNo >= 1 && sortNo <= 5 ? sortNo : 3,
    stars: Array.from({ length: 5 }, (_, index) => index < (sortNo >= 1 && sortNo <= 5 ? sortNo : 3)),
    createTimeText: item.createTime ? String(item.createTime).replace('T', ' ').slice(0, 16) : ''
  })
}

const EMPTY_FORM = { id: 0, title: '', scriptType: '接待开场', content: '', sortNo: 3 }

Page({
  data: {
    theme: {},
    categories: CATEGORIES,
    starOptions: [1, 2, 3, 4, 5],
    category: '全部',
    keyword: '',
    allRows: [],
    rows: [],
    pageNum: 1,
    totalCount: 0,
    hasMore: false,
    loadingMore: false,
    loading: false,
    error: '',
    drawer: false,
    copiedId: 0,
    saving: false,
    formError: '',
    editing: false,
    form: Object.assign({}, EMPTY_FORM)
  },
  onShow() {
    themeUtil.apply(this)
    if (session.requireLogin()) this.load()
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
  onKeyword(e) { this.setData({ keyword: e.detail.value }) },
  setCategory(e) {
    const category = e.currentTarget.dataset.category
    this.setData({ category, rows: this.filterRows(this.data.allRows, category) })
  },
  filterRows(rows, category) {
    return category === '全部' ? rows : rows.filter((item) => item.scriptType === category)
  },
  async load() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const data = await api.scriptList({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.keyword, status: 0 })
      const allRows = list.rows(data).map(decorateScript)
      const total = list.total(data) || allRows.length
      this.setData({
        allRows,
        rows: this.filterRows(allRows, this.data.category),
        pageNum: 1,
        totalCount: total,
        hasMore: allRows.length < total && allRows.length >= PAGE_SIZE
      })
    } catch (error) {
      this.setData({ error: error.message || '话术加载失败' })
    } finally {
      this.setData({ loading: false })
    }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.scriptList({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.keyword, status: 0 })
      const nextRows = list.rows(data).map(decorateScript)
      const allRows = this.data.allRows.concat(nextRows)
      const total = list.total(data) || allRows.length
      this.setData({
        allRows,
        rows: this.filterRows(allRows, this.data.category),
        pageNum: nextPage,
        totalCount: total,
        hasMore: nextRows.length >= PAGE_SIZE && allRows.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ loadingMore: false }) }
  },
  openDrawer() { this.setData({ drawer: true, editing: false, formError: '', form: Object.assign({}, EMPTY_FORM) }) },
  closeDrawer() { this.setData({ drawer: false }) },
  noop() {},
  onForm(e) { this.setData({ ['form.' + e.currentTarget.dataset.field]: e.detail.value }) },
  setFormType(e) { this.setData({ 'form.scriptType': e.currentTarget.dataset.type }) },
  setStars(e) {
    const sortNo = Number(e.currentTarget.dataset.stars)
    if (sortNo >= 1 && sortNo <= 5) this.setData({ 'form.sortNo': sortNo })
  },
  editScript(e) {
    const id = e.currentTarget.dataset.id
    const row = this.data.allRows.find((item) => String(item.id) === String(id))
    if (!row) return ui.toast('这条话术已不存在，请刷新')
    this.setData({
      drawer: true,
      editing: true,
      formError: '',
      form: { id: row.id, title: row.title || '', scriptType: row.scriptType || '其他', content: row.content || '', sortNo: row.sortNo || 3 }
    })
  },
  copy(e) {
    const id = e.currentTarget.dataset.id
    const row = this.data.allRows.find((item) => String(item.id) === String(id))
    if (!row || !row.content) return ui.toast('这条话术没有内容')
    wx.setClipboardData({ data: row.content, success: () => { this.setData({ copiedId: id }); ui.toast('话术已复制', 'success') } })
  },
  async save() {
    const title = String(this.data.form.title || '').trim()
    const content = String(this.data.form.content || '').trim()
    if (!title) return this.setData({ formError: '请填写话术标题' })
    if (!content) return this.setData({ formError: '请填写话术内容' })
    this.setData({ saving: true, formError: '' })
    try {
      await api.saveScript({ id: this.data.form.id || 0, title, content, scriptType: this.data.form.scriptType, status: 0, sortNo: this.data.form.sortNo || 3 })
      ui.toast(this.data.editing ? '话术已更新' : '话术已保存', 'success')
      this.setData({ drawer: false, editing: false, form: Object.assign({}, EMPTY_FORM) })
      await this.load()
    } catch (error) { this.setData({ formError: error.message || '保存失败' }) }
    finally { this.setData({ saving: false }) }
  },
  async removeScript(e) {
    const id = e.currentTarget.dataset.id
    const row = this.data.allRows.find((item) => String(item.id) === String(id))
    if (!row) return
    try {
      await ui.confirm('删除话术', '确认删除「' + (row.title || '未命名话术') + '」？删除后不可恢复。', '删除')
      await api.deleteScript(row.id)
      ui.toast('话术已删除', 'success')
      this.setData({
        allRows: this.data.allRows.filter((item) => String(item.id) !== String(row.id)),
        rows: this.data.rows.filter((item) => String(item.id) !== String(row.id)),
        totalCount: Math.max(0, Number(this.data.totalCount || 0) - 1)
      })
    } catch (error) {
      console.log(error)
      if (error && error.message === 'cancelled') return
      ui.toast(error.message || '删除失败')
    }
  },
  onTheme() { themeUtil.apply(this) }
})
