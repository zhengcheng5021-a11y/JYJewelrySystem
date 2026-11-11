<template>
  <div class="jy-stock">
    <section class="stock-panel">
      <div class="panel-head">
        <div>
          <span class="section-kicker">CABINET LEDGER</span>
          <h2>柜台商品</h2>
          <p>查询库存商品与流转记录</p>
        </div>
        <div class="filters">
          <el-input v-model="query.keyword" clearable placeholder="商品编号、条码或款式" :prefix-icon="Search" @keyup.enter="search" />
          <el-select v-model="query.styleItemId" clearable filterable placeholder="全部款式分类" @change="search">
            <el-option v-for="item in categories" :key="item.id" :label="categoryLabel(item)" :value="item.id" />
          </el-select>
          <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
            <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
          <el-button @click="search">查询</el-button>
          <el-button :loading="loading" plain @click="refreshAll">刷新</el-button>
        </div>
      </div>

      <div class="status-tabs">
        <button v-for="tab in statusTabs" :key="tab.value" type="button" :class="{ active: query.status === tab.value }" @click="pickStatus(tab.value)">
          {{ tab.label }}<small>{{ tab.value === undefined ? total : statusCount(tab.value) }}</small>
        </button>
      </div>

      <div v-loading="loading" class="stock-list">
        <template v-if="rows.length">
          <article v-for="row in rows" :key="row.id" class="stock-row" :class="'s-' + row.status" @click="openDetail(row)">
            <div class="item-sign"><el-icon><Gem /></el-icon></div>
            <div class="item-main">
              <div class="item-title"><b>{{ row.styleName }}</b><span :class="'badge status-' + row.status">{{ statusText(row.status) }}</span></div>
              <div class="item-code">
                <span>{{ codeText(row) }}</span>
                <el-tooltip content="复制条码" placement="top"><button type="button" class="copy-code" aria-label="复制条码" @click.stop="copyCode(row.barcode || row.goodsNo)"><el-icon><CopyDocument /></el-icon></button></el-tooltip>
              </div>
              <div v-if="lockText(row)" class="item-lock"><el-icon><Lock /></el-icon>{{ lockText(row) }}</div>
              <div class="item-tags"><span>{{ materialGroupText(row.materialGroup) }}</span><span>{{ row.styleItemName || '未分类' }}</span><span>{{ row.materialName || '未设材质' }}</span><span>{{ row.metalName || '未设成色' }}</span><span>{{ pricingText(row) }}</span><span>{{ laborText(row) }}</span><span>包装 ¥{{ money(row.packagingFee) }}/件</span></div>
              <div class="item-unit">{{ number(row.qty) }} {{ row.unit || '件' }}<i></i>{{ row.weight ? '银重 ' + number(row.weight) + ' g' : '银重未记录' }}</div>
            </div>
            <div class="item-quantity">
              <span>数量</span><b>{{ number(row.qty) }}</b><small>{{ row.itemMode === 1 ? '单件' : '按捆' }}</small>
            </div>
            <div class="item-price">
              <span>{{ Number(row.pricingMode) === 2 ? '金属基价/克' : '销售价/件' }}</span><b>{{ Number(row.pricingMode) === 2 ? '按当前金价' : '¥' + money(row.salePrice) }}</b><small>成本 ¥{{ money(row.costPrice) }}</small>
            </div>
            <div class="item-meta">
              <span>入库时间</span><b>{{ dateText(row.createTime) }}</b><small>{{ row.supplierName || '未标注供应商' }}</small>
            </div>
            <button class="row-action" type="button" @click.stop="openDetail(row)"><el-icon><Right /></el-icon></button>
          </article>
        </template>
        <div v-else class="empty-stock">
          <div class="empty-icon"><el-icon><Box /></el-icon></div>
          <h3>{{ query.keyword ? '没有找到匹配商品' : '库存柜台还是空的' }}</h3>
          <p>{{ query.keyword ? '换一个商品编号、条码或款式名称试试。' : '先在采购入库中确认到货，商品会自动进入库存。' }}</p>
          <el-button v-if="!query.keyword" type="primary" @click="$router.push('/business/inbound')">去采购入库</el-button>
        </div>
      </div>

      <div class="list-foot">
        <span>共 {{ total }} 件商品记录</span>
        <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
      </div>
    </section>

    <el-drawer v-model="detailOpen" size="620px" append-to-body class="stock-drawer">
      <template #header>
        <div class="drawer-heading"><span>ITEM PASSPORT</span><b>商品档案与流转</b></div>
      </template>
      <template v-if="detail">
        <section class="passport">
          <div class="passport-seal"><el-icon><Gem /></el-icon></div>
          <div class="passport-title"><span>{{ detail.goodsNo }}</span><h2>{{ detail.styleName }}</h2><p><span>{{ detail.barcode || '暂无条码' }}</span><el-tooltip content="复制条码" placement="top"><button type="button" class="copy-code" aria-label="复制条码" @click="copyCode(detail.barcode || detail.goodsNo)"><el-icon><CopyDocument /></el-icon></button></el-tooltip></p></div>
          <span :class="'badge status-' + detail.status">{{ statusText(detail.status) }}</span>
        </section>
        <div class="passport-grid">
          <div><span>款式分类</span><b>{{ detail.styleItemName || '未分类' }}</b></div>
          <div><span>材质大类</span><b>{{ materialGroupText(detail.materialGroup) }}</b></div>
          <div><span>材质 / 成色</span><b>{{ detail.materialName || '未设' }} · {{ detail.metalName || '未设' }}</b></div>
          <div><span>当前数量</span><b>{{ number(detail.qty) }} {{ detail.unit || (detail.itemMode === 1 ? '件' : '捆') }}</b></div>
          <div><span>单件重量</span><b>{{ detail.weight ? number(detail.weight) + ' g' : '未记录' }}</b></div>
          <div><span>计价方式</span><b>{{ pricingText(detail) }}</b></div>
          <div><span>工费</span><b>{{ laborText(detail) }}</b></div>
          <div><span>包装费</span><b>¥{{ money(detail.packagingFee) }} / 件</b></div>
          <div><span>{{ Number(detail.pricingMode) === 2 ? '金属计价' : '销售价' }}</span><b>{{ Number(detail.pricingMode) === 2 ? '按当前金价结算' : '¥' + money(detail.salePrice) }}</b></div>
          <div><span>采购成本</span><b>¥{{ money(detail.costPrice) }}</b></div>
        </div>
        <div class="print-entry">
          <div><span>LABEL PRINT</span><h3>商品标签</h3><p>选择模板，按当前商品的入库档案生成标签。</p></div>
          <el-button v-hasPermi="['jingyue:stock:list','jingyue:stock:query']" type="primary" plain @click="openPrintDialog"><el-icon><Printer /></el-icon>打印标签</el-button>
        </div>
        <div class="ledger-head"><div><span>MOVEMENT LOG</span><h3>库存流水</h3></div><el-button link type="primary" :loading="ledgerLoading" @click="loadLedger">刷新</el-button></div>
        <div v-loading="ledgerLoading" class="ledger-list">
          <template v-if="ledgerRows.length">
            <article v-for="item in ledgerRows" :key="item.id" class="ledger-item">
              <div class="ledger-dot" :class="item.changeQty >= 0 ? 'in' : 'out'"></div>
              <div class="ledger-copy">
                <b>{{ ledgerType(item.changeType) }}</b>
                <p>{{ item.bizTypeText || item.bizType || '库存变动' }}{{ item.remark ? ' · ' + item.remark : '' }}</p>
                <span>{{ dateTime(item.bizTime) }} · {{ item.createBy || 'system' }}</span>
              </div>
              <div class="ledger-change"><strong :class="item.changeQty >= 0 ? 'up' : 'down'">{{ item.changeQty >= 0 ? '+' : '' }}{{ number(item.changeQty) }}</strong><small>{{ number(item.beforeQty) }} → {{ number(item.afterQty) }}</small></div>
            </article>
          </template>
          <div v-else class="ledger-empty"><el-icon><Document /></el-icon><p>暂无库存流水，确认入库后会自动生成记录。</p></div>
        </div>
      </template>
    </el-drawer>

    <el-dialog v-model="printDialogOpen" title="选择商品标签模板" width="480px" append-to-body>
      <div class="print-item">
        <span>{{ detail?.goodsNo }}</span><b>{{ detail?.styleName }}</b><small>{{ detail?.barcode || '暂无条码' }} · {{ detail?.styleItemName || '未分类' }}</small>
      </div>
      <el-select v-model="printTemplateId" filterable placeholder="选择打印模板" style="width: 100%">
        <el-option v-for="template in availablePrintTemplates" :key="template.id" :label="template.templateName + ' · ' + template.widthMm + '×' + template.heightMm + 'mm'" :value="template.id" />
      </el-select>
      <p class="print-hint">{{ defaultPrintHint }}</p>
      <img v-if="printPreviewImage" class="stock-label-preview" :src="printPreviewImage" alt="标签预览" />
      <template #footer><el-button @click="printDialogOpen = false">取消</el-button><el-button type="primary" :loading="printLoading" :disabled="!printTemplateId" @click="printLabel">打印标签</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyStock">
import { Search, Goods, Warning, Opportunity as Gem, Right, Box, Document, Printer, CopyDocument } from '@element-plus/icons-vue'
import { useClipboard } from '@vueuse/core'
import { listStock, getStockSummary, getStockLedger } from '@/api/jingyue/stock'
import { getGoodsOptions } from '@/api/jingyue/goods'
import { getPrintOptions, renderPrintLabel } from '@/api/jingyue/homeConfig'
import { agentPrint, agentPreview } from '@/api/jingyue/printAgent'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const summaryLoading = ref(false)
const ledgerLoading = ref(false)
const rows = ref([])
const total = ref(0)
const categories = ref([])
const summary = reactive({ onShelfCount: 0, totalQty: 0, stockCost: 0, stockSale: 0, lowStockCount: 0 })
const query = reactive({ pageNum: 1, pageSize: 15, keyword: '', status: undefined, styleItemId: undefined })
const detailOpen = ref(false)
const detail = ref(null)
const ledgerRows = ref([])
const printDialogOpen = ref(false)
const printLoading = ref(false)
const printTemplates = ref([])
const printBindings = ref([])
const printTemplateId = ref(null)
const printPreviewImage = ref('')
const { copy, isSupported } = useClipboard({ legacy: true })
const statusOptions = [
  { value: 1, label: '在库' }, { value: 2, label: '定金锁定' }, { value: 3, label: '销售出库' },
  { value: 4, label: '赠送出库' }, { value: 7, label: '入库中' }, { value: 8, label: '返厂出库' },
  { value: 5, label: '盘亏' }, { value: 6, label: '已停用' }
]
const statusTabs = [{ value: undefined, label: '全部' }, ...statusOptions.slice(0, 5), statusOptions[5]]
const statusCountMap = reactive({})
const availablePrintTemplates = computed(() => printTemplates.value.filter((it) => it.status === 0))
const defaultPrintTemplate = computed(() => {
  if (!detail.value) return null
  const category = categories.value.find((it) => it.itemName === detail.value.styleItemName)
  const rootId = category?.parentId || category?.id
  const templateId = printBindings.value.find((it) => it.styleItemId === rootId)?.templateId
  return availablePrintTemplates.value.find((it) => it.id === templateId) || null
})
const defaultPrintHint = computed(() => defaultPrintTemplate.value
  ? '已按款式分类匹配「' + defaultPrintTemplate.value.templateName + '」，也可以临时选择其他模板。'
  : '该款式分类还没有绑定模板，可先在静悦基础 → 标签打印模板中绑定。')

const statusText = (status) => statusOptions.find((it) => it.value === status)?.label || '未知'
const statusCount = (status) => statusCountMap[status] ?? 0
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const number = (value) => Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
const dateText = (value) => value ? String(value).slice(0, 10) : '—'
const dateTime = (value) => value ? String(value).replace('T', ' ').slice(0, 16) : '—'
const categoryLabel = (item) => item.parentId ? '　└ ' + item.itemName : item.itemName
const ledgerType = (type) => ({ 1: '采购入库', 2: '销售出库', 3: '盘点调整', 4: '预定锁定', 5: '解锁释放', 6: '赠送出库', 7: '退货入库', 8: '入库草稿', 9: '销售作废回库', 10: '定制冲抵', 11: '返厂出库' }[type] || '库存调整')
const codeText = (row) => {
  const goodsNo = row?.goodsNo || ''
  const barcode = row?.barcode || ''
  if (!barcode || barcode === goodsNo) return goodsNo || '无条码'
  return goodsNo ? goodsNo + ' · ' + barcode : barcode
}
const pricingText = (row) => Number(row?.pricingMode) === 2 ? '按克计价' : '按件计价'
const materialGroupText = (value) => ({ 1: '足金类', 2: '足银类', 3: '其他类' }[Number(value)] || '其他类')
const laborText = (row) => {
  const fee = Number(row?.laborFee || 0)
  if (!fee) return '无工费'
  return Number(row?.laborFeeMode) === 2 ? '工费 ' + number(fee) + ' 元/克' : '工费 ' + number(fee) + ' 元/件'
}
const lockText = (row) => {
  if (row?.status === 2 && row.lockBizType === 'custom') return '定制定金锁定'
  if (row?.status === 7) return '入库草稿占用，确认到货后才可在库'
  if (row?.status === 8) return '已返厂出库，不再参与销售'
  return ''
}

async function copyCode(value) {
  const text = String(value || '').trim()
  if (!text) return proxy.$modal.msgWarning('没有可复制的条码')
  if (!isSupported.value) return proxy.$modal.msgWarning('当前浏览器不支持自动复制')
  await copy(text)
  proxy.$modal.msgSuccess('条码已复制')
}

function loadSummary() {
  summaryLoading.value = true
  getStockSummary().then((res) => {
    const data = res.data || {}
    Object.assign(summary, data)
    Object.keys(statusCountMap).forEach((key) => delete statusCountMap[key])
    Object.assign(statusCountMap, data.statusCounts || {})
  }).finally(() => { summaryLoading.value = false })
}
function loadCategories() {
  getGoodsOptions().then((res) => { categories.value = res.data?.categories || [] }).catch(() => {})
}
function loadPrintOptions() {
  getPrintOptions().then((res) => {
    printTemplates.value = res.data?.templates || []
    printBindings.value = res.data?.bindings || []
  }).catch(() => {})
}
function loadList() {
  loading.value = true
  listStock(query).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function refreshAll() { loadSummary(); loadList() }
function search() { query.pageNum = 1; loadList() }
function pickStatus(status) { query.status = status; search() }
function openDetail(row) {
  detail.value = row
  detailOpen.value = true
  ledgerRows.value = []
  loadLedger()
}
function openPrintDialog() {
  if (!detail.value) return
  if (!printTemplates.value.length) loadPrintOptions()
  printPreviewImage.value = ''
  printTemplateId.value = defaultPrintTemplate.value?.id || availablePrintTemplates.value[0]?.id || null
  printDialogOpen.value = true
}
function printLabel() {
  if (!detail.value || !printTemplateId.value) return
  printLoading.value = true
  printPreviewImage.value = ''
  renderPrintLabel({ goodsItemId: detail.value.id, templateId: printTemplateId.value }).then(async (res) => {
    const label = res.data?.label
    if (!label) throw new Error('后端没有返回可打印的标签数据')
    const request = {
      name: String(label.name || detail.value.styleName || '珠宝货品').slice(0, 30),
      sku: String(label.sku || detail.value.goodsNo || '').slice(0, 32),
      price: String(label.price || detail.value.salePrice || '').slice(0, 16),
      barcode: String(label.barcode || '').slice(0, 32),
      widthMm: Math.round(Number(label.widthMm || 50)),
      heightMm: Math.round(Number(label.heightMm || 30)),
      copies: Number(label.copies || 1),
      port: label.port || 'USB,',
      speed: Number(label.speed || 4),
      density: Number(label.density ?? 8),
      gapMm: Number(label.gapMm ?? 2),
      offsetMm: Number(label.offsetMm ?? 4),
      elements: label.elements || [],
      data: label.data || {},
      fields: label.fields || []
    }
    const preview = await agentPreview(request)
    printPreviewImage.value = preview?.previewImage || ''
    await agentPrint(request)
    printDialogOpen.value = false
    proxy?.$modal?.msgSuccess?.('打印指令已发送，请检查标签机出纸')
  }).catch((error) => { proxy?.$modal?.msgError?.(error.message || '标签生成失败') })
    .finally(() => { printLoading.value = false })
}
function loadLedger() {
  if (!detail.value) return
  ledgerLoading.value = true
  getStockLedger({ goodsItemId: detail.value.id, pageNum: 1, pageSize: 50 })
    .then((res) => { ledgerRows.value = res.data?.result || [] })
    .finally(() => { ledgerLoading.value = false })
}
onMounted(() => { loadSummary(); loadCategories(); loadList(); loadPrintOptions() })
</script>

<style scoped>
.jy-stock { min-height: calc(100vh - 110px); padding: 4px 2px 24px; color: #3e3035; }
.stock-hero { position: relative; display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; overflow: hidden; padding: 25px 30px; color: #fff; background: linear-gradient(118deg, #623640 0%, #8e4f5c 52%, #b77580 100%); border-radius: 22px; box-shadow: 0 18px 38px rgba(111,60,73,.17); }
.hero-copy { position: relative; z-index: 2; }
.eyebrow { color: #ecd6bd; font-size:11.5px; letter-spacing: 2.8px; }
.hero-copy h1 { margin: 7px 0 7px; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 31px; font-weight: 500; letter-spacing: 1px; }
.hero-copy p { max-width: 560px; margin: 0; color: rgba(255,255,255,.72); font-size: 12px; line-height: 1.7; }
.hero-actions { position: relative; z-index: 2; display: flex; align-items: center; gap: 13px; }
.hero-actions :deep(.el-button) { color: #fff; background: rgba(255,255,255,.1); border-color: rgba(255,255,255,.3); }
.live-note { display: flex; align-items: center; gap: 7px; color: rgba(255,255,255,.68); font-size:12px; }
.live-note i { width: 6px; height: 6px; background: #d6bc95; border-radius: 50%; box-shadow: 0 0 0 4px rgba(214,188,149,.12); }
.hero-orbit { position: absolute; border: 1px solid rgba(255,255,255,.09); border-radius: 50%; pointer-events: none; }
.orbit-one { right: 150px; top: -120px; width: 310px; height: 310px; }
.orbit-two { right: 80px; bottom: -175px; width: 330px; height: 330px; }
.metric-board { display: grid; grid-template-columns: 1.3fr 1fr 1fr 1fr; gap: 0; margin: -14px 18px 20px; background: #fff; border: 1px solid #f0e2e4; border-radius: 18px; box-shadow: 0 12px 30px rgba(98,54,64,.07); }
.metric-board article { position: relative; min-height: 124px; padding: 22px 24px; overflow: hidden; }
.metric-board article + article { border-left: 1px solid #f5e9ea; }
.metric-board article > span, .metric-label { display: block; color: #9b8189; font-size:12px; letter-spacing: .4px; }
.metric-board article > strong { display: block; margin-top: 14px; color: #4b353c; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 25px; font-weight: 500; }
.metric-board article > strong em { color: #a88b92; font-family: inherit; font-size: 11px; font-style: normal; }
.metric-board article > small { display: block; margin-top: 9px; color: #b29ba1; font-size:11.5px; }
.metric-feature { color: #fff; background: linear-gradient(140deg, #9c5b67, #b87882); }
.metric-feature .metric-head { display: flex; align-items: center; justify-content: space-between; color: rgba(255,255,255,.72); font-size:12px; }
.metric-feature .metric-head .el-icon { color: #e8d1b5; font-size: 17px; }
.metric-feature > strong { color: #fff !important; font-size: 30px !important; }
.metric-feature strong em { color: rgba(255,255,255,.65) !important; }
.metric-feature p { margin: 9px 0 0; color: rgba(255,255,255,.64); font-size:11.5px; }
.metric-foot { position: absolute; right: 22px; bottom: 19px; left: 24px; display: flex; justify-content: space-between; color: rgba(255,255,255,.62); font-size:11.5px; }
.metric-foot b { color: #f1ddc4; font-weight: 500; }
.money { letter-spacing: -.5px; }
.metric-spark { position: absolute; right: 20px; bottom: 18px; display: flex; align-items: flex-end; gap: 4px; height: 24px; }
.metric-spark i { width: 4px; border-radius: 3px; background: #f0dfe1; }
.metric-spark i:nth-child(1) { height: 9px; }.metric-spark i:nth-child(2) { height: 17px; }.metric-spark i:nth-child(3) { height: 12px; }.metric-spark i:nth-child(4) { height: 23px; }.metric-spark i:nth-child(5) { height: 16px; }.metric-spark i:nth-child(6) { height: 21px; }
.metric-spark.value i { background: #efe0c9; }
.metric-board article.alert { background: #fffaf5; }
.metric-board article.alert > strong { color: #a96853; }
.low-hint { position: absolute; right: 20px; bottom: 17px; display: flex; align-items: center; gap: 4px; padding: 4px 8px; color: #a96853; background: #fbece5; border-radius: 20px; font-size:11.5px; }
.stock-panel { background: #fff; border: 1px solid #f0e2e4; border-radius: 20px; box-shadow: 0 8px 22px rgba(111,60,73,.04); }
.panel-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 22px; padding: 22px 24px 16px; }
.section-kicker { color: #c0a080; font-size:11px; letter-spacing: 2.3px; }
.panel-head h2 { margin: 5px 0 3px; color: #4c363d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 22px; font-weight: 500; }
.panel-head p { margin: 0; color: #a78e95; font-size:12px; }
.filters { display: flex; gap: 8px; }
.filters .el-input { width: 240px; }.filters .el-select { width: 148px; }
.status-tabs { display: flex; gap: 3px; margin: 0 24px; padding: 4px; background: #fcf7f5; border-radius: 10px; width: fit-content; }
.status-tabs button { display: flex; align-items: center; gap: 7px; padding: 7px 12px; color: #987e85; background: transparent; border: 0; border-radius: 7px; font-size:12px; cursor: pointer; transition: .2s; }
.status-tabs button small { display: grid; place-items: center; min-width: 17px; height: 17px; padding: 0 4px; color: #ac9399; background: #f4e7e8; border-radius: 9px; font-size:11px; }
.status-tabs button.active { color: #97616b; background: #fff; box-shadow: 0 4px 12px rgba(124,73,84,.09); }
.status-tabs button.active small { color: #fff; background: #b77b85; }
.stock-list { min-height: 380px; padding: 10px 18px 4px; }
.stock-row { display: grid; grid-template-columns: 46px minmax(230px, 1.8fr) 82px 120px 150px 28px; align-items: center; gap: 14px; margin: 4px 0; padding: 13px 12px; border: 1px solid transparent; border-radius: 13px; cursor: pointer; transition: .2s; }
.stock-row:hover { background: #fdf9f8; border-color: #f3e5e7; transform: translateX(2px); }
.item-sign { display: grid; place-items: center; width: 42px; height: 42px; color: #a46a75; background: linear-gradient(145deg, #fbf0f1, #f4e1e4); border: 1px solid #efdadd; border-radius: 13px; }
.item-sign .el-icon { font-size: 18px; }
.item-main { min-width: 0; }
.item-title { display: flex; align-items: center; gap: 8px; }
.item-title b { overflow: hidden; color: #4d363d; font-size: 13px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.item-code { display: flex; align-items: center; gap: 4px; min-width: 0; margin-top: 3px; color: #a58d94; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; letter-spacing: .3px; }
.item-code > span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.copy-code { display: inline-grid; flex: none; place-items: center; width: 22px; height: 22px; margin: -2px 0; padding: 0; color: #b6969e; background: transparent; border: 0; border-radius: 5px; cursor: pointer; }
.copy-code:hover { color: #985364; background: #fbf0f2; }
.item-tags { display: flex; gap: 5px; margin-top: 7px; }
.item-tags span { padding: 2px 6px; color: #9c838a; background: #fbf7f6; border-radius: 4px; font-size:11px; }
.item-unit { display: flex; align-items: center; gap: 7px; margin-top: 6px; color: #a77e88; font-size: 11.5px; }
.item-unit i { width: 3px; height: 3px; background: #d8b6be; border-radius: 50%; }
.badge { display: inline-flex; align-items: center; gap: 4px; padding: 3px 7px; border-radius: 20px; font-size:11px; white-space: nowrap; }
.badge::before { content: ''; width: 4px; height: 4px; background: currentColor; border-radius: 50%; }
.status-1 { color: #55806a; background: #eff8f2; }.status-2 { color: #a47a3f; background: #fcf6e9; }.status-3 { color: #8d7580; background: #f5f1f3; }.status-4 { color: #8c6c9a; background: #f7f1f9; }.status-5 { color: #a55f55; background: #fbefed; }.status-6 { color: #998c8f; background: #f4f2f2; }.status-7 { color: #6b7f9c; background: #eef3fa; }.status-8 { color: #96606c; background: #fbeef0; }
.item-lock { display: flex; align-items: center; gap: 4px; margin-top: 4px; color: #a47a3f; font-size:11.5px; }
.item-lock .el-icon { font-size:12px; }
.item-quantity span, .item-price span, .item-meta span { display: block; color: #b39ba1; font-size:11px; }
.item-quantity b, .item-price b { display: block; margin-top: 4px; color: #5e444b; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; }
.item-quantity small, .item-price small, .item-meta small { display: block; margin-top: 3px; color: #b19a9f; font-size:11px; }
.item-meta b { display: block; margin-top: 4px; color: #725a61; font-size:12px; font-weight: 500; }
.row-action { display: grid; place-items: center; width: 26px; height: 26px; color: #c5b1b5; background: transparent; border: 0; border-radius: 7px; }
.row-action:hover { color: #9c5c68; background: #fbf0f1; }
.empty-stock { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 370px; }
.empty-icon { display: grid; place-items: center; width: 58px; height: 58px; color: #b88977; background: #fbf1ee; border-radius: 50%; }
.empty-icon .el-icon { font-size: 25px; }
.empty-stock h3 { margin: 15px 0 5px; color: #574047; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 18px; font-weight: 500; }
.empty-stock p { margin: 0 0 17px; color: #a58e94; font-size:12px; }
.list-foot { display: flex; align-items: center; justify-content: space-between; padding: 12px 24px 17px; border-top: 1px solid #f8eef0; }
.list-foot > span { color: #b19aa0; font-size:11.5px; }
.drawer-heading { display: flex; flex-direction: column; gap: 3px; }.drawer-heading span { color: #c0a080; font-size:11px; letter-spacing: 2px; }.drawer-heading b { color: #4b363d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }
.passport { display: flex; align-items: center; gap: 15px; padding: 17px; background: linear-gradient(135deg, #fdf6f4, #faf0f1); border: 1px solid #f1dfe1; border-radius: 15px; }
.passport-seal { display: grid; place-items: center; width: 51px; height: 51px; color: #9d626d; background: #fff; border: 1px solid #ebd3d7; border-radius: 50%; box-shadow: 0 5px 12px rgba(127,74,85,.08); }
.passport-seal .el-icon { font-size: 23px; }
.passport-title { flex: 1; }.passport-title span { color: #b18d95; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; letter-spacing: .8px; }.passport-title h2 { margin: 3px 0; color: #4c353c; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 21px; font-weight: 500; }.passport-title p { display: flex; align-items: center; gap: 2px; margin: 0; color: #a28b91; font-size:11.5px; }
.passport-grid { display: grid; grid-template-columns: 1fr 1fr; margin: 16px 0 23px; background: #fff; border: 1px solid #f2e5e7; border-radius: 13px; overflow: hidden; }
.passport-grid div { padding: 13px 15px; border-bottom: 1px solid #f7edef; }.passport-grid div:nth-child(odd) { border-right: 1px solid #f7edef; }.passport-grid div:nth-last-child(-n+2) { border-bottom: 0; }
.passport-grid span { display: block; color: #b19aa0; font-size:11px; }.passport-grid b { display: block; margin-top: 5px; color: #60474e; font-size: 11px; font-weight: 500; }
.print-entry { display: flex; align-items: center; justify-content: space-between; gap: 14px; margin: 0 0 18px; padding: 12px 14px; background: #fffaf8; border: 1px solid #f2e2de; border-radius: 10px; }
.print-entry span { color: #c0a080; font-size:11px; letter-spacing: 2px; }
.print-entry h3 { margin: 3px 0; color: #513a41; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 15px; font-weight: 500; }
.print-entry p { margin: 0; color: #a38a90; font-size:11.5px; }
.print-item { display: flex; flex-direction: column; gap: 4px; margin-bottom: 15px; padding: 12px; background: #fdf7f6; border: 1px solid #f1e2e4; border-radius: 8px; }
.print-item span { color: #b08d95; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; }
.print-item b { color: #513a41; font-size: 14px; font-weight: 500; }
.print-item small, .print-hint { color: #a38a90; font-size:12px; }
.print-hint { margin: 9px 0 0; line-height: 1.6; }
.stock-label-preview { display: block; max-width: 100%; max-height: 210px; margin: 12px auto 0; background: #fff; border: 1px solid #ecdfe1; box-shadow: 0 3px 10px rgba(77,48,55,.1); }
.ledger-head { display: flex; align-items: flex-end; justify-content: space-between; padding-bottom: 11px; border-bottom: 1px solid #f2e4e6; }.ledger-head span { color: #c0a080; font-size:11px; letter-spacing: 2px; }.ledger-head h3 { margin: 3px 0 0; color: #513a41; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.ledger-list { min-height: 100px; padding-top: 7px; }
.ledger-item { display: flex; gap: 12px; padding: 12px 2px; border-bottom: 1px solid #faf2f3; }
.ledger-dot { position: relative; flex: none; width: 9px; height: 9px; margin-top: 4px; border-radius: 50%; }.ledger-dot::after { content: ''; position: absolute; top: 12px; left: 4px; width: 1px; height: 44px; background: #f0e2e4; }
.ledger-dot.in { background: #79a889; box-shadow: 0 0 0 4px #f0f8f3; }.ledger-dot.out { background: #b77b85; box-shadow: 0 0 0 4px #fbf1f2; }
.ledger-copy { flex: 1; }.ledger-copy b { color: #5b434a; font-size: 11px; font-weight: 600; }.ledger-copy p { margin: 4px 0; color: #9a848a; font-size:11.5px; }.ledger-copy span { color: #b8a3a8; font-size:11px; }
.ledger-change { text-align: right; }.ledger-change strong { display: block; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; }.ledger-change .up { color: #5f9175; }.ledger-change .down { color: #a86470; }.ledger-change small { color: #b29ca1; font-size:11px; }
.ledger-empty { display: flex; flex-direction: column; align-items: center; padding: 32px 0; color: #b6a0a5; }.ledger-empty .el-icon { font-size: 24px; color: #cdb9a2; }.ledger-empty p { font-size:12px; }
@media (max-width: 1280px) { .metric-board { grid-template-columns: 1.2fr 1fr 1fr; }.metric-board article:nth-child(4) { border-top: 1px solid #f5e9ea; border-left: 0; }.stock-row { grid-template-columns: 46px minmax(210px, 1.8fr) 72px 108px 28px; }.item-meta { display: none; }.panel-head { align-items: flex-start; flex-direction: column; }.filters { width: 100%; }.filters .el-input { flex: 1; width: auto; } }
@media (max-width: 900px) { .stock-hero { align-items: flex-start; flex-direction: column; }.metric-board { grid-template-columns: 1fr 1fr; margin: -12px 8px 14px; }.metric-board article:nth-child(3) { border-top: 1px solid #f5e9ea; border-left: 0; }.filters { flex-wrap: wrap; }.filters .el-select { flex: 1; width: auto; }.stock-row { grid-template-columns: 42px 1fr 28px; }.item-quantity, .item-price { display: none; }.status-tabs { width: auto; margin: 0 14px; overflow-x: auto; }.list-foot { align-items: flex-start; flex-direction: column; } }
</style>

