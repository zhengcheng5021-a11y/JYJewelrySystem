const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const bridge = require('../../utils/bridge')
const ui = require('../../utils/ui')
const navigation = require('../../utils/navigation')

const PAGE_SIZE = 20

// 与后端 JyGoodsItem.Status 保持一致，避免把 2/4/5/7/8 显示成“未知”。
function stockStatusText(status) {
  return {
    1: '在库可售',
    2: '定制定金锁定',
    3: '已售出',
    4: '赠送出库',
    5: '盘亏',
    6: '已停用',
    7: '入库中',
    8: '返厂出库'
  }[Number(status)] || '未知状态'
}

function decorateStock(item) {
  return Object.assign({}, item, {
    goodsName: item.styleName || item.goodsName || '未命名货品',
    initial: String(item.styleName || item.goodsName || '货').slice(0, 1),
    saleText: format.money(item.salePrice),
    costText: format.money(item.costPrice),
    statusText: stockStatusText(item.status)
  })
}

function decorateLedger(item) {
  return Object.assign({}, item, {
    changeText: (Number(item.changeQty) > 0 ? '+' : '') + item.changeQty,
    bizTypeText: item.bizTypeText || ledgerTypeText(item.bizType, item.changeType, item.changeQty),
    timeText: format.dateTimeText(item.bizTime)
  })
}

function ledgerTypeText(bizType, changeType, changeQty) {
  const map = {
    inbound: '采购入库', purchase: '采购入库', sale: '销售出库', gift: '赠送出库',
    sale_return: '销售退货', factory_return: '返厂出库', sale_void: '销售作废',
    custom: '定制出库', inventory: '库存盘点', check: '库存盘点',
    outbound: '其他出库', transfer: '库存调拨'
  }
  const type = String(bizType || '').toLowerCase()
  if (map[type]) return map[type]
  const byChangeType = { 1: '采购入库', 2: '销售出库', 5: '销售退货', 6: '赠送出库', 9: '销售作废', 10: '库存盘点', 11: '返厂出库' }
  return byChangeType[Number(changeType)] || (Number(changeQty) >= 0 ? '入库' : '出库')
}

Page({
  data: {
    theme: {},
    mode: 'stock',
    keyword: '',
    ledgerKeyword: '',
    status: '',
    rows: [],
    pageNum: 1,
    totalCount: 0,
    hasMore: false,
    loadingMore: false,
    ledgerRows: [],
    ledgerPage: 1,
    ledgerTotal: 0,
    ledgerHasMore: false,
    ledgerLoadingMore: false,
    loading: false,
    ledgerLoading: false,
    error: '',
    showCost: false,
    detailOpen: false,
    detail: null,
    detailRows: [],
    detailPage: 1,
    detailTotal: 0,
    detailHasMore: false,
    detailLoading: false,
    detailLoadingMore: false,
    detailError: '',
    summary: { onShelfCount: '--', totalQty: '--', saleText: '--', costText: '--' }
  },
  onShow() {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    this.setData({ showCost: !!bridge.preference('showCost', false), mode: bridge.preference('stockMode', 'stock') })
    this.load()
  },
  async onPullDownRefresh() {
    await this.load()
    wx.stopPullDownRefresh()
  },
  onReachBottom() {
    if (this.data.detailOpen) {
      if (this.data.detailHasMore && !this.data.detailLoading && !this.data.detailLoadingMore) this.loadMoreDetail()
      return
    }
    if (this.data.mode === 'stock') {
      if (this.data.hasMore && !this.data.loading && !this.data.loadingMore) this.loadMore()
    } else {
      if (this.data.ledgerHasMore && !this.data.ledgerLoading && !this.data.ledgerLoadingMore) this.loadMoreLedger()
    }
  },
  scrollTop() { wx.pageScrollTo({ scrollTop: 0, duration: 260 }) },
  onKeyword(e) { this.setData({ keyword: e.detail.value }) },
  clearKeyword() { this.setData({ keyword: '' }); this.search() },
  onLedgerKeyword(e) { this.setData({ ledgerKeyword: e.detail.value }) },
  setMode(e) {
    const mode = e.currentTarget.dataset.mode
    bridge.setPreference('stockMode', mode)
    this.setData({ mode })
    if (mode === 'ledger' && !this.data.ledgerRows.length) this.loadLedger()
  },
  setStatus(e) { this.setData({ status: e.currentTarget.dataset.status }); this.search() },
  toggleCost() {
    const showCost = !this.data.showCost
    bridge.setPreference('showCost', showCost)
    this.setData({ showCost })
  },
  async load() {
    const tasks = [this.loadSummary(), this.search()]
    if (this.data.mode === 'ledger') tasks.push(this.loadLedger())
    await Promise.all(tasks)
  },
  async loadSummary() {
    try {
      const data = await api.stockSummary()
      this.setData({ summary: { onShelfCount: String(data.onShelfCount || 0), totalQty: String(data.totalQty || 0), saleText: format.money(data.stockSale), costText: format.money(data.stockCost) } })
    } catch (error) { this.setData({ error: error.message }) }
  },
  async search() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const data = await api.stockList({
        pageNum: 1,
        pageSize: PAGE_SIZE,
        keyword: this.data.keyword,
        status: this.data.status === '' ? undefined : Number(this.data.status)
      })
      const rows = list.rows(data).map(decorateStock)
      const total = list.total(data) || rows.length
      this.setData({
        rows,
        pageNum: 1,
        totalCount: total,
        hasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '查询失败' }) }
    finally { this.setData({ loading: false }) }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.stockList({
        pageNum: nextPage,
        pageSize: PAGE_SIZE,
        keyword: this.data.keyword,
        status: this.data.status === '' ? undefined : Number(this.data.status)
      })
      const nextRows = list.rows(data).map(decorateStock)
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
  async loadLedger() {
    this.setData({ ledgerLoading: true, error: '', ledgerPage: 1 })
    try {
      const data = await api.stockLedger({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.ledgerKeyword })
      const ledgerRows = list.rows(data).map(decorateLedger)
      const total = list.total(data) || ledgerRows.length
      this.setData({
        ledgerRows,
        ledgerPage: 1,
        ledgerTotal: total,
        ledgerHasMore: ledgerRows.length < total && ledgerRows.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '流水加载失败' }) }
    finally { this.setData({ ledgerLoading: false }) }
  },
  async loadMoreLedger() {
    if (this.data.ledgerLoadingMore || !this.data.ledgerHasMore) return
    const nextPage = this.data.ledgerPage + 1
    this.setData({ ledgerLoadingMore: true })
    try {
      const data = await api.stockLedger({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.ledgerKeyword })
      const nextRows = list.rows(data).map(decorateLedger)
      const merged = this.data.ledgerRows.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        ledgerRows: merged,
        ledgerPage: nextPage,
        ledgerTotal: total,
        ledgerHasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多流水失败') }
    finally { this.setData({ ledgerLoadingMore: false }) }
  },
  openDetail(e) {
    const index = Number(e.currentTarget.dataset.index)
    const row = this.data.rows[index]
    if (!row) return
    this.setData({
      detailOpen: true,
      detail: row,
      detailRows: [],
      detailPage: 1,
      detailTotal: 0,
      detailHasMore: false,
      detailError: ''
    })
    this.loadDetailLedger()
  },
  closeDetail() { this.setData({ detailOpen: false, detail: null, detailRows: [], detailError: '' }) },
  noop() {},
  async loadDetailLedger() {
    if (!this.data.detail) return
    this.setData({ detailLoading: true, detailError: '', detailPage: 1 })
    try {
      const data = await api.stockLedger({ pageNum: 1, pageSize: PAGE_SIZE, goodsItemId: this.data.detail.id })
      const rows = list.rows(data).map(decorateLedger)
      const total = list.total(data) || rows.length
      this.setData({
        detailRows: rows,
        detailPage: 1,
        detailTotal: total,
        detailHasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) {
      this.setData({ detailError: error.message || '货品流水加载失败' })
    } finally {
      this.setData({ detailLoading: false })
    }
  },
  async loadMoreDetail() {
    if (!this.data.detail || this.data.detailLoadingMore || !this.data.detailHasMore) return
    const nextPage = this.data.detailPage + 1
    this.setData({ detailLoadingMore: true })
    try {
      const data = await api.stockLedger({ pageNum: nextPage, pageSize: PAGE_SIZE, goodsItemId: this.data.detail.id })
      const nextRows = list.rows(data).map(decorateLedger)
      const merged = this.data.detailRows.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        detailRows: merged,
        detailPage: nextPage,
        detailTotal: total,
        detailHasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多流水失败') }
    finally { this.setData({ detailLoadingMore: false }) }
  },
  sendToSale(e) {
    const row = this.data.rows[e.currentTarget.dataset.index]
    if (!row) return
    bridge.putGoods({ goodsItemId: row.id, goodsName: row.goodsName, goodsNo: row.goodsNo || '', styleItemName: row.styleItemName || '', salePrice: Number(row.salePrice || 0) })
    navigation.open('/pages/sale/sale')
    ui.toast('已带入本单：' + row.goodsName)
  },
  onTheme() { themeUtil.apply(this) }
})
