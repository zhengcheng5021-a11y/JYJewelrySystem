const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const navigation = require('../../utils/navigation')

const PAGE_SIZE = 15

function decorateRow(item) {
  return Object.assign({}, item, {
    costText: format.money(item.totalCost),
    dateText: format.dateText(item.inboundTime)
  })
}

Page({
  data: {
    keyword: '',
    status: '',
    rows: [],
    pageNum: 1,
    totalCount: 0,
    hasMore: false,
    loading: false,
    loadingMore: false,
    error: '',
    dashboard: { draftCount: 0, todayCostText: '0.00' }
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
  noop() {},
  onKeyword(e) { this.setData({ keyword: e.detail.value }) },
  clearKeyword() { this.setData({ keyword: '' }); this.search() },
  setStatus(e) { this.setData({ status: e.currentTarget.dataset.status }); this.search() },
  create() { navigation.open('/pages/inbound/edit') },
  open(e) { navigation.open('/pages/inbound/edit', { id: e.currentTarget.dataset.id }) },
  scrollTop() { wx.pageScrollTo({ scrollTop: 0, duration: 260 }) },
  async load() { await Promise.all([this.loadDashboard(), this.search()]) },
  async loadDashboard() {
    try {
      const data = await api.inboundDashboard()
      this.setData({ dashboard: { draftCount: Number(data.draftCount || 0), todayCostText: format.money(data.todayCost) } })
    } catch (error) { this.setData({ error: error.message || '入库概览加载失败' }) }
  },
  async search() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const data = await api.inboundList({
        pageNum: 1,
        pageSize: PAGE_SIZE,
        keyword: this.data.keyword,
        status: this.data.status === '' ? undefined : Number(this.data.status)
      })
      const fetched = list.rows(data).map(decorateRow)
      const total = list.total(data) || fetched.length
      this.setData({
        rows: fetched,
        pageNum: 1,
        totalCount: total,
        hasMore: fetched.length < total && fetched.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '加载失败' }) }
    finally { this.setData({ loading: false }) }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.inboundList({
        pageNum: nextPage,
        pageSize: PAGE_SIZE,
        keyword: this.data.keyword,
        status: this.data.status === '' ? undefined : Number(this.data.status)
      })
      const nextRows = list.rows(data).map(decorateRow)
      const merged = this.data.rows.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        rows: merged,
        pageNum: nextPage,
        totalCount: total,
        hasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ loadingMore: false }) }
  },
  async confirmOne(e) {
    const id = e.currentTarget.dataset.id
    try { await ui.confirm('确认入库', '确认后货品进入库存，这张单据不能再改。', '确认入库') } catch (error) { return }
    try { await api.confirmInbound(id); ui.toast('已确认入库', 'success'); this.load() }
    catch (error) { ui.toast(error.message || '确认失败') }
  },
  async remove(e) {
    const id = e.currentTarget.dataset.id
    try { await ui.confirm('删除草稿', '只删除未确认的入库草稿，不影响库存。', '删除') } catch (error) { return }
    try { await api.deleteInbound(id); ui.toast('草稿已删除', 'success'); this.load() }
    catch (error) { ui.toast(error.message || '删除失败') }
  },
  onTheme() { themeUtil.apply(this) }
})
