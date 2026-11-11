<template>
  <div class="jy-workbench">
    <header class="wb-head">
      <div class="wb-title">
        <div class="wb-kicker"><span></span>WORK DESK</div>
        <div class="wb-title-line">
          <h1>待办与目标</h1>
          <span class="wb-date">{{ serverDateText }}</span>
        </div>
        <p>按四象限安排优先顺序，把重要的事先推进。</p>
      </div>
      <div class="wb-actions">
        <div class="mode-tabs">
          <button type="button" :class="{ active: mode === 'work' }" @click="switchMode('work')">待办与目标</button>
          <button type="button" :class="{ active: mode === 'daily' }" @click="switchMode('daily')">每日必做</button>
        </div>
        <template v-if="mode === 'work'">
          <el-button v-hasPermi="['jingyue:workitem:template']" plain :icon="MagicStick" @click="templateOpen = true">目标模板</el-button>
          <el-button plain :icon="Setting" @click="openQuickManage">快捷事项</el-button>
          <el-button v-hasPermi="['jingyue:workitem:add']" type="primary" :icon="Plus" @click="openCreate()">新建待办</el-button>
        </template>
      </div>
    </header>

    <section v-if="mode === 'work'" class="quick-strip">
      <div class="strip-label"><span>快捷创建</span><small>点一下带入标题，再补充时间与目标</small></div>
      <div class="quick-actions">
        <button v-for="action in quickActions" :key="action.id" type="button" class="quick-chip" @click="createFromQuick(action)">
          <el-icon><Lightning /></el-icon><span>{{ action.actionName }}</span>
        </button>
        <button type="button" class="quick-chip manage" @click="openQuickManage"><el-icon><EditPen /></el-icon><span>维护</span></button>
      </div>
    </section>

    <section v-if="mode === 'work'" class="quadrant-board" v-loading="statsLoading">
      <button v-for="item in quadrants" :key="item.value" type="button" class="quadrant-card" :class="[`q${item.value}`, { active: Number(query.priority) === item.value }]" @click="togglePriority(item.value)">
        <span class="quadrant-mark"></span>
        <span class="quadrant-copy"><b>{{ item.label }}</b><small>{{ item.hint }}</small></span>
        <strong>{{ quadrantCount(item.value) }}</strong>
        <el-icon><ArrowRight /></el-icon>
      </button>
      <div class="board-foot">
        <button type="button" :class="{ active: query.view === 'overdue' }" @click="switchView('overdue')"><el-icon><Warning /></el-icon><span>逾期</span><b>{{ stats.overdueCount || 0 }}</b></button>
        <button type="button" :class="{ active: query.view === 'today' }" @click="switchView('today')"><el-icon><Calendar /></el-icon><span>今日到期</span><b>{{ stats.todayCount || 0 }}</b></button>
        <div class="progress-summary"><span>整体完成</span><b>{{ Number(stats.completionRate || 0).toFixed(0) }}%</b><i><em :style="{ width: `${Math.min(100, Number(stats.completionRate || 0))}%` }"></em></i></div>
      </div>
    </section>

    <main v-if="mode === 'work'" class="work-panel">
      <div class="work-toolbar">
        <div class="view-tabs">
          <button v-for="view in views" :key="view.key" type="button" :class="{ active: query.view === view.key }" @click="switchView(view.key)">{{ view.label }}</button>
        </div>
        <div class="toolbar-filters">
          <el-input v-model="query.keyword" clearable :prefix-icon="Search" placeholder="搜索待办、备注或子目标" @keyup.enter="search" @clear="search" />
          <el-select v-model="query.ownerUserId" clearable filterable placeholder="全部负责人" @change="search">
            <el-option v-for="owner in owners" :key="owner.userId" :label="owner.nickName || owner.userName" :value="owner.userId" />
          </el-select>
          <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" start-placeholder="开始日期" end-placeholder="结束日期" range-separator="至" @change="search" />
          <el-button text :icon="Refresh" @click="resetFilters">重置</el-button>
        </div>
      </div>

      <div class="list-shell" v-loading="loading">
        <div v-for="row in rows" :key="row.id" class="work-group" :class="{ done: row.status === 1, overdue: row.isOverdue }">
          <div class="group-head">
            <button type="button" class="status-check" :class="{ checked: row.status === 1 }" @click="toggleComplete(row)">
              <el-icon v-if="row.status === 1"><Check /></el-icon>
            </button>
            <div class="group-copy" @click="openDetail(row)">
              <div class="group-title">
                <h2>{{ row.title }}</h2>
                <span class="priority-pill" :class="priorityClass(row)">{{ row.priorityName }}</span>
                <span v-if="row.isOverdue" class="overdue-label"><el-icon><Clock /></el-icon>逾期</span>
              </div>
              <div class="group-meta">
                <span>{{ row.itemNo || '待生成编号' }}</span>
                <span v-if="row.hasDateRange === 1">{{ periodText(row) }}</span>
                <span v-else>未设置时间区间</span>
                <span v-if="row.ownerName">{{ row.ownerName }}</span>
                <span v-if="row.isPerformanceRelated === 1" class="performance-tag"><el-icon><TrendCharts /></el-icon>关联业绩</span>
              </div>
              <p v-if="row.remark" class="row-remark" :title="row.remark">{{ row.remark }}</p>
            </div>
            <div class="group-progress">
              <b>{{ progress(row) }}%</b>
              <i><em :style="{ width: progress(row) + '%' }"></em></i>
              <small v-if="row.childCount">{{ row.doneChildCount }}/{{ row.childCount }} 个子目标</small>
              <small v-else>{{ row.status === 1 ? '已完成' : '待处理' }}</small>
            </div>
            <div class="group-tools">
              <el-button class="row-complete" :class="{ done: row.status === 1 }" size="small" @click.stop="toggleComplete(row)">{{ row.status === 1 ? '恢复' : '完成' }}</el-button>
              <el-tooltip content="添加子目标" placement="top"><el-button v-hasPermi="['jingyue:workitem:add']" text circle :icon="Plus" @click.stop="openCreate(row)" /></el-tooltip>
              <el-tooltip content="编辑" placement="top"><el-button v-hasPermi="['jingyue:workitem:edit']" text circle :icon="EditPen" @click.stop="openEdit(row)" /></el-tooltip>
              <el-dropdown trigger="click" @command="(command) => handleCommand(command, row)">
                <el-button text circle :icon="MoreFilled" @click.stop />
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="detail" :icon="View">查看详情</el-dropdown-item>
                    <el-dropdown-item v-if="!row.childCount" command="complete" :icon="row.status === 1 ? RefreshLeft : CircleCheck">{{ row.status === 1 ? '恢复进行中' : '标记完成' }}</el-dropdown-item>
                    <el-dropdown-item command="delete" :icon="Delete" divided>删除</el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </div>

          <div v-if="row.children?.length" class="child-list">
            <div v-for="child in row.children" :key="child.id" class="child-row" :class="{ done: child.status === 1, overdue: child.isOverdue }">
              <button type="button" class="status-check small" :class="{ checked: child.status === 1 }" @click.stop="toggleComplete(child)">
                <el-icon v-if="child.status === 1"><Check /></el-icon>
              </button>
              <div class="child-copy" @click="openDetail(child)">
                <div class="child-title">
                  <span>{{ child.title }}</span>
                  <span class="priority-pill small" :class="priorityClass(child)">{{ child.priorityName }}</span>
                  <span v-if="child.isOverdue" class="overdue-label"><el-icon><Clock /></el-icon>逾期</span>
                </div>
                <div class="child-meta">
                  <span v-if="child.categoryName">{{ child.categoryName }}</span>
                  <span v-if="child.isPerformanceRelated === 1">{{ targetText(child) }}</span>
                  <span v-if="child.ownerName">{{ child.ownerName }}</span>
                  <span v-if="child.hasDateRange === 1 && child.finishDate">计划完成 {{ dateText(child.finishDate) }}</span>
                </div>
                <p v-if="child.remark" class="row-remark" :title="child.remark">{{ child.remark }}</p>
              </div>
              <div class="child-progress-wrap">
                <div class="child-progress"><i :style="{ width: progress(child) + '%' }"></i></div>
                <b>{{ progress(child) }}%</b>
              </div>
              <div class="child-tools">
                <el-button class="row-complete small" :class="{ done: child.status === 1 }" size="small" @click.stop="toggleComplete(child)">{{ child.status === 1 ? '恢复' : '完成' }}</el-button>
                <el-button v-hasPermi="['jingyue:workitem:edit']" text circle size="small" :icon="EditPen" @click.stop="openEdit(child)" />
                <el-button v-hasPermi="['jingyue:workitem:remove']" text circle size="small" :icon="Delete" @click.stop="removeItem(child)" />
              </div>
            </div>
          </div>
          <div v-else class="group-note">
            <el-icon><InfoFilled /></el-icon>
            <span>{{ row.isPerformanceRelated === 1 ? '该待办关联业绩，可添加款式分类目标；未拆子目标时按主事项统计。' : '可直接完成，也可添加子目标拆解执行步骤。' }}</span>
          </div>
        </div>

        <div v-if="!rows.length && !loading" class="empty-state">
          <span class="empty-mark"><el-icon><Tickets /></el-icon></span>
          <h3>当前没有匹配的待办</h3>
          <p>新建一条待办，或从上方快捷事项开始。</p>
          <el-button type="primary" :icon="Plus" @click="openCreate()">新建待办</el-button>
        </div>
      </div>

      <div v-if="total > query.pageSize" class="list-foot">
        <el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="loadList" @current-change="loadList" />
      </div>
    </main>

    <daily-tasks v-else />

    <el-dialog v-model="quickManageOpen" title="快捷事项维护" width="560px" append-to-body class="quick-dialog">
      <p class="dialog-hint">快捷事项只生成待办标题，日期、优先级、业绩关联和子目标仍在待办编辑器中确认。</p>
      <div class="quick-manage-list">
        <div v-for="action in allQuickActions" :key="action.id" class="quick-manage-row">
          <span class="quick-manage-icon"><el-icon><Lightning /></el-icon></span>
          <div><b>{{ action.actionName }}</b><small>{{ action.titleTemplate || action.actionName }}</small></div>
          <el-tag v-if="action.isPreset === 1" size="small" effect="plain">预设</el-tag>
          <el-button text circle :icon="EditPen" @click="editQuickAction(action)" />
          <el-button v-if="action.isPreset !== 1" text circle type="danger" :icon="Delete" @click="removeQuickAction(action)" />
        </div>
      </div>
      <div class="quick-form">
        <el-input v-model="quickForm.actionName" maxlength="100" placeholder="快捷事项名称，例如：客户回访" />
        <el-input v-model="quickForm.titleTemplate" maxlength="200" placeholder="自动生成的待办标题" />
        <el-button type="primary" :loading="quickSaving" @click="saveQuickActionItem">{{ quickForm.id ? '保存修改' : '添加快捷事项' }}</el-button>
        <el-button v-if="quickForm.id" text @click="resetQuickForm">取消编辑</el-button>
      </div>
    </el-dialog>

    <work-item-editor v-model="editorOpen" :item-id="editorId" :parent-id="editorParentId" :initial-title="editorInitialTitle" :options="options" @success="afterSave" />
    <work-item-detail v-model="detailOpen" :item-id="detailId" :options="options" @edit="openEdit" @add-child="openCreate" @changed="refreshAll" @follow="openFollow" />
    <work-template-dialog v-model="templateOpen" :options="options" @applied="afterTemplateApplied" />
    <member-follow-dialog v-model="followOpen" :item="followItem" :options="options" @success="afterFollow" />
  </div>
</template>

<script setup name="DailySchedule">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Plus, Search, Refresh, RefreshLeft, Filter, MagicStick, Warning, CircleCheck, Check,
  Calendar, EditPen, MoreFilled, View, Delete, Clock, ArrowRight, Tickets, InfoFilled,
  Setting, Lightning, TrendCharts
} from '@element-plus/icons-vue'
import {
  listWorkItems, getWorkItemStats, getWorkItemOptions, completeWorkItem, deleteWorkItem,
  getQuickActions, saveQuickAction, deleteQuickAction
} from '@/api/jingyue/workitem'
import { syncServerTime, serverNow, serverTodayText, parseServerDateTime } from '@/utils/serverTime'
import WorkItemEditor from './WorkItemEditor.vue'
import WorkItemDetail from './WorkItemDetail.vue'
import WorkTemplateDialog from './WorkTemplateDialog.vue'
import MemberFollowDialog from './MemberFollowDialog.vue'
import DailyTasks from './DailyTasks.vue'

const mode = ref('work')
const loading = ref(false)
const statsLoading = ref(false)
const rows = ref([])
const total = ref(0)
const dateRange = ref([])
const options = reactive({ owners: [], categories: [], members: [], quickActions: [] })
const stats = reactive({
  groupCount: 0, totalCount: 0, openCount: 0, todayCount: 0, overdueCount: 0,
  doneThisWeek: 0, highPriorityCount: 0, importantUrgentCount: 0,
  importantNotUrgentCount: 0, notImportantUrgentCount: 0, notImportantNotUrgentCount: 0,
  completionRate: 0, serverDate: ''
})
const query = reactive({ pageNum: 1, pageSize: 10, keyword: '', view: 'all', status: undefined, priority: undefined, itemKind: undefined, ownerUserId: undefined, beginTime: null, endTime: null })
const editorOpen = ref(false)
const editorId = ref(0)
const editorParentId = ref(0)
const editorInitialTitle = ref('')
const detailOpen = ref(false)
const detailId = ref(0)
const templateOpen = ref(false)
const followOpen = ref(false)
const followItem = ref(null)
const quickManageOpen = ref(false)
const quickSaving = ref(false)
const allQuickActions = ref([])
const quickForm = reactive({ id: 0, actionName: '', titleTemplate: '', sortNo: 90, status: 0 })
const quadrants = [
  { value: 4, label: '重要且紧急', hint: '立即处理' },
  { value: 3, label: '重要不紧急', hint: '安排推进' },
  { value: 2, label: '不重要但紧急', hint: '尽快处理' },
  { value: 1, label: '不重要不紧急', hint: '有空再做' }
]
const views = [
  { key: 'all', label: '全部' }, { key: 'week', label: '未来 7 天' },
  { key: 'done', label: '已完成' }
]
const owners = computed(() => options.owners || [])
const quickActions = computed(() => options.quickActions || [])
const serverDateText = computed(() => {
  const now = serverNow()
  const week = ['日', '一', '二', '三', '四', '五', '六'][now.getDay()]
  return `${serverTodayText()} 周${week}`
})
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function dateText(value) { return value ? String(value).replace('T', ' ').slice(0, 10) : '' }
function progress(row) { return Math.max(0, Math.min(100, Math.round(Number(row.progressPercent || 0)))) }
function priorityClass(row) { return `q${Number(row.priority || 2)}` }
function periodText(row) {
  const start = dateText(row.startDate)
  const end = dateText(row.endDate)
  if (!start) return '未设置时间区间'
  return start === end ? start : `${start} 至 ${end}`
}
function targetText(row) {
  if (Number(row.targetMode) === 2) return `目标 ${number(row.targetValue)} 件`
  return `目标 ¥${number(row.targetValue)}`
}
function quadrantCount(value) {
  return ({ 4: stats.importantUrgentCount, 3: stats.importantNotUrgentCount, 2: stats.notImportantUrgentCount, 1: stats.notImportantNotUrgentCount })[value] || 0
}
function togglePriority(value) {
  query.priority = Number(query.priority) === Number(value) ? undefined : Number(value)
  search()
}
async function loadStats() {
  statsLoading.value = true
  try {
    const res = await getWorkItemStats()
    Object.assign(stats, res.data || {})
  } finally { statsLoading.value = false }
}
async function loadOptions() {
  const res = await getWorkItemOptions()
  Object.assign(options, res.data || {})
  allQuickActions.value = res.data?.quickActions || []
}
async function loadList() {
  loading.value = true
  try {
    query.beginTime = dateRange.value?.[0] || null
    query.endTime = dateRange.value?.[1] || null
    const res = await listWorkItems(query)
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  } finally { loading.value = false }
}
function search() { query.pageNum = 1; loadList() }
function switchView(view) { query.view = view; query.pageNum = 1; loadList() }
function switchMode(value) {
  mode.value = value
  if (value === 'work' && !rows.value.length) refreshAll()
}
function resetFilters() {
  Object.assign(query, { pageNum: 1, keyword: '', view: 'all', status: undefined, priority: undefined, itemKind: undefined, ownerUserId: undefined })
  dateRange.value = []
  loadList()
}
function refreshAll() { loadStats(); loadList() }
function openCreate(parent = null, title = '') {
  editorId.value = 0
  editorParentId.value = parent?.id || 0
  editorInitialTitle.value = title || ''
  editorOpen.value = true
}
function openEdit(row) {
  editorId.value = row.id
  editorParentId.value = row.parentId || 0
  editorInitialTitle.value = ''
  editorOpen.value = true
}
function openDetail(row) { detailId.value = row.id; detailOpen.value = true }
function openFollow(item) { followItem.value = item; followOpen.value = true }
function createFromQuick(action) {
  openCreate(null, action.titleTemplate || action.actionName)
}
async function toggleComplete(row) {
  try {
    await ElMessageBox.confirm(row.status === 1 ? `将「${row.title}」恢复为进行中？` : `确认完成「${row.title}」？`, row.status === 1 ? '恢复事项' : '完成事项', { type: 'info', confirmButtonText: '确认' })
  } catch (error) { return }
  await completeWorkItem(row.id, row.status !== 1)
  ElMessage.success(row.status === 1 ? '已恢复为进行中' : '已完成')
  refreshAll()
}
async function removeItem(row) {
  try {
    await ElMessageBox.confirm(`确认删除「${row.title}」？删除后不可恢复。`, '删除事项', { type: 'warning', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' })
  } catch (error) { return }
  await deleteWorkItem(row.id)
  ElMessage.success('已删除')
  refreshAll()
}
function handleCommand(command, row) {
  if (command === 'detail') openDetail(row)
  else if (command === 'complete') toggleComplete(row)
  else if (command === 'delete') removeItem(row)
}
async function openQuickManage() {
  quickManageOpen.value = true
  resetQuickForm()
  try {
    const res = await getQuickActions()
    allQuickActions.value = res.data || []
  } catch (error) {}
}
function resetQuickForm() {
  Object.assign(quickForm, { id: 0, actionName: '', titleTemplate: '', sortNo: (allQuickActions.value.length + 1) * 10, status: 0 })
}
function editQuickAction(action) {
  Object.assign(quickForm, {
    id: action.id, actionName: action.actionName || '', titleTemplate: action.titleTemplate || '',
    sortNo: Number(action.sortNo || 90), status: Number(action.status || 0)
  })
}
async function saveQuickActionItem() {
  if (!quickForm.actionName.trim()) return ElMessage.warning('请输入快捷事项名称')
  if (quickSaving.value) return
  quickSaving.value = true
  try {
    await saveQuickAction({ ...quickForm, titleTemplate: quickForm.titleTemplate.trim() || quickForm.actionName.trim() })
    ElMessage.success(quickForm.id ? '快捷事项已更新' : '快捷事项已添加')
    resetQuickForm()
    const res = await getQuickActions()
    allQuickActions.value = res.data || []
    await loadOptions()
  } finally { quickSaving.value = false }
}
async function removeQuickAction(action) {
  try {
    await ElMessageBox.confirm(`删除快捷事项「${action.actionName}」？`, '删除快捷事项', { type: 'warning', confirmButtonText: '删除' })
  } catch (error) { return }
  await deleteQuickAction(action.id)
  ElMessage.success('快捷事项已删除')
  const res = await getQuickActions()
  allQuickActions.value = res.data || []
  await loadOptions()
}
function afterSave() { editorOpen.value = false; refreshAll() }
function afterTemplateApplied() { templateOpen.value = false; refreshAll() }
function afterFollow() { followOpen.value = false; detailOpen.value = false; refreshAll() }

onMounted(async () => {
  await syncServerTime()
  await Promise.all([loadOptions(), loadStats(), loadList()])
})
</script>

<style scoped>
.jy-workbench { min-height: calc(100vh - 84px); padding: 18px 22px 24px; background: #fbf8f8; color: #4e4043; }
.wb-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 24px; margin-bottom: 15px; }
.wb-kicker { display: flex; align-items: center; gap: 7px; margin-bottom: 7px; color: #ad8a91; font-size: 10px; font-weight: 700; letter-spacing: 1.7px; }
.wb-kicker span { width: 17px; height: 2px; border-radius: 99px; background: #ce8491; }
.wb-title-line { display: flex; align-items: baseline; gap: 13px; }
.wb-title-line h1 { margin: 0; color: #382d30; font-size: 25px; font-weight: 680; letter-spacing: -.3px; }
.wb-date { color: #a18b90; font-size: 12px; }
.wb-title p { margin: 7px 0 0; color: #998489; font-size: 12.5px; }
.wb-actions { display: flex; gap: 8px; flex: none; }
.mode-tabs { display: flex; gap: 2px; padding: 2px; margin-right: 3px; border-radius: 6px; background: #f7eff1; }
.mode-tabs button { height: 29px; padding: 0 11px; border: 0; border-radius: 5px; background: transparent; color: #927c81; font-size: 11.5px; cursor: pointer; }
.mode-tabs button.active { background: #fff; color: #a14455; font-weight: 600; box-shadow: 0 1px 4px rgba(94, 60, 68, .08); }
.row-remark { overflow: hidden; margin: 4px 0 0; color: #a08b8f; font-size: 10.5px; line-height: 1.5; text-overflow: ellipsis; white-space: nowrap; }
.wb-actions :deep(.el-button) { height: 33px; border-radius: 7px; font-size: 12px; }
.quick-strip { display: flex; align-items: center; gap: 16px; padding: 11px 14px; margin-bottom: 12px; border: 1px solid #efe2e5; border-radius: 10px; background: #fff; }
.strip-label { flex: none; min-width: 118px; padding-right: 14px; border-right: 1px solid #f0e5e7; }
.strip-label span { display: block; color: #5b454a; font-size: 13px; font-weight: 650; }
.strip-label small { display: block; margin-top: 3px; color: #a18c90; font-size: 10.5px; }
.quick-actions { display: flex; flex-wrap: wrap; gap: 7px; }
.quick-chip { display: inline-flex; align-items: center; gap: 5px; height: 28px; padding: 0 10px; border: 1px solid #eedce0; border-radius: 6px; background: #fff8f9; color: #875b64; font-size: 12px; cursor: pointer; transition: border-color .15s, background .15s, color .15s; }
.quick-chip:hover { border-color: #dba9b3; background: #fff1f4; color: #a24455; }
.quick-chip.manage { border-style: dashed; color: #a18b90; background: #fff; }
.quadrant-board { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; margin-bottom: 14px; }
.quadrant-card { display: grid; grid-template-columns: 8px minmax(0, 1fr) auto 14px; align-items: center; gap: 9px; min-height: 70px; padding: 12px 13px; border: 1px solid #eee1e4; border-radius: 9px; background: #fff; text-align: left; cursor: pointer; transition: border-color .16s, box-shadow .16s, transform .16s; }
.quadrant-card:hover { transform: translateY(-1px); border-color: #dcb7bf; box-shadow: 0 5px 14px rgba(103, 66, 73, .06); }
.quadrant-card.active { border-color: #cd7e8d; box-shadow: 0 0 0 2px rgba(205, 126, 141, .09); }
.quadrant-mark { width: 7px; height: 30px; border-radius: 99px; background: #c99a9f; }
.quadrant-copy b { display: block; color: #574346; font-size: 12.5px; font-weight: 600; }
.quadrant-copy small { display: block; margin-top: 3px; color: #a38e92; font-size: 10.5px; }
.quadrant-card strong { color: #3f3235; font-size: 24px; font-weight: 650; line-height: 1; font-variant-numeric: tabular-nums; }
.quadrant-card .el-icon { color: #c2a5aa; font-size: 12px; }
.quadrant-card.q4 .quadrant-mark { background: #c95869; }
.quadrant-card.q3 .quadrant-mark { background: #d29755; }
.quadrant-card.q2 .quadrant-mark { background: #8d7db0; }
.quadrant-card.q1 .quadrant-mark { background: #9ca7af; }
.board-foot { grid-column: 1 / -1; display: flex; align-items: center; gap: 7px; min-height: 35px; padding: 0 4px; }
.board-foot button { display: inline-flex; align-items: center; gap: 5px; padding: 4px 7px; border: 0; border-radius: 5px; background: transparent; color: #927c81; font-size: 11px; cursor: pointer; }
.board-foot button:hover, .board-foot button.active { background: #fff0f2; color: #a54e5e; }
.board-foot button b { color: #8a535e; font-weight: 650; }
.progress-summary { display: flex; align-items: center; gap: 7px; margin-left: auto; color: #9b858a; font-size: 11px; }
.progress-summary b { color: #a34d5d; font-size: 12px; }
.progress-summary i { width: 84px; height: 4px; overflow: hidden; border-radius: 99px; background: #f0e3e6; }
.progress-summary em { display: block; height: 100%; border-radius: inherit; background: #cd7b8a; }
.work-panel { overflow: hidden; border: 1px solid #eee1e4; border-radius: 11px; background: #fff; }
.work-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 14px; padding: 10px 13px; border-bottom: 1px solid #f1e7e9; background: #fffdfd; }
.view-tabs { display: flex; gap: 3px; padding: 2px; border-radius: 6px; background: #f8f1f2; }
.view-tabs button { height: 26px; padding: 0 10px; border: 0; border-radius: 5px; background: transparent; color: #917b80; font-size: 11.5px; cursor: pointer; }
.view-tabs button.active { background: #fff; color: #a14455; box-shadow: 0 1px 4px rgba(94, 60, 68, .08); font-weight: 600; }
.toolbar-filters { display: flex; align-items: center; gap: 7px; }
.toolbar-filters > .el-input { width: 230px; }
.toolbar-filters > .el-select { width: 130px; }
.toolbar-filters :deep(.el-input__wrapper) { box-shadow: 0 0 0 1px #eadde0 inset; border-radius: 6px; }
.toolbar-filters :deep(.el-button) { font-size: 11.5px; }
.list-shell { min-height: 340px; }
.work-group { border-bottom: 1px solid #f3eaec; }
.work-group:last-child { border-bottom: 0; }
.work-group.done { background: #fdfcfc; }
.group-head { display: flex; align-items: center; gap: 10px; min-height: 72px; padding: 11px 14px; }
.group-head:hover { background: #fffafa; }
.status-check { flex: none; width: 20px; height: 20px; display: grid; place-items: center; padding: 0; border: 1px solid #dbc7cb; border-radius: 50%; background: #fff; color: #fff; font-size: 11px; cursor: pointer; }
.status-check:hover { border-color: #c47c8b; }
.status-check.checked { border-color: #79a78c; background: #79a78c; }
.status-check.small { width: 17px; height: 17px; font-size: 9px; }
.group-copy { flex: 1; min-width: 0; cursor: pointer; }
.group-title { display: flex; align-items: center; gap: 7px; min-width: 0; }
.group-title h2 { overflow: hidden; margin: 0; color: #453639; font-size: 14px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.group.done .group-title h2, .work-group.done .group-title h2 { color: #8f7e82; text-decoration: none; }
.priority-pill { flex: none; padding: 2px 6px; border-radius: 4px; background: #f7ecee; color: #94666f; font-size: 10px; line-height: 1.45; }
.priority-pill.q4 { background: #fbe8eb; color: #a63f50; }
.priority-pill.q3 { background: #fbf0df; color: #9a6b2e; }
.priority-pill.q2 { background: #f0ecf7; color: #6f6093; }
.priority-pill.q1 { background: #eef1f3; color: #68747c; }
.priority-pill.small { padding: 1px 5px; font-size: 9.5px; }
.overdue-label { display: inline-flex; align-items: center; gap: 3px; flex: none; color: #bd4e5f; font-size: 10px; }
.group-meta { display: flex; flex-wrap: wrap; align-items: center; gap: 4px 11px; margin-top: 5px; color: #a28d91; font-size: 10.5px; }
.performance-tag { display: inline-flex; align-items: center; gap: 3px; color: #a26d46; }
.group-progress { flex: none; width: 115px; text-align: right; }
.group-progress b { color: #88525d; font-size: 13px; font-weight: 650; }
.group-progress i { display: block; height: 4px; margin: 5px 0 4px; overflow: hidden; border-radius: 99px; background: #f2e7e9; }
.group-progress em { display: block; height: 100%; border-radius: inherit; background: #cb7b8a; }
.group-progress small { color: #a08b8f; font-size: 10px; }
.group-tools { display: flex; align-items: center; gap: 1px; opacity: .35; transition: opacity .16s; }
.row-complete { height: 24px; padding: 0 9px; border-color: #e4cdd2; border-radius: 5px; background: #fff; color: #9b6470; font-size: 11px; }
.row-complete:hover { border-color: #cc8d99; background: #fff3f5; color: #a54255; }
.row-complete.done { border-color: #bcd4c5; background: #f2f8f4; color: #5e8a70; }
.row-complete.small { height: 21px; padding: 0 7px; font-size: 10.5px; }
.group-head:hover .group-tools { opacity: 1; }
.group-tools :deep(.el-button) { width: 27px; height: 27px; margin-left: 0; font-size: 12px; }
.child-list { margin: 0 14px 10px 44px; border-left: 1px solid #eddde0; }
.child-row { display: flex; align-items: center; gap: 9px; min-height: 52px; padding: 7px 8px 7px 13px; border-radius: 0 6px 6px 0; }
.child-row:hover { background: #fffafa; }
.child-row.done { opacity: .62; }
.child-row.overdue { border-left: 2px solid #cb6a79; margin-left: -1px; }
.child-copy { flex: 1; min-width: 0; cursor: pointer; }
.child-title { display: flex; align-items: center; gap: 6px; overflow: hidden; color: #5b474b; font-size: 12.5px; }
.child-title > span:first-child { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.child-meta { display: flex; flex-wrap: wrap; gap: 3px 10px; margin-top: 3px; color: #a28d91; font-size: 10.5px; }
.child-progress-wrap { flex: none; width: 88px; display: flex; align-items: center; gap: 6px; }
.child-progress { flex: 1; height: 4px; overflow: hidden; border-radius: 99px; background: #f2e7e9; }
.child-progress i { display: block; height: 100%; border-radius: inherit; background: #cc7b89; }
.child-progress-wrap b { width: 28px; color: #916a72; font-size: 10.5px; text-align: right; }
.child-tools { display: flex; opacity: .28; transition: opacity .16s; }
.child-row:hover .child-tools { opacity: 1; }
.child-tools :deep(.el-button) { width: 24px; height: 24px; margin-left: 0; font-size: 10px; }
.group-note { display: flex; align-items: center; gap: 5px; margin: 0 14px 10px 44px; padding: 6px 9px; border-radius: 5px; background: #fff9f5; color: #a08a7e; font-size: 10.5px; }
.empty-state { min-height: 330px; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #9d888c; }
.empty-mark { width: 46px; height: 46px; display: grid; place-items: center; margin-bottom: 11px; border-radius: 11px; background: #fff0f2; color: #c17a89; font-size: 20px; }
.empty-state h3 { margin: 0 0 5px; color: #5b474b; font-size: 14px; font-weight: 600; }
.empty-state p { margin: 0 0 13px; font-size: 11.5px; }
.list-foot { display: flex; justify-content: flex-end; padding: 9px 13px; border-top: 1px solid #f2e8ea; }
.dialog-hint { margin: 0 0 12px; color: #978186; font-size: 12px; line-height: 1.6; }
.quick-manage-list { display: flex; flex-direction: column; gap: 4px; max-height: 300px; overflow-y: auto; margin-bottom: 13px; }
.quick-manage-row { display: flex; align-items: center; gap: 9px; min-height: 46px; padding: 7px 9px; border: 1px solid #f0e4e6; border-radius: 7px; background: #fffafa; }
.quick-manage-icon { width: 27px; height: 27px; display: grid; place-items: center; border-radius: 6px; background: #f9e8eb; color: #b85c6d; }
.quick-manage-row > div { flex: 1; min-width: 0; }
.quick-manage-row b { display: block; color: #59464a; font-size: 12.5px; font-weight: 600; }
.quick-manage-row small { display: block; margin-top: 2px; color: #a18b90; font-size: 11px; }
.quick-form { display: grid; grid-template-columns: 1fr 1.3fr auto auto; gap: 7px; padding-top: 12px; border-top: 1px solid #f0e4e6; }
.quick-form :deep(.el-input__wrapper) { box-shadow: 0 0 0 1px #eadde0 inset; border-radius: 6px; }
@media (max-width: 1280px) {
  .quadrant-board { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .toolbar-filters > .el-input { width: 190px; }
}
@media (max-width: 980px) {
  .jy-workbench { padding: 13px; }
  .wb-head { flex-direction: column; gap: 12px; }
  .quick-strip { align-items: flex-start; flex-direction: column; gap: 9px; }
  .strip-label { padding-right: 0; border-right: 0; }
  .work-toolbar { align-items: flex-start; flex-direction: column; }
  .toolbar-filters { flex-wrap: wrap; width: 100%; }
  .toolbar-filters > .el-input { flex: 1; min-width: 180px; }
  .group-progress { display: none; }
  .quick-form { grid-template-columns: 1fr; }
}
@media (max-width: 640px) {
  .quadrant-board { grid-template-columns: 1fr 1fr; }
  .board-foot { flex-wrap: wrap; }
  .progress-summary { width: 100%; margin-left: 0; }
  .wb-actions { flex-wrap: wrap; }
  .group-title { flex-wrap: wrap; }
  .group-meta span:nth-child(n+3) { display: none; }
  .child-list, .group-note { margin-left: 32px; }
}
</style>