<template>
  <div class="style-page">
    <header class="page-head">
      <div>
        <span class="kicker">CATEGORY PERFORMANCE</span>
        <h1>款式经营</h1>
        <p>全部款式固定参与对比；一级分类看整体，展开后查看二级款式。金额口径为整单实付分摊后的净额。</p>
      </div>
      <el-radio-group v-model="sortBy" size="small" @change="load">
        <el-radio-button value="amount">销售额</el-radio-button>
        <el-radio-button value="gross">毛利</el-radio-button>
        <el-radio-button value="grossRate">毛利率</el-radio-button>
        <el-radio-button value="pieces">件数</el-radio-button>
      </el-radio-group>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="shortcuts" :clearable="false" @change="load" />
      <el-button :icon="Refresh" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">{{ dateRange?.[0] }} 至 {{ dateRange?.[1] }} · 共 {{ report.rows?.length || 0 }} 个分类</span>
    </section>

    <section class="total-band" v-loading="loading">
      <div class="total-main">
        <span>净销售额</span>
        <b>¥{{ money(report.netAmount) }}</b>
        <small>{{ report.orderCount || 0 }} 张成交单 · {{ decimal(report.netPieces, 0) }} 件</small>
      </div>
      <div class="total-item"><span>毛利</span><b>¥{{ money(report.grossProfit) }}</b><small>净额减成本</small></div>
      <div class="total-item"><span>综合毛利率</span><b>{{ decimal(report.grossRate) }}%</b><small>结构健康度</small></div>
      <div class="total-item"><span>退货冲减</span><b class="warn">¥{{ money(report.returnAmount) }}</b><small>所选日期区间</small></div>
    </section>

    <section class="rank-panel" v-loading="loading">
      <div class="rank-head">
        <span>款式分类</span>
        <span>销售规模与结构</span>
        <span>毛利表现</span>
      </div>
      <div v-if="report.rows?.length" class="rank-list">
        <article v-for="(row, index) in report.rows" :key="row.categoryId" class="rank-row" :class="{ leader: index === 0 && hasSales(row), idle: !hasSales(row) }">
          <div class="rank-name">
            <span class="rank-index">{{ String(index + 1).padStart(2, '0') }}</span>
            <div><b>{{ row.categoryName }}</b><small>{{ hasSales(row) ? `${decimal(row.netPieces, 0)} 件 · ${row.orderCount} 单` : '本期未销售' }}<template v-if="row.children?.length"> · {{ row.children.length }} 个款式</template></small></div>
          </div>
          <div class="rank-scale">
            <div class="scale-copy"><b>¥{{ money(row.netAmount) }}</b><span>占比 {{ decimal(row.share) }}%</span></div>
            <div class="area-track">
              <i class="area-fill" :style="{ width: barWidth(row.netAmount) + '%' }"></i>
              <span class="area-marker" :style="{ left: barWidth(row.netAmount) + '%' }"></span>
            </div>
            <div class="scale-foot"><span>均单价 ¥{{ money(row.avgTicket) }}</span><span>退货 ¥{{ money(row.returnAmount) }}</span></div>
          </div>
          <div class="rank-profit">
            <div><b>¥{{ money(row.grossProfit) }}</b><strong :class="rateTone(row.grossRate)">{{ decimal(row.grossRate) }}%</strong></div>
            <small>成本 ¥{{ money(row.costAmount) }}</small>
          </div>
          <div v-if="row.children?.length" class="child-list">
            <div class="child-head"><span>{{ row.categoryName }} / 二级款式</span><span>净销售额</span><span>件数</span><span>毛利率</span></div>
            <div v-for="child in row.children" :key="child.categoryId" class="child-row">
              <span>{{ child.categoryName }}</span>
              <b>¥{{ money(child.netAmount) }}</b>
              <span>{{ decimal(child.netPieces, 0) }}</span>
              <span :class="rateTone(child.grossRate)">{{ child.netPieces > 0 ? decimal(child.grossRate) + '%' : '-' }}</span>
            </div>
          </div>
        </article>
      </div>
      <el-empty v-else description="还没有维护款式分类" :image-size="70" />
    </section>
  </div>
</template>

<script setup name="JyStylePerformance">
import { onActivated, onMounted, reactive, ref } from 'vue'
import { Refresh } from '@element-plus/icons-vue'
import { getStylePerformance } from '@/api/jingyue/report'
import { serverRange } from '@/utils/serverTime'

const loading = ref(false)
const dateRange = ref(serverRange(29))
const sortBy = ref('amount')
const report = reactive(emptyReport())
let lastLoadedAt = Date.now()
const shortcuts = [
  { text: '近 7 天', value: () => serverRange(6) },
  { text: '近 30 天', value: () => serverRange(29) },
  { text: '近 90 天', value: () => serverRange(89) }
]
function emptyReport() { return { netAmount: 0, grossProfit: 0, grossRate: 0, returnAmount: 0, netPieces: 0, orderCount: 0, rows: [] } }
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function decimal(value, digits = 1) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits }) }
function barWidth(value) {
  const max = Math.max(...(report.rows || []).map(it => Number(it.netAmount || 0)), 1)
  const amount = Number(value || 0)
  return amount <= 0 ? 0 : Math.max(3, Math.round((amount / max) * 100))
}
function hasSales(row) { return Number(row?.salesPieces || 0) > 0 || Number(row?.returnPieces || 0) > 0 }
function rateTone(value) { return Number(value) >= 45 ? 'good' : Number(value) >= 25 ? 'normal' : 'low' }
async function load() {
  loading.value = true
  try {
    const res = await getStylePerformance({ beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1], sortBy: sortBy.value })
    Object.assign(report, emptyReport(), res.data || {})
    lastLoadedAt = Date.now()
  } finally { loading.value = false }
}
onMounted(load)
onActivated(() => {
  if (Date.now() - lastLoadedAt > 1000) load()
})
</script>

<style scoped>
.style-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size:12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.filter-bar { display: flex; align-items: center; gap: 10px; padding: 9px 12px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.filter-bar :deep(.el-date-editor) { width: 255px; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.total-band { display: grid; grid-template-columns: 1.5fr 1fr 1fr 1fr; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.total-main, .total-item { display: flex; flex-direction: column; gap: 4px; padding: 14px 18px; border-right: 1px solid #f5e9ec; }
.total-item:last-child { border-right: 0; }
.total-band span { color: #967f86; font-size: 11px; }
.total-band b { color: #553e46; font-size: 22px; font-weight: 680; }
.total-main b { color: #8f4356; font-size: 27px; }
.total-band small { color: #ad989e; font-size:12px; }
.total-item .warn { color: #b06b63; font-size: 18px; }
.rank-panel { border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.rank-head { display: grid; grid-template-columns: 220px minmax(0, 1fr) 190px; gap: 24px; padding: 10px 18px; background: #fdf8f9; border-bottom: 1px solid #f3e6e9; color: #9b848b; font-size:12px; letter-spacing: .05em; }
.rank-list { display: flex; flex-direction: column; }
.rank-row { display: grid; grid-template-columns: 220px minmax(0, 1fr) 190px; gap: 24px; align-items: center; padding: 14px 18px; border-bottom: 1px solid #f7edef; transition: background .2s; }
.rank-row:last-child { border-bottom: 0; }
.rank-row:hover { background: #fdfafb; }
.rank-row.leader { background: linear-gradient(90deg, #fff9fa 0%, #fff 62%); }
.rank-name { display: flex; align-items: center; gap: 12px; min-width: 0; }
.rank-index { color: #c8adb5; font-size: 11px; font-variant-numeric: tabular-nums; }
.leader .rank-index { color: #a8566a; font-weight: 700; }
.rank-name div { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.rank-name b { font-size: 14px; font-weight: 650; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.rank-name small { color: #a79298; font-size:12px; }
.rank-scale { min-width: 0; }
.scale-copy, .scale-foot { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.scale-copy b { color: #644c54; font-size: 14px; font-weight: 650; }
.scale-copy span, .scale-foot span { color: #a38e94; font-size:12px; }
.area-track { position: relative; height: 18px; margin: 7px 0 5px; background: #faf3f5; overflow: hidden; }
.area-track::after { content: ''; position: absolute; inset: 0; background: repeating-linear-gradient(90deg, transparent 0, transparent calc(25% - 1px), #f2e2e6 calc(25% - 1px), #f2e2e6 25%); pointer-events: none; }
.area-fill { display: block; height: 100%; background: linear-gradient(90deg, #f1dce1 0%, #c9798b 100%); }
.leader .area-fill { background: linear-gradient(90deg, #f0d1d7 0%, #a95065 100%); }
.area-marker { position: absolute; top: 0; bottom: 0; width: 2px; background: #96485b; transform: translateX(-1px); }
.rank-row.idle { background: #fcfbfb; }
.rank-row.idle .area-marker { display: none; }
.rank-row.idle .rank-name b { color: #81747a; }
.rank-profit { display: flex; flex-direction: column; gap: 4px; padding-left: 18px; border-left: 1px solid #f6eaed; }
.rank-profit div { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; }
.rank-profit b { font-size: 14px; font-weight: 650; color: #5f4850; }
.rank-profit strong { font-size: 13px; font-weight: 700; }
.rank-profit small { color: #a38e94; font-size:12px; }
.child-list { grid-column: 2 / -1; margin: -2px 0 2px; padding: 7px 10px 6px 12px; background: #fdf8f9; border-left: 2px solid #eed6dc; }
.child-head, .child-row { display: grid; grid-template-columns: minmax(100px, 1fr) 120px 70px 70px; gap: 10px; align-items: center; }
.child-head { padding-bottom: 4px; color: #b49ba2; font-size:12px; }
.child-row { padding: 4px 0; color: #7a6269; font-size: 11.5px; border-top: 1px solid #f5e9ec; }
.child-row b { color: #6b4d56; font-weight: 600; font-variant-numeric: tabular-nums; }
.child-row > span:nth-child(3), .child-row > span:nth-child(4) { font-variant-numeric: tabular-nums; }
.good { color: #4f8063; }
.normal { color: #9a7840; }
.low { color: #b06262; }
@media (max-width: 1050px) {
  .page-head { align-items: flex-start; flex-direction: column; gap: 10px; }
  .total-band { grid-template-columns: 1fr 1fr; }
  .total-main, .total-item:nth-child(2) { border-bottom: 1px solid #f5e9ec; }
  .rank-head { display: none; }
  .rank-row { grid-template-columns: 190px minmax(0, 1fr); }
  .rank-profit { grid-column: span 2; flex-direction: row; justify-content: space-between; padding-left: 0; border-left: 0; }
  .child-list { grid-column: 1 / -1; }
}
@media (max-width: 700px) {
  .style-page { padding: 12px; }
  .filter-bar { align-items: stretch; flex-direction: column; }
  .filter-bar :deep(.el-date-editor) { width: 100%; }
  .range-note { margin-left: 0; }
  .total-band { grid-template-columns: 1fr; }
  .total-main, .total-item { border-right: 0; border-bottom: 1px solid #f5e9ec; }
  .rank-row { grid-template-columns: 1fr; gap: 10px; padding: 12px; }
  .rank-profit { grid-column: auto; }
  .child-head { display: none; }
  .child-row { grid-template-columns: minmax(70px, 1fr) 100px 48px 52px; gap: 6px; font-size: 11px; }
}
</style>
