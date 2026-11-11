<template>
  <section class="daily-panel" v-loading="loading">
    <div class="daily-summary">
      <div class="daily-heading">
        <span class="daily-mark"><el-icon><Sunny /></el-icon></span>
        <div>
          <h2>每日必做</h2>
          <p>{{ dateLabel }}<template v-if="isToday"> · 今天</template></p>
        </div>
      </div>
      <div class="date-switch">
        <el-button text circle :icon="ArrowLeft" @click="shiftDate(-1)" />
        <el-date-picker v-model="taskDate" type="date" value-format="YYYY-MM-DD" :clearable="false" :disabled-date="disableFuture" @change="load" />
        <el-button text circle :icon="ArrowRight" :disabled="isToday" @click="shiftDate(1)" />
        <el-button plain size="small" :disabled="isToday" @click="goToday">回到今天</el-button>
      </div>
      <div class="daily-progress">
        <div class="progress-number"><b>{{ summary.percent }}%</b><span>{{ summary.done }} / {{ summary.total }} 项完成</span></div>
        <i><em :style="{ width: summary.percent + '%' }"></em></i>
      </div>
      <el-button v-if="isToday" type="primary" class="add-button" :icon="Plus" @click="startAdd">新增</el-button>
    </div>

    <div class="daily-toolbar">
      <div class="filter-tabs">
        <button v-for="tab in filters" :key="String(tab.value)" type="button" :class="{ active: status === tab.value }" @click="status = tab.value">
          {{ tab.label }}<b>{{ tab.count }}</b>
        </button>
      </div>
      <span class="server-hint">{{ isToday ? '当天数据可编辑，次日自动归档并按模板生成新一天' : '历史数据只读，可回看当天完成情况' }}</span>
    </div>

    <div v-if="isToday && adding" class="daily-row add-row">
      <span class="daily-check ghost"><el-icon><Plus /></el-icon></span>
      <div class="daily-edit-row add-edit-row">
        <el-input ref="addTitleInput" v-model="addForm.title" maxlength="200" placeholder="标题" @keyup.enter="saveAdd" />
        <el-input v-model="addForm.content" maxlength="200" placeholder="数量 / 文本" @keyup.enter="saveAdd" />
        <el-input v-model="addForm.remark" type="textarea" :autosize="{ minRows: 1, maxRows: 4 }" maxlength="500" placeholder="备注说明" />
      </div>
      <div class="daily-actions editing-actions">
        <el-button size="small" @click="cancelAdd">取消</el-button>
        <el-button type="primary" size="small" :loading="saving" :disabled="!addForm.title.trim()" @click="saveAdd">保存</el-button>
      </div>
    </div>

    <div v-if="visibleItems.length" class="daily-list">
      <div v-for="(item, index) in visibleItems" :key="item.id" class="daily-row" :class="{ done: item.isCompleted === 1, editing: editingId === item.id }">
        <button type="button" class="daily-check" :class="{ checked: item.isCompleted === 1, readonly: !isToday }" :disabled="!isToday" @click="toggle(item)">
          <el-icon v-if="item.isCompleted === 1"><Check /></el-icon>
        </button>

        <div v-if="editingId === item.id" class="daily-edit-row">
          <el-input v-model="editForm.title" maxlength="200" placeholder="必做标题" @keyup.enter="saveEdit" />
          <el-input v-model="editForm.content" maxlength="200" placeholder="数量 / 说明" @keyup.enter="saveEdit" />
          <el-input v-model="editForm.remark" type="textarea" :autosize="{ minRows: 1, maxRows: 4 }" maxlength="500" placeholder="备注说明" />
        </div>
        <div v-else class="daily-edit-row saved-row" @dblclick="startEdit(item)">
          <el-input :model-value="item.title" disabled maxlength="200" />
          <el-input :model-value="item.content" disabled maxlength="200" />
          <el-input :model-value="item.remark" type="textarea" :autosize="{ minRows: 1, maxRows: 4 }" disabled maxlength="500" />
        </div>

        <template v-if="editingId === item.id">
          <div class="daily-actions editing-actions">
            <el-button size="small" @click="cancelEdit">取消</el-button>
            <el-button type="primary" size="small" :loading="saving" @click="saveEdit">保存</el-button>
          </div>
        </template>
        <template v-else>
          <div v-if="isToday" class="daily-actions">
            <el-button size="small" :type="item.isCompleted === 1 ? 'success' : 'danger'" plain @click="toggle(item)">{{ item.isCompleted === 1 ? '已完成' : '完成' }}</el-button>
            <el-tooltip content="上移" placement="top"><el-button text circle size="small" :icon="ArrowUp" :disabled="index === 0" @click="move(index, -1)" /></el-tooltip>
            <el-tooltip content="下移" placement="top"><el-button text circle size="small" :icon="ArrowDown" :disabled="index === visibleItems.length - 1" @click="move(index, 1)" /></el-tooltip>
            <el-tooltip content="编辑" placement="top"><el-button text circle size="small" :icon="EditPen" @click="startEdit(item)" /></el-tooltip>
            <el-tooltip content="删除" placement="top"><el-button text circle size="small" type="danger" :icon="Delete" @click="removeItem(item)" /></el-tooltip>
          </div>
          <span v-else class="history-state" :class="item.isCompleted === 1 ? 'is-done' : 'is-open'">{{ item.isCompleted === 1 ? '已完成' : '未完成' }}</span>
        </template>
      </div>
    </div>
    <div v-else class="daily-empty">
      <span><el-icon><Tickets /></el-icon></span>
      <h3>{{ status === undefined ? '还没有每日必做' : '当前筛选下没有内容' }}</h3>
      <p>{{ isToday ? '点击右上角“新增”，在这一行填写标题、数量和备注。' : '这一天没有生成必做记录。' }}</p>
    </div>
  </section>
</template>

<script setup name="DailyTasks">
import { ref, reactive, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  Plus, Check, EditPen, Delete, ArrowUp, ArrowDown, ArrowLeft, ArrowRight, Tickets, Sunny
} from '@element-plus/icons-vue'
import {
  listDailyTasks, saveDailyTaskTemplate, completeDailyTask, updateDailyTask, deleteDailyTask, sortDailyTasks
} from '@/api/jingyue/workitem'
import { syncServerTime, serverTodayText, serverNow } from '@/utils/serverTime'

const loading = ref(false)
const saving = ref(false)
const today = ref(serverTodayText())
const taskDate = ref(today.value)
const status = ref(undefined)
const items = ref([])
const summary = reactive({ total: 0, done: 0, percent: 0 })
const addForm = reactive({ title: '', content: '', remark: '' })
const editForm = reactive({ title: '', content: '', remark: '' })
const editingId = ref(0)
const adding = ref(false)
const addTitleInput = ref()
let midnightTimer = null

const filters = computed(() => {
  const done = items.value.filter((it) => it.isCompleted === 1).length
  const total = items.value.length
  return [
    { label: '全部', value: undefined, count: total },
    { label: '未完成', value: 0, count: total - done },
    { label: '已完成', value: 1, count: done }
  ]
})
const visibleItems = computed(() => {
  if (status.value === undefined) return items.value
  return items.value.filter((it) => it.isCompleted === Number(status.value))
})
const isToday = computed(() => taskDate.value === today.value)
const dateLabel = computed(() => {
  if (!taskDate.value) return ''
  const value = new Date(taskDate.value + 'T00:00:00')
  const week = ['日', '一', '二', '三', '四', '五', '六'][value.getDay()]
  return `${taskDate.value} 周${week}`
})

function applyList(data) {
  const payload = data || {}
  items.value = payload.items || []
  Object.assign(summary, {
    total: Number(payload.summary?.total || 0),
    done: Number(payload.summary?.done || 0),
    percent: Number(payload.summary?.percent || 0)
  })
}
function timeText(value) {
  return value ? String(value).replace('T', ' ').slice(11, 16) : ''
}
async function load() {
  loading.value = true
  editingId.value = 0
  try {
    const res = await listDailyTasks({ taskDate: taskDate.value, status: status.value })
    applyList(res.data)
  } finally {
    loading.value = false
  }
}
function disableFuture(date) {
  return date > new Date(today.value + 'T23:59:59')
}
function shiftDate(days) {
  const value = new Date(taskDate.value + 'T00:00:00')
  value.setDate(value.getDate() + days)
  const text = `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
  if (text > today.value) return
  taskDate.value = text
  status.value = undefined
  load()
}
function goToday() {
  taskDate.value = today.value
  status.value = undefined
  load()
}
function startAdd() {
  if (!isToday.value) return
  editingId.value = 0
  adding.value = true
  Object.assign(addForm, { title: '', content: '', remark: '' })
  nextTick(() => addTitleInput.value?.focus?.())
}
function cancelAdd() {
  adding.value = false
  Object.assign(addForm, { title: '', content: '', remark: '' })
}
async function saveAdd() {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  if (!adding.value) return
  const title = addForm.title.trim()
  if (!title) return ElMessage.warning('请输入必做标题')
  if (saving.value) return
  saving.value = true
  try {
    // 新增模板会立即生成当天新行，下一次打开也按同一模板生成。
    await saveDailyTaskTemplate({ id: 0, title, content: addForm.content, remark: addForm.remark, sortNo: (items.value.length + 1) * 10, status: 0 })
    ElMessage.success('已新增')
    Object.assign(addForm, { title: '', content: '', remark: '' })
    adding.value = false
    await load()
  } finally { saving.value = false }
}
function startEdit(item) {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  editingId.value = item.id
  Object.assign(editForm, { title: item.title || '', content: item.content || '', remark: item.remark || '' })
}
function cancelEdit() {
  editingId.value = 0
}
async function saveEdit() {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  if (!editForm.title.trim()) return ElMessage.warning('请输入必做标题')
  saving.value = true
  try {
    await updateDailyTask(editingId.value, { id: editingId.value, ...editForm })
    ElMessage.success('已保存')
    editingId.value = 0
    load()
  } finally { saving.value = false }
}
async function removeItem(item) {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  try {
    await ElMessageBox.confirm(`删除「${item.title}」？当天记录会移除，明天也不再按此模板生成。`, '删除必做', { type: 'warning', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' })
  } catch (error) { return }
  await deleteDailyTask(item.id)
  ElMessage.success('已删除')
  load()
}
async function toggle(item) {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  const next = item.isCompleted !== 1
  if (!next) {
    try {
      await ElMessageBox.confirm(`将「${item.title}」反选为未完成？`, '反选确认', { type: 'warning', confirmButtonText: '反选' })
    } catch (error) { return }
  }
  await completeDailyTask(item.id, next)
  ElMessage.success(next ? '已完成' : '已恢复未完成')
  load()
}
async function move(index, offset) {
  if (!isToday.value) return ElMessage.warning('历史必做只读')
  const list = [...visibleItems.value]
  const target = index + offset
  if (target < 0 || target >= list.length) return
  ;[list[index], list[target]] = [list[target], list[index]]
  const moving = list[index]
  const anchor = list[target]
  const all = [...items.value]
  const from = all.findIndex((it) => it.id === moving.id)
  const to = all.findIndex((it) => it.id === anchor.id)
  if (from < 0 || to < 0) return
  all.splice(from, 1)
  all.splice(to, 0, moving)
  items.value = all
  await sortDailyTasks(all.map((it) => it.id))
  load()
}

function scheduleMidnightRefresh() {
  if (midnightTimer) clearTimeout(midnightTimer)
  const now = serverNow()
  const next = new Date(now)
  next.setHours(24, 0, 1, 0)
  midnightTimer = setTimeout(async () => {
    await syncServerTime(true)
    const nextToday = serverTodayText()
    const wasToday = taskDate.value === today.value
    today.value = nextToday
    if (wasToday) taskDate.value = nextToday
    adding.value = false
    await load()
    scheduleMidnightRefresh()
  }, Math.max(1000, next.getTime() - now.getTime()))
}

onMounted(async () => {
  await syncServerTime()
  today.value = serverTodayText()
  taskDate.value = today.value
  await load()
  scheduleMidnightRefresh()
})
onBeforeUnmount(() => {
  if (midnightTimer) clearTimeout(midnightTimer)
  midnightTimer = null
})
</script>

<style scoped>
.daily-panel { overflow: hidden; border: 1px solid #efe1e4; border-radius: 11px; background: #fff; }
.daily-summary { display: flex; align-items: center; gap: 18px; padding: 15px 18px; border-bottom: 1px solid #f3e8ea; background: linear-gradient(105deg, #fff7f8 0%, #fffdfd 62%); }
.daily-heading { display: flex; align-items: center; gap: 10px; min-width: 156px; }
.daily-mark { width: 34px; height: 34px; display: grid; place-items: center; border-radius: 9px; background: #fbe8ec; color: #b75566; font-size: 17px; }
.daily-heading h2 { margin: 0; color: #423337; font-size: 15px; font-weight: 650; }
.daily-heading p { margin: 3px 0 0; color: #a28c91; font-size: 11px; }
.date-switch { display: flex; align-items: center; gap: 2px; }
.date-switch :deep(.el-input__wrapper) { box-shadow: 0 0 0 1px #ecdde0 inset; border-radius: 6px; }
.date-switch :deep(.el-date-editor) { width: 138px; }
.daily-progress { flex: 1; min-width: 150px; max-width: 300px; margin-left: auto; }
.progress-number { display: flex; align-items: baseline; gap: 8px; }
.progress-number b { color: #a8495b; font-size: 21px; font-weight: 680; font-variant-numeric: tabular-nums; }
.progress-number span { color: #9c878c; font-size: 11px; }
.daily-progress i { display: block; height: 5px; margin-top: 7px; overflow: hidden; border-radius: 99px; background: #f2e4e7; }
.daily-progress em { display: block; height: 100%; border-radius: inherit; background: linear-gradient(90deg, #dda3ae, #c16b7c); transition: width .24s; }
.add-button { flex: none; height: 32px; border-radius: 6px; }
.add-row { padding: 10px 4px; background: #fffafa; border-bottom: 1px solid #f4e7ea; }
.daily-check.ghost { border-color: #eddadd; background: #fdf5f6; color: #c18491; cursor: default; }
.add-edit-row { padding-right: 2px; }
.daily-edit-row :deep(.el-input__wrapper) { border-radius: 6px; box-shadow: 0 0 0 1px #eadde0 inset; }
.daily-toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 8px 16px; border-bottom: 1px solid #f4ebed; background: #fffdfd; }
.filter-tabs { display: flex; gap: 3px; padding: 2px; border-radius: 6px; background: #f8f1f2; }
.filter-tabs button { display: inline-flex; align-items: center; gap: 5px; height: 26px; padding: 0 10px; border: 0; border-radius: 5px; background: transparent; color: #917b80; font-size: 11.5px; cursor: pointer; }
.filter-tabs button b { color: #ae8a91; font-size: 10px; font-weight: 600; }
.filter-tabs button.active { background: #fff; color: #a14455; box-shadow: 0 1px 4px rgba(94, 60, 68, .08); font-weight: 600; }
.server-hint { color: #ae989d; font-size: 10.5px; }
.daily-list { padding: 4px 14px 10px; }
.daily-row { display: flex; align-items: center; gap: 11px; padding: 11px 4px; border-bottom: 1px solid #f5edee; }
.daily-row:last-child { border-bottom: 0; }
.daily-row:hover { background: #fffbfb; }
.daily-row.editing { gap: 8px; background: #fffafa; }
.daily-row.done .daily-title { color: #9b8a8e; text-decoration: line-through; }
.daily-row:not(.done) { background: #fff8f9; }
.daily-row.done { background: #f3fbf6; }
.daily-row.done:hover { background: #ecf8f0; }
.daily-row:not(.done):hover { background: #fff3f5; }
.daily-check { flex: none; width: 19px; height: 19px; display: grid; place-items: center; padding: 0; border: 1px solid #dcc7cb; border-radius: 50%; background: #fff; color: #fff; font-size: 11px; cursor: pointer; transition: background .15s, border-color .15s; }
.daily-check:hover { border-color: #c57c8b; }
.daily-check.checked { border-color: #79a78c; background: #79a78c; }
.daily-check.readonly { cursor: default; opacity: .8; }
.daily-body { flex: 1; min-width: 0; }
.daily-title-line { display: flex; align-items: center; gap: 7px; flex-wrap: wrap; }
.daily-title { color: #4c3b3f; font-size: 14px; font-weight: 550; }
.daily-quantity { padding: 1px 6px; border-radius: 4px; background: #f9eaed; color: #9b5b68; font-size: 10.5px; }
.daily-remark { overflow: hidden; margin: 4px 0 0; color: #9f8a8f; font-size: 11px; line-height: 1.5; text-overflow: ellipsis; white-space: nowrap; }
.daily-remark.empty { color: #c3b1b5; }
.daily-edit-row { flex: 1; min-width: 0; display: grid; grid-template-columns: minmax(150px, 1fr) minmax(110px, .52fr) minmax(260px, 2fr); align-items: center; gap: 8px; }
.daily-edit-row :deep(.el-textarea__inner) { min-height: 32px !important; padding: 6px 11px; line-height: 1.5; font-size: 13px; }
.daily-edit-row :deep(.el-input__inner) { font-size: 13px; }
.saved-row :deep(.el-input.is-disabled .el-input__wrapper),
.saved-row :deep(.el-textarea.is-disabled .el-textarea__inner) { background: transparent; box-shadow: none; cursor: default; }
.saved-row :deep(.el-input.is-disabled .el-input__inner),
.saved-row :deep(.el-textarea.is-disabled .el-textarea__inner) { color: #59474d; -webkit-text-fill-color: #59474d; opacity: 1; cursor: default; }
.saved-row :deep(.el-input.is-disabled .el-input__wrapper) { padding-left: 2px; }
.saved-row :deep(.el-textarea.is-disabled .el-textarea__inner) { padding-left: 2px; }
.done-text { flex: none; color: #9aab9f; font-size: 10.5px; }
.daily-actions { flex: none; display: flex; opacity: .4; transition: opacity .15s; }
.daily-row:hover .daily-actions, .daily-actions.editing-actions { opacity: 1; }
.daily-actions :deep(.el-button) { width: 25px; height: 25px; margin-left: 0; }
.daily-actions :deep(.el-button.el-button--small) { width: auto; height: 26px; padding: 0 9px; }
.editing-actions :deep(.el-button) { width: auto; padding: 0 9px; }
.history-state { flex: none; font-size: 11px; }
.history-state.is-done { color: #4f8b68; }
.history-state.is-open { color: #b85c69; }
.daily-empty { min-height: 260px; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #9d888c; }
.daily-empty > span { width: 42px; height: 42px; display: grid; place-items: center; margin-bottom: 10px; border-radius: 10px; background: #fff0f2; color: #c17a89; font-size: 18px; }
.daily-empty h3 { margin: 0 0 5px; color: #5b474b; font-size: 13.5px; font-weight: 600; }
.daily-empty p { margin: 0; font-size: 11.5px; }
@media (max-width: 1180px) {
  .daily-summary { flex-wrap: wrap; gap: 10px; }
  .daily-progress { width: 100%; max-width: none; margin-left: 0; }
}
@media (max-width: 760px) {
  .daily-summary { align-items: flex-start; flex-direction: column; padding: 13px 12px; }
  .date-switch { flex-wrap: wrap; width: 100%; }
  .date-switch :deep(.el-date-editor) { width: 132px; }
  .daily-edit-row { grid-template-columns: 1fr; padding: 0; }
  .add-row { padding: 10px 0; }
  .add-edit-row { padding: 0; }
  .add-row .daily-actions { width: 100%; justify-content: flex-end; }
  .daily-toolbar { align-items: flex-start; flex-direction: column; padding: 8px 12px; }
  .daily-list { padding: 2px 10px 8px; }
  .daily-row { gap: 8px; padding: 10px 0; flex-wrap: wrap; }
  .daily-title { font-size: 12.5px; }
  .done-text, .server-hint { display: none; }
  .daily-actions { opacity: 1; }
}
</style>
