const themeUtil = require('../../utils/theme')
const api = require('../../utils/api')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const navigation = require('../../utils/navigation')
const session = require('../../utils/session')
const bridge = require('../../utils/bridge')

function blankLine(seed) {
  const base = seed || {}
  return {
    key: 'k' + Date.now() + Math.random().toString(36).slice(2, 7),
    id: 0,
    goodsName: '',
    costPrice: base.costPrice || '',
    salePrice: base.salePrice || '',
    imageFileId: '',
    imageUrl: '',
    uploading: false,
    remark: '',
    styleItemId: base.styleItemId || '',
    styleItemName: base.styleItemName || '',
    materialItemId: base.materialItemId || '',
    materialName: base.materialName || '',
    metalItemId: base.metalItemId || '',
    metalName: base.metalName || '',
    certTypeId: base.certTypeId || '',
    certTypeName: base.certTypeName || '',
    profitText: '0.00',
    profitRate: 0
  }
}

function labelOf(item) { return (item && (item.itemName || item.name || item.label)) || '未命名' }

function buildAutoName(item) {
  const parts = [item.metalName, item.materialName, item.styleItemName].filter(Boolean)
  return parts.join('')
}

function decorateLine(item) {
  const cost = Number(item.costPrice || 0)
  const sale = Number(item.salePrice || 0)
  const profit = sale - cost
  return Object.assign({}, item, {
    profitText: format.money(profit),
    profitRate: sale > 0 ? Math.round((profit / sale) * 100) : 0
  })
}

const ATTR_MAP = {
  style: { listKey: 'categories', idKey: 'styleItemId', nameKey: 'styleItemName', title: '选择款式分类（必选）', next: 'material' },
  material: { listKey: 'materials', idKey: 'materialItemId', nameKey: 'materialName', title: '选择主石 / 材质', next: 'metal' },
  metal: { listKey: 'metals', idKey: 'metalItemId', nameKey: 'metalName', title: '选择贵金属材质', next: 'cert' },
  cert: { listKey: 'certTypes', idKey: 'certTypeId', nameKey: 'certTypeName', title: '选择鉴定证书', next: '' }
}

Page({
  data: {
    theme: {},
    id: '',
    loading: true,
    saving: false,
    confirming: false,
    readOnly: false,
    error: '',
    options: { suppliers: [], categories: [], materials: [], metals: [], certTypes: [] },
    quickCategories: [],
    header: { inboundNo: '', inboundTime: '', supplierId: '', supplierName: '', remark: '', status: 0 },
    lines: [],
    photoCount: 0,
    costSumText: '0.00',
    saleSumText: '0.00',
    profitSumText: '0.00',
    specDrawer: false,
    specMode: 'attr',
    specLineIndex: 0,
    specKind: 'style',
    specTitle: '',
    specSubTitle: '',
    specKeyword: '',
    currentSpecVal: '',
    filteredSpecList: []
  },
  onLoad(query) {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    this.setData({ id: (query && query.id) || '', 'header.inboundTime': format.todayText() })
    this.bootstrap()
  },
  onShow() { themeUtil.apply(this) },
  onHide() { this.persistDraft() },
  onUnload() { this.persistDraft() },
  noop() {},
  async bootstrap() {
    this.setData({ loading: true, error: '' })
    try {
      const options = await api.inboundOptions()
      const categories = (options.categories || [])
        .filter((item) => !item.level || item.level === 2 || item.parentId)
        .map((item) => Object.assign({}, item, { label: labelOf(item) }))
      const suppliers = (options.suppliers || []).map((item) => Object.assign({}, item, { label: item.name || labelOf(item) }))
      const materials = (options.materials || []).map((item) => Object.assign({}, item, { label: labelOf(item) }))
      const metals = (options.metals || []).map((item) => Object.assign({}, item, { label: labelOf(item) }))
      const certTypes = (options.certTypes || []).map((item) => Object.assign({}, item, { label: labelOf(item) }))

      const next = {
        options: { suppliers, categories, materials, metals, certTypes },
        quickCategories: categories.slice(0, 6)
      }
      if (this.data.id) {
        const detail = await api.inboundDetail(this.data.id)
        const parsedDate = detail.inboundTime ? format.dateText(detail.inboundTime) : format.todayText()
        next.header = {
          inboundNo: detail.inboundNo || '',
          inboundTime: /^\d{4}-\d{2}-\d{2}$/.test(parsedDate) ? parsedDate : format.todayText(),
          supplierId: detail.supplierId || '',
          supplierName: detail.supplierName || '',
          remark: detail.remark || '',
          status: detail.status
        }
        next.readOnly = detail.status === 1
        next.lines = (detail.lines || []).map((item, index) => decorateLine(Object.assign(blankLine(), item, { key: String(item.id || index), costPrice: String(item.costPrice || ''), salePrice: String(item.salePrice || '') })))
      } else {
        const draft = bridge.getInboundDraft()
        if (draft && Array.isArray(draft.lines) && draft.lines.length) {
          const draftHeader = draft.header || {}
          next.header = Object.assign({}, this.data.header, draftHeader, {
            inboundTime: /^\d{4}-\d{2}-\d{2}$/.test(draftHeader.inboundTime) ? draftHeader.inboundTime : format.todayText()
          })
          next.lines = draft.lines.map(decorateLine)
        } else next.lines = [blankLine()]
      }
      this.setData(next)
      this.summarize()
    } catch (error) { this.setData({ error: error.message || '加载失败' }) }
    finally { this.setData({ loading: false }) }
  },
  summarize() {
    const cost = this.data.lines.reduce((sum, item) => sum + Number(item.costPrice || 0), 0)
    const sale = this.data.lines.reduce((sum, item) => sum + Number(item.salePrice || 0), 0)
    const photoCount = this.data.lines.filter((item) => Boolean(item.imageUrl)).length
    this.setData({
      costSumText: format.money(cost),
      saleSumText: format.money(sale),
      profitSumText: format.money(sale - cost),
      photoCount
    })
  },
  persistDraft() {
    if (this.data.readOnly || this.data.id) return
    bridge.saveInboundDraft({ header: this.data.header, lines: this.data.lines })
  },
  onDate(e) { this.setData({ 'header.inboundTime': e.detail.value }); this.persistDraft() },
  onRemark(e) { this.setData({ 'header.remark': e.detail.value }); this.persistDraft() },
  onLine(e) {
    const index = Number(e.currentTarget.dataset.index)
    const field = e.currentTarget.dataset.field
    const lines = this.data.lines.slice()
    lines[index] = decorateLine(Object.assign({}, lines[index], { [field]: e.detail.value }))
    this.setData({ lines })
    this.summarize()
    this.persistDraft()
  },
  applyMarkup(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    const mult = Number(e.currentTarget.dataset.mult || 2)
    const lines = this.data.lines.slice()
    const item = lines[index]
    const cost = Number(item.costPrice || 0)
    if (cost <= 0) return ui.toast('请先填写拿货成本')
    const rawSale = Math.round(cost * mult)
    lines[index] = decorateLine(Object.assign({}, item, { salePrice: String(rawSale) }))
    this.setData({ lines })
    this.summarize()
    this.persistDraft()
  },
  autoNameLine(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    const lines = this.data.lines.slice()
    const item = lines[index]
    const generated = buildAutoName(item)
    if (!generated) return ui.toast('请先选择款式或材质规格')
    lines[index] = Object.assign({}, item, { goodsName: generated })
    this.setData({ lines })
    this.persistDraft()
    ui.toast('已生成名称：' + generated)
  },
  quickSelectSpec(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    const id = e.currentTarget.dataset.id
    const name = e.currentTarget.dataset.name
    const lines = this.data.lines.slice()
    const prev = lines[index]
    const nextItem = Object.assign({}, prev, { styleItemId: id, styleItemName: name })
    if (!String(nextItem.goodsName || '').trim()) {
      nextItem.goodsName = buildAutoName(nextItem)
    }
    lines[index] = decorateLine(nextItem)
    this.setData({ lines })
    this.persistDraft()
  },
  syncFirstSpecToAll() {
    if (this.data.readOnly || this.data.lines.length < 2) return
    const first = this.data.lines[0]
    if (!first.styleItemId && !first.materialItemId && !first.metalItemId && !first.certTypeId) {
      return ui.toast('请先为第 1 件选择规格')
    }
    const lines = this.data.lines.map((item, idx) => {
      if (idx === 0) return item
      const merged = Object.assign({}, item, {
        styleItemId: item.styleItemId || first.styleItemId,
        styleItemName: item.styleItemName || first.styleItemName,
        materialItemId: item.materialItemId || first.materialItemId,
        materialName: item.materialName || first.materialName,
        metalItemId: item.metalItemId || first.metalItemId,
        metalName: item.metalName || first.metalName,
        certTypeId: item.certTypeId || first.certTypeId,
        certTypeName: item.certTypeName || first.certTypeName
      })
      if (!String(merged.goodsName || '').trim()) {
        merged.goodsName = buildAutoName(merged)
      }
      return decorateLine(merged)
    })
    this.setData({ lines })
    this.persistDraft()
    ui.toast('已将首件规格同步到其余货品', 'success')
  },
  addLine() {
    const last = this.data.lines[this.data.lines.length - 1]
    const nextLine = decorateLine(blankLine(last))
    if (!nextLine.goodsName && (nextLine.styleItemName || nextLine.materialName || nextLine.metalName)) {
      nextLine.goodsName = buildAutoName(nextLine)
    }
    this.setData({ lines: this.data.lines.concat(nextLine) })
    this.summarize()
    this.persistDraft()
  },
  cloneLine(e) {
    const index = Number(e.currentTarget.dataset.index)
    const source = this.data.lines[index]
    const copy = decorateLine(Object.assign({}, source, {
      key: 'k' + Date.now() + Math.random().toString(36).slice(2, 7),
      id: 0,
      imageFileId: '',
      imageUrl: '',
      uploading: false,
      goodsName: source.goodsName || buildAutoName(source) || '同款货品'
    }))
    const lines = this.data.lines.slice()
    lines.splice(index + 1, 0, copy)
    this.setData({ lines })
    this.summarize()
    this.persistDraft()
    ui.toast('已复制同款规格，请补拍新照片')
  },
  removeLine(e) {
    const lines = this.data.lines.slice()
    lines.splice(Number(e.currentTarget.dataset.index), 1)
    this.setData({ lines })
    this.summarize()
    this.persistDraft()
  },
  pickSupplier() {
    if (this.data.readOnly) return
    const list = this.data.options.suppliers || []
    this.setData({
      specDrawer: true,
      specMode: 'supplier',
      specTitle: '选择供货来源 / 工坊',
      specSubTitle: '共 ' + list.length + ' 家供货方，支持搜索筛选',
      specKeyword: '',
      currentSpecVal: this.data.header.supplierId,
      filteredSpecList: list
    })
  },
  pickAttr(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    const kind = e.currentTarget.dataset.kind || 'style'
    this.openAttrDrawer(index, kind)
  },
  openAttrDrawer(index, kind) {
    const conf = ATTR_MAP[kind] || ATTR_MAP.style
    const list = this.data.options[conf.listKey] || []
    const line = this.data.lines[index] || {}
    this.setData({
      specDrawer: true,
      specMode: 'attr',
      specLineIndex: index,
      specKind: kind,
      specTitle: '第 ' + (index + 1) + ' 件 · ' + conf.title,
      specSubTitle: '点选后自动跳至下一项规格，并智能补全货品名称',
      specKeyword: '',
      currentSpecVal: line[conf.idKey] || '',
      filteredSpecList: list
    })
  },
  switchSpecKind(e) {
    const kind = e.currentTarget.dataset.kind
    this.openAttrDrawer(this.data.specLineIndex, kind)
  },
  onSpecSearch(e) {
    const kw = String(e.detail.value || '').trim().toLowerCase()
    const baseList = this.data.specMode === 'supplier'
      ? (this.data.options.suppliers || [])
      : (this.data.options[(ATTR_MAP[this.data.specKind] || ATTR_MAP.style).listKey] || [])
    const filteredSpecList = kw
      ? baseList.filter((item) => String(item.label || '').toLowerCase().includes(kw))
      : baseList
    this.setData({ specKeyword: e.detail.value, filteredSpecList })
  },
  clearSpecSearch() {
    this.onSpecSearch({ detail: { value: '' } })
  },
  clearCurrentAttr() {
    if (this.data.specMode !== 'attr') return
    const index = this.data.specLineIndex
    const conf = ATTR_MAP[this.data.specKind]
    if (!conf) return
    const lines = this.data.lines.slice()
    lines[index] = Object.assign({}, lines[index], { [conf.idKey]: '', [conf.nameKey]: '' })
    this.setData({ lines, currentSpecVal: '' })
    this.persistDraft()
  },
  selectSpecOption(e) {
    const id = e.currentTarget.dataset.id
    const name = e.currentTarget.dataset.name
    if (this.data.specMode === 'supplier') {
      this.setData({
        'header.supplierId': id,
        'header.supplierName': name,
        specDrawer: false
      })
      this.persistDraft()
      return
    }
    const index = this.data.specLineIndex
    const kind = this.data.specKind
    const conf = ATTR_MAP[kind]
    if (!conf) return
    const lines = this.data.lines.slice()
    const prev = lines[index] || {}
    const prevAuto = buildAutoName(prev)
    const updated = Object.assign({}, prev, { [conf.idKey]: id, [conf.nameKey]: name })
    if (!String(updated.goodsName || '').trim() || updated.goodsName === prevAuto) {
      updated.goodsName = buildAutoName(updated)
    }
    lines[index] = decorateLine(updated)
    this.setData({ lines, currentSpecVal: id })
    this.persistDraft()

    if (conf.next && !updated[ATTR_MAP[conf.next].idKey]) {
      this.openAttrDrawer(index, conf.next)
    } else {
      this.setData({ specDrawer: false })
    }
  },
  closeSpecDrawer() { this.setData({ specDrawer: false }) },
  onPhotoTap(e) {
    const index = Number(e.currentTarget.dataset.index)
    const line = this.data.lines[index]
    if (line && line.imageUrl) {
      const urls = this.data.lines.map((it) => it.imageUrl).filter(Boolean)
      wx.previewImage({ current: line.imageUrl, urls: urls.length ? urls : [line.imageUrl] })
      return
    }
    this.chooseImage(e)
  },
  removeImage(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    const lines = this.data.lines.slice()
    lines[index] = Object.assign({}, lines[index], { imageFileId: '', imageUrl: '' })
    this.setData({ lines })
    this.summarize()
    this.persistDraft()
  },
  chooseImage(e) {
    if (this.data.readOnly) return
    const index = Number(e.currentTarget.dataset.index)
    wx.chooseMedia({
      count: 1,
      mediaType: ['image'],
      sourceType: ['camera', 'album'],
      sizeType: ['compressed'],
      success: async (res) => {
        const file = res.tempFiles && res.tempFiles[0] && res.tempFiles[0].tempFilePath
        if (!file) return
        await this.uploadForLine(index, file)
      }
    })
  },
  batchShoot() {
    if (this.data.readOnly) return
    wx.chooseMedia({
      count: 9,
      mediaType: ['image'],
      sourceType: ['camera', 'album'],
      sizeType: ['compressed'],
      success: async (res) => {
        const files = (res.tempFiles || []).map((f) => f.tempFilePath).filter(Boolean)
        if (!files.length) return
        const lines = this.data.lines.slice()
        const template = lines[lines.length - 1] || {}
        const targetIndices = []

        files.forEach((filePath) => {
          const emptyIdx = lines.findIndex((it, idx) => !it.imageUrl && !targetIndices.includes(idx))
          if (emptyIdx >= 0) {
            targetIndices.push(emptyIdx)
          } else {
            const created = decorateLine(blankLine(template))
            if (!created.goodsName) created.goodsName = buildAutoName(created)
            lines.push(created)
            targetIndices.push(lines.length - 1)
          }
        })
        this.setData({ lines })
        this.summarize()

        ui.loading('批量上传 ' + files.length + ' 张图')
        try {
          for (let i = 0; i < files.length; i += 1) {
            await this.uploadForLine(targetIndices[i], files[i], true)
          }
          ui.toast('已连拍导入 ' + files.length + ' 件货品', 'success')
        } finally {
          ui.hideLoading()
        }
      }
    })
  },
  async uploadForLine(index, filePath, silent) {
    const lines = this.data.lines.slice()
    if (!lines[index]) return
    lines[index] = Object.assign({}, lines[index], { imageUrl: filePath, uploading: true })
    this.setData({ lines })
    if (!silent) ui.loading('上传照片')
    try {
      const uploaded = await api.upload(filePath)
      const latest = this.data.lines.slice()
      if (latest[index]) {
        latest[index] = Object.assign({}, latest[index], {
          imageFileId: (uploaded && uploaded.fileId) || '',
          imageUrl: (uploaded && uploaded.url) || filePath,
          uploading: false
        })
        this.setData({ lines: latest })
        this.summarize()
        this.persistDraft()
      }
    } catch (error) {
      const latest = this.data.lines.slice()
      if (latest[index]) {
        latest[index] = Object.assign({}, latest[index], { uploading: false })
        this.setData({ lines: latest })
      }
      ui.toast(error.message || '图片上传失败，已保留本地预览')
    } finally {
      if (!silent) ui.hideLoading()
    }
  },
  payload() {
    return {
      id: Number(this.data.id || 0),
      inboundTime: this.data.header.inboundTime + ' 00:00:00',
      sourceType: 1,
      supplierId: this.data.header.supplierId || null,
      remark: this.data.header.remark || '',
      lines: this.data.lines.map((item) => ({
        id: Number(item.id || 0),
        goodsName: String(item.goodsName || '').trim(),
        costPrice: Number(item.costPrice || 0),
        salePrice: Number(item.salePrice || 0),
        imageFileId: item.imageFileId || null,
        remark: item.remark || '',
        styleItemId: item.styleItemId || null,
        materialItemId: item.materialItemId || null,
        metalItemId: item.metalItemId || null,
        certTypeId: item.certTypeId || null
      }))
    }
  },
  validate() {
    if (!this.data.header.inboundTime || !/^\d{4}-\d{2}-\d{2}$/.test(this.data.header.inboundTime)) return '请选择有效的入库日期'
    if (!this.data.lines.length) return '请至少添加一件货品'
    const missingStyleIdx = this.data.lines.findIndex((item) => !item.styleItemId)
    if (missingStyleIdx >= 0) return '第 ' + (missingStyleIdx + 1) + ' 件货品尚未选择款式分类'
    const missingNameIdx = this.data.lines.findIndex((item) => !String(item.goodsName || '').trim())
    if (missingNameIdx >= 0) return '第 ' + (missingNameIdx + 1) + ' 件货品尚未填写名称'
    return ''
  },
  async save() {
    const message = this.validate()
    if (message) {
      ui.toast(message)
      return this.setData({ error: message })
    }
    this.setData({ saving: true, error: '' })
    try {
      const id = await api.saveInbound(this.payload())
      bridge.clearInboundDraft()
      ui.toast('入库草稿已保存', 'success')
      if (!this.data.id && id) navigation.redirect('/pages/inbound/edit', { id })
      else this.bootstrap()
    } catch (error) { this.setData({ error: error.message || '保存失败' }) }
    finally { this.setData({ saving: false }) }
  },
  async confirm() {
    const message = this.validate()
    if (message) {
      ui.toast(message)
      return this.setData({ error: message })
    }
    try { await ui.confirm('确认入库', '确认后货品进入在库台账，这张单据将锁定不能再改。', '确认入库') } catch (error) { return }
    this.setData({ confirming: true, error: '' })
    try {
      const id = this.data.id || await api.saveInbound(this.payload())
      await api.confirmInbound(id)
      bridge.clearInboundDraft()
      ui.toast('已正式确认入库', 'success')
      navigation.tab('/pages/inbound/inbound')
    } catch (error) { this.setData({ error: error.message || '确认失败' }) }
    finally { this.setData({ confirming: false }) }
  },
  onTheme() { themeUtil.apply(this) }
})
