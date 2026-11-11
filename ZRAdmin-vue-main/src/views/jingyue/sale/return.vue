<template>
  <div class="jy-return">
    <header class="return-hero">
      <div class="hero-copy">
        <span class="eyebrow">SALES RETURN</span>
        <h1>销售退货</h1>
        <p>按客户或销售单定位原单，可整单退货，也可勾选部分明细按件退。</p>
      </div>
      <div class="hero-actions">
        <el-button plain @click="router.push('/business/sale')"><el-icon><Plus /></el-icon>柜台开单</el-button>
        <el-button plain @click="router.push('/business/sale-records')"><el-icon><Tickets /></el-icon>开单记录</el-button>
      </div>
    </header>

    <el-tabs v-model="activeTab" class="return-tabs" @tab-change="onTabChange">
      <el-tab-pane label="可退销售单" name="orders" />
      <el-tab-pane label="退货记录" name="history" />
    </el-tabs>

    <template v-if="activeTab === 'orders'">
      <section class="filter-card">
        <div class="filter-main">
          <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable :shortcuts="dateShortcuts" @change="searchOrders" />
          <el-input v-model="query.keyword" clearable placeholder="销售单号 / 客户姓名 / 手机号" :prefix-icon="Search" @keyup.enter="searchOrders" @clear="searchOrders" />
          <el-button type="primary" plain @click="searchOrders"><el-icon><Search /></el-icon>查询</el-button>
        </div>
        <span class="filter-hint">默认显示最近 7 天已完成且未全部退货的销售单</span>
      </section>

      <div class="return-workspace">
        <section class="order-rail">
          <div class="rail-head"><div><span class="section-kicker">ORDERS</span><h2>可退销售单</h2></div><b>{{ orderTotal }}</b></div>
          <div v-loading="orderLoading" class="order-list">
            <template v-if="orders.length">
              <button v-for="row in orders" :key="row.id" type="button" class="order-item" :class="{ active: activeSale && activeSale.id === row.id }" @click="openSale(row)">
                <div class="order-top"><b>{{ row.orderNo }}</b><el-tag size="small" effect="plain" round :type="row.returnStatus === 1 ? 'warning' : 'success'">{{ row.returnStatus === 1 ? '部分退货' : '可退' }}</el-tag></div>
                <div class="order-customer">{{ row.customerName || '散客' }}<small>{{ row.customerPhone || '未留手机号' }}</small></div>
                <div class="order-foot"><span>{{ dateText(row.dealTime) }}</span><b>¥{{ money(row.payAmount) }}</b></div>
              </button>
            </template>
            <div v-else class="rail-empty"><el-icon><Document /></el-icon><b>没有可退的销售单</b><p>换一个日期区间或关键词试试。</p></div>
          </div>
        </section>

        <section class="detail-panel">
          <template v-if="activeSale">
            <div class="detail-head">
              <div>
                <span class="section-kicker">ORIGINAL ORDER</span>
                <h2>{{ activeSale.orderNo }}</h2>
                <p>{{ activeSale.customerName || '散客' }} · {{ activeSale.customerPhone || '未留手机号' }} · {{ dateText(activeSale.dealTime) }}</p>
              </div>
              <div class="detail-money"><span>原单实付</span><b>¥{{ money(activeSale.payAmount) }}</b><small v-if="activeSale.returnedAmount > 0">已退 ¥{{ money(activeSale.returnedAmount) }}</small></div>
              <p v-if="Number(activeSale.customOffsetAmount) > 0" class="offset-note">本单定金对冲 ¥{{ money(activeSale.customOffsetAmount) }}，退货只冲销售成交额，不退定制定金。</p>
            </div>

            <div class="return-body" v-loading="detailLoading">
              <el-table ref="lineTableRef" :data="activeSale.items || []" row-key="id" class="line-table" @selection-change="onSelectionChange">
                <el-table-column type="selection" width="46" :selectable="(row) => row.returnableQty > 0" />
                <el-table-column label="货品" min-width="220">
                  <template #default="{ row }">
                    <div class="goods-cell"><span class="goods-gem"><el-icon><Gem /></el-icon></span><div><b>{{ row.styleName || row.goodsName }}</b><small>{{ codeText(row) }}</small></div></div>
                  </template>
                </el-table-column>
                <el-table-column label="成交价" width="105" align="right">
                  <template #default="{ row }">¥{{ money(row.dealPrice) }}</template>
                </el-table-column>
                <el-table-column label="售出" width="72" align="center">
                  <template #default="{ row }">{{ number(row.qty) }}</template>
                </el-table-column>
                <el-table-column label="已退" width="72" align="center">
                  <template #default="{ row }"><span :class="{ returned: row.returnedQty > 0 }">{{ number(row.returnedQty) }}</span></template>
                </el-table-column>
                <el-table-column label="可退" width="72" align="center">
                  <template #default="{ row }"><b class="returnable">{{ number(row.returnableQty) }}</b></template>
                </el-table-column>
                <el-table-column label="退货数量" width="120" align="center">
                  <template #default="{ row }">
                    <el-input-number v-model="row.returnQty" :min="0" :max="row.returnableQty" :precision="0" size="small" controls-position="right" :disabled="row.returnableQty <= 0 || !isSelected(row)" />
                  </template>
                </el-table-column>
                <el-table-column label="预估退款" width="110" align="right">
                  <template #default="{ row }"><b class="pay-text">¥{{ money(estimateRow(row)) }}</b></template>
                </el-table-column>
              </el-table>

              <div class="return-form">
                <div class="form-fields">
                  <label><span>退货原因</span><el-input v-model="form.reason" maxlength="200" placeholder="例如：尺寸不合适 / 客户改款" /></label>
                  <label class="remark"><span>退货备注</span><el-input v-model="form.remark" maxlength="500" placeholder="补充说明，选填" /></label>
                </div>
                <div class="return-summary">
                  <div><span>已选明细</span><b>{{ pickedItems.length }} <em>条</em></b></div>
                  <div><span>退货件数</span><b>{{ pickedQty }} <em>件</em></b></div>
                  <div class="estimate"><span>预估退款</span><b>¥{{ money(estimateTotal) }}</b></div>
                </div>
              </div>

              <div class="return-actions">
                <el-button plain @click="clearSelection">重置选择</el-button>
                <el-button type="primary" :loading="submitting" :disabled="!pickedItems.length" @click="submitReturn">提交退货</el-button>
              </div>
              <p class="return-note"><el-icon><InfoFilled /></el-icon>退款金额由后端按原单实付比例分摊计算；退货时间由服务器自动记录，提交后自动回补库存并扣回相应积分。</p>
            </div>
          </template>
          <div v-else class="panel-empty"><el-icon><RefreshLeft /></el-icon><b>从左侧选择一张销售单</b><p>选择后可勾选明细进行整单或部分退货。</p></div>
        </section>
      </div>
    </template>

    <template v-else>
      <section class="filter-card">
        <div class="filter-main">
          <el-date-picker v-model="historyRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable :shortcuts="dateShortcuts" @change="searchHistory" />
          <el-input v-model="historyQuery.keyword" clearable placeholder="退货单号或原因" :prefix-icon="Search" @keyup.enter="searchHistory" @clear="searchHistory" />
          <el-button type="primary" plain @click="searchHistory"><el-icon><Search /></el-icon>查询</el-button>
        </div>
      </section>
      <section class="history-card">
        <el-table v-loading="historyLoading" :data="historyRows" class="line-table" empty-text="所选日期内没有退货记录">
          <el-table-column label="退货单号" min-width="160"><template #default="{ row }"><b>{{ row.returnNo }}</b></template></el-table-column>
          <el-table-column label="原销售单" min-width="160"><template #default="{ row }">{{ row.saleOrderNo }}</template></el-table-column>
          <el-table-column label="客户" min-width="130"><template #default="{ row }">{{ row.customerName || '散客' }}<small class="sub">{{ row.customerPhone }}</small></template></el-table-column>
          <el-table-column label="退货时间" width="150" align="center"><template #default="{ row }">{{ dateText(row.returnTime) }}</template></el-table-column>
          <el-table-column label="件数" width="72" align="center"><template #default="{ row }">{{ number(row.returnQty) }}</template></el-table-column>
          <el-table-column label="退款金额" width="120" align="right"><template #default="{ row }"><b class="pay-text">¥{{ money(row.refundAmount) }}</b></template></el-table-column>
          <el-table-column label="原因" min-width="140"><template #default="{ row }">{{ row.reason || '—' }}</template></el-table-column>
        </el-table>
        <div class="pager-row"><el-pagination v-model:current-page="historyQuery.pageNum" v-model:page-size="historyQuery.pageSize" :total="historyTotal" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="loadHistory" @current-change="loadHistory" /></div>
      </section>
    </template>
  </div>
</template>

<script setup name="JySaleReturn">
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { listReturnOrders, getReturnSaleDetail, saveSaleReturn, listSaleReturns } from '@/api/jingyue/sale'
import { serverRange } from '@/utils/serverTime'

const router = useRouter()
const activeTab = ref('orders')
const orderLoading = ref(false)
const detailLoading = ref(false)
const submitting = ref(false)
const orders = ref([])
const orderTotal = ref(0)
const activeSale = ref(null)
const selectedRows = ref([])
const lineTableRef = ref()
const dateRange = ref(defaultRange())
const historyRange = ref(defaultRange())

const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1] })
const form = reactive({ reason: '', remark: '' })

const historyLoading = ref(false)
const historyRows = ref([])
const historyTotal = ref(0)
const historyQuery = reactive({ pageNum: 1, pageSize: 20, keyword: '', beginTime: historyRange.value?.[0], endTime: historyRange.value?.[1] })

const dateShortcuts = [
  { text: '最近一周', value: () => rangeOf(6) },
  { text: '最近两周', value: () => rangeOf(13) },
  { text: '最近一月', value: () => rangeOf(29) },
  { text: '今天', value: () => rangeOf(0) }
]

function rangeOf(daysAgo) { return serverRange(daysAgo) }
function defaultRange() { return serverRange(6) }
function money(value) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function dateText(value) {
  if (!value) return '—'
  return String(value).replace('T', ' ').slice(0, 16)
}
function codeText(row) {
  const goodsNo = row.goodsNo || ''
  const barcode = row.barcode || ''
  if (!barcode || barcode === goodsNo) return goodsNo || '无条码'
  return goodsNo ? `${goodsNo} · ${barcode}` : barcode
}

const isSelected = (row) => selectedRows.value.some((it) => it.id === row.id)
const pickedItems = computed(() => selectedRows.value
  .filter((row) => Number(row.returnQty || 0) > 0)
  .map((row) => ({ saleOrderItemId: row.id, qty: Number(row.returnQty) })))
const pickedQty = computed(() => pickedItems.value.reduce((sum, it) => sum + it.qty, 0))
const estimateTotal = computed(() => selectedRows.value.reduce((sum, row) => sum + estimateRow(row), 0))

function estimateRow(row) {
  if (!isSelected(row)) return 0
  return Number(row.dealPrice || 0) * Number(row.returnQty || 0)
}

function onSelectionChange(rows) {
  selectedRows.value = rows
  const current = activeSale.value?.items || []
  current.forEach((row) => {
    if (rows.some((it) => it.id === row.id)) {
      if (!row.returnQty || row.returnQty <= 0) row.returnQty = row.returnableQty
    } else {
      row.returnQty = 0
    }
  })
}

async function loadOrders() {
  orderLoading.value = true
  try {
    query.beginTime = dateRange.value?.[0] || null
    query.endTime = dateRange.value?.[1] || null
    const res = await listReturnOrders(query)
    orders.value = res.data?.result || []
    orderTotal.value = res.data?.totalNum || 0
    if (!orders.value.length) {
      activeSale.value = null
      selectedRows.value = []
      return
    }
    const current = activeSale.value && orders.value.find((it) => it.id === activeSale.value.id)
    if (!current) {
      activeSale.value = null
      selectedRows.value = []
      await openSale(orders.value[0])
    }
  } finally {
    orderLoading.value = false
  }
}
function searchOrders() { query.pageNum = 1; loadOrders() }

async function openSale(row) {
  detailLoading.value = true
  selectedRows.value = []
  form.reason = ''
  form.remark = ''
  try {
    const res = await getReturnSaleDetail(row.id)
    const detail = res.data
    detail.items = (detail.items || []).map((item) => ({ ...item, returnQty: 0 }))
    activeSale.value = detail
    await nextTick()
    lineTableRef.value?.clearSelection?.()
  } catch (error) {
    activeSale.value = null
  } finally {
    detailLoading.value = false
  }
}

function clearSelection() {
  selectedRows.value = []
  ;(activeSale.value?.items || []).forEach((row) => { row.returnQty = 0 })
  lineTableRef.value?.clearSelection?.()
}

async function submitReturn() {
  const items = pickedItems.value
  if (!items.length) return ElMessage.warning('请勾选要退货的明细并填写数量')
  if (!form.reason?.trim()) return ElMessage.warning('请填写退货原因')
  try {
    await ElMessageBox.confirm(`确认退 ${items.length} 条明细、共 ${pickedQty.value} 件？提交后将回补库存并扣回积分。`, '提交退货', { type: 'warning', confirmButtonText: '确认退货', cancelButtonText: '取消' })
  } catch (error) {
    return
  }
  submitting.value = true
  try {
    await saveSaleReturn({
      saleOrderId: activeSale.value.id,
      reason: form.reason?.trim() || null,
      remark: form.remark?.trim() || null,
      items
    })
    ElMessage.success('退货已完成，库存与积分已同步')
    activeSale.value = null
    selectedRows.value = []
    loadOrders()
  } catch (error) {
  } finally {
    submitting.value = false
  }
}

async function loadHistory() {
  historyLoading.value = true
  try {
    historyQuery.beginTime = historyRange.value?.[0] || null
    historyQuery.endTime = historyRange.value?.[1] || null
    const res = await listSaleReturns(historyQuery)
    historyRows.value = res.data?.result || []
    historyTotal.value = res.data?.totalNum || 0
  } finally {
    historyLoading.value = false
  }
}
function searchHistory() { historyQuery.pageNum = 1; loadHistory() }

function onTabChange(name) {
  if (name === 'history') loadHistory()
}

onMounted(loadOrders)
</script>

<style scoped>
.jy-return {
  --rose: #c4788a;
  --rose-deep: #a85c6e;
  --rose-soft: #f8e9ed;
  --ink: #3d3236;
  --muted: #8b7a80;
  --line: #f0dfe3;
  --ivory: #fffbfc;
  min-height: 100%;
  padding: 14px 16px 20px;
  background: linear-gradient(180deg, #fdf7f8 0%, #fbf3f5 100%);
  color: var(--ink);
  font-size: 13px;
}
.eyebrow, .section-kicker { display: inline-block; font-size:12px; letter-spacing: 0.22em; color: var(--rose); font-weight: 700; }

.return-hero { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 10px 16px; background: linear-gradient(112deg, #fffafb 0%, #fdf4f6 100%); border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 14px rgba(196, 120, 138, 0.045); position: relative; overflow: hidden; }
.return-hero::before { content: ""; position: absolute; left: 0; top: 12px; bottom: 12px; width: 3px; border-radius: 3px; background: linear-gradient(180deg, var(--rose), #e7b7c2); }
.hero-copy h1 { margin: 3px 0 2px; font-size: 19px; font-weight: 700; letter-spacing: 0.035em; }
.hero-copy p { margin: 0; color: var(--muted); font-size: 11.5px; line-height: 1.55; }
.hero-actions { display: flex; gap: 10px; }

.return-tabs { margin-top: 8px; }
.return-tabs :deep(.el-tabs__header) { margin: 0 0 10px; }
.return-tabs :deep(.el-tabs__item) { font-size: 13px; }

.filter-card { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 10px 16px; background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); margin-bottom: 12px; }
.filter-main { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
.filter-main :deep(.el-input) { width: 230px; }
.filter-main :deep(.el-date-editor) { width: 260px; }
.filter-hint { font-size: 11px; color: var(--muted); white-space: nowrap; }

.return-workspace { display: grid; grid-template-columns: 348px minmax(0, 1fr); gap: 16px; align-items: start; }
.order-rail { background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); overflow: hidden; }
.rail-head { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px 9px; border-bottom: 1px solid #f7edef; }
.rail-head h2 { margin: 3px 0 0; font-size: 15px; font-weight: 700; }
.rail-head > b { font-size: 18px; color: var(--rose-deep); }
.order-list { max-height: 620px; overflow-y: auto; padding: 10px; display: flex; flex-direction: column; gap: 8px; }
.order-item { text-align: left; border: 1px solid var(--line); border-radius: 12px; background: #fff; padding: 11px 13px; cursor: pointer; transition: all 0.16s ease; display: flex; flex-direction: column; gap: 6px; }
.order-item:hover { border-color: #e6bcc6; background: #fdf8f9; }
.order-item.active { border-color: var(--rose); background: var(--rose-soft); box-shadow: 0 6px 14px rgba(196, 120, 138, 0.12); }
.order-top { display: flex; align-items: center; justify-content: space-between; }
.order-top b { font-size: 12.5px; }
.order-customer { font-size: 12px; color: #5c4d52; display: flex; align-items: baseline; gap: 8px; }
.order-customer small { font-size:12px; color: var(--muted); }
.order-foot { display: flex; align-items: center; justify-content: space-between; font-size: 11px; color: var(--muted); }
.order-foot b { color: var(--rose-deep); font-size: 13px; }
.rail-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 50px 0; color: var(--muted); }
.rail-empty .el-icon { font-size: 28px; color: #e3c3cb; }
.rail-empty b { font-size: 13px; color: #7c6c72; }
.rail-empty p { margin: 0; font-size: 11.5px; }

.detail-panel { background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); overflow: hidden; min-height: 420px; }
.detail-head { display: flex; align-items: flex-start; justify-content: space-between; padding: 16px 20px; border-bottom: 1px solid #f7edef; background: #fdfafb; }
.detail-head h2 { margin: 3px 0 4px; font-size: 17px; font-weight: 700; }
.detail-head p { margin: 0; font-size: 12px; color: var(--muted); }
.detail-money { text-align: right; display: flex; flex-direction: column; gap: 2px; }
.detail-money span { font-size:12px; color: var(--muted); }
.detail-money b { font-size: 20px; color: var(--rose-deep); }
.detail-money small { font-size:12px; color: #c0742f; }
.return-body { padding: 0 0 16px; }
.line-table :deep(th.el-table__cell) { background: #fdf6f8; color: #7c6c72; font-size: 11.5px; font-weight: 600; }
.goods-cell { display: flex; align-items: center; gap: 10px; }
.goods-gem { width: 32px; height: 32px; flex: 0 0 32px; border-radius: 9px; display: flex; align-items: center; justify-content: center; background: var(--rose-soft); color: var(--rose); font-size: 15px; }
.goods-cell b { display: block; font-size: 12.5px; }
.goods-cell small { display: block; font-size: 11px; color: var(--muted); margin-top: 2px; }
.returned { color: #c0742f; }
.returnable { color: var(--rose-deep); }
.pay-text { color: var(--rose-deep); }

.return-form { display: flex; flex-direction: column; gap: 14px; padding: 16px 20px 0; }
.form-fields { display: grid; grid-template-columns: 1fr 1.6fr; gap: 12px; }
.form-fields label { display: flex; flex-direction: column; gap: 5px; }
.form-fields label > span { font-size: 11.5px; color: var(--muted); font-weight: 600; }
.return-summary { display: flex; gap: 22px; align-items: flex-end; padding: 12px 16px; background: #fdf8f9; border: 1px solid #f2dfe4; border-radius: 12px; }
.return-summary > div { display: flex; flex-direction: column; gap: 2px; }
.return-summary span { font-size:12px; color: var(--muted); }
.return-summary b { font-size: 16px; color: var(--ink); }
.return-summary b em { font-style: normal; font-size:12px; color: var(--muted); }
.return-summary .estimate { margin-left: auto; text-align: right; }
.return-summary .estimate b { font-size: 22px; color: var(--rose-deep); }
.return-actions { display: flex; justify-content: flex-end; gap: 10px; padding: 0 20px; margin-top: 14px; }
.return-actions :deep(.el-button) { border-radius: 10px; height: 36px; }
.return-note { display: flex; gap: 6px; margin: 10px 20px 0; font-size:12px; color: var(--muted); line-height: 1.6; }
.return-note .el-icon { color: var(--rose); margin-top: 2px; flex: 0 0 auto; }

.panel-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 90px 0; color: var(--muted); }
.panel-empty .el-icon { font-size: 34px; color: #e3c3cb; }
.panel-empty b { font-size: 13px; color: #7c6c72; }
.panel-empty p { margin: 0; font-size: 11.5px; }

.history-card { background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); overflow: hidden; }
.sub { display: block; font-size:12px; color: var(--muted); }
.pager-row { display: flex; justify-content: flex-end; padding: 12px 20px; }

@media (max-width: 1180px) {
  .return-workspace { grid-template-columns: 1fr; }
  .order-list { max-height: 300px; }
  .form-fields { grid-template-columns: 1fr; }
}
@media (max-width: 700px) {
  .jy-return { padding: 10px 10px 16px; overflow-x: hidden; }
  .return-hero { align-items: flex-start; flex-direction: column; padding: 12px 14px; }
  .hero-actions { width: 100%; }
  .hero-actions :deep(.el-button) { flex: 1; margin-left: 0; }
  .filter-card { align-items: stretch; flex-direction: column; gap: 8px; padding: 10px 12px; }
  .filter-main { align-items: stretch; }
  .filter-main :deep(.el-input), .filter-main :deep(.el-date-editor), .filter-main :deep(.el-button) { width: 100%; }
  .filter-hint { white-space: normal; line-height: 1.5; }
  .return-workspace, .order-rail, .detail-panel, .history-card { width: 100%; min-width: 0; }
  .detail-head { gap: 10px; padding: 13px 14px; }
  .detail-head h2 { font-size: 15px; }
  .detail-money b { font-size: 17px; }
  .return-form { padding: 14px 12px 0; }
  .return-summary { align-items: stretch; flex-wrap: wrap; gap: 10px 18px; }
  .return-summary .estimate { margin-left: 0; text-align: left; }
  .return-actions { padding: 0 12px; }
  .return-actions :deep(.el-button) { flex: 1; }
  .return-note { margin: 10px 12px 0; }
  .pager-row { justify-content: center; padding: 10px; overflow-x: auto; }
}
.offset-note { margin: 6px 0 0; color: #a85c6e; font-size: 12px; }
</style>
