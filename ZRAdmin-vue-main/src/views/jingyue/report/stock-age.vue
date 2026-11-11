<template>
  <div class="age-page">
    <header class="page-head">
      <div>
        <span class="kicker">STOCK AGE DISTRIBUTION</span>
        <h1>库龄分析</h1>
        <p>按材质、款式大类、款式分类查看库存年龄结构，点击百分比直接查看对应商品。</p>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="query.asOfDate" type="date" value-format="YYYY-MM-DD" placeholder="截止日期" :clearable="false" @change="load" />
      <el-select v-model="query.materialGroup" clearable placeholder="全部材质大类" class="material-select" @change="load">
        <el-option v-for="item in materialOptions" :key="item.value" :label="item.label" :value="item.value" />
      </el-select>
      <el-select v-model="query.styleParentId" clearable filterable placeholder="全部款式大类" class="style-select" @change="load">
        <el-option v-for="item in styleParents" :key="item.id" :label="item.itemName" :value="item.id" />
      </el-select>
      <el-button type="primary" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">库存 {{ number(report.totalQty) }} 件 · 成本 ¥{{ money(report.totalCost) }} · 总重 {{ number(report.totalWeight) }} g</span>
    </section>

    <section class="bucket-band" v-loading="loading">
      <article v-for="bucket in report.buckets" :key="bucket.key" :class="'bucket-' + bucket.key" @click="openDetails(bucket, null)">
        <span>{{ bucket.label }}</span>
        <b>{{ decimal(bucket.percent) }}<em>%</em></b>
        <div class="bucket-track"><i :style="{ width: Math.min(100, Number(bucket.percent || 0)) + '%' }"></i></div>
        <small>{{ number(bucket.qty) }} 件 · 成本占比 {{ decimal(bucket.costPercent) }}%</small>
      </article>
    </section>

    <section class="table-panel" v-loading="loading">
      <div class="panel-head"><b>分类库龄</b><span>百分比按库存件数计算，颜色越深代表停留时间越长</span></div>
      <el-table :data="report.rows" row-key="rowKey" :tree-props="{ children: 'children' }" :indent="30" :row-class-name="rowClassName" default-expand-all class="age-table" empty-text="当前没有在库商品" max-height="calc(100vh - 365px)">
        <el-table-column label="材质 / 款式" min-width="285" fixed="left">
          <template #default="{ row }">
            <div class="name-cell" :class="'level-' + row.level"><span class="level-dot"></span><div><b><span class="level-chip">{{ levelText(row.level) }}</span>{{ rowName(row) }}</b><small>{{ number(row.totalQty) }} 件 · ¥{{ money(row.totalCost) }}</small></div></div>
          </template>
        </el-table-column>
        <el-table-column v-for="bucket in bucketColumns" :key="bucket.key" :label="bucket.label" min-width="116" align="center">
          <template #default="{ row }">
            <button type="button" class="age-cell" :class="'age-' + bucket.key" :disabled="Number(bucketOf(row, bucket.key)?.qty || 0) <= 0" @click="openDetails(bucketOf(row, bucket.key), row)">
              <span class="age-percent">{{ decimal(bucketOf(row, bucket.key)?.percent) }}%</span>
              <span class="age-bar"><i :style="{ width: Math.min(100, Number(bucketOf(row, bucket.key)?.percent || 0)) + '%' }"></i></span>
              <small>{{ number(bucketOf(row, bucket.key)?.qty) }} 件</small>
            </button>
          </template>
        </el-table-column>
      </el-table>
    </section>

    <el-dialog v-model="detailOpen" :title="detailTitle" fullscreen destroy-on-close class="age-detail-dialog" :close-on-click-modal="false">
      <div class="detail-toolbar">
        <div><b>{{ detailContext.label }}</b><span>{{ detailContext.scope }} · {{ total }} 件商品</span></div>
        <el-button :icon="Refresh" :loading="detailLoading" @click="loadDetails">刷新</el-button>
      </div>
      <el-table v-loading="detailLoading" :data="detailRows" row-key="id" class="detail-table" empty-text="当前区间没有匹配商品" height="calc(100vh - 205px)">
        <el-table-column label="货品" min-width="230" fixed="left"><template #default="{ row }"><div class="goods-cell"><b>{{ row.styleName || '未命名商品' }}</b><small>{{ row.goodsNo }}<template v-if="row.barcode"> · {{ row.barcode }}</template></small></div></template></el-table-column>
        <el-table-column label="材质 / 款式" min-width="200"><template #default="{ row }">{{ row.materialGroupName }} · {{ row.styleParentName }} / {{ row.styleItemName }}</template></el-table-column>
        <el-table-column label="入库日期" width="115"><template #default="{ row }">{{ dateText(row.inboundTime) }}</template></el-table-column>
        <el-table-column label="库龄" width="110" align="right"><template #default="{ row }"><span :class="'age-text age-' + row.bucket">{{ row.bucketLabel }} · {{ row.ageDays }} 天</span></template></el-table-column>
        <el-table-column label="数量" width="86" align="right"><template #default="{ row }">{{ number(row.qty) }} {{ row.unit || '件' }}</template></el-table-column>
        <el-table-column label="单件重量" width="100" align="right"><template #default="{ row }">{{ row.weight ? number(row.weight) + ' g' : '-' }}</template></el-table-column>
        <el-table-column label="成本" width="120" align="right"><template #default="{ row }">¥{{ money(row.costPrice) }}</template></el-table-column>
        <el-table-column label="售价" width="120" align="right"><template #default="{ row }">¥{{ money(row.salePrice) }}</template></el-table-column>
        <el-table-column label="库存成本" width="125" align="right"><template #default="{ row }"><b>¥{{ money(row.stockCost) }}</b></template></el-table-column>
      </el-table>
      <div class="detail-foot"><pagination v-show="total > 0" :total="total" v-model:page="detailQuery.pageNum" v-model:limit="detailQuery.pageSize" @pagination="loadDetails" /></div>
    </el-dialog>
  </div>
</template>

<script setup name="JyStockAgeReport">
import { computed, onActivated, onMounted, reactive, ref } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import { getStockAgeDetails, getStockAgeReport } from '@/api/jingyue/report'
import { getGoodsOptions } from '@/api/jingyue/goods'
import { serverTodayText } from '@/utils/serverTime'

const loading = ref(false)
const detailLoading = ref(false)
const report = reactive(emptyReport())
const categories = ref([])
const query = reactive({ asOfDate: serverTodayText(), materialGroup: undefined, styleParentId: undefined })
const detailOpen = ref(false)
const detailRows = ref([])
const total = ref(0)
const detailContext = reactive({ label: '', scope: '', materialGroup: undefined, styleParentId: undefined, styleItemId: undefined, bucket: '' })
const detailQuery = reactive({ pageNum: 1, pageSize: 20 })
const materialOptions = [{ value: 1, label: '足金类' }, { value: 2, label: '足银类' }, { value: 3, label: '其他类' }]
const styleParents = computed(() => categories.value.filter((item) => !item.parentId))
const bucketColumns = computed(() => report.buckets || [])
const detailTitle = computed(() => detailContext.label ? detailContext.label + ' · ' + detailContext.scope : '库龄商品明细')
let lastLoadedAt = 0

function emptyReport() { return { asOfDate: '', itemCount: 0, totalQty: 0, totalCost: 0, totalWeight: 0, buckets: [], rows: [] } }
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function decimal(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) }
function dateText(value) { return value ? String(value).replace('T', ' ').slice(0, 10) : '-' }
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
function bucketOf(row, key) { return (row.buckets || []).find((bucket) => bucket.key === key) }
async function loadOptions() {
  const res = await getGoodsOptions()
  categories.value = res.data?.categories || []
}
async function load() {
  loading.value = true
  try {
    const res = await getStockAgeReport({ asOfDate: query.asOfDate, materialGroup: query.materialGroup, styleParentId: query.styleParentId })
    const data = res.data || {}
    Object.assign(report, emptyReport(), data, { rows: decorate(data.rows) })
    lastLoadedAt = Date.now()
  } finally { loading.value = false }
}
function openDetails(bucket, row) {
  if (!bucket || Number(bucket.qty || 0) <= 0) return
  detailContext.materialGroup = row?.materialGroup ?? query.materialGroup
  detailContext.styleParentId = row?.styleParentId ?? query.styleParentId
  detailContext.styleItemId = row?.level === 'styleItem' ? row.styleItemId : undefined
  detailContext.bucket = bucket.key
  detailContext.label = bucket.label
  detailContext.scope = row ? rowName(row) : '全部库存'
  detailQuery.pageNum = 1
  detailOpen.value = true
  loadDetails()
}
async function loadDetails() {
  detailLoading.value = true
  try {
    const res = await getStockAgeDetails({ ...detailQuery, asOfDate: query.asOfDate, materialGroup: detailContext.materialGroup, styleParentId: detailContext.styleParentId, styleItemId: detailContext.styleItemId, bucket: detailContext.bucket })
    detailRows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  } finally { detailLoading.value = false }
}
onMounted(() => { loadOptions(); load() })
onActivated(() => { if (Date.now() - lastLoadedAt > 1000) load() })
</script>

<style scoped>
.age-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size: 12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.filter-bar { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; padding: 9px 12px; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; }
.filter-bar :deep(.el-date-editor) { width: 160px; }.material-select { width: 145px; }.style-select { width: 185px; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.bucket-band { display: grid; grid-template-columns: repeat(6, 1fr); gap: 9px; }
.bucket-band article { display: flex; flex-direction: column; gap: 4px; min-width: 0; padding: 11px 12px; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; cursor: pointer; transition: transform .18s, box-shadow .18s; }
.bucket-band article:hover { transform: translateY(-1px); box-shadow: 0 4px 13px rgba(114,66,78,.08); }
.bucket-band span { color: #9c858b; font-size: 11px; }.bucket-band b { color: #5d444c; font-size: 22px; font-weight: 680; }.bucket-band b em { color: #a68f95; font-size: 11px; font-style: normal; font-weight: 400; }
.bucket-band small { color: #b19ca1; font-size: 10.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.bucket-track { height: 4px; overflow: hidden; background: #f6ecef; border-radius: 99px; }.bucket-track i { display: block; height: 100%; background: #b97788; border-radius: inherit; }
.bucket-lt3m b { color: #518067; }.bucket-lt3m .bucket-track i { background: #83a98d; }
.bucket-m3to5 b { color: #8c8051; }.bucket-m3to5 .bucket-track i { background: #baaa75; }
.bucket-within1y b { color: #9e784c; }.bucket-within1y .bucket-track i { background: #c59a65; }
.bucket-y1to3 b { color: #a95f50; }.bucket-y1to3 .bucket-track i { background: #c97b67; }
.bucket-y3to5 b { color: #a34e5f; }.bucket-y3to5 .bucket-track i { background: #bc6a7c; }
.bucket-over5 b { color: #864456; }.bucket-over5 .bucket-track i { background: #9d5568; }
.table-panel { background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; overflow: hidden; }
.panel-head { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; padding: 10px 15px; background: #fdf8f9; border-bottom: 1px solid #f3e6e9; }
.panel-head b { color: #61474f; font-size: 13px; }.panel-head span { color: #aa979c; font-size: 11px; }
.name-cell { display: flex; align-items: center; gap: 8px; min-width: 0; }.name-cell > div { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.name-cell b { display: flex; align-items: center; gap: 6px; overflow: hidden; color: #594148; font-size: 12.5px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.name-cell small { color: #b2a0a4; font-size: 10.5px; }
.level-dot { width: 7px; height: 7px; flex: none; background: #cba5ad; border-radius: 50%; }.level-styleParent .level-dot { background: #b99b6e; }.level-styleItem .level-dot { background: #8e9f8a; }
.level-material b { color: #7f3f50; font-size: 13px; }
.level-chip { flex: none; padding: 1px 5px; color: #9a7a81; background: #f8edef; border: 1px solid #efdce0; border-radius: 4px; font-size: 9.5px; font-weight: 600; line-height: 15px; }
.level-styleParent { margin-left: 16px; padding-left: 11px; border-left: 2px solid #e3c996; }
.level-styleItem { margin-left: 34px; padding-left: 11px; border-left: 2px solid #c7d5c1; }
.level-styleParent .level-chip { color: #927342; background: #fbf5e9; border-color: #ead9b5; }
.level-styleItem .level-chip { color: #5f7f68; background: #f1f7f1; border-color: #d3e3d4; }
.age-table :deep(.el-table__row) { height: 50px; }
.age-table :deep(.el-table__expand-icon) { color: #9b626f; font-size: 15px; transform: scale(1.12); }
.age-table :deep(.el-table__expand-icon--expanded) { transform: rotate(90deg) scale(1.12); }
.age-table :deep(.hierarchy-material > td) { background: #fbf5f6; }
.age-table :deep(.hierarchy-styleParent > td) { background: #fdfaf5; }
.age-table :deep(.hierarchy-styleItem > td) { background: #fbfdfb; }
.age-table :deep(.hierarchy-row:hover > td) { background: #fff2f4 !important; }
.age-cell { display: flex; flex-direction: column; gap: 3px; width: 100%; padding: 4px 2px; color: #7e656c; text-align: left; background: transparent; border: 0; border-radius: 5px; cursor: pointer; }.age-cell:hover:not(:disabled) { background: #fff7f8; }.age-cell:disabled { cursor: default; opacity: .28; }
.age-percent { font-size: 12px; font-weight: 650; font-variant-numeric: tabular-nums; }.age-bar { height: 3px; overflow: hidden; background: #f7edef; border-radius: 99px; }.age-bar i { display: block; height: 100%; background: currentColor; }.age-cell small { color: #b19da2; font-size: 9.5px; }
.age-lt3m { color: #6b9780; }.age-m3to5 { color: #a59664; }.age-within1y { color: #b18957; }.age-y1to3 { color: #bf7160; }.age-y3to5 { color: #b45e71; }.age-over5 { color: #984b60; }
.detail-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 10px; padding: 9px 12px; background: #fdf8f9; border: 1px solid #f1e2e6; border-radius: 7px; }.detail-toolbar > div { display: flex; align-items: baseline; gap: 8px; }.detail-toolbar b { color: #61474f; font-size: 14px; }.detail-toolbar span { color: #a58e95; font-size: 11px; }
.goods-cell { display: flex; flex-direction: column; gap: 2px; min-width: 0; }.goods-cell b { overflow: hidden; color: #594148; text-overflow: ellipsis; white-space: nowrap; }.goods-cell small { color: #ab979c; font-size: 10.5px; }
.age-text { font-size: 11px; }.detail-foot { display: flex; justify-content: flex-end; padding-top: 10px; }
@media (max-width: 1250px) { .bucket-band { grid-template-columns: repeat(3, 1fr); } }
@media (max-width: 760px) { .age-page { padding: 12px; }.page-head { align-items: flex-start; flex-direction: column; }.filter-bar :deep(.el-date-editor), .material-select, .style-select { width: 100%; }.range-note { margin-left: 0; }.bucket-band { grid-template-columns: 1fr 1fr; }.panel-head span { display: none; } }
</style>
