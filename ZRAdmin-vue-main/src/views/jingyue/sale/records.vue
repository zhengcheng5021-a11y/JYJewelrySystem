<template>
  <div class="jy-records">
    <header class="records-hero">
      <div class="hero-copy">
        <span class="eyebrow">SALES LEDGER</span>
        <h1>开单记录</h1>
              </div>
      <div class="hero-actions">
        <el-button plain @click="router.push('/business/sale')"><el-icon><Plus /></el-icon>柜台开单</el-button>
        <el-button plain @click="router.push('/business/sale-return')"><el-icon><RefreshLeft /></el-icon>销售退货</el-button>
      </div>
    </header>

    <section class="filter-card">
      <div class="filter-main">
        <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable :shortcuts="dateShortcuts" @change="search" />
        <el-input v-model="query.keyword" clearable placeholder="单据号 / 客户姓名 / 手机号" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
        <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
          <el-option label="草稿" :value="0" />
          <el-option label="已完成" :value="1" />
          <el-option label="作废" :value="9" />
        </el-select>
        <el-select v-model="query.orderType" clearable placeholder="销售类型" @change="search">
          <el-option label="正常销售" :value="1" />
          <el-option label="赠品出库" :value="2" />
        </el-select>
        <el-button type="primary" plain @click="search"><el-icon><Search /></el-icon>查询</el-button>
      </div>
      <div class="filter-total">共 <b>{{ total }}</b> 张单据</div>
    </section>

    <section class="records-card list-card">
      <div class="card-head">
        <div><span class="section-kicker">ORDERS</span><h2>单据摘要</h2></div>
        <span class="head-hint">点击一行查看明细</span>
      </div>
      <el-table v-loading="loading" :data="rows" :height="320" highlight-current-row row-key="id" class="record-table" empty-text="所选日期内没有销售单" @current-change="selectRow">
        <el-table-column type="index" label="#" width="54" align="center" :index="orderRowIndex" />
        <el-table-column label="单据编号" min-width="170">
          <template #default="{ row }"><b class="order-no">{{ row.orderNo }}</b></template>
        </el-table-column>
        <el-table-column label="客户" min-width="150">
          <template #default="{ row }">
            <div class="customer-cell"><b>{{ row.customerName || '散客' }}</b><small>{{ row.customerPhone || '未留手机号' }}</small></div>
          </template>
        </el-table-column>
        <el-table-column label="成交时间" width="150" align="center">
          <template #default="{ row }"><span class="time-text">{{ dateText(row.dealTime) }}</span></template>
        </el-table-column>
        <el-table-column label="件数" width="76" align="center">
          <template #default="{ row }">{{ number(row.pieceQty) }}</template>
        </el-table-column>
        <el-table-column label="商品金额" width="115" align="right">
          <template #default="{ row }">¥{{ money(row.totalAmount) }}</template>
        </el-table-column>
        <el-table-column label="会员折扣" width="100" align="right">
          <template #default="{ row }"><span class="discount-text">{{ Number(row.discountAmount) ? ('-¥' + money(row.discountAmount)) : '—' }}</span></template>
        </el-table-column>
        <el-table-column label="应收金额" width="110" align="right">
          <template #default="{ row }"><b class="pay-text">¥{{ money(row.payAmount) }}</b></template>
        </el-table-column>
        <el-table-column label="定金对冲" width="100" align="right">
          <template #default="{ row }">{{ Number(row.customOffsetAmount) ? ('¥' + money(row.customOffsetAmount)) : '—' }}</template>
        </el-table-column>
        <el-table-column label="实收" width="100" align="right">
          <template #default="{ row }">¥{{ money(row.cashAmount || row.payAmount) }}</template>
        </el-table-column>
        <el-table-column label="状态" width="92" align="center">
          <template #default="{ row }"><el-tag :type="statusType(row.status)" effect="plain" round size="small">{{ statusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="132" align="center">
          <template #default="{ row }">
            <el-button v-if="row.status === 0" link type="primary" @click.stop="editDraft(row)">继续编辑</el-button>
            <el-button v-if="row.status === 0 || row.status === 1" v-hasPermi="['jingyue:sale:remove']" link type="danger" @click.stop="removeOrder(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pager-row">
        <el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="load" @current-change="load" />
      </div>
    </section>

    <section class="records-card detail-card" v-loading="detailLoading">
      <div class="card-head">
        <div><span class="section-kicker">ITEMS</span><h2>单据明细<em v-if="selected">{{ selected.orderNo }}</em></h2></div>
        <div class="detail-summary" v-if="selected">
          <span>成交价合计 ¥{{ money(selected.totalAmount) }}</span>
          <span v-if="selected.discountAmount" class="discount-text">会员折扣 -¥{{ money(selected.discountAmount) }}</span>
          <span v-if="selected.activityBenefit">活动优惠 -¥{{ money(selected.activityBenefit) }}</span>
          <span>折后金额 ¥{{ money(selected.basePayAmount) }}</span>
          <span v-if="selected.roundOffAmount">抹零 -¥{{ money(selected.roundOffAmount) }}</span>
          <span>应收 <b>¥{{ money(selected.payAmount) }}</b></span>
          <span v-if="selected.customOffsetAmount">定金对冲 -¥{{ money(selected.customOffsetAmount) }}</span>
          <span>实收 ¥{{ money(selected.cashAmount || selected.payAmount) }}</span>
          <span v-if="selected.adjustAmount">调价 ¥{{ money(selected.adjustAmount) }}</span>
        </div>
      </div>
      <template v-if="selected">
        <div class="detail-meta">
          <div><span>客户</span><b>{{ selected.customerName || '散客' }}</b><small>{{ selected.customerPhone || '未留手机号' }}</small></div>
          <div><span>开单时间</span><b>{{ dateText(selected.dealTime) }}</b></div>
          <div><span>备注</span><b>{{ selected.remark || '未填写' }}</b></div>
        </div>
        <el-table :data="selected.items || []" :height="300" class="detail-table" empty-text="该单据没有明细">
          <el-table-column type="index" label="#" width="46" align="center" />
          <el-table-column label="货品" min-width="220">
            <template #default="{ row }">
              <div class="goods-cell"><span class="goods-gem"><el-icon><Gem /></el-icon></span><div><b>{{ row.styleName || row.goodsName }}</b><small>{{ codeText(row) }}</small></div></div>
            </template>
          </el-table-column>
          <el-table-column label="款式分类" min-width="155">
            <template #default="{ row }"><span class="meta-text">{{ row.styleCategoryName || row.styleItemName || '未分类' }}</span></template>
          </el-table-column>
          <el-table-column label="材质" min-width="110">
            <template #default="{ row }"><span class="meta-text">{{ row.materialName || '未设材质' }}</span></template>
          </el-table-column>
          <el-table-column label="销售数量 / 单位" width="132" align="center">
            <template #default="{ row }">{{ number(row.qty) }} {{ row.unit || '件' }}</template>
          </el-table-column>
          <el-table-column label="库存属性数量" width="112" align="center">
            <template #default="{ row }">{{ row.stockQty == null ? '—' : number(row.stockQty) }}</template>
          </el-table-column>
          <el-table-column label="成交价" width="110" align="right">
            <template #default="{ row }">¥{{ money(row.dealPrice) }}</template>
          </el-table-column>
          <el-table-column label="小计" width="110" align="right">
            <template #default="{ row }"><b class="pay-text">¥{{ money(row.lineAmount) }}</b></template>
          </el-table-column>
          <el-table-column label="已退" width="76" align="center">
            <template #default="{ row }"><span :class="{ 'returned': row.returnedQty > 0 }">{{ number(row.returnedQty) }}</span></template>
          </el-table-column>
          <el-table-column label="赠品" width="72" align="center">
            <template #default="{ row }"><el-tag v-if="row.isGift === 1" size="small" effect="plain" type="warning">赠品</el-tag><span v-else class="muted">—</span></template>
          </el-table-column>
        </el-table>
      </template>
      <div v-else class="detail-empty"><el-icon><Document /></el-icon><b>选择上方一张单据</b><p>这里会展示该单的商品明细与金额构成。</p></div>
    </section>
  </div>
</template>

<script setup name="JySaleRecords">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { listSale, getSale, deleteSale } from '@/api/jingyue/sale'
import { serverRange } from '@/utils/serverTime'

const router = useRouter()
const loading = ref(false)
const detailLoading = ref(false)
const rows = ref([])
const total = ref(0)
const selected = ref(null)
const dateRange = ref(defaultRange())

const query = reactive({
  pageNum: 1,
  pageSize: 20,
  keyword: '',
  status: null,
  orderType: null,
  beginTime: dateRange.value?.[0],
  endTime: dateRange.value?.[1]
})

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
function orderRowIndex(index) { return (query.pageNum - 1) * query.pageSize + index + 1 }
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
function statusText(status) {
  return status === 0 ? '草稿' : status === 1 ? '已完成' : status === 9 ? '作废' : '未知'
}
function statusType(status) {
  return status === 0 ? 'warning' : status === 1 ? 'success' : 'info'
}

async function load() {
  loading.value = true
  try {
    query.beginTime = dateRange.value?.[0] || null
    query.endTime = dateRange.value?.[1] || null
    const res = await listSale(query)
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
    if (rows.value.length && !rows.value.some((it) => it.id === selected.value?.id)) {
      await selectRow(rows.value[0])
    }
    if (!rows.value.length) selected.value = null
  } finally {
    loading.value = false
  }
}
function search() {
  query.pageNum = 1
  load()
}
async function selectRow(row) {
  if (!row) return
  detailLoading.value = true
  try {
    const res = await getSale(row.id)
    selected.value = res.data || row
  } catch (error) {
    selected.value = row
  } finally {
    detailLoading.value = false
  }
}
async function removeOrder(row) {
  const tip = row.status === 0
    ? '确定删除草稿 ' + row.orderNo + '？'
    : '确定删除 ' + row.orderNo + '？只有没有退货、并且这张销售单仍是相关货品的最新业务时才能删除。删除后库存、积分和已对冲定金会按原单回退。'
  try {
    await ElMessageBox.confirm(tip, '删除开单', { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' })
  } catch (error) { return }
  await deleteSale(row.id)
  ElMessage.success('销售单已删除')
  if (selected.value?.id === row.id) selected.value = null
  load()
}
function editDraft(row) {
  router.push({ path: '/business/sale', query: { id: row.id } })
}

onMounted(load)
</script>

<style scoped>
.jy-records {
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
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.eyebrow, .section-kicker { display: inline-block; font-size:12px; letter-spacing: 0.22em; color: var(--rose); font-weight: 700; }

.records-hero { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 10px 16px; background: linear-gradient(112deg, #fffafb 0%, #fdf4f6 100%); border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 14px rgba(196, 120, 138, 0.045); position: relative; overflow: hidden; }
.records-hero::before { content: ""; position: absolute; left: 0; top: 12px; bottom: 12px; width: 3px; border-radius: 3px; background: linear-gradient(180deg, var(--rose), #e7b7c2); }
.hero-copy h1 { margin: 3px 0 2px; font-size: 19px; font-weight: 700; letter-spacing: 0.035em; }
.hero-copy p { margin: 0; color: var(--muted); font-size: 11.5px; line-height: 1.55; }
.hero-actions { display: flex; gap: 10px; }

.filter-card { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 10px 16px; background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); }
.filter-main { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; }
.filter-main :deep(.el-input) { width: 220px; }
.filter-main :deep(.el-select) { width: 132px; }
.filter-main :deep(.el-date-editor) { width: 260px; }
.filter-total { font-size: 12px; color: var(--muted); white-space: nowrap; }
.filter-total b { color: var(--rose-deep); font-size: 15px; }

.records-card { background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); overflow: hidden; }
.card-head { display: flex; align-items: center; justify-content: space-between; padding: 15px 20px 12px; border-bottom: 1px solid #f7edef; }
.card-head h2 { margin: 3px 0 0; font-size: 16px; font-weight: 700; display: flex; align-items: baseline; gap: 8px; }
.card-head h2 em { font-style: normal; font-size: 11.5px; color: var(--muted); font-weight: 500; }
.head-hint { font-size: 11px; color: var(--muted); }
.detail-summary { display: flex; gap: 16px; font-size: 12px; color: var(--muted); }
.detail-summary b { color: var(--rose-deep); font-size: 14px; }

.record-table :deep(th.el-table__cell) { background: #fdf6f8; color: #7c6c72; font-size: 11.5px; font-weight: 600; }
.record-table :deep(.el-table__row) { cursor: pointer; }
.record-table :deep(.current-row > td.el-table__cell) { background: #fdf1f4 !important; }
.order-no { color: var(--ink); font-weight: 600; font-size: 12.5px; letter-spacing: 0.01em; }
.customer-cell { display: flex; flex-direction: column; gap: 2px; }
.customer-cell b { font-size: 12.5px; }
.customer-cell small { font-size: 11px; color: var(--muted); }
.time-text { font-size: 12px; color: #6c5c62; }
.pay-text { color: var(--rose-deep); }
.discount-text { color: #c0392b; font-weight: 600; }
.pager-row { display: flex; justify-content: flex-end; padding: 12px 20px; }

.detail-meta { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; padding: 14px 20px; background: #fdfafb; }
.detail-meta > div { display: flex; flex-direction: column; gap: 3px; }
.detail-meta span { font-size:12px; color: var(--muted); }
.detail-meta b { font-size: 12.5px; font-weight: 600; }
.detail-meta small { font-size: 11px; color: var(--muted); }
.detail-table :deep(th.el-table__cell) { background: #fdf6f8; color: #7c6c72; font-size: 11.5px; font-weight: 600; }
.goods-cell { display: flex; align-items: center; gap: 10px; }
.goods-gem { width: 32px; height: 32px; flex: 0 0 32px; border-radius: 9px; display: flex; align-items: center; justify-content: center; background: var(--rose-soft); color: var(--rose); font-size: 15px; }
.goods-cell b { display: block; font-size: 12.5px; }
.goods-cell small { display: block; font-size: 11px; color: var(--muted); margin-top: 2px; }
.meta-text { font-size: 12px; color: #6c5c62; }
.returned { color: #c0742f; }
.muted { color: #c4b6ba; }
.detail-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 46px 0; color: var(--muted); }
.detail-empty .el-icon { font-size: 30px; color: #e3c3cb; }
.detail-empty b { font-size: 13px; color: #7c6c72; }
.detail-empty p { margin: 0; font-size: 11.5px; }

@media (max-width: 1100px) {
  .filter-card { flex-direction: column; align-items: stretch; }
  .detail-meta { grid-template-columns: 1fr 1fr; }
}
</style>
