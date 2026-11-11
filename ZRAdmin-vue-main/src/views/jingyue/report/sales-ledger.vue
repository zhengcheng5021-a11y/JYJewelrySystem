<template>
  <div class="ledger-page">
    <header class="page-head">
      <div>
        <span class="kicker">SALES & RETURN LEDGER</span>
        <h1>销售报表</h1>
        <p>按明细实付汇总，抹零按行实付比例分摊；退货按退货发生日期冲减，成本与利润同步核算。</p>
      </div>
      <div class="head-actions">
        <el-segmented v-model="viewMode" :options="viewOptions" size="default" />
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
      </div>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="shortcuts" :clearable="false" @change="load" />
      <el-radio-group v-model="quickRange" size="small" @change="applyQuickRange">
        <el-radio-button value="today">今日</el-radio-button>
        <el-radio-button value="week">近 7 天</el-radio-button>
        <el-radio-button value="month">近 30 天</el-radio-button>
      </el-radio-group>
      <div class="group-switch">
        <span>汇总维度</span>
        <el-segmented v-model="groupBy" :options="groupOptions" :disabled="viewMode !== 'summary'" @change="load" />
      </div>
      <div v-if="viewMode === 'details'" class="column-switch">
        <span>明细列</span>
        <el-checkbox-group v-model="visibleDetailColumns" size="small">
          <el-checkbox-button value="material">材质大类</el-checkbox-button>
          <el-checkbox-button value="style">款式大类</el-checkbox-button>
          <el-checkbox-button value="category">款式分类</el-checkbox-button>
        </el-checkbox-group>
      </div>
      <span class="range-note">{{ dateRange?.[0] }} 至 {{ dateRange?.[1] }}</span>
    </section>

    <section class="total-band" v-loading="loading">
      <div class="total-main">
        <span>净销售额</span>
        <b>¥{{ money(report.netAmount) }}</b>
        <small>{{ report.orderCount || 0 }} 张成交单 · 销售 {{ decimal(report.salesPieces, 0) }} 件 · 退货 {{ decimal(report.returnPieces, 0) }} 件</small>
      </div>
      <div class="total-item"><span>销售成本</span><b>¥{{ money(report.costAmount) }}</b><small>退货成本已同步冲减</small></div>
      <div class="total-item"><span>退货金额</span><b class="warn">¥{{ money(report.returnAmount) }}</b><small>所选区间发生额</small></div>
      <div class="total-item"><span>毛利 / 利润率</span><b>¥{{ money(report.grossProfit) }}</b><small :class="rateTone(report.grossRate)">{{ decimal(report.grossRate) }}%</small></div>
      <div class="total-item soft"><span>优惠 / 抹零</span><b>¥{{ money(report.discountAmount) }}</b><small>抹零 ¥{{ money(report.roundOffAmount) }}</small></div>
    </section>

    <section v-show="viewMode === 'summary'" v-loading="loading" class="summary-panel">
      <div v-if="report.rows?.length" class="summary-list">
        <article v-for="(row, index) in report.rows" :key="row.groupKey" class="summary-row" :class="{ leader: index === 0, idle: !row.salesPieces && !row.returnPieces }">
          <div class="row-name">
            <span class="rank">{{ String(index + 1).padStart(2, '0') }}</span>
            <div><b>{{ row.groupName }}</b><small>{{ groupText[groupBy] }} · {{ row.orderCount }} 单 · 销售 {{ decimal(row.salesPieces, 0) }} 件<template v-if="row.returnPieces"> · 退 {{ decimal(row.returnPieces, 0) }} 件</template></small></div>
          </div>
          <div class="row-scale">
            <div class="scale-copy"><b>¥{{ money(row.netAmount) }}</b><span>占比 {{ decimal(row.share) }}%</span></div>
            <div class="scale-track"><i :style="{ width: barWidth(row.netAmount) + '%' }"></i></div>
            <div class="scale-foot"><span>销售 ¥{{ money(row.salesAmount) }}</span><span>退货 <em>¥{{ money(row.returnAmount) }}</em></span></div>
          </div>
          <div class="row-profit">
            <b>¥{{ money(row.grossProfit) }}</b>
            <strong :class="rateTone(row.grossRate)">{{ decimal(row.grossRate) }}%</strong>
            <small>成本 ¥{{ money(row.costAmount) }}</small>
          </div>
          <div class="row-discount">
            <span>优惠</span><b>¥{{ money(row.discountAmount) }}</b>
            <span>抹零</span><b>¥{{ money(row.roundOffAmount) }}</b>
          </div>
        </article>
      </div>
      <el-empty v-else description="所选日期没有销售或退货数据" :image-size="76" />
    </section>

    <section v-show="viewMode === 'details'" v-loading="loading" class="detail-panel">
      <el-table :data="report.details" row-key="detailKey" size="small" border stripe class="detail-table" empty-text="所选日期没有销售明细" max-height="calc(100vh - 330px)" show-overflow-tooltip>
        <el-table-column type="index" label="#" width="46" fixed />
        <el-table-column prop="dealTime" label="成交时间" width="140" fixed>
          <template #default="{ row }">{{ dateTime(row.dealTime) }}</template>
        </el-table-column>
        <el-table-column prop="orderNo" label="销售单号" width="145" fixed show-overflow-tooltip />
        <el-table-column prop="customerName" label="客户" width="100"><template #default="{ row }">{{ row.customerName || '散客' }}</template></el-table-column>
        <el-table-column prop="goodsName" label="商品" min-width="160" fixed show-overflow-tooltip />
        <el-table-column prop="barcode" label="条码 / 货号" width="140" show-overflow-tooltip><template #default="{ row }">{{ row.barcode || row.goodsNo || '-' }}</template></el-table-column>
        <el-table-column v-if="visibleDetailColumns.includes('material')" prop="materialGroupName" label="材质大类" width="92" />
        <el-table-column v-if="visibleDetailColumns.includes('style')" prop="styleParentName" label="款式大类" width="100" />
        <el-table-column v-if="visibleDetailColumns.includes('category')" prop="styleItemName" label="款式分类" width="110" />
        <el-table-column prop="metalName" label="金属成色" width="92" />
        <el-table-column label="克重" width="78" align="right"><template #default="{ row }">{{ row.weight ? decimal(row.weight, 2) : '-' }}</template></el-table-column>
        <el-table-column label="数量" width="86" align="right"><template #default="{ row }">{{ decimal(row.qty, 0) }} {{ row.unit || '件' }}</template></el-table-column>
        <el-table-column label="原价" width="94" align="right"><template #default="{ row }">¥{{ money(row.listAmount) }}</template></el-table-column>
        <el-table-column label="优惠" width="86" align="right"><template #default="{ row }" class="discount-cell">¥{{ money(row.discountAmount) }}</template></el-table-column>
        <el-table-column label="抹零" width="78" align="right"><template #default="{ row }">¥{{ money(row.roundOffAmount) }}</template></el-table-column>
        <el-table-column label="实付" width="100" align="right"><template #default="{ row }"><b class="pay">¥{{ money(row.payAmount) }}</b></template></el-table-column>
        <el-table-column label="退货" width="88" align="right"><template #default="{ row }"><span :class="{ 'return-text': row.returnAmount > 0 }">¥{{ money(row.returnAmount) }}</span></template></el-table-column>
        <el-table-column label="净额" width="100" align="right"><template #default="{ row }">¥{{ money(row.netAmount) }}</template></el-table-column>
        <el-table-column label="成本" width="94" align="right"><template #default="{ row }">¥{{ money(row.costAmount) }}</template></el-table-column>
        <el-table-column label="毛利" width="94" align="right"><template #default="{ row }">¥{{ money(row.grossProfit) }}</template></el-table-column>
        <el-table-column label="利润率" width="82" align="right"><template #default="{ row }"><span :class="rateTone(row.grossRate)">{{ decimal(row.grossRate) }}%</span></template></el-table-column>
        <el-table-column label="退货时间" width="140"><template #default="{ row }">{{ row.returnTime ? dateTime(row.returnTime) : '-' }}</template></el-table-column>
        <el-table-column prop="orderTypeName" label="单据类型" width="92" />
      </el-table>
    </section>
  </div>
</template>

<script setup name="JySalesLedgerReport">
import { onActivated, onMounted, reactive, ref } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import { getSalesLedger } from '@/api/jingyue/report'
import { serverRange, serverTodayText } from '@/utils/serverTime'

const loading = ref(false)
const viewMode = ref('summary')
const groupBy = ref('material')
const quickRange = ref('today')
const dateRange = ref([serverTodayText(), serverTodayText()])
const visibleDetailColumns = ref(['material', 'style', 'category'])
const report = reactive(emptyReport())
let lastLoadedAt = Date.now()

const viewOptions = [{ label: '汇总', value: 'summary' }, { label: '明细', value: 'details' }]
const groupOptions = [{ label: '材质大类', value: 'material' }, { label: '款式大类', value: 'style' }, { label: '款式分类', value: 'category' }]
const shortcuts = [
  { text: '今日', value: () => [serverTodayText(), serverTodayText()] },
  { text: '近 7 天', value: () => serverRange(6) },
  { text: '近 30 天', value: () => serverRange(29) }
]

function emptyReport() {
  return { salesAmount: 0, costAmount: 0, returnAmount: 0, netAmount: 0, grossProfit: 0, grossRate: 0, discountAmount: 0, roundOffAmount: 0, salesPieces: 0, returnPieces: 0, orderCount: 0, rows: [], details: [] }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function decimal(value, digits = 1) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits }) }
function dateTime(value) { if (!value) return '-'; const d = new Date(value); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}` }
function rateTone(value) { return Number(value) >= 45 ? 'good' : Number(value) >= 25 ? 'normal' : 'low' }
function barWidth(value) { const max = Math.max(...(report.rows || []).map(it => Number(it.netAmount || 0)), 1); const amount = Number(value || 0); return amount <= 0 ? 0 : Math.max(3, Math.round(amount / max * 100)) }
const groupText = { material: '材质大类', style: '款式大类', category: '款式分类' }

function applyQuickRange(value) {
  dateRange.value = value === 'week' ? serverRange(6) : value === 'month' ? serverRange(29) : [serverTodayText(), serverTodayText()]
  load()
}
async function load() {
  loading.value = true
  try {
    const res = await getSalesLedger({ beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1], groupBy: groupBy.value })
    Object.assign(report, emptyReport(), res.data || {})
    lastLoadedAt = Date.now()
  } finally { loading.value = false }
}
onMounted(load)
onActivated(() => { if (Date.now() - lastLoadedAt > 1000) load() })
</script>

<style scoped>
.ledger-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size: 12px; font-weight: 700; letter-spacing: .16em; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.head-actions { display: flex; align-items: center; gap: 8px; }
.filter-bar { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; padding: 9px 12px; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; }
.filter-bar :deep(.el-date-editor) { width: 255px; }
.group-switch { display: flex; align-items: center; gap: 8px; padding-left: 10px; border-left: 1px solid #f2e6e8; }
.group-switch > span { color: #a38e94; font-size: 11.5px; }
.column-switch { display: flex; align-items: center; gap: 8px; padding-left: 10px; border-left: 1px solid #f2e6e8; }
.column-switch > span { color: #a38e94; font-size: 11.5px; }
.column-switch :deep(.el-checkbox-group) { display: flex; gap: 4px; }
.column-switch :deep(.el-checkbox-button__inner) { padding: 5px 9px; color: #8b737a; font-size: 11.5px; }
.column-switch :deep(.el-checkbox-button.is-checked .el-checkbox-button__inner) { color: #8f5561; background: #fbf0f2; border-color: #e7cbd1; box-shadow: none; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.total-band { display: grid; grid-template-columns: 1.5fr 1fr 1fr 1.05fr .95fr; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.total-main, .total-item { display: flex; flex-direction: column; gap: 4px; padding: 13px 16px; border-right: 1px solid #f5e9ec; }
.total-item:last-child { border-right: 0; }
.total-main span, .total-item span { color: #a18c92; font-size: 11.5px; }
.total-main b { color: #563e45; font-size: 24px; font-weight: 680; font-variant-numeric: tabular-nums; }
.total-item b { color: #604950; font-size: 17px; font-weight: 650; font-variant-numeric: tabular-nums; }
.total-main small, .total-item small { color: #ad989e; font-size: 11.5px; }
.total-item .warn { color: #b06b63; }
.total-item.soft b { color: #8e7460; }
.summary-panel { background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.summary-row { display: grid; grid-template-columns: minmax(190px, 1.1fr) minmax(260px, 2fr) 150px 112px; gap: 20px; align-items: center; padding: 13px 16px; border-bottom: 1px solid #f7edef; }
.summary-row:last-child { border-bottom: 0; }
.summary-row:hover { background: #fdfafb; }
.summary-row.leader { background: linear-gradient(90deg, #fff8fa, #fff 62%); }
.summary-row.idle { background: #fcfbfb; }
.row-name { display: flex; align-items: center; gap: 10px; min-width: 0; }
.rank { color: #c8adb5; font-size: 11px; font-variant-numeric: tabular-nums; }
.leader .rank { color: #a8566a; font-weight: 700; }
.row-name div { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.row-name b { overflow: hidden; font-size: 13.5px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.row-name small { color: #a79298; font-size: 11.5px; }
.scale-copy, .scale-foot { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.scale-copy b { color: #644c54; font-size: 13.5px; font-weight: 650; font-variant-numeric: tabular-nums; }
.scale-copy span, .scale-foot span { color: #a38e94; font-size: 11.5px; }
.scale-foot em { color: #b06b63; font-style: normal; }
.scale-track { height: 7px; margin: 6px 0 5px; overflow: hidden; background: #faf1f3; border-radius: 4px; }
.scale-track i { display: block; height: 100%; background: linear-gradient(90deg, #eed5da, #c47788); border-radius: inherit; }
.leader .scale-track i { background: linear-gradient(90deg, #edccd3, #a95065); }
.row-profit { display: flex; flex-wrap: wrap; align-items: baseline; gap: 4px 8px; padding-left: 14px; border-left: 1px solid #f5e9ec; }
.row-profit b { color: #5f4850; font-size: 13.5px; font-variant-numeric: tabular-nums; }
.row-profit strong { font-size: 12.5px; font-weight: 700; }
.row-profit small { flex-basis: 100%; color: #a38e94; font-size: 11.5px; }
.row-discount { display: grid; grid-template-columns: 32px 1fr; gap: 4px 8px; color: #a18c92; font-size: 11px; }
.row-discount b { color: #6e565e; font-size: 12px; font-weight: 600; text-align: right; font-variant-numeric: tabular-nums; }
.good { color: #4f8063; }.normal { color: #9a7840; }.low { color: #b06262; }
.detail-panel { background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.detail-table { width: 100%; }
.pay { color: #4f6f95; }.return-text { color: #b06060; }.discount-cell { color: #9c7b55; }
@media (max-width: 1180px) { .total-band { grid-template-columns: 1fr 1fr 1fr; }.total-item:nth-child(3) { border-right: 0; }.total-main, .total-item:nth-child(4) { border-bottom: 1px solid #f5e9ec; }.summary-row { grid-template-columns: minmax(180px, 1fr) minmax(220px, 1.7fr) 140px; }.row-discount { display: none; } }
@media (max-width: 760px) { .ledger-page { padding: 12px; }.page-head { align-items: flex-start; flex-direction: column; }.head-actions { width: 100%; justify-content: space-between; }.filter-bar { align-items: stretch; flex-direction: column; }.filter-bar :deep(.el-date-editor) { width: 100%; }.group-switch, .column-switch { flex-wrap: wrap; padding: 8px 0 0; border-left: 0; border-top: 1px solid #f3e7e9; }.column-switch :deep(.el-checkbox-group) { flex-wrap: wrap; }.range-note { margin-left: 0; }.total-band { grid-template-columns: 1fr 1fr; }.total-item { border-bottom: 1px solid #f5e9ec; }.summary-row { grid-template-columns: 1fr; gap: 10px; padding: 12px; }.row-profit { padding-left: 0; border-left: 0; } }
</style>
