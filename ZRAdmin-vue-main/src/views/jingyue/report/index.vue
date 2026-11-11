<template>
  <div class="report-page">
    <header class="report-head">
      <div class="head-copy">
        <span class="kicker">SALES PERFORMANCE</span>
        <h1>销售指标</h1>
        <p>客件数衡量连带能力，件单数衡量价格带；退货按发生时间冲减。</p>
      </div>
      <div class="period-tabs" role="tablist">
        <button v-for="item in granularities" :key="item.value" type="button" :class="{ active: granularity === item.value }" @click="changeGranularity(item.value)">{{ item.label }}</button>
      </div>
    </header>

    <section class="filter-band">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels :shortcuts="shortcuts" :clearable="false" @change="load" />
      <el-button type="primary" plain :icon="Refresh" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">{{ rangeNote }}</span>
    </section>

    <section class="metric-strip" v-loading="loading">
      <div class="primary-metric">
        <span>客件数</span>
        <b>{{ decimal(report.piecesPerOrder, 2) }}</b>
        <small :class="piecesTone">{{ piecesHint }}</small>
      </div>
      <div class="primary-metric">
        <span>件单数</span>
        <b>{{ money(report.amountPerPiece) }}</b>
        <small>平均每件成交金额</small>
      </div>
      <div class="secondary-metrics">
        <div><span>净销售额</span><b>{{ money(report.netAmount) }}</b></div>
        <div><span>净销售件数</span><b>{{ decimal(report.netPieces, 0) }}</b></div>
        <div><span>成交单数</span><b>{{ report.orderCount || 0 }}</b></div>
        <div><span>退货冲减</span><b class="return-value">{{ money(report.returnAmount) }}</b></div>
      </div>
    </section>

    <section class="chart-panel">
      <div class="panel-head">
        <div><h2>趋势对比</h2><span>柱形为客件数，折线为件单数</span></div>
        <div class="legend"><i class="bar"></i>客件数<i class="line"></i>件单数</div>
      </div>
      <div ref="chartRef" class="chart" />
      <div v-if="!loading && !report.orderCount" class="empty-tip">所选区间没有已完成的销售单</div>
    </section>

    <section class="detail-panel">
      <div class="panel-head"><div><h2>分期明细</h2><span>金额口径为商品实际实付</span></div></div>
      <el-table :data="pagedTrend" size="small" class="metric-table" empty-text="暂无数据">
        <el-table-column prop="label" label="期间" min-width="110" />
        <el-table-column label="净销售额" min-width="120" align="right"><template #default="{ row }">¥{{ money(row.netAmount) }}</template></el-table-column>
        <el-table-column label="净件数" width="95" align="right"><template #default="{ row }">{{ decimal(row.netPieces, 0) }}</template></el-table-column>
        <el-table-column label="成交单数" width="95" align="right"><template #default="{ row }">{{ row.orderCount }}</template></el-table-column>
        <el-table-column label="客件数" width="100" align="right"><template #default="{ row }"><b class="accent">{{ decimal(row.piecesPerOrder, 2) }}</b></template></el-table-column>
        <el-table-column label="件单数" width="120" align="right"><template #default="{ row }">¥{{ money(row.amountPerPiece) }}</template></el-table-column>
      </el-table>
      <div v-if="trendTotal > trendPageSize" class="trend-pager">
        <el-pagination v-model:current-page="trendPage" v-model:page-size="trendPageSize" :total="trendTotal" :page-sizes="[5, 10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="resetTrendPage" @current-change="syncTrendPage" />
      </div>
    </section>
  </div>
</template>

<script setup name="JySalesReport">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import * as echarts from 'echarts'
import { Refresh } from '@element-plus/icons-vue'
import { getSalesMetrics } from '@/api/jingyue/report'
import { serverRange, serverTodayText } from '@/utils/serverTime'

const loading = ref(false)
const granularity = ref('day')
const dateRange = ref(serverRange(6))
const trendPage = ref(1)
const trendPageSize = ref(5)
const trendTotal = computed(() => (report.trend || []).length)
const pagedTrend = computed(() => {
  const rows = report.trend || []
  const start = (trendPage.value - 1) * trendPageSize.value
  return rows.slice(start, start + trendPageSize.value)
})
const chartRef = ref(null)
const report = reactive(emptyReport())
let chart = null
let resizeObserver = null

const granularities = [
  { value: 'day', label: '日' },
  { value: 'week', label: '周' },
  { value: 'month', label: '月' },
  { value: 'year', label: '年' }
]
const shortcuts = [
  { text: '近 7 天', value: () => serverRange(6) },
  { text: '近 30 天', value: () => serverRange(29) },
  { text: '近 90 天', value: () => serverRange(89) },
  { text: '本年', value: () => [serverTodayText().slice(0, 4) + '-01-01', serverTodayText()] }
]

const rangeNote = computed(() => `${dateRange.value?.[0] || ''} 至 ${dateRange.value?.[1] || ''}`)
const piecesHint = computed(() => {
  const value = Number(report.piecesPerOrder || 0)
  if (!report.orderCount) return '暂无可分析成交'
  if (value >= 3.5) return '优秀，连带销售表现突出'
  if (value >= 2) return '中等偏上，搭配销售有效'
  if (value >= 1.2) return '自然销售水平'
  return '偏低，建议加强搭配推荐'
})
const piecesTone = computed(() => {
  const value = Number(report.piecesPerOrder || 0)
  return value >= 2 ? 'tone-good' : value >= 1.2 ? 'tone-normal' : 'tone-low'
})

function emptyReport() {
  return { beginTime: '', endTime: '', granularity: 'day', salesAmount: 0, returnAmount: 0, netAmount: 0, salesPieces: 0, returnPieces: 0, netPieces: 0, orderCount: 0, piecesPerOrder: 0, amountPerPiece: 0, trend: [] }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function decimal(value, digits = 2) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits }) }
function changeGranularity(value) {
  if (granularity.value === value) return
  granularity.value = value
  if (value === 'year') dateRange.value = [serverTodayText().slice(0, 4) + '-01-01', serverTodayText()]
  else if (value === 'month') dateRange.value = [serverTodayText().slice(0, 7) + '-01', serverTodayText()]
  else dateRange.value = serverRange(value === 'week' ? 83 : 29)
  load()
}

function renderChart() {
  if (!chartRef.value) return
  if (!chart) chart = echarts.init(chartRef.value)
  const rows = report.trend || []
  chart.setOption({
    animationDuration: 450,
    grid: { left: 52, right: 58, top: 30, bottom: 38 },
    tooltip: {
      trigger: 'axis',
      backgroundColor: 'rgba(60, 42, 48, .94)',
      borderWidth: 0,
      textStyle: { color: '#fff', fontSize: 12 },
      formatter(params) {
        const row = rows[params?.[0]?.dataIndex] || {}
        return `${row.label}<br/>客件数 ${decimal(row.piecesPerOrder, 2)}<br/>件单数 ¥${money(row.amountPerPiece)}<br/>成交 ${row.orderCount} 单 / ${decimal(row.netPieces, 0)} 件`
      }
    },
    xAxis: { type: 'category', data: rows.map((it) => it.label), axisLine: { lineStyle: { color: '#ead8dd' } }, axisTick: { show: false }, axisLabel: { color: '#87747a', fontSize: 11 } },
    yAxis: [
      { type: 'value', name: '客件数', nameTextStyle: { color: '#a58b92', fontSize: 11 }, splitLine: { lineStyle: { color: '#f5ecee' } }, axisLabel: { color: '#a58b92', fontSize: 11 } },
      { type: 'value', name: '件单数', nameTextStyle: { color: '#a58b92', fontSize: 11 }, splitLine: { show: false }, axisLabel: { color: '#a58b92', fontSize: 11, formatter: (value) => '¥' + value } }
    ],
    series: [
      { name: '客件数', type: 'bar', barMaxWidth: 26, data: rows.map((it) => Number(it.piecesPerOrder || 0)), itemStyle: { color: '#d991a2', borderRadius: [3, 3, 0, 0] } },
      { name: '件单数', type: 'line', yAxisIndex: 1, smooth: true, symbol: 'circle', symbolSize: 6, data: rows.map((it) => Number(it.amountPerPiece || 0)), lineStyle: { color: '#7d9d8b', width: 2 }, itemStyle: { color: '#7d9d8b' } }
    ]
  }, true)
}
async function load() {
  loading.value = true
  try {
    const res = await getSalesMetrics({ granularity: granularity.value, beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1] })
    Object.assign(report, emptyReport(), res.data || {})
    if (trendPage.value > Math.max(1, Math.ceil((report.trend || []).length / trendPageSize.value))) trendPage.value = 1
    await nextTick()
    renderChart()
  } finally { loading.value = false }
}
function resetTrendPage() { trendPage.value = 1 }
function syncTrendPage() {}
onMounted(async () => {
  await load()
  resizeObserver = new ResizeObserver(() => chart?.resize())
  if (chartRef.value) resizeObserver.observe(chartRef.value)
})
onBeforeUnmount(() => { resizeObserver?.disconnect(); chart?.dispose(); chart = null })
</script>

<style scoped>
.report-page { min-height: 100%; padding: 18px 22px 26px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.report-head { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 2px 2px 4px; }
.kicker { color: #b16d7d; font-size:12px; letter-spacing: .18em; font-weight: 700; }
.head-copy h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; letter-spacing: .01em; }
.head-copy p { margin: 0; color: #938087; font-size: 12px; }
.period-tabs { display: flex; padding: 3px; border: 1px solid #efdde2; border-radius: 8px; background: #fff; }
.period-tabs button { height: 28px; min-width: 44px; border: 0; border-radius: 6px; background: transparent; color: #8f7a80; font-size: 12px; cursor: pointer; }
.period-tabs button.active { background: #f7e7eb; color: #a24c60; font-weight: 650; }
.filter-band { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border: 1px solid #f0dfe3; border-radius: 9px; background: #fff; }
.filter-band :deep(.el-date-editor) { width: 255px; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.metric-strip { display: grid; grid-template-columns: 175px 175px minmax(0, 1fr); min-height: 112px; border: 1px solid #f0dfe3; border-radius: 10px; background: #fff; overflow: hidden; }
.primary-metric { display: flex; flex-direction: column; justify-content: center; gap: 4px; padding: 15px 20px; border-right: 1px solid #f5e9ec; }
.primary-metric span { color: #927d83; font-size: 11.5px; }
.primary-metric b { color: #6f3948; font-size: 27px; line-height: 1.1; font-weight: 680; }
.primary-metric small { font-size:12px; color: #aa959a; }
.primary-metric small.tone-good { color: #4e8968; }
.primary-metric small.tone-normal { color: #9b7b45; }
.primary-metric small.tone-low { color: #b05d68; }
.secondary-metrics { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); align-items: center; padding: 12px 8px; }
.secondary-metrics div { display: flex; flex-direction: column; gap: 6px; padding: 0 18px; border-right: 1px solid #f6edef; }
.secondary-metrics div:last-child { border-right: 0; }
.secondary-metrics span { color: #9c888d; font-size: 11px; }
.secondary-metrics b { font-size: 16px; font-weight: 650; }
.return-value { color: #b56a74 !important; }
.chart-panel, .detail-panel { border: 1px solid #f0dfe3; border-radius: 10px; background: #fff; overflow: hidden; }
.panel-head { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px 0; }
.panel-head h2 { margin: 0 0 3px; font-size: 14px; font-weight: 650; }
.panel-head span { color: #a28e93; font-size: 11px; }
.legend { display: flex; align-items: center; gap: 5px; color: #9b868c; font-size:12px; }
.legend i { width: 14px; height: 4px; margin-left: 8px; border-radius: 2px; }
.legend i.bar { background: #d991a2; }
.legend i.line { height: 2px; background: #7d9d8b; }
.chart { height: 318px; padding: 0 4px 4px; }
.empty-tip { margin-top: -170px; padding-bottom: 150px; text-align: center; color: #b7a4a9; font-size: 12px; pointer-events: none; }
.detail-panel { padding-bottom: 8px; }
.metric-table { margin-top: 8px; }
.metric-table :deep(th.el-table__cell) { background: #fdf7f8; color: #8b747b; font-size: 11.5px; font-weight: 600; }
.metric-table :deep(td.el-table__cell) { color: #5a494e; font-size: 12px; }
.accent { color: #a25364; }
.trend-pager { display: flex; justify-content: flex-end; padding: 12px 14px 14px; }
@media (max-width: 1050px) {
  .report-head { align-items: flex-start; flex-direction: column; gap: 10px; }
  .metric-strip { grid-template-columns: 1fr 1fr; }
  .secondary-metrics { grid-column: span 2; border-top: 1px solid #f5e9ec; }
  .primary-metric:nth-child(2) { border-right: 0; }
}
@media (max-width: 700px) {
  .report-page { padding: 12px; }
  .filter-band { align-items: stretch; flex-direction: column; }
  .filter-band :deep(.el-date-editor) { width: 100%; }
  .range-note { margin-left: 0; }
  .secondary-metrics { grid-template-columns: 1fr 1fr; gap: 12px; padding: 12px; }
  .secondary-metrics div { padding: 0 4px; border-right: 0; }
  .chart { height: 270px; }
}
</style>
