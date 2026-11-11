<template>
  <div class="report-page">
    <header class="page-head">
      <div>
        <span class="kicker">INVENTORY MOVEMENT</span>
        <h1>进销存报表</h1>
        <p>按库存流水还原期初、入库、出库、盘点调整与期末结存，按材质和款式逐级展开。</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="shortcuts" :clearable="false" />
      <el-select v-model="query.materialGroup" clearable placeholder="全部材质大类" class="material-select" @change="load">
        <el-option v-for="item in materialOptions" :key="item.value" :label="item.label" :value="item.value" />
      </el-select>
      <el-select v-model="query.styleParentId" clearable filterable placeholder="全部款式大类" class="style-select" @change="load">
        <el-option v-for="item in styleParents" :key="item.id" :label="item.itemName" :value="item.id" />
      </el-select>
      <el-button type="primary" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">{{ report.beginTime || dateRange?.[0] }} 至 {{ report.endTime || dateRange?.[1] }}</span>
    </section>

    <section class="summary-band" v-loading="loading">
      <article class="summary-main"><span>期末结存</span><b>{{ number(report.endingQty) }}<em> 件</em></b><small>{{ report.itemCount || 0 }} 条库存商品</small></article>
      <article><span>期初结存</span><b>{{ number(report.openingQty) }}</b><small>区间开始前账面</small></article>
      <article><span>期间入库</span><b class="in">{{ number(report.inboundQty) }}</b><small>采购、退货恢复等</small></article>
      <article><span>期间出库</span><b class="out">{{ number(report.outboundQty) }}</b><small>销售、赠送、返厂等</small></article>
      <article><span>盘点调整</span><b :class="tone(report.adjustQty)">{{ signed(report.adjustQty) }}</b><small>盘盈为正、盘亏为负</small></article>
      <article><span>库存成本</span><b>¥{{ money(report.stockCost) }}</b><small>当前结存成本</small></article>
      <article><span>库存售价</span><b>¥{{ money(report.stockSale) }}</b><small>当前结存零售价</small></article>
    </section>

    <section class="table-panel" v-loading="loading">
      <div class="panel-head"><b>库存流转明细</b><span>结存数量按截止日期反推，点击分类可逐级展开</span></div>
      <el-table :data="report.rows" row-key="rowKey" :tree-props="{ children: 'children' }" :indent="30" :row-class-name="rowClassName" default-expand-all class="report-table" empty-text="所选区间没有库存数据" max-height="calc(100vh - 360px)">
        <el-table-column label="材质 / 款式" min-width="300" fixed="left">
          <template #default="{ row }">
            <div class="name-cell" :class="'level-' + row.level">
              <span class="level-dot"></span>
              <div><b><span class="level-chip">{{ levelText(row.level) }}</span>{{ rowName(row) }}</b><small>{{ row.level === 'material' ? '材质大类' : row.level === 'styleParent' ? '款式大类' : '款式分类' }}</small></div>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="商品数" width="82" align="right"><template #default="{ row }">{{ number(row.itemCount) }}</template></el-table-column>
        <el-table-column label="期初" width="95" align="right"><template #default="{ row }">{{ number(row.openingQty) }}</template></el-table-column>
        <el-table-column label="入库" width="95" align="right"><template #default="{ row }"><span class="in">{{ number(row.inboundQty) }}</span></template></el-table-column>
        <el-table-column label="出库" width="95" align="right"><template #default="{ row }"><span class="out">{{ number(row.outboundQty) }}</span></template></el-table-column>
        <el-table-column label="盘点调整" width="105" align="right"><template #default="{ row }"><span :class="tone(row.adjustQty)">{{ signed(row.adjustQty) }}</span></template></el-table-column>
        <el-table-column label="期末" width="95" align="right"><template #default="{ row }"><b class="ending">{{ number(row.endingQty) }}</b></template></el-table-column>
        <el-table-column label="库存成本" width="125" align="right"><template #default="{ row }">¥{{ money(row.stockCost) }}</template></el-table-column>
        <el-table-column label="库存售价" width="125" align="right"><template #default="{ row }">¥{{ money(row.stockSale) }}</template></el-table-column>
        <el-table-column label="总克重" width="100" align="right"><template #default="{ row }">{{ number(row.stockWeight) }} g</template></el-table-column>
      </el-table>
    </section>
  </div>
</template>

<script setup name="JyInventoryReport">
import { computed, onActivated, onMounted, reactive, ref } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import { getInventoryReport } from '@/api/jingyue/report'
import { getGoodsOptions } from '@/api/jingyue/goods'
import { serverRange } from '@/utils/serverTime'

const loading = ref(false)
const dateRange = ref(serverRange(29))
const report = reactive(emptyReport())
const categories = ref([])
const query = reactive({ materialGroup: undefined, styleParentId: undefined })
const shortcuts = [
  { text: '近 7 天', value: () => serverRange(6) },
  { text: '近 30 天', value: () => serverRange(29) },
  { text: '近 90 天', value: () => serverRange(89) }
]
const materialOptions = [{ value: 1, label: '足金类' }, { value: 2, label: '足银类' }, { value: 3, label: '其他类' }]
const styleParents = computed(() => categories.value.filter((item) => !item.parentId))
let lastLoadedAt = 0

function emptyReport() {
  return { beginTime: '', endTime: '', itemCount: 0, openingQty: 0, inboundQty: 0, outboundQty: 0, adjustQty: 0, endingQty: 0, stockCost: 0, stockSale: 0, stockWeight: 0, rows: [] }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function signed(value) { const amount = Number(value || 0); return (amount > 0 ? '+' : '') + number(amount) }
function tone(value) { return Number(value) > 0 ? 'in' : Number(value) < 0 ? 'out' : '' }
function rowName(row) {
  if (row.level === 'material') return row.materialGroupName || '其他类'
  if (row.level === 'styleParent') return row.styleParentName || '未分类'
  return row.styleItemName || row.styleParentName || '未分类'
}
function levelText(level) {
  if (level === 'material') return '一级'
  if (level === 'styleParent') return '二级'
  return '三级'
}
function rowClassName({ row }) {
  return 'hierarchy-row hierarchy-' + row.level
}
function decorate(rows) {
  return (rows || []).map((row) => ({ ...row, rowKey: [row.level, row.materialGroup, row.styleParentId, row.styleItemId, rowName(row)].join('-'), children: decorate(row.children) }))
}
async function loadOptions() {
  const res = await getGoodsOptions()
  categories.value = res.data?.categories || []
}
async function load() {
  loading.value = true
  try {
    const res = await getInventoryReport({ beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1], materialGroup: query.materialGroup, styleParentId: query.styleParentId })
    const data = res.data || {}
    Object.assign(report, emptyReport(), data, { rows: decorate(data.rows) })
    lastLoadedAt = Date.now()
  } finally { loading.value = false }
}
onMounted(() => { loadOptions(); load() })
onActivated(() => { if (Date.now() - lastLoadedAt > 1000) load() })
</script>

<style scoped>
.report-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size: 12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.filter-bar { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; padding: 9px 12px; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; }
.filter-bar :deep(.el-date-editor) { width: 255px; }
.material-select { width: 145px; }.style-select { width: 185px; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.summary-band { display: grid; grid-template-columns: 1.45fr repeat(6, minmax(105px, 1fr)); background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.summary-band article { display: flex; flex-direction: column; gap: 4px; padding: 13px 15px; border-left: 1px solid #f5e9ec; }
.summary-band article:first-child { border-left: 0; background: linear-gradient(135deg, #fffaf9, #fdf4f5); }
.summary-band span { color: #967f86; font-size: 11px; }
.summary-band b { color: #553e46; font-size: 21px; font-weight: 680; font-variant-numeric: tabular-nums; }
.summary-band b em { color: #a58c92; font-size: 11px; font-style: normal; font-weight: 400; }
.summary-band .summary-main b { color: #8f4356; font-size: 27px; }
.summary-band small { color: #ad989e; font-size: 11px; }
.in { color: #4f8063; }.out { color: #b06262; }.ending { color: #844556; }
.table-panel { background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.panel-head { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; padding: 10px 15px; background: #fdf8f9; border-bottom: 1px solid #f3e6e9; }
.panel-head b { color: #61474f; font-size: 13px; }.panel-head span { color: #aa979c; font-size: 11px; }
.name-cell { display: flex; align-items: center; gap: 8px; min-width: 0; transition: margin .18s ease; }
.name-cell > div { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.name-cell b { display: flex; align-items: center; gap: 6px; overflow: hidden; color: #594148; font-size: 12.5px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.name-cell small { color: #b2a0a4; font-size: 10px; }
.level-dot { width: 7px; height: 7px; flex: none; background: #cba5ad; border-radius: 50%; }
.level-styleParent .level-dot { background: #b99b6e; }.level-styleItem .level-dot { background: #8e9f8a; }
.level-material b { color: #7f3f50; font-size: 13px; }
.level-chip { flex: none; padding: 1px 5px; color: #9a7a81; background: #f8edef; border: 1px solid #efdce0; border-radius: 4px; font-size: 9.5px; font-weight: 600; line-height: 15px; }
.level-styleParent { margin-left: 16px; padding-left: 11px; border-left: 2px solid #e3c996; }
.level-styleItem { margin-left: 34px; padding-left: 11px; border-left: 2px solid #c7d5c1; }
.level-styleParent .level-chip { color: #927342; background: #fbf5e9; border-color: #ead9b5; }
.level-styleItem .level-chip { color: #5f7f68; background: #f1f7f1; border-color: #d3e3d4; }
.report-table :deep(.el-table__row) { height: 48px; }
.report-table :deep(.el-table__expand-icon) { color: #9b626f; font-size: 15px; transform: scale(1.12); }
.report-table :deep(.el-table__expand-icon--expanded) { transform: rotate(90deg) scale(1.12); }
.report-table :deep(.hierarchy-material > td) { background: #fbf5f6; }
.report-table :deep(.hierarchy-styleParent > td) { background: #fdfaf5; }
.report-table :deep(.hierarchy-styleItem > td) { background: #fbfdfb; }
.report-table :deep(.hierarchy-row:hover > td) { background: #fff2f4 !important; }
@media (max-width: 1250px) { .summary-band { grid-template-columns: repeat(4, 1fr); }.summary-band article:nth-child(5) { border-left: 0; } }
@media (max-width: 760px) { .report-page { padding: 12px; }.page-head { align-items: flex-start; flex-direction: column; }.filter-bar :deep(.el-date-editor), .material-select, .style-select { width: 100%; }.range-note { margin-left: 0; }.summary-band { grid-template-columns: 1fr 1fr; }.summary-band article { border-top: 1px solid #f5e9ec; }.summary-band article:nth-child(odd) { border-left: 0; }.summary-band article:first-child { grid-column: 1 / -1; } }
</style>
