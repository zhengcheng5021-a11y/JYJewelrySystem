<template>
  <div class="review-page">
    <header class="review-head">
      <div>
        <span class="head-kicker">BUSINESS REVIEW</span>
        <h1>多周期复盘</h1>
        <p>按日、周、月、季、年沉淀经营判断，需要跟进的事项可直接生成待办。</p>
      </div>
      <el-button v-hasPermi="['jingyue:member:edit']" type="primary" :icon="Plus" @click="openEditor()">新增复盘</el-button>
    </header>

    <section class="review-toolbar">
      <div class="period-tabs">
        <button v-for="period in periods" :key="period.value" type="button" :class="{ active: query.periodType === period.value }" @click="selectPeriod(period.value)">
          <span>{{ period.short }}</span>
          <small>{{ period.label }}</small>
        </button>
      </div>
      <div class="date-filter">
        <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" :shortcuts="dateShortcuts" @change="search" />
        <el-button :icon="Refresh" @click="resetFilter">重置</el-button>
      </div>
    </section>

    <section class="review-summary">
      <div class="summary-main">
        <span>{{ currentPeriod.label }}记录</span>
        <strong>{{ total }}</strong>
        <small>条复盘沉淀</small>
      </div>
      <div><span>已生成待办</span><strong>{{ generatedCount }}</strong></div>
      <div><span>最近复盘</span><strong>{{ rows[0] ? rows[0].reviewDate.slice(0, 10) : '暂无' }}</strong></div>
    </section>

    <section v-loading="loading" class="review-list">
      <article v-for="row in rows" :key="row.id" class="review-card">
        <div class="review-date">
          <b>{{ dayText(row.reviewDate) }}</b>
          <span>{{ monthText(row.reviewDate) }}</span>
        </div>
        <div class="review-body">
          <div class="review-title-line">
            <h3>{{ row.title }}</h3>
            <span class="period-badge">{{ row.periodName }}</span>
            <span v-if="row.workItemNo" class="todo-badge"><el-icon><Calendar /></el-icon>待办 {{ row.workItemNo }}</span>
          </div>
          <p>{{ row.content }}</p>
          <div class="review-meta">
            <span>{{ row.periodStart.slice(0, 10) }} 至 {{ row.periodEnd.slice(0, 10) }}</span>
            <span>{{ row.createBy || '系统' }} · {{ dateTime(row.createTime) }}</span>
          </div>
        </div>
        <div class="review-actions">
          <el-button v-hasPermi="['jingyue:member:edit']" link type="primary" :icon="EditPen" @click="openEditor(row)">编辑</el-button>
          <el-button v-hasPermi="['jingyue:member:remove']" link type="danger" :icon="Delete" @click="remove(row)">删除</el-button>
        </div>
      </article>
      <div v-if="!rows.length && !loading" class="review-empty">
        <div class="empty-mark"><el-icon><Notebook /></el-icon></div>
        <h3>{{ currentPeriod.empty }}</h3>
        <p>把今天看到的问题、亮点和下一步记下来。</p>
        <el-button v-hasPermi="['jingyue:member:edit']" type="primary" :icon="Plus" @click="openEditor()">写一条复盘</el-button>
      </div>
    </section>
    <div class="review-foot">
      <span>复盘内容会保留在当前门店，不会自动修改销售数据。</span>
      <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
    </div>

    <el-dialog v-model="editorOpen" :title="form.id ? '编辑复盘' : '新增复盘'" width="660px" append-to-body destroy-on-close class="review-dialog" @closed="resetEditor">
      <div class="editor-hint"><el-icon><Opportunity /></el-icon><span>{{ editorPeriod.template }}</span></div>
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top">
        <div class="editor-grid">
          <el-form-item label="复盘周期">
            <el-select v-model="form.periodType" style="width: 100%" @change="syncTitle">
              <el-option v-for="period in periods" :key="period.value" :label="period.label" :value="period.value" />
            </el-select>
          </el-form-item>
          <el-form-item label="复盘日期" prop="reviewDate">
            <el-date-picker v-model="form.reviewDate" type="date" value-format="YYYY-MM-DD" :disabled-date="disableFuture" style="width: 100%" @change="syncTitle" />
          </el-form-item>
        </div>
        <el-form-item label="复盘标题">
          <el-input v-model="form.title" maxlength="100" show-word-limit placeholder="留空将按周期和日期自动生成" />
        </el-form-item>
        <el-form-item label="复盘内容" prop="content">
          <el-input v-model="form.content" type="textarea" :rows="9" maxlength="4000" show-word-limit :placeholder="editorPeriod.placeholder" />
        </el-form-item>
        <div class="todo-link">
          <div>
            <b>生成到待办事项</b>
            <span>把这次复盘转成可在待办与目标中跟踪的事项。</span>
          </div>
          <el-switch v-model="form.generateTodo" :active-value="1" :inactive-value="0" />
        </div>
        <div v-if="form.generateTodo === 1" class="priority-row">
          <span>待办优先级</span>
          <el-radio-group v-model="form.priority">
            <el-radio-button :value="4">重要紧急</el-radio-button>
            <el-radio-button :value="3">重要不紧急</el-radio-button>
            <el-radio-button :value="2">不重要紧急</el-radio-button>
            <el-radio-button :value="1">不重要不紧急</el-radio-button>
          </el-radio-group>
        </div>
      </el-form>
      <template #footer><el-button @click="editorOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submit">保存复盘</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyReview">
import { Calendar, Delete, EditPen, Notebook, Opportunity, Plus, Refresh } from '@element-plus/icons-vue'
import { listMemberReviews, saveMemberReview, deleteMemberReview } from '@/api/jingyue/member'
import { serverTodayText, serverRange } from '@/utils/serverTime'

const { proxy } = getCurrentInstance()
const periods = [
  { value: 1, short: '日', label: '每日复盘', empty: '今天还没有复盘', template: '记录今日亮点、卡点与明日优化行动。', placeholder: '例如：今日卖出 2 条手串，客户更关注材质与佩戴场景……' },
  { value: 2, short: '周', label: '周复盘', empty: '本周还没有复盘', template: '回顾本周业绩、品类结构、客户跟进与下周重点。', placeholder: '本周完成了什么，哪些客户需要继续跟进……' },
  { value: 3, short: '月', label: '月度复盘', empty: '本月还没有复盘', template: '复盘目标达成、客户沉淀、问题原因与下月计划。', placeholder: '本月目标达成情况、主要问题和下月动作……' },
  { value: 4, short: '季', label: '季度复盘', empty: '本季度还没有复盘', template: '审视季度整体目标、品类趋势与长期规划。', placeholder: '季度目标完成度、品类趋势与下一步策略……' },
  { value: 5, short: '年', label: '年复盘', empty: '今年还没有复盘', template: '总结全年目标、品类结构变化、客户沉淀与明年方向。', placeholder: '今年做成了什么，明年先攻哪一块……' }
]
const query = reactive({ pageNum: 1, pageSize: 10, periodType: 1, beginDate: undefined, endDate: undefined })
const dateRange = ref([])
const rows = ref([])
const total = ref(0)
const loading = ref(false)
const editorOpen = ref(false)
const saving = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const rules = {
  reviewDate: [{ required: true, message: '请选择复盘日期', trigger: 'change' }],
  content: [{ required: true, message: '请填写复盘内容', trigger: 'blur' }]
}
const dateShortcuts = [
  { text: '最近 7 天', value: () => fromTextRange(serverRange(6)) },
  { text: '最近 30 天', value: () => fromTextRange(serverRange(29)) },
  { text: '本月', value: () => { const text = serverTodayText(); return [new Date(text.slice(0, 7) + '-01'), new Date(text)] } }
]
const currentPeriod = computed(() => periods.find((it) => it.value === query.periodType) || periods[0])
const editorPeriod = computed(() => periods.find((it) => it.value === form.periodType) || periods[0])
const generatedCount = computed(() => rows.value.filter((it) => it.workItemId).length)

function emptyForm() {
  return { id: 0, periodType: 1, reviewDate: serverTodayText(), title: '', content: '', generateTodo: 0, priority: 3 }
}
function fromTextRange(range) { return range.map((it) => new Date(it + 'T00:00:00')) }
function dateText(value) { return String(value || '').slice(0, 10) }
function dateTime(value) { return value ? String(value).replace('T', ' ').slice(0, 16) : '' }
function dayText(value) { return String(value || '').slice(8, 10) }
function monthText(value) { return String(value || '').slice(5, 7) + '月' }
function disableFuture(date) { return date > new Date(serverTodayText() + 'T23:59:59') }
function loadList() {
  loading.value = true
  listMemberReviews({ ...query, beginDate: dateRange.value?.[0], endDate: dateRange.value?.[1] })
    .then((res) => { rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 })
    .finally(() => { loading.value = false })
}
function search() { query.pageNum = 1; loadList() }
function resetFilter() { dateRange.value = []; search() }
function selectPeriod(value) { query.periodType = value; search() }
function openEditor(row) {
  Object.assign(form, emptyForm(), row ? {
    id: row.id, periodType: row.periodType, reviewDate: dateText(row.reviewDate),
    title: row.title, content: row.content, generateTodo: row.workItemId ? 1 : 0, priority: 3
  } : { periodType: query.periodType, reviewDate: serverTodayText() })
  if (row && row.workItemId) form.generateTodo = 1
  editorOpen.value = true
}
function syncTitle() { /* 标题留空时由后端按周期和日期生成，避免前端日期规则漂移。 */ }
function resetEditor() { formRef.value?.clearValidate() }
function submit() {
  formRef.value?.validate((valid) => {
    if (!valid) return
    saving.value = true
    saveMemberReview({ ...form, reviewDate: form.reviewDate || null })
      .then(() => {
        proxy.$modal.msgSuccess(form.generateTodo === 1 ? '复盘已保存，并生成待办' : '复盘已保存')
        editorOpen.value = false
        loadList()
      }).finally(() => { saving.value = false })
  })
}
function remove(row) {
  proxy.$modal.confirm('删除这条复盘？若已生成待办，需要先删除关联待办。').then(() => deleteMemberReview(row.id)).then(() => {
    proxy.$modal.msgSuccess('已删除')
    loadList()
  }).catch(() => {})
}
onMounted(loadList)
</script>

<style scoped>
.review-page { color: #3f3035; }
.review-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; padding: 24px 27px; background: linear-gradient(125deg, #fdf4f4, #fff 68%); border: 1px solid #f1dfe3; border-radius: 14px; }
.head-kicker { color: #b47784; font-size: 11px; letter-spacing: 2px; }
.review-head h1 { margin: 7px 0 5px; color: #4b343b; font-size: 26px; font-weight: 650; letter-spacing: 1px; }
.review-head p { margin: 0; color: #947d83; font-size: 13px; line-height: 1.7; }
.review-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin: 14px 0; padding: 12px 14px; background: #fff; border: 1px solid #f0e2e5; border-radius: 10px; }
.period-tabs { display: flex; gap: 4px; }
.period-tabs button { display: flex; align-items: baseline; gap: 5px; padding: 7px 12px; color: #a0878d; background: transparent; border: 1px solid transparent; border-radius: 7px; cursor: pointer; }
.period-tabs button span { font-size: 14px; font-weight: 600; }
.period-tabs button small { font-size: 12px; }
.period-tabs button.active { color: #9b5263; background: #fdf1f4; border-color: #f0d4da; }
.date-filter { display: flex; align-items: center; gap: 8px; }
.date-filter :deep(.el-date-editor) { width: 250px; }
.review-summary { display: grid; grid-template-columns: 1.4fr 1fr 1fr; margin-bottom: 14px; background: #fff; border: 1px solid #f0e2e5; border-radius: 10px; }
.review-summary > div { display: flex; flex-direction: column; gap: 4px; padding: 13px 17px; border-left: 1px solid #f6ebee; }
.review-summary > div:first-child { border-left: 0; }
.review-summary span { color: #9f878d; font-size: 12px; }
.review-summary strong { color: #5e454d; font-size: 20px; font-weight: 650; }
.review-summary small { color: #ad989d; font-size: 11px; }
.review-summary .summary-main { display: grid; grid-template-columns: 1fr auto; gap: 2px 8px; align-items: baseline; }
.review-summary .summary-main strong { grid-row: span 2; color: #9f5263; font-size: 27px; }
.review-list { display: flex; flex-direction: column; gap: 9px; min-height: 300px; }
.review-card { display: grid; grid-template-columns: 63px minmax(0, 1fr) auto; gap: 15px; align-items: flex-start; padding: 16px 17px; background: #fff; border: 1px solid #f0e2e5; border-radius: 10px; transition: border-color .18s, box-shadow .18s; }
.review-card:hover { border-color: #e7c5cd; box-shadow: 0 5px 16px rgba(120, 70, 82, .05); }
.review-date { display: flex; flex-direction: column; align-items: center; padding: 8px 4px; background: #fdf5f6; border-radius: 7px; }
.review-date b { color: #9e5363; font-size: 24px; font-weight: 650; line-height: 1.1; }
.review-date span { margin-top: 3px; color: #af9298; font-size: 11px; }
.review-body { min-width: 0; }
.review-title-line { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.review-title-line h3 { margin: 0; color: #584149; font-size: 15px; font-weight: 650; }
.period-badge, .todo-badge { padding: 3px 7px; color: #a66d78; background: #fdf0f2; border-radius: 4px; font-size: 11px; }
.todo-badge { display: inline-flex; align-items: center; gap: 3px; color: #5d8069; background: #f0f7f2; }
.review-body > p { display: -webkit-box; overflow: hidden; margin: 8px 0; color: #756067; font-size: 13px; line-height: 1.75; white-space: pre-wrap; -webkit-box-orient: vertical; -webkit-line-clamp: 3; }
.review-meta { display: flex; flex-wrap: wrap; gap: 10px; color: #aa959a; font-size: 11px; }
.review-actions { display: flex; flex-direction: column; align-items: flex-end; gap: 3px; }
.review-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 330px; color: #a38c92; }
.empty-mark { display: grid; place-items: center; width: 50px; height: 50px; margin-bottom: 10px; color: #b47784; background: #fdf1f3; border-radius: 12px; }
.empty-mark .el-icon { font-size: 22px; }
.review-empty h3 { margin: 0 0 5px; color: #59434a; font-size: 16px; font-weight: 600; }
.review-empty p { margin: 0 0 14px; font-size: 12px; }
.review-foot { display: flex; align-items: center; justify-content: space-between; margin-top: 10px; padding-top: 9px; border-top: 1px solid #f4e7ea; }
.review-foot > span { color: #b19ca1; font-size: 11px; }
.editor-hint { display: flex; align-items: flex-start; gap: 7px; margin-bottom: 15px; padding: 10px 12px; color: #8d606c; background: #fdf5f6; border: 1px solid #f3e2e5; border-radius: 7px; font-size: 12px; line-height: 1.6; }
.editor-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.todo-link { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 2px; padding: 12px 13px; background: #fffaf8; border: 1px solid #f3e5e2; border-radius: 8px; }
.todo-link > div { display: flex; flex-direction: column; gap: 3px; }
.todo-link b { color: #5d474e; font-size: 13px; font-weight: 600; }
.todo-link span { color: #a28c91; font-size: 11px; }
.priority-row { display: flex; align-items: center; gap: 13px; margin-top: 10px; padding: 10px 12px; background: #fdf8f9; border-radius: 7px; }
.priority-row > span { flex: none; color: #927b82; font-size: 12px; }
@media (max-width: 1020px) { .review-toolbar { align-items: flex-start; flex-direction: column; } .date-filter { width: 100%; } .date-filter :deep(.el-date-editor) { flex: 1; width: auto; } }
@media (max-width: 700px) { .review-page { padding: 10px; } .review-head { align-items: flex-start; flex-direction: column; padding: 18px; } .review-head h1 { font-size: 23px; } .period-tabs { flex-wrap: wrap; } .review-summary { grid-template-columns: 1fr 1fr; } .review-summary > div:last-child { display: none; } .review-card { grid-template-columns: 51px minmax(0, 1fr); gap: 10px; } .review-actions { grid-column: 2; flex-direction: row; justify-content: flex-end; } .editor-grid { grid-template-columns: 1fr; gap: 0; } .priority-row { align-items: flex-start; flex-direction: column; } .priority-row :deep(.el-radio-group) { display: flex; flex-wrap: wrap; gap: 4px; } }
</style>
