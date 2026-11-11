const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const bridge = require('../../utils/bridge')
const navigation = require('../../utils/navigation')

const PAGE_SIZE = 20

function blankForm() {
  return {
    id: 0,
    memberId: null,
    customerName: '',
    customerPhone: '',
    customerSex: 0,
    goodsName: '',
    goodsItemId: null,
    goodsNo: '',
    barcode: '',
    styleItemId: '',
    styleItemIdText: '',
    materialItemId: '',
    materialItemIdText: '',
    metalItemId: '',
    metalItemIdText: '',
    weight: '',
    expectPrice: '',
    depositAmount: '',
    customDate: format.todayText(),
    pickupDate: '',
    remark: ''
  }
}

function progressOf(deposit, total) {
  const amount = Number(deposit || 0) + Number(total || 0)
  return amount > 0 ? Math.max(0, Math.min(100, Math.round(Number(deposit || 0) * 100 / amount))) : 0
}

function statusText(status) {
  return { 1: '已收定金', 2: '部分处理', 3: '已对冲完', 8: '已退清', 9: '已作废' }[Number(status)] || '待确认'
}

function decorateCustom(item) {
  const remain = Number(item.remainAmount || 0)
  const status = Number(item.status)
  const contactPending = Number(item.arrivalStatus) === 1 && remain > 0 && status !== 9
  return Object.assign({}, item, {
    arrived: Number(item.arrivalStatus) === 1,
    contactPending,
    arrivalText: contactPending ? '待联系客户' : (Number(item.arrivalStatus) === 1 ? '已联系/已交付' : '待工坊到货'),
    customerText: item.customerName || item.memberName || '未记客户',
    goodsText: item.goodsName || item.styleItemName || '定制货品',
    depositText: format.money(item.depositAmount),
    remainText: format.money(remain),
    progress: progressOf(item.depositAmount, remain),
    pickupText: item.pickupDate ? format.dateText(item.pickupDate) : '未约定',
    statusText: statusText(status),
    canEdit: [1, 2].indexOf(status) >= 0 && Number(item.offsetAmount || 0) === 0 && Number(item.refundAmount || 0) === 0,
    canRefund: [1, 2].indexOf(status) >= 0 && remain > 0,
    canDelete: Number(item.offsetAmount || 0) === 0 && remain <= 0 && status !== 9,
    metaText: [item.styleItemName, item.materialName, item.metalName].filter(Boolean).join(' · ') || '未填写属性'
  })
}

Page({
  data: {
    theme: {},
    filter: 'all',
    keyword: '',
    allRows: [],
    rows: [],
    pageNum: 1,
    hasMore: false,
    loadingMore: false,
    loading: false,
    error: '',
    arrivingId: 0,
    totalText: '0',
    editorOpen: false,
    editingId: 0,
    saving: false,
    formError: '',
    form: blankForm(),
    options: { styles: [], materials: [], metals: [] },
    arrivalOpen: false,
    arrivalRow: null,
    arrivalBarcode: '',
    arrivalRemark: '',
    arrivalSaving: false,
    refundOpen: false,
    refundRow: null,
    refundAmount: '',
    refundRemark: '',
    refundSaving: false,
    detailOpen: false,
    detail: null,
    detailLoading: false,
    detailLedgers: [],
    ledgerTexts: { 1: '收定金', 2: '销售对冲', 3: '作废退回', 4: '退定金', 5: '到货登记' }
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
  noop() {},
  onKeyword(e) { this.setData({ keyword: e.detail.value }) },
  setFilter(e) {
    const filter = e.currentTarget.dataset.filter
    this.setData({ filter, rows: this.filterRows(this.data.allRows, filter) })
  },
  filterRows(rows, filter) {
    if (filter === 'waiting') return rows.filter((item) => Number(item.arrivalStatus) !== 1)
    if (filter === 'arrived') return rows.filter((item) => Number(item.arrivalStatus) === 1)
    if (filter === 'contact') return rows.filter((item) => item.contactPending)
    return rows
  },
  async load() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const data = await api.customList({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.keyword })
      const allRows = list.rows(data).map(decorateCustom)
      const total = list.total(data) || allRows.length
      this.setData({
        allRows,
        rows: this.filterRows(allRows, this.data.filter),
        pageNum: 1,
        totalText: String(total),
        hasMore: allRows.length < total && allRows.length >= PAGE_SIZE
      })
    } catch (error) {
      this.setData({ error: error.message || '加载失败' })
    } finally {
      this.setData({ loading: false })
    }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.customList({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.keyword })
      const nextRows = list.rows(data).map(decorateCustom)
      const allRows = this.data.allRows.concat(nextRows)
      const total = list.total(data) || allRows.length
      this.setData({
        allRows,
        rows: this.filterRows(allRows, this.data.filter),
        pageNum: nextPage,
        totalText: String(total),
        hasMore: nextRows.length >= PAGE_SIZE && allRows.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ loadingMore: false }) }
  },
  async loadOptions() {
    try {
      const [styles, materials, metals] = await Promise.all([
        api.lookupOptions('style'), api.lookupOptions('material'), api.lookupOptions('metal')
      ])
      const withLabel = (rows) => (rows || []).map((item) => Object.assign({}, item, {
        displayName: Number(item.level) === 2 ? '　└ ' + (item.itemName || '未命名') : (item.itemName || '未命名')
      }))
      this.setData({ options: { styles: withLabel(styles), materials: withLabel(materials), metals: withLabel(metals) } })
    } catch (error) { ui.toast(error.message || '分类加载失败') }
  },
  openCreate() {
    this.setData({ editorOpen: true, editingId: 0, formError: '', form: blankForm() })
    if (!this.data.options.styles.length) this.loadOptions()
  },
  async openEdit(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    if (!row.canEdit) return ui.toast('已发生对冲、退款或已完成的定制单不能修改')
    ui.loading('读取定制单')
    try {
      const detail = await api.customDetail(row.id)
      this.setData({
        editorOpen: true,
        editingId: Number(row.id),
        formError: '',
        form: Object.assign(blankForm(), {
          id: Number(detail.id),
          memberId: detail.memberId || null,
          customerName: detail.customerName || '',
          customerPhone: detail.customerPhone || '',
          customerSex: Number(detail.customerSex || 0),
          goodsName: detail.goodsName || '',
          goodsItemId: detail.goodsItemId || null,
          goodsNo: detail.goodsNo || '',
          barcode: detail.barcode || '',
          styleItemId: detail.styleItemId || '',
          styleItemIdText: detail.styleItemName || '',
          materialItemId: detail.materialItemId || '',
          materialItemIdText: detail.materialName || '',
          metalItemId: detail.metalItemId || '',
          metalItemIdText: detail.metalName || '',
          weight: detail.weight == null ? '' : String(detail.weight),
          expectPrice: String(detail.expectPrice == null ? '' : detail.expectPrice),
          depositAmount: String(detail.depositAmount == null ? '' : detail.depositAmount),
          customDate: format.dateText(detail.customDate),
          pickupDate: detail.pickupDate ? format.dateText(detail.pickupDate) : '',
          remark: detail.remark || ''
        })
      })
      if (!this.data.options.styles.length) this.loadOptions()
    } catch (error) { ui.toast(error.message || '定制单读取失败') }
    finally { ui.hideLoading() }
  },
  closeEditor() { this.setData({ editorOpen: false }) },
  onForm(e) { this.setData({ ['form.' + e.currentTarget.dataset.field]: e.detail.value }) },
  pickSex(e) { this.setData({ 'form.customerSex': Number(e.currentTarget.dataset.value) }) },
  onCustomDate(e) { this.setData({ 'form.customDate': e.detail.value }) },
  onPickupDate(e) { this.setData({ 'form.pickupDate': e.detail.value }) },
  pickOption(e) {
    const field = e.currentTarget.dataset.field
    const listKey = e.currentTarget.dataset.list
    const options = this.data.options[listKey] || []
    const index = Number(e.detail.value)
    const item = options[index]
    if (!item) return
    this.setData({ ['form.' + field]: item.id, ['form.' + field + 'Text']: item.itemName || '' })
  },
  clearOption(e) {
    const field = e.currentTarget.dataset.field
    this.setData({ ['form.' + field]: '', ['form.' + field + 'Text']: '' })
  },
  async saveCustom() {
    const form = this.data.form
    if (!String(form.customerName || '').trim()) return this.setData({ formError: '请填写客户姓名' })
    if (!String(form.customerPhone || '').trim()) return this.setData({ formError: '请填写客户手机号' })
    if (!String(form.goodsName || '').trim()) return this.setData({ formError: '请填写定制货品' })
    if (!this.data.editingId && Number(form.depositAmount || 0) <= 0) return this.setData({ formError: '请填写收取定金' })
    this.setData({ saving: true, formError: '' })
    try {
      await api.saveCustom({
        id: Number(this.data.editingId || 0),
        memberId: form.memberId || null,
        customerName: String(form.customerName).trim(),
        customerPhone: String(form.customerPhone).trim(),
        customerSex: Number(form.customerSex || 0),
        goodsName: String(form.goodsName).trim(),
        goodsItemId: form.goodsItemId || null,
        goodsNo: form.goodsNo || '',
        barcode: form.barcode || '',
        styleItemId: form.styleItemId || null,
        materialItemId: form.materialItemId || null,
        metalItemId: form.metalItemId || null,
        weight: form.weight === '' ? null : Number(form.weight),
        expectPrice: Number(form.expectPrice || 0),
        depositAmount: Number(form.depositAmount || 0),
        customDate: form.customDate ? form.customDate + ' 00:00:00' : null,
        pickupDate: form.pickupDate || null,
        remark: form.remark || ''
      })
      ui.toast(this.data.editingId ? '定制单已更新' : (form.goodsNo || form.barcode || form.goodsItemId ? '定金已入账，货品已到店，请及时联系客户' : '定金已入账，货品待到店'), 'success')
      this.setData({ editorOpen: false })
      await this.load()
    } catch (error) { this.setData({ formError: error.message || '保存失败' }) }
    finally { this.setData({ saving: false }) }
  },
  openArrival(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    this.setData({ arrivalOpen: true, arrivalRow: row, arrivalBarcode: row.barcode || '', arrivalRemark: '' })
  },
  closeArrival() { this.setData({ arrivalOpen: false, arrivalRow: null }) },
  onArrivalBarcode(e) { this.setData({ arrivalBarcode: e.detail.value }) },
  onArrivalRemark(e) { this.setData({ arrivalRemark: e.detail.value }) },
  async submitArrival() {
    const barcode = String(this.data.arrivalBarcode || '').trim()
    if (!barcode) return ui.toast('请录入到货货品条码')
    if (!this.data.arrivalRow) return
    this.setData({ arrivalSaving: true })
    try {
      await api.customArrive(this.data.arrivalRow.id, { barcode, remark: this.data.arrivalRemark || '门店确认到货' })
      ui.toast('已标记到店，请及时联系客户', 'success')
      this.setData({ arrivalOpen: false, arrivalRow: null })
      await this.load()
    } catch (error) { ui.toast(error.message || '到货确认失败') }
    finally { this.setData({ arrivalSaving: false }) }
  },
  openRefund(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    if (!row.canRefund) return ui.toast('这张定制单没有可退定金')
    this.setData({ refundOpen: true, refundRow: row, refundAmount: String(row.remainAmount || 0), refundRemark: '' })
  },
  closeRefund() { this.setData({ refundOpen: false, refundRow: null }) },
  onRefundAmount(e) { this.setData({ refundAmount: e.detail.value }) },
  onRefundRemark(e) { this.setData({ refundRemark: e.detail.value }) },
  async submitRefund() {
    const amount = Number(this.data.refundAmount || 0)
    if (!(amount > 0)) return ui.toast('退定金金额必须大于 0')
    try { await ui.confirm('确认退定金', '将退回 ¥' + format.money(amount) + '，定金流水会保留。', '确认退款') } catch (error) { return }
    this.setData({ refundSaving: true })
    try {
      await api.refundCustom({ id: this.data.refundRow.id, amount, remark: this.data.refundRemark || '门店退定金' })
      ui.toast('定金已退回', 'success')
      this.setData({ refundOpen: false, refundRow: null })
      await this.load()
    } catch (error) { ui.toast(error.message || '退定金失败') }
    finally { this.setData({ refundSaving: false }) }
  },
  async refundAll(e) {
    const row = (e && e.currentTarget && e.currentTarget.dataset.id
      ? this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
      : this.data.refundRow) || this.data.refundRow
    if (!row) return
    if (!row.canRefund) return ui.toast('这张定制单没有可退定金')
    const amount = Number(row.remainAmount || 0)
    if (!(amount > 0)) return ui.toast('当前没有可退余额')
    try { await ui.confirm('退清全部定金', '将退回 ¥' + format.money(amount) + '，退清后绑定商品会自动解锁。', '退清') } catch (error) { return }
    this.setData({ refundSaving: true })
    try {
      await api.refundCustom({ id: row.id, amount, remark: '门店退清全部定金' })
      ui.toast('定金已退清', 'success')
      this.setData({ refundOpen: false, refundRow: null })
      await this.load()
    } catch (error) { ui.toast(error.message || '退定金失败') }
    finally { this.setData({ refundSaving: false }) }
  },
  async remove(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    if (!row.canDelete) return ui.toast('请先退清定金；已对冲的定制单不能删除')
    try { await ui.confirm('删除定制单', '删除后列表不再显示，定金流水仍保留可追溯；仍绑定的在库商品会同时解除锁定。', '确认删除') } catch (error) { return }
    try { await api.deleteCustom(row.id); ui.toast('定制单已删除', 'success'); await this.load() }
    catch (error) { ui.toast(error.message || '删除失败') }
  },
  async openDetail(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    this.setData({ detailOpen: true, detailLoading: true, detail: row, detailLedgers: [] })
    try {
      const detail = await api.customDetail(row.id)
      this.setData({
        detail: decorateCustom(detail),
        detailLoading: false,
        detailLedgers: (detail.ledgers || []).map((item) => Object.assign({}, item, {
          timeText: format.dateTimeText(item.bizTime),
          amountText: format.money(item.amount),
          balanceText: format.money(item.balanceAfter),
          changeText: this.data.ledgerTexts[Number(item.changeType)] || '定金变动'
        }))
      })
    } catch (error) { this.setData({ detailLoading: false }); ui.toast(error.message || '流水加载失败') }
  },
  closeDetail() { this.setData({ detailOpen: false, detail: null }) },
  payRemain(e) {
    const row = this.data.allRows.find((item) => String(item.id) === String(e.currentTarget.dataset.id))
    if (!row) return
    bridge.putCustom(row)
    navigation.open('/pages/sale/sale')
    ui.toast('已带入定制客户：' + (row.customerText || '定制客户'))
  },
  onTheme() { themeUtil.apply(this) }
})
