<template>
  <div class="traffic-page">
    <header class="page-head">
      <div>
        <span class="kicker">STORE TRAFFIC</span>
        <h1>客流分析</h1>
        <p>进店与流失手工登记，成交单数和成交金额自动取自销售单据。</p>
      </div>
      <div class="period-tabs">
        <button v-for="item in granularities" :key="item.value" type="button" :class="{ active: granularity === item.value }" @click="changeGranularity(item.value)">{{ item.label }}</button>
      </div>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="shortcuts" :clearable="false" @change="load" />
      <el-button :icon="Refresh" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">{{ dateRange?.[0] }} 至 {{ dateRange?.[1] }}</span>
    </section>

    <section class="summary-band" v-loading="loading">
      <div class="hero-stat">
        <span>进店人数</span>
        <b>{{ number(report.visitorCount) }}</b>
        <small>期间累计</small>
      </div>
      <div class="hero-stat">
        <span>成交单数</span>
        <b>{{ number(report.dealOrderCount) }}</b>
        <small>销售单据自动统计</small>
      </div>
      <div class="hero-stat accent">
        <span>成交率</span>
        <b>{{ report.visitorCount ? decimal(report.dealRate) + '%' : '--' }}</b>
        <small>成交单数 / 进店人数</small>
      </div>
      <div class="hero-stat">
        <span>成交金额</span>
        <b>¥{{ money(report.dealAmount) }}</b>
        <small>已扣除退货</small>
      </div>
      <div class="hero-stat">
        <span>加微信</span>
        <b>{{ number(report.wechatCount) }}</b>
        <small>加微率 {{ decimal(report.wechatRate) }}%</small>
      </div>
      <div class="hero-stat muted">
        <span>未成交流失</span>
        <b>{{ number(report.lostTotal) }}</b>
        <small>四类流失合计</small>
      </div>
    </section>

    <section class="content-grid">
      <div class="chart-panel">
        <div class="panel-head">
          <div><h2>客流与成交趋势</h2><span>柱形为进店人数，折线为成交率</span></div>
          <div class="legend"><i class="visitor"></i>进店<i class="rate"></i>成交率</div>
        </div>
        <div ref="chartRef" class="chart" />
      </div>

      <aside class="entry-panel" v-loading="saving">
        <div class="panel-head">
          <div><h2>{{ isToday ? '今日登记' : '历史记录' }}</h2><span>{{ entry.trafficDate }}</span></div>
          <el-tag v-if="!isToday" type="warning" effect="plain" size="small">只读</el-tag>
        </div>
        <div class="entry-form">
          <label><span>日期</span><el-date-picker v-model="entry.trafficDate" type="date" value-format="YYYY-MM-DD" :clearable="false" @change="loadEntry" /></label>
          <div class="entry-pair">
            <label><span>进店人数</span><el-input-number v-model="entry.visitorCount" :min="0" :max="1000000" controls-position="right" /></label>
            <label><span>加微信</span><el-input-number v-model="entry.wechatCount" :min="0" :max="entry.visitorCount" controls-position="right" /></label>
          </div>
          <div class="lost-block">
            <span class="block-label">流失分类</span>
            <label><span>随便逛逛</span><el-input-number v-model="entry.lostStrollCount" :min="0" :max="entry.visitorCount" controls-position="right" /></label>
            <label><span>嫌价格偏高</span><el-input-number v-model="entry.lostPriceCount" :min="0" :max="entry.visitorCount" controls-position="right" /></label>
            <label><span>款式品类没有</span><el-input-number v-model="entry.lostStyleCount" :min="0" :max="entry.visitorCount" controls-position="right" /></label>
            <label><span>回去考虑对比</span><el-input-number v-model="entry.lostConsiderCount" :min="0" :max="entry.visitorCount" controls-position="right" /></label>
          </div>
          <label><span>备注</span><el-input v-model="entry.remark" type="textarea" :rows="2" maxlength="500" placeholder="补充说明，可空" /></label>
          <div class="entry-actions">
            <span v-if="!isToday" class="readonly-note">历史数据已锁定，编辑会二次确认</span>
            <el-button type="primary" :loading="saving" @click="submit">{{ isToday ? '保存今日客流' : '修改历史记录' }}</el-button>
          </div>
        </div>
      </aside>
    </section>

    <section class="analysis-grid">
      <div class="loss-panel">
        <div class="panel-head"><div><h2>流失构成</h2><span>按人次统计，用于调整接待与货品</span></div></div>
        <div class="loss-rows">
          <div v-for="item in lossRows" :key="item.key" class="loss-row">
            <div class="loss-copy"><span>{{ item.label }}</span><b>{{ number(item.value) }}</b></div>
            <div class="loss-track"><i :style="{ width: share(item.value, report.lostTotal) + '%', background: item.color }"></i></div>
            <small>{{ decimal(share(item.value, report.lostTotal)) }}%</small>
          </div>
        </div>
      </div>
      <div class="detail-panel">
        <div class="panel-head"><div><h2>期间明细</h2><span>点击一行回看该日登记</span></div></div>
        <el-table :data="report.trend || []" size="small" max-height="310" highlight-current-row @row-click="pickDay">
          <el-table-column prop="label" label="期间" min-width="90" />
          <el-table-column label="进店" width="70" align="right"><template #default="{ row }">{{ row.visitorCount }}</template></el-table-column>
          <el-table-column label="成交" width="70" align="right"><template #default="{ row }">{{ row.dealOrderCount }}</template></el-table-column>
          <el-table-column label="成交率" width="88" align="right"><template #default="{ row }"><span v-if="hasTraffic(row)">{{ decimal(row.dealRate) }}%</span><span v-else class="no-data">未登记</span></template></el-table-column>
          <el-table-column label="成交额" width="108" align="right"><template #default="{ row }">¥{{ money(row.dealAmount) }}</template></el-table-column>
          <el-table-column label="流失" width="70" align="right"><template #default="{ row }">{{ row.lostTotal }}</template></el-table-column>
        </el-table>
      </div>
    </section>
  </div>
</template>

<script setup name="JyStoreTraffic">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import * as echarts from 'echarts'
import { Refresh } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getStoreTraffic, saveStoreTraffic } from '@/api/jingyue/report'
import { serverRange, serverTodayText } from '@/utils/serverTime'

const loading = ref(false)
const saving = ref(false)
const granularity = ref('day')
const dateRange = ref(serverRange(6))
const report = reactive(emptyReport())
const entry = reactive(emptyEntry(serverTodayText()))
const chartRef = ref(null)
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
  { text: '今天', value: () => serverRange(0) }
]
const isToday = computed(() => entry.trafficDate === serverTodayText())
const lossRows = computed(() => [
  { key: 'stroll', label: '随便逛逛', value: report.lostStrollCount, color: '#d8a6b2' },
  { key: 'price', label: '嫌价格偏高', value: report.lostPriceCount, color: '#c78b72' },
  { key: 'style', label: '款式品类没有', value: report.lostStyleCount, color: '#8e9f91' },
  { key: 'consider', label: '回去考虑对比', value: report.lostConsiderCount, color: '#9a8aa0' }
])

function emptyReport() {
  return { visitorCount: 0, wechatCount: 0, dealOrderCount: 0, dealAmount: 0, dealRate: 0, wechatRate: 0, lostStrollCount: 0, lostPriceCount: 0, lostStyleCount: 0, lostConsiderCount: 0, lostTotal: 0, trend: [] }
}
function emptyEntry(date) {
  return { id: 0, trafficDate: date, visitorCount: 0, wechatCount: 0, lostStrollCount: 0, lostPriceCount: 0, lostStyleCount: 0, lostConsiderCount: 0, remark: '', confirmHistorical: false }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN') }
function decimal(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) }
function share(value, total) { return !total ? 0 : Math.round((Number(value || 0) * 1000) / total) / 10 }
function hasTraffic(row) { return Number(row?.visitorCount || 0) > 0 }
function changeGranularity(value) { granularity.value = value; load() }

async function load() {
  loading.value = true
  try {
    const res = await getStoreTraffic({ granularity: granularity.value, beginTime: dateRange.value?.[0], endTime: dateRange.value?.[1] })
    Object.assign(report, emptyReport(), res.data || {})
    await nextTick()
    renderChart()
  } finally { loading.value = false }
}

async function loadEntry() {
  if (!entry.trafficDate) return
  const res = await getStoreTraffic({ granularity: 'day', beginTime: entry.trafficDate, endTime: entry.trafficDate })
  const row = (res.data?.trend || [])[0]
  Object.assign(entry, emptyEntry(entry.trafficDate), {
    id: row?.id || 0,
    visitorCount: row?.visitorCount || 0,
    wechatCount: row?.wechatCount || 0,
    lostStrollCount: row?.lostStrollCount || 0,
    lostPriceCount: row?.lostPriceCount || 0,
    lostStyleCount: row?.lostStyleCount || 0,
    lostConsiderCount: row?.lostConsiderCount || 0,
    remark: row?.remark || ''
  })
}

function pickDay(row) {
  if (granularity.value !== 'day') return
  entry.trafficDate = row.date
  loadEntry()
}

async function submit() {
  if (!isToday.value) {
    try {
      await ElMessageBox.confirm('这是历史客流记录，修改后会影响报表。确认继续修改吗？', '修改历史数据', { type: 'warning', confirmButtonText: '确认修改', cancelButtonText: '取消' })
    } catch { return }
  }
  saving.value = true
  try {
    await saveStoreTraffic({ ...entry, confirmHistorical: !isToday.value })
    ElMessage.success('客流数据已保存')
    await Promise.all([load(), loadEntry()])
  } finally { saving.value = false }
}

function renderChart() {
  if (!chartRef.value) return
  if (!chart) chart = echarts.init(chartRef.value)
  const rows = report.trend || []
  const latestTraffic = [...rows].reverse().find(it => hasTraffic(it))
  const hasTrafficTrend = !!latestTraffic
  const maximumDealRate = Math.max(0, ...rows.filter(it => hasTraffic(it)).map(it => Number(it.dealRate || 0)))
  chart.setOption({
    grid: { left: 45, right: 48, top: 42, bottom: 34 },
    tooltip: {
      trigger: 'axis',
      formatter(params) {
        const index = params?.[0]?.dataIndex ?? 0
        const row = rows[index] || {}
        if (!hasTraffic(row)) return `<b>${row.label || ''}</b><br/>客流未登记<br/>成交 ${row.dealOrderCount || 0} 单`
        return `<b>${row.label || ''}</b><br/>进店 ${row.visitorCount || 0} 人<br/>成交 ${row.dealOrderCount || 0} 单<br/>成交率 ${decimal(row.dealRate)}%`
      }
    },
    legend: { show: false },
    xAxis: { type: 'category', data: rows.map(it => it.label), axisLine: { lineStyle: { color: '#e8dadd' } }, axisTick: { show: false }, axisLabel: { color: '#99838a', fontSize: 11 } },
    yAxis: [
      { type: 'value', name: '人数', nameTextStyle: { color: '#a48c93', fontSize: 10 }, splitLine: { lineStyle: { color: '#f5e9ec' } }, axisLabel: { color: '#99838a', fontSize: 10 } },
      { type: 'value', name: '成交率 %', max: 100, nameTextStyle: { color: '#a48c93', fontSize: 10 }, splitLine: { show: false }, axisLabel: { color: '#99838a', fontSize: 10, formatter: '{value}%' } }
    ],
    graphic: hasTrafficTrend && maximumDealRate <= 0 ? {
      type: 'text', left: '12%', top: 20, silent: true,
      style: { text: '成交率 0%（本期尚无成交）', fill: '#9b858c', fontSize: 11 }
    } : [],
    series: [
      { name: '进店', type: 'bar', barMaxWidth: 22, data: rows.map(it => it.visitorCount), itemStyle: { color: '#e3b7c1', borderRadius: [3, 3, 0, 0] } },
      { name: '成交率', type: 'line', yAxisIndex: 1, smooth: true, connectNulls: false, symbol: 'circle', symbolSize: 5, data: rows.map(it => hasTraffic(it) ? Number(it.dealRate) : null), lineStyle: { color: '#a5586a', width: 2 }, itemStyle: { color: '#a5586a' } }
    ]
  }, true)
}

onMounted(async () => {
  await Promise.all([load(), loadEntry()])
  resizeObserver = new ResizeObserver(() => chart?.resize())
  if (chartRef.value) resizeObserver.observe(chartRef.value)
})
onBeforeUnmount(() => { resizeObserver?.disconnect(); chart?.dispose(); chart = null })
</script>

<style scoped>
.traffic-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size:12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.period-tabs { display: flex; padding: 3px; border: 1px solid #efdde2; border-radius: 8px; background: #fff; }
.period-tabs button { height: 28px; min-width: 44px; border: 0; border-radius: 6px; background: transparent; color: #8f7a80; font-size: 12px; cursor: pointer; }
.period-tabs button.active { background: #f7e7eb; color: #a24c60; font-weight: 650; }
.filter-bar { display: flex; align-items: center; gap: 10px; padding: 9px 12px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.filter-bar :deep(.el-date-editor) { width: 255px; }
.range-note { margin-left: auto; color: #ab979c; font-size: 11px; }
.summary-band { display: grid; grid-template-columns: 1.1fr 1fr 1fr 1.2fr 1fr 1fr; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.hero-stat { display: flex; flex-direction: column; gap: 3px; padding: 14px 16px; border-right: 1px solid #f5e9ec; min-width: 0; }
.hero-stat:last-child { border-right: 0; }
.hero-stat span { color: #967f86; font-size: 11px; }
.hero-stat b { color: #553e46; font-size: 21px; font-weight: 680; white-space: nowrap; }
.hero-stat small { color: #ad989e; font-size:12px; }
.hero-stat.accent b { color: #a45063; }
.hero-stat.muted b { color: #9c7d6d; }
.content-grid { display: grid; grid-template-columns: minmax(0, 1.55fr) minmax(300px, .75fr); gap: 12px; }
.chart-panel, .entry-panel, .loss-panel, .detail-panel { border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.panel-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; padding: 12px 14px 0; }
.panel-head h2 { margin: 0 0 3px; font-size: 14px; font-weight: 650; }
.panel-head span { color: #a28e93; font-size: 11px; }
.legend { display: flex; align-items: center; gap: 5px; color: #9b868c; font-size:12px; }
.legend i { width: 14px; height: 4px; border-radius: 2px; margin-left: 8px; }
.legend i.visitor { background: #e3b7c1; }
.legend i.rate { background: #a5586a; height: 2px; }
.chart { height: 300px; padding: 4px; }
.entry-form { display: flex; flex-direction: column; gap: 9px; padding: 12px 14px 14px; }
.entry-form label { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.entry-form label > span, .block-label { color: #8c747b; font-size: 11px; }
.entry-form :deep(.el-date-editor), .entry-form :deep(.el-input-number), .entry-form :deep(.el-input) { width: 100%; }
.entry-pair { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
.lost-block { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; padding: 9px; border: 1px solid #f3e5e9; border-radius: 6px; background: #fdf8f9; }
.block-label { grid-column: span 2; }
.entry-actions { display: flex; align-items: center; justify-content: flex-end; gap: 8px; padding-top: 2px; }
.readonly-note { color: #b17a54; font-size:12px; }
.analysis-grid { display: grid; grid-template-columns: minmax(280px, .7fr) minmax(0, 1.3fr); gap: 12px; }
.loss-rows { display: flex; flex-direction: column; gap: 11px; padding: 14px; }
.loss-row { display: grid; grid-template-columns: 145px minmax(0, 1fr) 45px; align-items: center; gap: 10px; }
.loss-copy { display: flex; justify-content: space-between; gap: 8px; font-size: 11.5px; color: #69565c; }
.loss-copy b { font-weight: 650; }
.loss-track { height: 7px; border-radius: 2px; background: #f6edef; overflow: hidden; }
.loss-track i { display: block; height: 100%; border-radius: 2px; }
.loss-row small { color: #9e888e; text-align: right; font-size:12px; }
.detail-panel :deep(th.el-table__cell) { background: #fdf7f8; color: #8b747b; font-size: 11px; }
.detail-panel :deep(td.el-table__cell) { font-size: 11.5px; }
.no-data { color: #b09ba1; font-size:12px; }
@media (max-width: 1200px) {
  .summary-band { grid-template-columns: repeat(3, 1fr); }
  .hero-stat:nth-child(3) { border-right: 0; }
  .hero-stat:nth-child(-n+3) { border-bottom: 1px solid #f5e9ec; }
  .content-grid, .analysis-grid { grid-template-columns: 1fr; }
}
@media (max-width: 700px) {
  .traffic-page { padding: 12px; }
  .page-head, .filter-bar { align-items: stretch; flex-direction: column; }
  .filter-bar :deep(.el-date-editor) { width: 100%; }
  .range-note { margin-left: 0; }
  .summary-band { grid-template-columns: 1fr 1fr; }
  .hero-stat:nth-child(2n) { border-right: 0; }
  .hero-stat { border-bottom: 1px solid #f5e9ec; }
  .hero-stat b { font-size: 18px; }
  .lost-row { grid-template-columns: 110px minmax(0, 1fr) 40px; }
  .entry-pair, .lost-block { grid-template-columns: 1fr; }
  .block-label { grid-column: auto; }
}
</style>
