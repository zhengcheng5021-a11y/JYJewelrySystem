const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const list = require('../../utils/list')
const bridge = require('../../utils/bridge')

const PAGE_SIZE = 20

function firstRow(data) {
  if (!data) return null
  if (Array.isArray(data)) return data[0] || null
  const arr = data.rows || data.items || data.goods || data.result
  if (Array.isArray(arr)) return arr[0] || null
  return data
}

function initialOf(name) { return String(name || '客').slice(0, 1) }

function decorateHistory(item) {
  return Object.assign({}, item, {
    payText: format.money(item.payAmount),
    totalText: format.money(item.totalAmount),
    discountText: format.money(item.discountAmount),
    baseText: format.money(item.basePayAmount),
    roundOffText: format.money(item.roundOffAmount),
    customerText: item.customerName || item.memberName || '散客',
    timeText: format.dateTimeText(item.dealTime || item.createTime)
  })
}

function decorateGoods(item) {
  return Object.assign({}, item, {
    goodsName: item.styleName || item.goodsName || '未命名货品',
    saleText: format.money(item.salePrice)
  })
}

Page({
  data: {
    theme: {},
    mode: 'pos',
    customerKeyword: '',
    goodsKeyword: '',
    drawerKeyword: '',
    drawer: '',
    drawerMembers: [],
    drawerMemberPage: 1,
    drawerMemberHasMore: false,
    drawerGoods: [],
    drawerGoodsPage: 1,
    drawerGoodsHasMore: false,
    drawerLoadingMore: false,
    customer: {},
    customerInitial: '客',
    lines: [],
    history: [],
    historyPage: 1,
    historyTotal: 0,
    historyHasMore: false,
    historyLoadingMore: false,
    memberDiscount: true,
    roundOff: '0',
    rawTotalText: '0.00',
    discountText: '0.00',
    basePayText: '0.00',
    roundOffText: '0.00',
    previewText: '0.00',
    previewHint: '加入货品后自动预览',
    orderId: 0,
    customOrderId: 0,
    customOrderNo: '',
    customRemain: '0.00',
    previewAmount: 0,
    discountHint: '识别会员后自动应用等级折扣',
    customHasGoods: false,
    customOffsetText: '0.00',
    customCashText: '0.00',
    matching: false,
    finding: false,
    saving: false,
    confirming: false,
    error: ''
  },
  onShow() {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    this.restoreDraft()
    this.consumeBridge()
    this.loadHistory()
  },
  async onPullDownRefresh() {
    if (this.data.mode === 'history') await this.loadHistory()
    else await this.preview()
    wx.stopPullDownRefresh()
  },
  onReachBottom() {
    if (this.data.mode === 'history' && this.data.historyHasMore && !this.data.historyLoadingMore) {
      this.loadMoreHistory()
    }
  },
  scrollTop() { wx.pageScrollTo({ scrollTop: 0, duration: 260 }) },
  onHide() { this.persistDraft() },
  onUnload() { this.persistDraft() },
  noop() {},
  setMode(e) {
    const mode = e.currentTarget.dataset.mode
    bridge.setPreference('saleMode', mode)
    this.setData({ mode })
    if (mode === 'history') this.loadHistory()
  },
  onCustomerKeyword(e) { this.setData({ customerKeyword: e.detail.value }) },
  onGoodsKeyword(e) { this.setData({ goodsKeyword: e.detail.value }) },
  onDrawerKeyword(e) { this.setData({ drawerKeyword: e.detail.value }) },
  onRoundOff(e) { this.setData({ roundOff: e.detail.value || '0' }); this.persistDraft(); this.preview() },
  closeDrawer() { this.setData({ drawer: '', drawerKeyword: '' }) },
  walkIn() {
    this.setCustomer({ memberName: '到店散客', customerName: '到店散客', phone: '柜面直购', levelName: '散客直开', discountRate: 1 })
    ui.toast('已切换为散客直开模式')
  },
  clearCustomOffset() {
    this.setData({ customOrderId: 0, customOrderNo: '', customRemain: '0.00', customHasGoods: false, customOffsetText: '0.00', customCashText: '0.00' })
    this.persistDraft()
    this.preview()
    ui.toast('已取消定制单尾款对冲')
  },
  discountHint(customer) {
    customer = customer || this.data.customer || {}
    const rate = Number(customer.discountRate || 1)
    if (!customer.memberId) {
      return customer.levelName === '散客直开' ? '散客直开 · 暂无折扣' : '识别会员后自动应用等级折扣'
    }
    if (!(rate > 0 && rate < 1)) return customer.levelName ? customer.levelName + ' 暂无折扣' : '当前会员暂无等级折扣'
    return (customer.levelName || '会员') + ' · ' + Math.round(rate * 100) + '% 折'
  },
  setCustomer(row) {
    const customer = {
      memberId: row.memberId || row.id || null,
      memberName: row.memberName || row.name || '',
      customerName: row.memberName || row.customerName || row.name || '',
      phone: row.phone || '',
      levelName: row.levelName || '会员',
      discountRate: row.discountRate || 1
    }
    const hasMember = !!(customer.memberId && customer.discountRate < 1)
    this.setData({
      customer,
      customerInitial: initialOf(customer.memberName),
      customerKeyword: customer.memberName || customer.phone || '',
      memberDiscount: hasMember,
      discountHint: this.discountHint(customer)
    })
    this.persistDraft()
    this.preview()
  },
  async matchCustomer() {
    const keyword = String(this.data.customerKeyword || '').trim()
    if (!keyword) return ui.toast('请输入客户姓名或手机号')
    this.setData({ matching: true, error: '' })
    try {
      const data = await api.saleCustomer(keyword)
      const candidate = (data && data.candidates && data.candidates[0]) || data || {}
      if (!candidate.memberName && !candidate.customerName && !(data && data.exists)) {
      this.setData({ customer: { customerName: keyword, memberName: keyword, levelName: '新客户' }, customerInitial: initialOf(keyword), memberDiscount: false, discountHint: '新客户暂无等级折扣' })
      } else this.setCustomer(Object.assign({}, candidate, { memberId: candidate.memberId || (data && data.memberId) }))
      this.persistDraft()
    } catch (error) { this.setData({ error: error.message || '识别失败' }) }
    finally { this.setData({ matching: false }) }
  },
  async openMemberDrawer() {
    this.setData({ drawer: 'member', drawerKeyword: '', drawerMembers: [], drawerMemberPage: 1, drawerMemberHasMore: false })
    await this.searchDrawerMembers()
  },
  async searchDrawerMembers() {
    try {
      const data = await api.memberList({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.drawerKeyword, status: 0 })
      const rows = list.rows(data)
      const total = list.total(data) || rows.length
      this.setData({
        drawerMembers: rows,
        drawerMemberPage: 1,
        drawerMemberHasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '客户加载失败' }) }
  },
  async loadMoreDrawerMembers() {
    if (this.data.drawerLoadingMore || !this.data.drawerMemberHasMore) return
    const nextPage = this.data.drawerMemberPage + 1
    this.setData({ drawerLoadingMore: true })
    try {
      const data = await api.memberList({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.drawerKeyword, status: 0 })
      const nextRows = list.rows(data)
      const merged = this.data.drawerMembers.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        drawerMembers: merged,
        drawerMemberPage: nextPage,
        drawerMemberHasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载失败') }
    finally { this.setData({ drawerLoadingMore: false }) }
  },
  selectMember(e) {
    const id = String(e.currentTarget.dataset.id)
    const row = this.data.drawerMembers.find((item) => String(item.id) === id)
    if (row) this.setCustomer(row)
    this.closeDrawer()
  },
  async openGoodsDrawer() {
    this.setData({ drawer: 'goods', drawerKeyword: '', drawerGoods: [], drawerGoodsPage: 1, drawerGoodsHasMore: false })
    await this.searchDrawerGoods()
  },
  async searchDrawerGoods() {
    try {
      const data = await api.stockList({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.drawerKeyword, status: 1 })
      const rows = list.rows(data).map(decorateGoods)
      const total = list.total(data) || rows.length
      this.setData({
        drawerGoods: rows,
        drawerGoodsPage: 1,
        drawerGoodsHasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '货品加载失败' }) }
  },
  async loadMoreDrawerGoods() {
    if (this.data.drawerLoadingMore || !this.data.drawerGoodsHasMore) return
    const nextPage = this.data.drawerGoodsPage + 1
    this.setData({ drawerLoadingMore: true })
    try {
      const data = await api.stockList({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.drawerKeyword, status: 1 })
      const nextRows = list.rows(data).map(decorateGoods)
      const merged = this.data.drawerGoods.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        drawerGoods: merged,
        drawerGoodsPage: nextPage,
        drawerGoodsHasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载失败') }
    finally { this.setData({ drawerLoadingMore: false }) }
  },
  selectGoods(e) {
    const row = this.data.drawerGoods[e.currentTarget.dataset.index]
    if (row) this.addLine(row)
    this.closeDrawer()
  },
  async findGoods() {
    const keyword = String(this.data.goodsKeyword || '').trim()
    if (!keyword) return ui.toast('请输入货号、条码或名称')
    this.setData({ finding: true, error: '' })
    try {
      const row = firstRow(await api.saleGoods(keyword))
      if (!row || !(row.goodsItemId || row.id)) throw new Error('没有找到可售货品')
      this.addLine(row)
      this.setData({ goodsKeyword: '' })
    } catch (error) { this.setData({ error: error.message || '查找失败' }) }
    finally { this.setData({ finding: false }) }
  },
  addLine(row) {
    const goodsItemId = row.goodsItemId || row.id
    if (!goodsItemId) return ui.toast('货品信息不完整')
    if (this.data.lines.some((line) => String(line.goodsItemId) === String(goodsItemId))) return ui.toast('这件货已在当前开单中')
    const salePrice = Number(row.salePrice || 0)
    const line = {
      goodsItemId,
      goodsName: row.goodsName || row.styleName || '未命名货品',
      goodsNo: row.goodsNo || row.barcode || '',
      styleItemName: row.styleItemName || '',
      salePrice,
      saleText: format.money(salePrice),
      dealPrice: String(salePrice),
      isGift: false
    }
    this.setData({ lines: this.data.lines.concat(line), error: '' })
    this.persistDraft()
    this.preview()
  },
  onPrice(e) {
    const lines = this.data.lines.slice()
    lines[e.currentTarget.dataset.index].dealPrice = e.detail.value
    this.setData({ lines })
    this.persistDraft()
    this.preview()
  },
  toggleGift(e) {
    const lines = this.data.lines.slice()
    const line = lines[e.currentTarget.dataset.index]
    line.isGift = !line.isGift
    if (line.isGift) line.dealPrice = '0'
    else line.dealPrice = String(line.salePrice || 0)
    this.setData({ lines })
    this.persistDraft()
    this.preview()
  },
  removeLine(e) {
    const lines = this.data.lines.slice()
    lines.splice(e.currentTarget.dataset.index, 1)
    this.setData({ lines })
    this.persistDraft()
    this.preview()
  },
  toggleDiscount() {
    this.setData({ memberDiscount: !this.data.memberDiscount })
    this.persistDraft()
    this.preview()
  },
  payload() {
    const c = this.data.customer
    const offsetAmount = this.customOffsetAmount()
    const offsets = this.data.customOrderId && offsetAmount > 0 ? [{ customOrderId: Number(this.data.customOrderId), offsetAmount }] : []
    return {
      id: Number(this.data.orderId || 0),
      orderType: 1,
      memberId: c.memberId || null,
      customerName: c.customerName || c.memberName || null,
      customerPhone: c.phone || null,
      customerSex: 0,
      applyMemberDiscount: !!this.data.memberDiscount,
      adjustAmount: 0,
      roundOffAmount: Number(this.data.roundOff || 0),
      items: this.data.lines.map((item) => ({ goodsItemId: Number(item.goodsItemId), qty: 1, dealPrice: Number(item.dealPrice || 0), isGift: item.isGift ? 1 : 0 })),
      customOffsets: offsets
    }
  },
  rawTotal() { return this.data.lines.reduce((sum, item) => sum + (item.isGift ? 0 : Number(item.dealPrice || 0)), 0) },
  customOffsetAmount() {
    if (!this.data.customOrderId) return 0
    const remain = Number(String(this.data.customRemain || '0').replace(/,/g, '')) || 0
    return Math.max(0, Math.min(remain, Number(this.data.previewAmount || 0)))
  },
  async preview() {
    const raw = this.rawTotal()
    const requestedRoundOff = Math.max(0, Number(this.data.roundOff || 0))
    this.setData({ rawTotalText: format.money(raw), roundOffText: format.money(requestedRoundOff) })
    if (!this.data.lines.length) {
      return this.setData({
        previewAmount: 0,
        discountText: '0.00',
        basePayText: '0.00',
        roundOffText: '0.00',
        previewText: '0.00',
        customOffsetText: '0.00',
        customCashText: '0.00',
        previewHint: '加入货品后自动预览'
      })
    }
    try {
      const data = await api.previewSale(this.payload())
      const payAmount = Number(data.payAmount || 0)
      const offsetAmount = Math.max(0, Math.min(Number(String(this.data.customRemain || '0').replace(/,/g, '')) || 0, payAmount))
      this.setData({
        previewAmount: payAmount,
        discountText: format.money(data.memberDiscount || data.discountAmount || 0),
        basePayText: format.money(data.basePayAmount || 0),
        roundOffText: format.money(data.roundOffAmount || requestedRoundOff),
        previewText: format.money(payAmount),
        customOffsetText: format.money(offsetAmount),
        customCashText: format.money(payAmount - offsetAmount),
        previewHint: '商品成交价合计 ¥' + format.money(data.totalAmount || raw)
      })
    } catch (error) { this.setData({ previewHint: error.message || '预览失败' }) }
  },
  persistDraft() {
    bridge.saveSaleDraft({
      orderId: this.data.orderId,
      customer: this.data.customer,
      lines: this.data.lines,
      memberDiscount: this.data.memberDiscount,
      roundOff: this.data.roundOff,
      customOrderId: this.data.customOrderId,
      customOrderNo: this.data.customOrderNo,
      customRemain: this.data.customRemain,
      customHasGoods: this.data.customHasGoods
    })
  },
  restoreDraft() {
    const draft = bridge.getSaleDraft()
    if (!draft) return
    const customer = draft.customer || {}
    this.setData({
      orderId: Number(draft.orderId || 0),
      customer,
      customerInitial: initialOf(customer.memberName || customer.customerName),
      customerKeyword: customer.memberName || customer.customerName || '',
      lines: Array.isArray(draft.lines) ? draft.lines : [],
      memberDiscount: draft.memberDiscount === undefined ? true : !!draft.memberDiscount,
      roundOff: String(draft.roundOff || 0),
      customOrderId: draft.customOrderId || 0,
      customOrderNo: draft.customOrderNo || '',
      customRemain: draft.customRemain || '0.00',
      customHasGoods: !!draft.customHasGoods,
      customOffsetText: '0.00',
      customCashText: '0.00'
    })
    this.preview()
  },
  consumeBridge() {
    const goods = bridge.takeGoods()
    const member = bridge.takeSaleMember()
    const custom = bridge.takeCustom()
    if (member || goods || custom) {
      this.setData({ mode: 'pos' })
      bridge.setPreference('saleMode', 'pos')
    }
    if (member) this.setCustomer(member)
    if (goods) this.addLine(goods)
    if (custom) {
      this.setData({ customOrderId: custom.id || 0, customOrderNo: custom.customNo || '', customRemain: format.money(custom.remainAmount), customHasGoods: !!custom.goodsItemId, customOffsetText: '0.00', customCashText: '0.00', customer: { memberId: custom.memberId || null, memberName: custom.customerName || '定制客户', customerName: custom.customerName || '定制客户', phone: custom.customerPhone || '', levelName: '定制客户' }, customerInitial: initialOf(custom.customerName) })
      if (custom.goodsItemId) {
        this.addLine({ goodsItemId: custom.goodsItemId, goodsNo: custom.goodsNo || custom.barcode || '', goodsName: custom.goodsName || '定制货品', salePrice: custom.expectPrice || 0 })
      }
      this.persistDraft()
      this.preview()
      ui.toast(custom.goodsItemId ? '已带入定制货品与尾款' : '已带入定制客户，请再挑选本次货品')
    }
  },
  async loadHistory() {
    try {
      const data = await api.saleList({ pageNum: 1, pageSize: PAGE_SIZE })
      const rows = list.rows(data).map(decorateHistory)
      const total = list.total(data) || rows.length
      this.setData({
        history: rows,
        historyPage: 1,
        historyTotal: total,
        historyHasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) { if (this.data.mode === 'history') this.setData({ error: error.message || '销售记录加载失败' }) }
  },
  async loadMoreHistory() {
    if (this.data.historyLoadingMore || !this.data.historyHasMore) return
    const nextPage = this.data.historyPage + 1
    this.setData({ historyLoadingMore: true })
    try {
      const data = await api.saleList({ pageNum: nextPage, pageSize: PAGE_SIZE })
      const nextRows = list.rows(data).map(decorateHistory)
      const merged = this.data.history.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        history: merged,
        historyPage: nextPage,
        historyTotal: total,
        historyHasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ historyLoadingMore: false }) }
  },
  async save() {
    if (!this.data.lines.length) return this.setData({ error: '请先加入货品' })
    this.setData({ saving: true, error: '' })
    try {
      const id = await api.saveSale(this.payload())
      this.setData({ orderId: id || this.data.orderId })
      this.persistDraft()
      this.loadHistory()
      ui.toast('开单草稿已保存', 'success')
    } catch (error) { this.setData({ error: error.message || '保存失败' }) }
    finally { this.setData({ saving: false }) }
  },
  async confirm() {
    if (!this.data.customer.customerName && !this.data.customer.memberId) return this.setData({ error: '请先识别或填写客户' })
    if (!this.data.lines.length) return this.setData({ error: '请先加入货品' })
    try { await ui.confirm('确认收款', '确认后扣减库存。应收 ¥' + this.data.previewText, '确认收款') } catch (e) { return }
    this.setData({ confirming: true, error: '' })
    try {
      const id = this.data.orderId || await api.saveSale(this.payload())
      await api.confirmSale(id)
      bridge.clearSaleDraft()
      this.setData({ orderId: 0, lines: [], customer: {}, customerInitial: '客', memberDiscount: false, roundOff: '0', previewText: '0.00', rawTotalText: '0.00', discountText: '0.00', basePayText: '0.00', roundOffText: '0.00', customOrderId: 0, customOrderNo: '', customRemain: '0.00', customHasGoods: false, customOffsetText: '0.00', customCashText: '0.00', previewAmount: 0, mode: 'history' })
      this.loadHistory()
      ui.toast('收款成功，库存已核减', 'success')
    } catch (error) { this.setData({ error: error.message || '收款失败' }) }
    finally { this.setData({ confirming: false }) }
  },
  async confirmHistory(e) {
    try { await ui.confirm('确认收款', '确认后会核减库存。', '确认收款') } catch (error) { return }
    try { await api.confirmSale(e.currentTarget.dataset.id); this.loadHistory(); ui.toast('草稿单已确认收款', 'success') }
    catch (error) { ui.toast(error.message || '确认失败') }
  },
  onTheme() { themeUtil.apply(this) }
})
