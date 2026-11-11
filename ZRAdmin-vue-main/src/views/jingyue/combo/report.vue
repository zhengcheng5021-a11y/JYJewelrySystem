<template>
  <div class="report-page">
    <header class="page-head">
      <div>
        <span class="kicker">COMBO PERFORMANCE</span>
        <h1>联动组合分析</h1>
        <p>按所选周期查看各组合的销售净额与占比，趋势按日、周、月、年汇总。</p>
      </div>
      <div class="head-actions">
        <el-button :icon="Setting" @click="$router.push('/personal/combo-setting')">配置组合</el-button>
        <el-button :icon="Back" @click="$router.push('/index')">返回首页</el-button>
      </div>
    </header>

    <section class="filter-bar">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="shortcuts" :clearable="false" @change="load" />
      <div class="granularity-tabs">
        <button v-for="item in granularities" :key="item.value" type="button" :class="{ active: granularity === item.value }" @click="changeGranularity(item.value)">{{ item.label }}</button>
      </div>
      <el-button :icon="Refresh" :loading="loading" @click="load">查询</el-button>
      <span class="range-note">{{ report.beginTime || dateRange?.[0] }} 至 {{ report.endTime || dateRange?.[1] }}</span>
    </section>

    <section class="summary-band" v-loading="loading">
      <div class="hero-stat"><span>全部款式净销售额</span><b>¥{{ money(report.totalAmount) }}</b><small>所选周期，已扣退货</small></div>
      <div class="hero-stat accent"><span>启用组合数</span><b>{{ report.combos?.length || 0 }}</b><small>停用组合不计入分析</small></div>
      <div class="hero-stat"><span>组合最高占比</span><b>{{ decimal(topCombo?.percent, 2) }}%</b><small>{{ topCombo?.name || '暂无组合' }}</small></div>
      <p class="summary-note">组合占比 = 组合内两个一级款式分类的销售净额合计 ÷ 全部一级款式销售净额。两个分类分别计算后相加，不做商品共购去重。</p>
    </section>

    <section class="content-grid" v-loading="loading">
      <article class="panel chart-panel">
        <header class="panel-head">
          <div><h2>组合占比趋势</h2><span>各组合在对应周期的销售占比</span></div>
          <span class="legend-note">{{ granularityLabel }} · {{ report.trend?.length || 0 }} 个数据点</span>
        </header>
        <div ref="chartRef" class="chart" />
      </article>

      <article class="panel rank-panel">
        <header class="panel-head"><div><h2>组合排行</h2><span>按所选周期占比排序</span></div></header>
        <div v-if="sortedCombos.length" class="rank-list">
          <div v-for="(row, index) in sortedCombos" :key="row.id" class="rank-row">
            <div class="rank-title"><span class="rank-no">{{ String(index + 1).padStart(2, '0') }}</span><div><b>{{ row.name }}</b><small>{{ (row.categories || []).join(' + ') }}</small></div></div>
            <div class="rank-scale"><div class="scale-copy"><b>¥{{ money(row.amount) }}</b><span>{{ decimal(row.percent, 2) }}%</span></div><div class="scale-track"><i :style="{ width: Math.min(100, row.percent) + '%' }"></i></div></div>
          </div>
        </div>
        <el-empty v-else description="还没有启用中的联动组合" :image-size="64" />
      </article>
    </section>

    <section class="panel detail-panel" v-loading="loading">
      <header class="panel-head"><div><h2>组合明细</h2><span>人数与场景用于指导搭配陈列和话术，不参与金额计算</span></div></header>
      <div class="detail-head"><span>组合</span><span>适用人群 / 场景</span><span>销售净额</span><span>占比</span><span>数据依据</span></div>
      <div v-for="row in sortedCombos" :key="row.id" class="detail-row">
        <div class="detail-name"><b>{{ row.name }}</b><small>{{ (row.categories || []).join(' + ') }}</small></div>
        <div class="detail-fit"><span>{{ row.fit || '未填写' }}</span><small>{{ row.scene || '未填写' }}</small></div>
        <b class="amount">¥{{ money(row.amount) }}</b>
        <strong class="percent">{{ decimal(row.percent, 2) }}%</strong>
        <small class="basis">{{ row.basis }}</small>
      </div>
      <el-empty v-if="!sortedCombos.length" description="暂无启用组合数据" :image-size="60" />
    </section>
  </div>
</template>

<script setup name="JyComboReport">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import * as echarts from 'echarts'
import { Back, Refresh, Setting } from '@element-plus/icons-vue'
import { getComboReport } from '@/api/jingyue/report'
import { serverRange } from '@/utils/serverTime'

const loading = ref(false)
const granularity = ref('day')
const dateRange = ref(serverRange(29))
const chartRef = ref()
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
  { text: '近 90 天', value: () => serverRange(89) }
]
const sortedCombos = computed(() => [...(report.combos || [])].sort((a, b) => Number(b.percent || 0) - Number(a.percent || 0)))
const topCombo = computed(() => sortedCombos.value[0])
const granularityLabel = computed(() => granularities.find((it) => it.value === granularity.value)?.label || '日')

function emptyReport() {
  return { beginTime: '', endTime: '', granularity: 'day', totalAmount: 0, combos: [], trend: [] }
}
function money(value) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function decimal(value, digits = 2) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits })
}

async function load() {
  loading.value = true
  try {
    const res = await getComboReport({
      granularity: granularity.value,
      beginTime: dateRange.value?.[0],
      endTime: dateRange.value?.[1]
    })
    Object.assign(report, emptyReport(), res.data || {})
    await nextTick()
    renderChart()
  } finally {
    loading.value = false
  }
}

function changeGranularity(value) {
  granularity.value = value
  load()
}

function renderChart() {
  if (!chartRef.value) return
  if (!chart) chart = echarts.init(chartRef.value)
  const trend = report.trend || []
  const labels = [...new Set(trend.map((it) => it.label))]
  const names = [...new Set(trend.map((it) => it.comboName))]
  const palette = ['#a95569', '#b88360', '#6f8f7d', '#8d7895', '#b06f75', '#a58a57']
  chart.setOption({
    color: palette,
    grid: { left: 48, right: 24, top: 45, bottom: 38 },
    tooltip: {
      trigger: 'axis',
      valueFormatter: (value) => `${Number(value || 0).toFixed(2)}%`
    },
    legend: { top: 8, type: 'scroll', textStyle: { color: '#8e767d', fontSize: 11 } },
    xAxis: { type: 'category', data: labels, axisTick: { show: false }, axisLine: { lineStyle: { color: '#eadde0' } }, axisLabel: { color: '#9b858b', fontSize: 11 } },
    yAxis: { type: 'value', name: '占比 %', nameTextStyle: { color: '#a48c93', fontSize: 10 }, splitLine: { lineStyle: { color: '#f5e9ec' } }, axisLabel: { color: '#9b858b', fontSize: 10, formatter: '{value}%' } },
    series: names.map((name) => ({
      name,
      type: 'line',
      smooth: true,
      symbol: 'circle',
      symbolSize: 5,
      connectNulls: true,
      data: labels.map((label) => {
        const row = trend.find((it) => it.label === label && it.comboName === name)
        return row ? Number(row.percent || 0) : null
      })
    }))
  }, true)
}

onMounted(async () => {
  await load()
  resizeObserver = new ResizeObserver(() => chart?.resize())
  if (chartRef.value) resizeObserver.observe(chartRef.value)
})
onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  chart?.dispose()
  chart = null
})
</script>

<style scoped>
.report-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size: 11px; letter-spacing: .14em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.head-actions { display: flex; gap: 8px; flex: none; }
.filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.filter-bar :deep(.el-date-editor) { width: 255px; }
.granularity-tabs { display: inline-flex; padding: 2px; border: 1px solid #efdde2; border-radius: 6px; background: #fff; }
.granularity-tabs button { min-width: 38px; padding: 5px 9px; border: 0; border-radius: 4px; color: #947e85; font-size: 12px; background: transparent; cursor: pointer; }
.granularity-tabs button.active { color: #fff; background: #b06779; }
.range-note { margin-left: auto; color: #a28e93; font-size: 11px; }
.summary-band { display: grid; grid-template-columns: 205px 165px 205px minmax(260px, 1fr); align-items: center; gap: 0; border: 1px solid #efdde2; border-radius: 8px; background: #fff; overflow: hidden; }
.hero-stat { display: flex; flex-direction: column; gap: 4px; padding: 15px 18px; border-right: 1px solid #f5e9ec; }
.hero-stat span { color: #967f86; font-size: 11px; }
.hero-stat b { color: #5d454e; font-size: 23px; font-weight: 680; }
.hero-stat.accent b { color: #a24d61; }
.hero-stat small { color: #ab969c; font-size: 11px; }
.summary-note { margin: 0; padding: 0 18px; color: #9b858c; font-size: 11px; line-height: 1.7; }
.content-grid { display: grid; grid-template-columns: minmax(0, 1.5fr) minmax(300px, .9fr); gap: 12px; }
.panel { border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.panel-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 10px; padding: 12px 14px 0; }
.panel-head h2 { margin: 0 0 3px; font-size: 14px; font-weight: 650; }
.panel-head span { color: #a28e93; font-size: 11px; }
.chart { height: 310px; padding: 2px 4px 4px; }
.rank-list { display: flex; flex-direction: column; padding: 5px 14px 12px; }
.rank-row { display: flex; flex-direction: column; gap: 6px; padding: 9px 0; border-bottom: 1px solid #f7edef; }
.rank-row:last-child { border-bottom: 0; }
.rank-title { display: flex; align-items: flex-start; gap: 8px; }
.rank-no { padding-top: 1px; color: #c1a8af; font-size: 10px; font-variant-numeric: tabular-nums; }
.rank-title div { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.rank-title b { overflow: hidden; font-size: 12.5px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.rank-title small { color: #aa949a; font-size: 11px; }
.rank-scale { padding-left: 20px; }
.scale-copy { display: flex; justify-content: space-between; color: #8f7880; font-size: 11px; }
.scale-copy b { color: #654c55; font-weight: 650; }
.scale-track { height: 5px; margin-top: 5px; background: #f7e9ed; }
.scale-track i { display: block; height: 100%; background: linear-gradient(90deg, #e6bcc6, #ac5b6f); }
.detail-panel { overflow: hidden; }
.detail-head, .detail-row { display: grid; grid-template-columns: minmax(190px, 1.15fr) minmax(180px, 1fr) 135px 80px minmax(190px, 1.15fr); gap: 16px; align-items: center; }
.detail-head { padding: 8px 14px; background: #fdf8f9; border-top: 1px solid #f5e9ec; border-bottom: 1px solid #f3e6e9; color: #9b848b; font-size: 11px; }
.detail-row { padding: 11px 14px; border-bottom: 1px solid #f7edef; }
.detail-row:last-child { border-bottom: 0; }
.detail-name, .detail-fit { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.detail-name b { font-size: 12.5px; font-weight: 650; }
.detail-name small, .detail-fit small, .basis { color: #aa949a; font-size: 11px; }
.detail-fit span { color: #745d64; font-size: 12px; }
.amount { color: #654c55; font-size: 13px; font-weight: 650; }
.percent { color: #a24d61; font-size: 14px; font-weight: 680; }
@media (max-width: 1050px) {
  .summary-band { grid-template-columns: 1fr 1fr 1fr; }
  .summary-note { grid-column: 1 / -1; padding: 0 18px 13px; }
  .content-grid { grid-template-columns: 1fr; }
}
@media (max-width: 760px) {
  .report-page { padding: 12px; }
  .page-head { align-items: flex-start; flex-direction: column; }
  .filter-bar :deep(.el-date-editor) { width: 100%; }
  .range-note { margin-left: 0; }
  .summary-band { grid-template-columns: 1fr 1fr; }
  .hero-stat { border-bottom: 1px solid #f5e9ec; }
  .summary-note { grid-column: 1 / -1; }
  .detail-head { display: none; }
  .detail-row { grid-template-columns: 1fr 1fr; gap: 8px 12px; }
  .detail-fit, .basis { grid-column: 1 / -1; }
}
</style>
