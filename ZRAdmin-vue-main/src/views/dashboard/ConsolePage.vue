<template>
  <div class="jy-dash">
    <header class="dash-head">
      <div class="dash-title">
        <p class="kicker">经营控制台</p>
        <h1>{{ greeting }}，{{ userName || '店长' }}</h1>
        <p class="dash-sub">
          {{ dateText }}
          <template v-if="sale.data">
            · 今日成交 <b>{{ sale.data.todayOrders }}</b> 单
            · 实收 <b>{{ money(sale.data.todayAmount) }}</b>
          </template>
        </p>
      </div>
      <nav class="quick-actions">
        <button v-for="q in quickActions" :key="q.to" class="quick-btn" @click="$router.push(q.to)">
          <el-icon><component :is="q.icon" /></el-icon>
          <span>{{ q.label }}</span>
        </button>
      </nav>
    </header>

    <section class="dash-main">
      <div class="col-main">
        <article class="card trend-card">
          <header class="card-head">
            <div>
              <h2>近 7 日成交趋势</h2>
              <p>销售额与成交单量走势</p>
            </div>
            <div class="legend" v-if="hasTrend">
              <i class="dot amount"></i>销售额
              <i class="dot orders"></i>单量
            </div>
          </header>
          <div v-if="sale.loading" class="chart-state">数据加载中…</div>
          <template v-else-if="hasTrend">
            <div ref="trendRef" class="trend-chart"></div>
            <div class="trend-facts">
              <div>
                <span>今日营收</span>
                <strong>{{ money(sale.data.todayAmount) }}</strong>
              </div>
              <div>
                <span>今日单量</span>
                <strong>{{ sale.data.todayOrders }}<em>单</em></strong>
              </div>
              <div>
                <span>客单价</span>
                <strong>{{ money(sale.data.avgTicket) }}</strong>
              </div>
              <div>
                <span>今日件数</span>
                <strong>{{ sale.data.todayPieces }}<em>件</em></strong>
              </div>
            </div>
          </template>
          <div v-else class="chart-state" :class="{ muted: sale.error }">
            {{ sale.error ? '销售数据暂不可用' : '近 7 日暂无成交数据' }}
          </div>
        </article>

        <article class="card recent-card">
          <header class="card-head">
            <div>
              <h2>最近成交</h2>
              <p>最新完成的正品销售单</p>
            </div>
            <button class="link-btn" @click="$router.push('/business/sale-records')">全部记录 →</button>
          </header>
          <div v-if="sale.loading" class="chart-state small">加载中…</div>
          <ul v-else-if="recentSales.length" class="recent-list">
            <li v-for="row in recentSales" :key="row.id">
              <div class="who">
                <b>{{ row.customerName || row.memberName || '散客' }}</b>
                <span>{{ row.orderNo }}</span>
              </div>
              <div class="pieces">{{ row.pieceQty || 0 }} 件</div>
              <div class="amount">{{ money(row.payAmount) }}</div>
              <div class="time">{{ shortTime(row.dealTime) }}</div>
            </li>
          </ul>
          <div v-else class="chart-state small" :class="{ muted: sale.error }">
            {{ sale.error ? '销售数据暂不可用' : '暂无成交记录' }}
          </div>
        </article>
      </div>

      <aside class="col-side">
        <article class="card birthday-card">
          <header class="card-head">
            <div>
              <h2>生日临近</h2>
              <p>未来 7 天，越近越优先</p>
            </div>
            <span class="count-badge soft" v-if="birthdays.data?.upcomingCount">{{ birthdays.data.upcomingCount }}</span>
          </header>
          <ul v-if="birthdayRows.length" class="birthday-list">
            <li v-for="item in birthdayRows" :key="item.memberId" :class="{ today: item.daysUntil === 0 }" @click="$router.push('/business/member')">
              <span class="birthday-day"><b>{{ item.daysUntil === 0 ? '今天' : item.daysUntil }}</b><small v-if="item.daysUntil">{{ item.daysUntil === 1 ? '明天' : '天后' }}</small></span>
              <span class="birthday-main"><b>{{ item.memberName }}</b><small>{{ item.calendarText }} · {{ item.phone || '未留电话' }}</small></span>
              <span class="birthday-level">{{ item.levelName }}</span>
            </li>
          </ul>
          <div v-else class="todo-empty compact">
            <el-icon><Present /></el-icon>
            <p>{{ birthdays.error ? '生日提醒暂不可用' : '未来 7 天没有临近生日' }}</p>
          </div>
        </article>

        <article class="card alert-card">
          <header class="card-head">
            <div>
              <h2>库存预警</h2>
              <p>在库 {{ number(alerts.data?.totalQty) }} 件{{ alerts.data?.unclassifiedQty ? ` · 未分类 ${number(alerts.data.unclassifiedQty)} 件` : '' }}</p>
            </div>
            <button class="link-btn" @click="$router.push({ path: '/jingyue/dict', query: { tab: 'stock-alert' } })">预警设置 →</button>
          </header>
          <ul v-if="stockAlertRows.length" class="alert-list">
            <li v-for="item in stockAlertRows" :key="item.styleItemId" :class="'level-' + item.alertLevel">
              <div><b>{{ item.styleItemName }}</b><small>预警线 {{ number(item.minQty) }} 件</small></div>
              <strong>{{ number(item.currentQty) }}<em> 件</em></strong>
              <span>{{ item.alertLevel === 2 ? '库存偏低' : '临近预警' }}</span>
            </li>
          </ul>
          <div v-else class="todo-empty compact">
            <el-icon><Bell /></el-icon>
            <p>{{ alerts.error ? '库存预警暂不可用' : '当前没有库存预警' }}</p>
          </div>
        </article>

        <article class="card todo-card">
          <header class="card-head">
            <div>
              <h2>今日待办</h2>
              <p>重要且紧急，先处理这些</p>
            </div>
            <span class="count-badge" v-if="todos.length">{{ todos.length }}</span>
          </header>
          <ul v-if="todos.length" class="todo-list">
            <li v-for="item in todos" :key="item.key" :class="item.tone" @click="$router.push(item.to)">
              <div class="todo-main">
                <b>{{ item.label }}</b>
                <span>{{ item.hint }}</span>
              </div>
              <strong>{{ item.value }}</strong>
            </li>
          </ul>
          <div v-else class="todo-empty">
            <el-icon><CircleCheck /></el-icon>
            <p>今天没有需要紧急处理的事项</p>
          </div>
        </article>

        <article class="card member-card">
          <header class="card-head">
            <div>
              <h2>会员经营</h2>
              <p>客群与等级结构</p>
            </div>
            <button class="link-btn" @click="$router.push('/business/member')">会员档案 →</button>
          </header>
          <template v-if="member.data">
            <div class="member-facts">
              <div><span>会员总数</span><strong>{{ member.data.totalMembers }}</strong></div>
              <div><span>本月新增</span><strong>{{ member.data.newThisMonth }}</strong></div>
              <div><span>本月活跃</span><strong>{{ member.data.activeThisMonth }}</strong></div>
            </div>
            <div class="level-bars" v-if="levelStats.length">
              <div v-for="lv in levelStats" :key="lv.levelName" class="level-row">
                <span>{{ lv.levelName }}</span>
                <div class="bar"><i :style="{ width: lv.percent + '%' }"></i></div>
                <b>{{ lv.count }}</b>
              </div>
            </div>
            <p v-else class="tiny-note">暂无等级分布</p>
          </template>
          <div v-else class="chart-state small" :class="{ muted: member.error }">
            {{ member.error ? '会员数据暂不可用' : '加载中…' }}
          </div>
        </article>
      </aside>
    </section>

    <section class="dash-lower">
      <article class="card stock-card">
        <header class="card-head">
          <div>
            <h2>库存结构</h2>
            <p>在库、锁定与流转中的货品</p>
          </div>
          <button class="link-btn" @click="$router.push('/business/stock')">库存查询 →</button>
        </header>
        <template v-if="stock.data">
          <div class="stock-facts">
            <div><span>在库件数</span><strong>{{ stock.data.onShelfCount }}</strong></div>
            <div><span>库存成本</span><strong>{{ money(stock.data.stockCost) }}</strong></div>
            <div><span>库存售价</span><strong>{{ money(stock.data.stockSale) }}</strong></div>
          </div>
          <div class="status-chips">
            <span v-for="s in stockStatus" :key="s.status" class="chip" :class="{ zero: !s.count }">
              {{ s.label }} <b>{{ s.count }}</b>
            </span>
          </div>
        </template>
        <div v-else class="chart-state small" :class="{ muted: stock.error }">
          {{ stock.error ? '库存数据暂不可用' : '加载中…' }}
        </div>
      </article>

      <article class="card activity-card">
        <header class="card-head">
          <div>
            <h2>营销活动</h2>
            <p>进行中与即将开始</p>
          </div>
          <button class="link-btn" @click="$router.push('/business/activity')">活动管理 →</button>
        </header>
        <template v-if="activity.data">
          <div class="activity-facts">
            <div><span>进行中</span><strong>{{ activity.data.runningCount }}</strong></div>
            <div><span>即将开始</span><strong>{{ activity.data.upcomingCount }}</strong></div>
            <div><span>本月让利</span><strong>{{ money(activity.data.monthBenefit) }}</strong></div>
          </div>
          <ul v-if="activity.data.running?.length" class="activity-list">
            <li v-for="a in activity.data.running.slice(0, 3)" :key="a.id">
              <b>{{ a.activityName }}</b>
              <span>{{ dateOnly(a.startTime) }} 起</span>
            </li>
          </ul>
          <p v-else class="tiny-note">当前没有进行中的活动</p>
        </template>
        <div v-else class="chart-state small" :class="{ muted: activity.error }">
          {{ activity.error ? '活动数据暂不可用' : '加载中…' }}
        </div>
      </article>
    </section>
  </div>
</template>

<script setup name="JyConsolePage">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import * as echarts from 'echarts'
import {
  Plus, DocumentAdd, ShoppingCart, User, Tickets, Box, CircleCheck, Present, Bell
} from '@element-plus/icons-vue'
import useUserStore from '@/store/modules/user'
import { getSaleDashboard } from '@/api/jingyue/sale'
import { getInboundDashboard } from '@/api/jingyue/inbound'
import { getCustomDashboard } from '@/api/jingyue/custom'
import { getMemberDashboard } from '@/api/jingyue/member'
import { getActivityDashboard } from '@/api/jingyue/activity'
import { getStockSummary } from '@/api/jingyue/stock'
import { getBirthdayReminders, getStockAlerts } from '@/api/jingyue/homeConfig'
import { serverNow, parseServerDateTime } from '@/utils/serverTime'

const { proxy } = getCurrentInstance()
const userStore = useUserStore()
const userName = computed(() => userStore.name)

const sale = reactive({ loading: true, error: false, data: null })
const inbound = reactive({ loading: true, error: false, data: null })
const custom = reactive({ loading: true, error: false, data: null })
const member = reactive({ loading: true, error: false, data: null })
const activity = reactive({ loading: true, error: false, data: null })
const stock = reactive({ loading: true, error: false, data: null })
const birthdays = reactive({ loading: true, error: false, data: null })
const alerts = reactive({ loading: true, error: false, data: null })

const trendRef = ref(null)
let chart = null

const hasPermi = (code) => proxy?.$auth?.hasPermi?.(code) ?? false
const hasAnyPermi = (codes) => (proxy?.$auth?.hasPermiOr?.(codes) ?? false)

const quickActions = computed(() =>
  [
    { label: '柜台开单', to: '/business/sale', icon: ShoppingCart, perm: 'jingyue:sale:list' },
    { label: '采购入库', to: '/business/inbound', icon: DocumentAdd, perm: 'jingyue:inbound:list' },
    { label: '销售定制', to: '/business/sale-custom', icon: Plus, perm: 'jingyue:custom:list' },
    { label: '会员档案', to: '/business/member', icon: User, perm: 'jingyue:member:list' },
    { label: '开单记录', to: '/business/sale-records', icon: Tickets, perm: 'jingyue:sale:query' },
    { label: '库存查询', to: '/business/stock', icon: Box, perm: 'jingyue:stock:list' }
  ].filter((it) => hasPermi(it.perm))
)

const greeting = computed(() => {
  const h = serverNow().getHours()
  if (h < 6) return '夜深了'
  if (h < 11) return '早上好'
  if (h < 14) return '中午好'
  if (h < 18) return '下午好'
  return '晚上好'
})

const dateText = computed(() => {
  const now = serverNow()
  const week = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'][now.getDay()]
  return `${now.getFullYear()} 年 ${now.getMonth() + 1} 月 ${now.getDate()} 日 · ${week}`
})

const hasTrend = computed(() => {
  const rows = sale.data?.trend || []
  return rows.some((it) => Number(it.amount) > 0 || Number(it.orders) > 0)
})

const recentSales = computed(() => (sale.data?.recent || []).slice(0, 6))
const birthdayRows = computed(() => (birthdays.data?.rows || []).slice(0, 4))
const stockAlertRows = computed(() => (alerts.data?.rows || []).filter((it) => it.alertLevel > 0).slice(0, 4))

const levelStats = computed(() => {
  const rows = member.data?.levels || []
  const max = Math.max(...rows.map((it) => it.count), 1)
  return rows.map((it) => ({ ...it, percent: Math.round((it.count / max) * 100) }))
})

const todos = computed(() => {
  const list = []
  if (inbound.data?.draftCount > 0) {
    list.push({ key: 'draft', tone: 'warn', label: '入库草稿待确认', value: `${inbound.data.draftCount} 单`, hint: '草稿确认后货品才真正在库', to: '/business/inbound' })
  }
  if (custom.data?.pendingCount > 0) {
    list.push({ key: 'custom', tone: 'warn', label: '定制待收尾 / 定金待冲抵', value: `${custom.data.pendingCount} 单`, hint: `剩余定金 ${money(custom.data.remainDepositAmount)}`, to: '/business/sale-custom' })
  }
  if (stock.data?.lowStockCount > 0) {
    list.push({ key: 'stock', tone: 'danger', label: '库存异常待核对', value: `${stock.data.lowStockCount} 项`, hint: '件数或克重与台账不一致', to: '/business/stock' })
  }
  if (activity.data?.upcomingCount > 0) {
    list.push({ key: 'activity', tone: 'info', label: '活动即将开始', value: `${activity.data.upcomingCount} 场`, hint: '请确认参与货品与话术', to: '/business/activity' })
  }
  return list
})

const STOCK_STATUS = [
  { status: 1, label: '在库' },
  { status: 2, label: '定金锁定' },
  { status: 7, label: '入库中' },
  { status: 3, label: '销售出库' },
  { status: 8, label: '返厂出库' },
  { status: 6, label: '停用' }
]
const stockStatus = computed(() => {
  const counts = stock.data?.statusCounts || {}
  return STOCK_STATUS.map((it) => ({ ...it, count: Number(counts[it.status] || 0) }))
})

function money(value) {
  const num = Number(value || 0)
  return '¥ ' + num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function number(value) {
  return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}
function shortTime(value) {
  if (!value) return '-'
  const d = parseServerDateTime(value)
  if (!d || Number.isNaN(d.getTime())) return String(value).slice(5, 16)
  const pad = (n) => String(n).padStart(2, '0')
  return `${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}
function dateOnly(value) {
  if (!value) return '-'
  return String(value).slice(0, 10)
}

function renderTrend() {
  const rows = sale.data?.trend || []
  if (!trendRef.value || rows.length === 0) {
    if (chart) { chart.dispose(); chart = null }
    return
  }
  if (!chart) chart = echarts.init(trendRef.value)
  chart.setOption({
    grid: { top: 18, right: 14, bottom: 4, left: 4, containLabel: true },
    tooltip: {
      trigger: 'axis',
      backgroundColor: 'rgba(61, 44, 50, .92)',
      borderWidth: 0,
      textStyle: { color: '#fff', fontSize: 12 },
      axisPointer: { type: 'line', lineStyle: { color: 'rgba(196,120,138,.4)' } }
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: rows.map((it) => it.date),
      axisLine: { lineStyle: { color: '#f0e1e5' } },
      axisTick: { show: false },
      axisLabel: { color: '#a4888f', fontSize: 11 }
    },
    yAxis: [
      {
        type: 'value',
        splitLine: { lineStyle: { color: '#f7eef0' } },
        axisLabel: { color: '#b39aa1', fontSize: 10 }
      }
    ],
    series: [
      {
        name: '销售额',
        type: 'line',
        smooth: true,
        symbol: 'circle',
        symbolSize: 5,
        data: rows.map((it) => Number(it.amount || 0)),
        lineStyle: { width: 2, color: '#c4788a' },
        itemStyle: { color: '#c4788a' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(196,120,138,.22)' },
            { offset: 1, color: 'rgba(196,120,138,0)' }
          ])
        }
      },
      {
        name: '单量',
        type: 'bar',
        barWidth: 8,
        data: rows.map((it) => Number(it.orders || 0)),
        itemStyle: { color: 'rgba(214,171,142,.35)', borderRadius: [3, 3, 0, 0] }
      }
    ]
  }, true)
  chart.resize()
}

function loadSale() {
  if (!hasPermi('jingyue:sale:list')) { sale.loading = false; sale.error = true; return }
  getSaleDashboard().then((res) => { sale.data = res.data || null })
    .catch(() => { sale.error = true })
    .finally(() => { sale.loading = false; nextTick(renderTrend) })
}
function loadInbound() {
  if (!hasPermi('jingyue:inbound:list')) { inbound.loading = false; inbound.error = true; return }
  getInboundDashboard().then((res) => { inbound.data = res.data || null })
    .catch(() => { inbound.error = true })
    .finally(() => { inbound.loading = false })
}
function loadCustom() {
  if (!hasAnyPermi(['jingyue:custom:list', 'jingyue:custom:query', 'jingyue:custom:add'])) { custom.loading = false; custom.error = true; return }
  getCustomDashboard().then((res) => { custom.data = res.data || null })
    .catch(() => { custom.error = true })
    .finally(() => { custom.loading = false })
}
function loadMember() {
  if (!hasPermi('jingyue:member:list')) { member.loading = false; member.error = true; return }
  getMemberDashboard().then((res) => { member.data = res.data || null })
    .catch(() => { member.error = true })
    .finally(() => { member.loading = false })
}
function loadActivity() {
  if (!hasPermi('jingyue:activity:list')) { activity.loading = false; activity.error = true; return }
  getActivityDashboard().then((res) => { activity.data = res.data || null })
    .catch(() => { activity.error = true })
    .finally(() => { activity.loading = false })
}
function loadStock() {
  if (!hasAnyPermi(['jingyue:stock:list', 'jingyue:stock:query'])) { stock.loading = false; stock.error = true; return }
  getStockSummary().then((res) => { stock.data = res.data || null })
    .catch(() => { stock.error = true })
    .finally(() => { stock.loading = false })
}
function loadBirthdays() {
  if (!hasAnyPermi(['jingyue:member:list', 'jingyue:member:query'])) { birthdays.loading = false; birthdays.error = true; return }
  getBirthdayReminders().then((res) => { birthdays.data = res.data || null })
    .catch(() => { birthdays.error = true })
    .finally(() => { birthdays.loading = false })
}
function loadAlerts() {
  if (!hasAnyPermi(['jingyue:stock:list', 'jingyue:stock:query', 'jingyue:dict:list'])) { alerts.loading = false; alerts.error = true; return }
  getStockAlerts().then((res) => { alerts.data = res.data || null })
    .catch(() => { alerts.error = true })
    .finally(() => { alerts.loading = false })
}

function onResize() {
  chart?.resize()
}

onMounted(() => {
  loadSale(); loadInbound(); loadCustom(); loadMember(); loadActivity(); loadStock(); loadBirthdays(); loadAlerts()
  window.addEventListener('resize', onResize)
})
onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  chart?.dispose()
  chart = null
})
</script>

<style scoped lang="scss">
.jy-dash {
  padding: 2px 2px 16px;
  color: #3d3236;
}

.dash-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 18px;
  padding: 4px 2px 16px;
  border-bottom: 1px solid #f0e1e5;
}
.kicker { margin: 0 0 6px; color: #c4788a; font-size: 11px; letter-spacing: 3px; }
.dash-title h1 { margin: 0; font-size: 24px; font-weight: 600; letter-spacing: .5px; }
.dash-sub { margin: 7px 0 0; color: #9a8189; font-size: 12px; }
.dash-sub b { color: #b76e79; font-weight: 600; }

.quick-actions { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 6px; }
.quick-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 14px; color: #8a6b74; font-size: 12px;
  background: #fff; border: 1px solid #f0e1e5; border-radius: 999px;
  cursor: pointer; transition: all .18s ease;
  &:hover { color: #b76e79; border-color: #e6c3cb; background: #fdf6f7; transform: translateY(-1px); }
  .el-icon { font-size: 14px; }
}

.dash-main { display: grid; grid-template-columns: minmax(0, 1.65fr) minmax(280px, .85fr); gap: 14px; margin-top: 16px; }
.col-main, .col-side { display: flex; flex-direction: column; gap: 14px; min-width: 0; }

.card { background: #fff; border: 1px solid #f0e1e5; border-radius: 12px; padding: 16px 18px; }
.card-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; }
.card-head h2 { margin: 0; font-size: 14px; font-weight: 600; color: #43343a; }
.card-head p { margin: 4px 0 0; color: #a68f95; font-size: 11px; }
.link-btn { padding: 0; color: #bf8894; font-size: 11px; background: none; border: 0; cursor: pointer; }
.link-btn:hover { color: #a9636e; text-decoration: underline; }
.count-badge { min-width: 20px; padding: 1px 7px; color: #fff; font-size: 11px; text-align: center; background: #c4788a; border-radius: 999px; }
.count-badge.soft { color: #a9636e; background: #faeef1; }

.legend { display: flex; align-items: center; gap: 6px; color: #a68f95; font-size: 11px; }
.legend .dot { width: 7px; height: 7px; border-radius: 50%; }
.legend .dot.amount { background: #c4788a; }
.legend .dot.orders { margin-left: 8px; background: rgba(214,171,142,.6); }

.trend-chart { height: 226px; margin-top: 8px; }
.chart-state { display: flex; align-items: center; justify-content: center; height: 226px; color: #b9a3a9; font-size: 12px; }
.chart-state.small { height: 96px; }
.chart-state.muted { color: #c4b1b6; }

.trend-facts { display: grid; grid-template-columns: repeat(4, 1fr); margin-top: 10px; padding-top: 12px; border-top: 1px dashed #f3e5e8; }
.trend-facts div { display: flex; flex-direction: column; gap: 3px; padding-left: 12px; border-left: 1px solid #f6ebee; }
.trend-facts div:first-child { padding-left: 0; border-left: 0; }
.trend-facts span { color: #a68f95; font-size: 11px; }
.trend-facts strong { color: #3d3236; font-size: 17px; font-weight: 600; }
.trend-facts em { margin-left: 2px; color: #a68f95; font-size: 11px; font-style: normal; font-weight: 400; }

.recent-list { list-style: none; margin: 10px 0 0; padding: 0; }
.recent-list li { display: grid; grid-template-columns: minmax(0, 1.6fr) 60px 96px 82px; align-items: center; gap: 10px; padding: 9px 0; border-bottom: 1px solid #f8eff1; }
.recent-list li:last-child { border-bottom: 0; padding-bottom: 0; }
.recent-list .who { min-width: 0; }
.recent-list .who b { display: block; font-size: 12px; font-weight: 500; color: #4a3b40; }
.recent-list .who span { color: #b19ba1; font-size:12px; letter-spacing: .3px; }
.recent-list .pieces { color: #9c848b; font-size: 11px; }
.recent-list .amount { color: #a9636e; font-size: 13px; font-weight: 600; }
.recent-list .time { color: #b6a1a7; font-size: 11px; }

.todo-list { list-style: none; margin: 10px 0 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
.todo-list li {
  display: flex; align-items: center; justify-content: space-between; gap: 10px;
  padding: 10px 12px; border-radius: 9px; cursor: pointer;
  background: #fdf8f8; border: 1px solid #f5e8ea; border-left: 3px solid #d9b4bd;
  transition: all .16s ease;
  &:hover { background: #fcf3f5; border-color: #ecd3d9; }
  &.warn { border-left-color: #d8a76d; }
  &.danger { border-left-color: #c4788a; }
  &.info { border-left-color: #a9b9c9; }
}
.todo-main { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.todo-main b { color: #4a3b40; font-size: 12px; font-weight: 500; }
.todo-main span { color: #a68f95; font-size:12px; }
.todo-list strong { color: #a9636e; font-size: 13px; font-weight: 600; white-space: nowrap; }
.todo-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 26px 0; color: #c0a9af; }
.todo-empty .el-icon { font-size: 22px; color: #d6bfc5; }
.todo-empty p { margin: 0; font-size: 11.5px; }
.todo-empty.compact { padding: 18px 0 8px; }

.birthday-list, .alert-list { list-style: none; margin: 9px 0 0; padding: 0; }
.birthday-list li { display: grid; grid-template-columns: 42px minmax(0, 1fr) auto; align-items: center; gap: 9px; padding: 8px 0; border-bottom: 1px solid #f8eff1; cursor: pointer; }
.birthday-list li:last-child, .alert-list li:last-child { border-bottom: 0; padding-bottom: 0; }
.birthday-list li:hover .birthday-main b { color: #b76e79; }
.birthday-day { display: flex; flex-direction: column; align-items: center; justify-content: center; width: 38px; height: 38px; color: #a66c78; background: #fdf1f3; border: 1px solid #f2dfe4; border-radius: 50%; }
.birthday-day b { font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 500; line-height: 1; }
.birthday-day small { margin-top: 2px; color: #b899a0; font-size:11px; }
.birthday-list li.today .birthday-day { color: #fff; background: #bd7481; border-color: #bd7481; }
.birthday-list li.today .birthday-day small { color: rgba(255,255,255,.75); }
.birthday-main { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.birthday-main b { overflow: hidden; color: #4a3b40; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.birthday-main small { overflow: hidden; color: #ad979d; font-size:12px; text-overflow: ellipsis; white-space: nowrap; }
.birthday-level { color: #a58a91; font-size:12px; white-space: nowrap; }
.alert-list li { display: grid; grid-template-columns: minmax(0, 1fr) auto 62px; align-items: center; gap: 8px; padding: 9px 0; border-bottom: 1px solid #f8eff1; }
.alert-list li div { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.alert-list b { overflow: hidden; color: #4a3b40; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.alert-list small { color: #ad979d; font-size:12px; }
.alert-list strong { color: #5e474e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 500; white-space: nowrap; }
.alert-list strong em { color: #a58a91; font-family: inherit; font-size:11.5px; font-style: normal; }
.alert-list li > span { padding: 3px 6px; color: #b18448; font-size:11.5px; text-align: center; background: #fdf7e9; border-radius: 4px; white-space: nowrap; }
.alert-list li.level-2 > span { color: #b25e5b; background: #fcefee; }

.member-facts { display: grid; grid-template-columns: repeat(3, 1fr); margin-top: 12px; }
.member-facts div { display: flex; flex-direction: column; gap: 2px; padding-left: 10px; border-left: 1px solid #f6ebee; }
.member-facts div:first-child { padding-left: 0; border-left: 0; }
.member-facts span { color: #a68f95; font-size:12px; }
.member-facts strong { color: #3d3236; font-size: 16px; font-weight: 600; }

.level-bars { margin-top: 12px; display: flex; flex-direction: column; gap: 6px; }
.level-row { display: grid; grid-template-columns: 62px minmax(0, 1fr) 28px; align-items: center; gap: 8px; }
.level-row span { color: #9c848b; font-size:12px; }
.level-row b { color: #8a6b74; font-size: 11px; font-weight: 500; text-align: right; }
.level-row .bar { height: 5px; background: #f7ecef; border-radius: 99px; overflow: hidden; }
.level-row .bar i { display: block; height: 100%; background: linear-gradient(90deg, #e6c1c9, #c4788a); border-radius: 99px; }
.tiny-note { margin: 10px 0 0; color: #b6a1a7; font-size: 11px; }

.dash-lower { display: grid; grid-template-columns: minmax(0, 1.35fr) minmax(280px, 1fr); gap: 14px; margin-top: 14px; }
.stock-facts, .activity-facts { display: grid; grid-template-columns: repeat(3, 1fr); margin-top: 12px; }
.stock-facts div, .activity-facts div { display: flex; flex-direction: column; gap: 2px; padding-left: 10px; border-left: 1px solid #f6ebee; }
.stock-facts div:first-child, .activity-facts div:first-child { padding-left: 0; border-left: 0; }
.stock-facts span, .activity-facts span { color: #a68f95; font-size:12px; }
.stock-facts strong, .activity-facts strong { color: #3d3236; font-size: 15px; font-weight: 600; }

.status-chips { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 12px; }
.chip { padding: 3px 10px; color: #8a6b74; font-size: 11px; background: #fdf6f7; border: 1px solid #f3e3e6; border-radius: 999px; }
.chip b { margin-left: 4px; color: #b76e79; font-weight: 600; }
.chip.zero { color: #c3b1b6; background: #fbf8f9; }
.chip.zero b { color: #c3b1b6; }

.activity-list { list-style: none; margin: 10px 0 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.activity-list li { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 7px 10px; background: #fdf8f8; border-radius: 8px; }
.activity-list b { color: #4a3b40; font-size: 11.5px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.activity-list span { color: #b6a1a7; font-size:12px; white-space: nowrap; }

@media (max-width: 1280px) {
  .dash-main { grid-template-columns: 1fr; }
  .dash-lower { grid-template-columns: 1fr; }
}
@media (max-width: 860px) {
  .dash-head { align-items: flex-start; flex-direction: column; }
  .quick-actions { justify-content: flex-start; }
  .trend-facts { grid-template-columns: 1fr 1fr; row-gap: 10px; }
  .trend-facts div:nth-child(3) { padding-left: 0; border-left: 0; }
  .recent-list li { grid-template-columns: minmax(0, 1fr) 80px 74px; }
  .recent-list .pieces { display: none; }
}
</style>
