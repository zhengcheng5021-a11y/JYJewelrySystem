<template>
  <div class="goal-page" v-loading="loading">
    <header class="page-head">
      <div>
        <span class="kicker">GOAL CONTROL</span>
        <h1>目标管理</h1>
        <p>年度目标折算到月，月目标折算到天；完成进度实时读取销售与退货数据。</p>
      </div>
      <div class="month-switch">
        <el-button text :icon="ArrowLeft" @click="shiftMonth(-1)" />
        <el-date-picker v-model="month" type="month" value-format="YYYY-MM" :clearable="false" @change="load" />
        <el-button text :icon="ArrowRight" @click="shiftMonth(1)" />
      </div>
    </header>

    <section class="control-grid">
      <div class="setting-panel">
        <div class="panel-title"><b>{{ report.goalYear }} 年目标</b><span>一年维护一次</span></div>
        <div class="setting-fields">
          <label><span>年目标金额</span><el-input-number v-model="yearForm.targetAmount" :min="0" :max="999999999" :controls="false" placeholder="0" /></label>
          <label><span>年目标件数</span><el-input-number v-model="yearForm.targetQty" :min="0" :max="9999999" :controls="false" placeholder="选填" /></label>
          <el-button type="primary" plain :loading="savingYear" @click="saveYear">保存年目标</el-button>
        </div>
        <p class="setting-note">月均折算 <b>¥{{ money(report.yearToMonthAmount) }}</b></p>
      </div>

      <div class="setting-panel">
        <div class="panel-title"><b>{{ report.month }} 月目标</b><span>本月单独维护</span></div>
        <div class="setting-fields">
          <label><span>月目标金额</span><el-input-number v-model="monthForm.targetAmount" :min="0" :max="999999999" :controls="false" placeholder="0" /></label>
          <label><span>月目标件数</span><el-input-number v-model="monthForm.targetQty" :min="0" :max="9999999" :controls="false" placeholder="选填" /></label>
          <el-button type="primary" plain :loading="savingMonth" @click="saveMonth">保存月目标</el-button>
        </div>
        <p class="setting-note">日均要求 <b>¥{{ money(report.monthToDayAmount) }}</b></p>
      </div>
    </section>

    <section class="progress-band">
      <div class="progress-main">
        <div class="progress-copy">
          <span>本月已完成</span>
          <b>¥{{ money(report.doneAmount) }}</b>
          <small>目标 ¥{{ money(report.monthTargetAmount) }} · {{ decimal(report.doneQty, 0) }} 件</small>
        </div>
        <div class="progress-visual">
          <div class="progress-track"><i :style="{ width: Math.min(100, report.percent || 0) + '%' }"></i></div>
          <div class="progress-scale"><span>0</span><span>{{ decimal(report.percent) }}%</span><span>100%</span></div>
        </div>
        <div class="progress-kpis">
          <div><span>缺口</span><b>¥{{ money(report.remainingAmount) }}</b></div>
          <div><span>剩余天数</span><b>{{ report.daysLeft }} 天</b></div>
          <div><span>日均要求</span><b>¥{{ money(report.needPerDay) }}</b></div>
          <div><span>今日成交</span><b>¥{{ money(report.todayAmount) }}</b></div>
        </div>
      </div>
    </section>

    <section class="period-strip">
      <div class="period-copy"><b>期间复盘</b><span>按所选区间复盘实际完成与目标</span></div>
      <el-date-picker v-model="periodRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :clearable="false" @change="loadPeriod" />
      <div class="period-metrics">
        <span>完成 <b>¥{{ money(report.periodDoneAmount) }}</b></span>
        <span>目标 <b>¥{{ money(report.periodTargetAmount) }}</b></span>
        <span>达成 <b class="accent">{{ decimal(report.periodPercent) }}%</b></span>
        <span>件数 <b>{{ decimal(report.periodDoneQty, 0) }}</b></span>
      </div>
    </section>

    <section class="category-panel">
      <div class="panel-head">
        <div><h2>分类目标</h2><span>销售明细按款式二级分类归并到大类，进度随销售单自动更新</span></div>
        <span class="panel-tip">细类没有目标也可直接点编辑补录</span>
      </div>
      <div class="category-head">
        <span>品类</span><span>目标与完成</span><span>达成率</span><span>毛利</span><span>操作</span>
      </div>
      <div v-if="report.categories?.length" class="category-list">
        <article v-for="row in report.categories" :key="row.categoryId" class="category-row">
          <div class="cat-name"><b>{{ row.categoryName }}</b><small>实际均价 ¥{{ money(row.actualUnitPrice || row.unitPrice) }}</small></div>
          <div class="cat-amount">
            <div><b>¥{{ money(row.doneAmount) }}</b><span>/ ¥{{ money(row.targetAmount) }}</span></div>
            <div class="cat-track"><i :style="{ width: Math.min(100, row.percent || 0) + '%' }"></i></div>
            <small>{{ decimal(row.doneQty, 0) }} 件{{ row.needQty ? ' · 还需 ' + row.needQty + ' 件' : '' }}</small>
          </div>
          <div class="cat-percent" :class="percentTone(row.percent)">{{ row.targetAmount ? decimal(row.percent) + '%' : '未设' }}</div>
          <div class="cat-gross"><b>¥{{ money(row.grossProfit) }}</b><small>{{ decimal(row.grossRate) }}%</small></div>
          <div class="cat-actions"><el-button link type="primary" @click="openEdit(row)">编辑目标</el-button></div>
        </article>
      </div>
      <el-empty v-else description="数据字典里还没有款式大类" :image-size="64" />
    </section>

    <el-dialog v-model="editOpen" title="分类目标" width="430px" append-to-body>
      <div class="goal-dialog">
        <div class="dialog-cat">{{ editForm.categoryName }}</div>
        <label><span>目标金额</span><el-input-number v-model="editForm.targetAmount" :min="0" :max="999999999" :controls="false" /></label>
        <label><span>目标件数（选填）</span><el-input-number v-model="editForm.targetQty" :min="0" :max="9999999" :controls="false" /></label>
        <label><span>预估客单价</span><el-input-number v-model="editForm.unitPrice" :min="0" :max="9999999" :controls="false" /></label>
        <p>客单价用于估算还需销售件数，留空则按实际均价计算。</p>
      </div>
      <template #footer>
        <el-button @click="editOpen = false">取消</el-button>
        <el-button type="primary" :loading="savingCategory" @click="saveCategory">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="JySalesGoals">
import { onMounted, reactive, ref } from 'vue'
import { ArrowLeft, ArrowRight } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getSalesGoals, saveGoalYear, saveGoalMonth, saveGoalCategory } from '@/api/jingyue/report'
import { serverNow, serverRange } from '@/utils/serverTime'

const loading = ref(false)
const savingYear = ref(false)
const savingMonth = ref(false)
const savingCategory = ref(false)
const editOpen = ref(false)
const now = serverNow()
const month = ref(now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0'))
const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate()
const periodRange = ref([month.value + '-01', month.value + '-' + String(lastDay).padStart(2, '0')])
const report = reactive(emptyReport())
const yearForm = reactive({ targetAmount: 0, targetQty: 0 })
const monthForm = reactive({ targetAmount: 0, targetQty: 0 })
const editForm = reactive({ id: 0, categoryId: 0, categoryName: '', targetAmount: 0, targetQty: 0, unitPrice: 0 })

function emptyReport() {
  return { goalYear: now.getFullYear(), goalMonth: now.getMonth() + 1, month: month.value, yearTargetAmount: 0, yearTargetQty: 0, monthTargetAmount: 0, monthTargetQty: 0, yearToMonthAmount: 0, monthToDayAmount: 0, daysLeft: 1, doneAmount: 0, doneQty: 0, remainingAmount: 0, needPerDay: 0, percent: 0, todayAmount: 0, periodDoneAmount: 0, periodDoneQty: 0, periodTargetAmount: 0, periodPercent: 0, categories: [] }
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function decimal(value, digits = 1) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits }) }
function percentTone(value) { return Number(value) >= 100 ? 'good' : Number(value) >= 60 ? 'normal' : 'low' }

function shiftMonth(step) {
  const [year, monthNo] = month.value.split('-').map(Number)
  const target = new Date(year, monthNo - 1 + step, 1)
  month.value = target.getFullYear() + '-' + String(target.getMonth() + 1).padStart(2, '0')
  const days = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate()
  periodRange.value = [month.value + '-01', month.value + '-' + String(days).padStart(2, '0')]
  load()
}
function loadPeriod() { load() }
async function load() {
  loading.value = true
  try {
    const res = await getSalesGoals({ month: month.value, periodBegin: periodRange.value?.[0], periodEnd: periodRange.value?.[1] })
    Object.assign(report, emptyReport(), res.data || {})
    yearForm.targetAmount = Number(report.yearTargetAmount || 0)
    yearForm.targetQty = Number(report.yearTargetQty || 0)
    monthForm.targetAmount = Number(report.monthTargetAmount || 0)
    monthForm.targetQty = Number(report.monthTargetQty || 0)
  } finally { loading.value = false }
}
async function saveYear() {
  savingYear.value = true
  try {
    await saveGoalYear({ goalYear: report.goalYear || Number(month.value.slice(0, 4)), targetAmount: yearForm.targetAmount || 0, targetQty: yearForm.targetQty || 0 })
    ElMessage.success('年目标已保存')
    await load()
  } finally { savingYear.value = false }
}
async function saveMonth() {
  savingMonth.value = true
  try {
    const [year, monthNo] = month.value.split('-').map(Number)
    await saveGoalMonth({ goalYear: year, goalMonth: monthNo, targetAmount: monthForm.targetAmount || 0, targetQty: monthForm.targetQty || 0 })
    ElMessage.success('月目标已保存')
    await load()
  } finally { savingMonth.value = false }
}
function openEdit(row) {
  Object.assign(editForm, { id: row.id || 0, categoryId: row.categoryId, categoryName: row.categoryName, targetAmount: Number(row.targetAmount || 0), targetQty: Number(row.targetQty || 0), unitPrice: Number(row.unitPrice || row.actualUnitPrice || 0) })
  editOpen.value = true
}
async function saveCategory() {
  savingCategory.value = true
  try {
    const [year, monthNo] = month.value.split('-').map(Number)
    await saveGoalCategory({ id: editForm.id, goalYear: year, goalMonth: monthNo, categoryId: editForm.categoryId, targetAmount: editForm.targetAmount || 0, targetQty: editForm.targetQty || 0, unitPrice: editForm.unitPrice || 0 })
    ElMessage.success('分类目标已保存')
    editOpen.value = false
    await load()
  } finally { savingCategory.value = false }
}
onMounted(load)
</script>

<style scoped>
.goal-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size:12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.month-switch { display: flex; align-items: center; gap: 2px; padding: 3px; border: 1px solid #efdde2; border-radius: 8px; background: #fff; }
.month-switch :deep(.el-date-editor) { width: 128px; }
.month-switch :deep(.el-input__wrapper) { box-shadow: none; }
.control-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.setting-panel { padding: 12px 14px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.panel-title { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; margin-bottom: 10px; }
.panel-title b { font-size: 14px; font-weight: 650; }
.panel-title span { color: #a28e93; font-size:12px; }
.setting-fields { display: grid; grid-template-columns: 1fr 1fr auto; gap: 8px; align-items: end; }
.setting-fields label, .goal-dialog label { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.setting-fields label span, .goal-dialog label span { color: #8c747b; font-size: 11px; }
.setting-fields :deep(.el-input-number), .goal-dialog :deep(.el-input-number) { width: 100%; }
.setting-note { margin: 9px 0 0; color: #a38e94; font-size:12px; }
.setting-note b { color: #a15063; font-weight: 650; }
.progress-band { border: 1px solid #ecd4da; border-radius: 8px; background: #fff; overflow: hidden; }
.progress-main { display: grid; grid-template-columns: 235px minmax(220px, 1fr) 1.55fr; gap: 24px; align-items: center; padding: 16px 20px; }
.progress-copy { display: flex; flex-direction: column; gap: 3px; }
.progress-copy span { color: #967f86; font-size: 11px; }
.progress-copy b { color: #7e3d50; font-size: 29px; line-height: 1.2; font-weight: 680; }
.progress-copy small { color: #a38e94; font-size:12px; }
.progress-visual { min-width: 0; }
.progress-track { height: 9px; background: #f7e9ed; overflow: hidden; border-radius: 2px; }
.progress-track i { display: block; height: 100%; background: linear-gradient(90deg, #e0a8b6, #a94f64); border-radius: 2px; transition: width .35s ease; }
.progress-scale { display: flex; justify-content: space-between; margin-top: 5px; color: #ad969d; font-size:12px; }
.progress-scale span:nth-child(2) { color: #a34b60; font-weight: 650; }
.progress-kpis { display: grid; grid-template-columns: repeat(4, 1fr); gap: 0; }
.progress-kpis div { display: flex; flex-direction: column; gap: 4px; padding: 0 14px; border-left: 1px solid #f3e4e8; }
.progress-kpis span { color: #9c888d; font-size:12px; }
.progress-kpis b { color: #5b454d; font-size: 14px; font-weight: 650; white-space: nowrap; }
.period-strip { display: flex; align-items: center; gap: 12px; padding: 10px 14px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.period-copy { display: flex; flex-direction: column; gap: 2px; margin-right: 4px; }
.period-copy b { font-size: 13px; font-weight: 650; }
.period-copy span { color: #a28e93; font-size:12px; }
.period-strip :deep(.el-date-editor) { width: 245px; }
.period-metrics { display: flex; align-items: center; gap: 18px; margin-left: auto; color: #99838a; font-size: 11px; white-space: nowrap; }
.period-metrics b { color: #604951; font-weight: 650; margin-left: 3px; }
.period-metrics b.accent { color: #a34b60; }
.category-panel { border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.panel-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 12px; padding: 12px 16px; border-bottom: 1px solid #f5e9ec; }
.panel-head h2 { margin: 0 0 3px; font-size: 14px; font-weight: 650; }
.panel-head span { color: #a28e93; font-size: 11px; }
.panel-tip { padding-top: 3px; white-space: nowrap; }
.category-head { display: grid; grid-template-columns: 1.05fr 2fr 0.65fr 0.85fr 0.55fr; gap: 20px; padding: 8px 16px; background: #fdf8f9; color: #9b848b; font-size:12px; }
.category-list { display: flex; flex-direction: column; }
.category-row { display: grid; grid-template-columns: 1.05fr 2fr 0.65fr 0.85fr 0.55fr; gap: 20px; align-items: center; padding: 13px 16px; border-top: 1px solid #f7edef; }
.category-row:first-child { border-top: 0; }
.cat-name { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.cat-name b { font-size: 13.5px; font-weight: 650; }
.cat-name small, .cat-amount small, .cat-gross small { color: #a38e94; font-size:12px; }
.cat-amount { min-width: 0; }
.cat-amount > div:first-child { display: flex; align-items: baseline; gap: 4px; }
.cat-amount b { font-size: 14px; font-weight: 650; color: #604951; }
.cat-amount span { color: #a38e94; font-size: 11px; }
.cat-track { height: 6px; margin: 6px 0 4px; background: #f7edef; overflow: hidden; }
.cat-track i { display: block; height: 100%; background: #ca8193; }
.cat-percent { font-size: 13px; font-weight: 650; }
.cat-gross { display: flex; flex-direction: column; gap: 3px; }
.cat-gross b { font-size: 13px; font-weight: 650; color: #604951; }
.good { color: #4d8062; }
.normal { color: #98713b; }
.low { color: #b06267; }
.cat-actions { text-align: right; }
.goal-dialog { display: flex; flex-direction: column; gap: 12px; }
.dialog-cat { padding: 9px 12px; background: #fdf5f7; color: #8b4355; font-weight: 650; font-size: 14px; }
.goal-dialog p { margin: 0; color: #a38e94; font-size:12px; }
@media (max-width: 1100px) {
  .progress-main { grid-template-columns: 210px 1fr; }
  .progress-kpis { grid-column: span 2; padding-top: 12px; border-top: 1px solid #f5e9ec; }
  .period-strip { flex-wrap: wrap; }
  .period-metrics { margin-left: 0; }
}
@media (max-width: 760px) {
  .goal-page { padding: 12px; }
  .page-head { align-items: flex-start; flex-direction: column; gap: 10px; }
  .control-grid { grid-template-columns: 1fr; }
  .setting-fields { grid-template-columns: 1fr 1fr; }
  .setting-fields .el-button { grid-column: span 2; }
  .progress-main { grid-template-columns: 1fr; gap: 14px; }
  .progress-kpis { grid-column: auto; grid-template-columns: 1fr 1fr; gap: 12px 0; }
  .period-strip { align-items: stretch; flex-direction: column; }
  .period-strip :deep(.el-date-editor) { width: 100%; }
  .period-metrics { flex-wrap: wrap; gap: 8px 14px; }
  .category-head { display: none; }
  .category-row { grid-template-columns: 1fr 1fr; gap: 9px 12px; }
  .cat-amount { grid-column: span 2; }
  .cat-actions { text-align: left; }
}
</style>
