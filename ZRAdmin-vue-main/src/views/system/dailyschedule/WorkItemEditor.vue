<template>
  <el-dialog
    v-model="visible"
    :title="dialogTitle"
    width="760px"
    top="5vh"
    append-to-body
    destroy-on-close
    class="work-editor-dialog"
    @closed="handleClosed">
    <div class="editor-shell" v-loading="loading">
      <div v-if="isChild && parentContext" class="parent-strip">
        <span class="parent-mark"><el-icon><Connection /></el-icon></span>
        <div>
          <span>所属待办</span>
          <b>{{ parentContext.title }}</b>
        </div>
        <em v-if="parentHasDateRange">{{ dateRangeText }}</em>
      </div>

      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="editor-form">
        <section class="form-block">
          <div class="block-head">
            <b>{{ isChild ? '子目标信息' : '待办信息' }}</b>
            <span>{{ isChild ? '拆清楚要完成的结果' : '先把最重要的事情记下来' }}</span>
          </div>

          <el-form-item label="名称" prop="title">
            <el-input v-model="form.title" maxlength="200" show-word-limit :placeholder="isChild ? '例如：银饰销售额达到 3 万元' : '例如：本周客户回访 / 10 月银饰销售目标'" @keyup.enter="focusNext" />
          </el-form-item>

          <el-form-item label="优先级">
            <div class="priority-grid">
              <button v-for="item in priorityOptions" :key="item.value" type="button" class="priority-option" :class="[`q${item.value}`, { active: Number(form.priority) === item.value }]" @click="form.priority = item.value">
                <i></i><b>{{ item.label }}</b><span>{{ item.hint }}</span>
              </button>
            </div>
          </el-form-item>

          <div class="form-row">
            <el-form-item label="负责人">
              <el-select v-model="form.ownerUserId" clearable filterable style="width: 100%" placeholder="默认创建人" @change="handleOwnerChange">
                <el-option v-for="owner in owners" :key="owner.userId" :label="owner.nickName || owner.userName" :value="owner.userId" />
              </el-select>
            </el-form-item>
            <el-form-item v-if="isChild" label="计划完成日期">
              <el-date-picker v-model="form.finishDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" :disabled="!parentHasDateRange" :disabled-date="disableFinishDate" :placeholder="parentHasDateRange ? '默认父待办最后一天' : '父待办未设置日期'" />
            </el-form-item>
          </div>
        </section>

        <template v-if="!isChild">
          <section class="form-block">
            <div class="block-head">
              <b>时间与业绩</b>
              <span>普通记事可以不设日期，业绩目标必须设置区间</span>
            </div>
            <div class="switch-row">
              <div class="switch-copy"><b>设置时间区间</b><span>用于到期提醒、逾期识别和业绩统计</span></div>
              <el-switch v-model="form.hasDateRange" :disabled="form.isPerformanceRelated" @change="handleDateToggle" />
            </div>
            <div v-if="form.hasDateRange" class="form-row date-row">
              <el-form-item label="开始日期" prop="startDate">
                <el-date-picker v-model="form.startDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" :disabled-date="disableParentStart" />
              </el-form-item>
              <el-form-item label="结束日期" prop="endDate">
                <el-date-picker v-model="form.endDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" :disabled-date="disableParentEnd" />
              </el-form-item>
            </div>
            <div class="switch-row performance-row">
              <div class="switch-copy"><b>关联业绩</b><span>启用后，子目标必须选择商品品类并填写金额或件数</span></div>
              <el-switch v-model="form.isPerformanceRelated" @change="handlePerformanceToggle" />
            </div>
          </section>
        </template>

        <section v-if="isChild" class="form-block">
          <div class="block-head">
            <b>目标口径</b>
            <span>{{ parentIsPerformance ? '金额与件数二选一' : '普通子目标，完成后计入父待办进度' }}</span>
          </div>
          <template v-if="parentIsPerformance">
            <el-form-item label="款式分类（父级）" prop="categoryId">
              <el-select v-model="form.categoryId" filterable style="width: 100%" placeholder="请选择一个父级款式分类">
                <el-option v-for="item in categories" :key="item.id" :label="item.itemName" :value="item.id" />
              </el-select>
            </el-form-item>
            <div class="target-mode">
              <button type="button" :class="{ active: Number(form.targetMode) === 1 }" @click="setTargetMode(1)">按金额</button>
              <button type="button" :class="{ active: Number(form.targetMode) === 2 }" @click="setTargetMode(2)">按件数</button>
            </div>
            <div class="form-row target-value-row">
              <el-form-item label="目标金额（元）" :class="{ 'is-inactive': Number(form.targetMode) !== 1 }">
                <el-input-number v-model="form.amountValue" :min="0" :precision="2" :step="100" :disabled="Number(form.targetMode) !== 1" controls-position="right" style="width: 100%" placeholder="请输入目标金额" />
              </el-form-item>
              <el-form-item label="目标件数" :class="{ 'is-inactive': Number(form.targetMode) !== 2 }">
                <el-input-number v-model="form.qtyValue" :min="0" :precision="0" :step="1" :disabled="Number(form.targetMode) !== 2" controls-position="right" style="width: 100%" placeholder="请输入整数件数" />
              </el-form-item>
            </div>
            <p class="target-mode-tip">金额和件数按当前口径二选一，另一个输入框置灰保留，切换口径不会清空已填内容。</p>
          </template>
          <div v-else class="plain-child-note">
            <el-icon><Aim /></el-icon>
            <span>该子目标用于拆解执行动作，不参与商品品类业绩统计。完成全部子目标后，父待办自动完成。</span>
          </div>
        </section>

        <section class="form-block compact-block">
          <div class="block-head"><b>备注</b><span>可不填</span></div>
          <el-form-item label="" prop="remark">
            <el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="补充判断依据、沟通结果或交付标准" />
          </el-form-item>
          <div v-if="canSaveTemplate" class="template-save">
            <el-checkbox v-model="form.saveAsTemplate">保存为可复用模板</el-checkbox>
            <el-input v-if="form.saveAsTemplate" v-model="form.templateName" maxlength="100" placeholder="模板名称，默认使用当前名称" />
          </div>
        </section>
      </el-form>
    </div>

    <template #footer>
      <div class="editor-footer">
        <span>日期按服务器时间口径</span>
        <div>
          <el-button @click="visible = false">取消</el-button>
          <el-button type="primary" :loading="submitting" :disabled="submitting" @click="submit">{{ isChild ? '保存子目标' : '保存待办' }}</el-button>
        </div>
      </div>
    </template>
  </el-dialog>
</template>

<script setup name="WorkItemEditor">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { Aim, Connection } from '@element-plus/icons-vue'
import { getWorkItem, saveWorkItem } from '@/api/jingyue/workitem'
import { formatServerDate, parseServerDateTime, serverNow } from '@/utils/serverTime'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  itemId: { type: [Number, String], default: 0 },
  parentId: { type: [Number, String], default: 0 },
  initialTitle: { type: String, default: '' },
  options: { type: Object, default: () => ({}) }
})
const emit = defineEmits(['update:modelValue', 'success'])

const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})
const formRef = ref()
const loading = ref(false)
const submitting = ref(false)
const parentContext = ref(null)

const priorityOptions = [
  { value: 4, label: '重要且紧急', hint: '立即处理' },
  { value: 3, label: '重要不紧急', hint: '安排推进' },
  { value: 2, label: '不重要但紧急', hint: '尽快处理' },
  { value: 1, label: '不重要不紧急', hint: '有空再做' }
]
const owners = computed(() => props.options?.owners || [])
const categories = computed(() => props.options?.categories || [])
const isChild = computed(() => Number(form.parentId || 0) > 0)
const parentHasDateRange = computed(() => Number(parentContext.value?.hasDateRange || 0) === 1)
const parentIsPerformance = computed(() => Number(parentContext.value?.isPerformanceRelated || 0) === 1)
const canSaveTemplate = computed(() => !isChild.value)
const dialogTitle = computed(() => Number(form.id) > 0 ? (isChild.value ? '编辑子目标' : '编辑待办') : (isChild.value ? '添加子目标' : '新建待办'))
const dateRangeText = computed(() => {
  if (!parentContext.value) return ''
  return `${normalizeDate(parentContext.value.startDate)} 至 ${normalizeDate(parentContext.value.endDate)}`
})

const rules = {
  title: [{ required: true, message: '请输入名称', trigger: 'blur' }],
  startDate: [{ validator: (_, value, callback) => !form.hasDateRange || value ? callback() : callback(new Error('请选择开始日期')), trigger: 'change' }],
  endDate: [{ validator: (_, value, callback) => !form.hasDateRange || value ? callback() : callback(new Error('请选择结束日期')), trigger: 'change' }],
  categoryId: [{ validator: (_, value, callback) => !isChild.value || !parentIsPerformance.value || Number(value) > 0 ? callback() : callback(new Error('请选择款式分类')), trigger: 'change' }]
}

function pad(value) { return String(value).padStart(2, '0') }
function monthEnd(value) {
  const date = parseServerDateTime(value) || serverNow()
  return formatServerDate(new Date(date.getFullYear(), date.getMonth() + 1, 0))
}
function emptyForm() {
  const today = formatServerDate(serverNow())
  return {
    id: 0,
    parentId: null,
    title: '',
    itemKind: 1,
    periodType: 3,
    startDate: today,
    endDate: monthEnd(today),
    finishDate: '',
    dueTime: '',
    priority: 4,
    ownerUserId: undefined,
    ownerName: '',
    hasDateRange: false,
    isPerformanceRelated: false,
    targetMode: 1,
    amountValue: 0,
    qtyValue: 0,
    categoryId: undefined,
    progressMode: 1,
    remark: '',
    saveAsTemplate: false,
    templateName: ''
  }
}
const form = reactive(emptyForm())

function normalizeDate(value) {
  if (!value) return ''
  const date = parseServerDateTime(value)
  if (!date) return String(value).slice(0, 10).replace('T', ' ')
  return formatServerDate(date)
}
function reset() {
  Object.assign(form, emptyForm())
  form.parentId = Number(props.parentId || 0) || null
  form.title = props.initialTitle || ''
  formRef.value?.clearValidate()
  if (isChild.value && parentContext.value) {
    form.itemKind = 1
    form.priority = 4
    form.hasDateRange = parentHasDateRange.value
    form.isPerformanceRelated = parentIsPerformance.value
    if (parentHasDateRange.value) {
      form.startDate = normalizeDate(parentContext.value.startDate)
      form.endDate = normalizeDate(parentContext.value.endDate)
      form.finishDate = form.endDate
    }
  }
}
async function loadParent() {
  parentContext.value = null
  if (!Number(props.parentId || 0)) return
  const res = await getWorkItem(props.parentId)
  parentContext.value = res.data || null
}
async function loadDetail() {
  if (Number(props.itemId || 0) <= 0) return
  loading.value = true
  try {
    const res = await getWorkItem(props.itemId)
    const data = res.data || {}
    if (data.parentId) {
      const parentRes = await getWorkItem(data.parentId)
      parentContext.value = parentRes.data || null
    }
    Object.assign(form, {
      id: Number(data.id),
      parentId: data.parentId || null,
      title: data.title || '',
      itemKind: Number(data.itemKind || 1),
      periodType: Number(data.periodType || 3),
      startDate: normalizeDate(data.startDate),
      endDate: normalizeDate(data.endDate),
      finishDate: normalizeDate(data.finishDate),
      dueTime: data.dueTime ? String(data.dueTime).replace('T', ' ').slice(0, 19) : '',
      priority: Number(data.priority || 4),
      ownerUserId: data.ownerUserId || undefined,
      ownerName: data.ownerName || '',
      hasDateRange: Number(data.hasDateRange || 0) === 1,
      isPerformanceRelated: Number(data.isPerformanceRelated || 0) === 1,
      targetMode: Number(data.targetMode || 1),
      amountValue: Number(data.targetMode || 1) === 1 ? Number(data.targetValue || 0) : 0,
      qtyValue: Number(data.targetMode || 1) === 2 ? Number(data.targetValue || 0) : 0,
      categoryId: data.categoryId || undefined,
      progressMode: Number(data.progressMode || 1),
      remark: data.remark || '',
      saveAsTemplate: false,
      templateName: ''
    })
  } finally {
    loading.value = false
  }
}
function handleOwnerChange(value) {
  const owner = owners.value.find((item) => Number(item.userId) === Number(value))
  form.ownerName = owner ? (owner.nickName || owner.userName) : ''
}
function handleDateToggle(value) {
  if (value) {
    if (!form.startDate) form.startDate = formatServerDate(serverNow())
    if (!form.endDate || form.endDate < form.startDate) form.endDate = monthEnd(form.startDate)
  }
}
function handlePerformanceToggle(value) {
  if (value) {
    form.hasDateRange = true
    if (!form.startDate) form.startDate = formatServerDate(serverNow())
    if (!form.endDate || form.endDate < form.startDate) form.endDate = monthEnd(form.startDate)
  } else {
    form.targetMode = 1
  }
}
function setTargetMode(mode) {
  // 两个口径各自保留输入值，切换时只改变当前生效口径。
  form.targetMode = Number(mode)
}
function disableParentStart(value) { return form.endDate && formatServerDate(value) > form.endDate }
function disableParentEnd(value) { return form.startDate && formatServerDate(value) < form.startDate }
function disableFinishDate(value) {
  if (!parentHasDateRange.value) return true
  const day = formatServerDate(value)
  return day < normalizeDate(parentContext.value?.startDate) || day > normalizeDate(parentContext.value?.endDate)
}
function focusNext() {
  formRef.value?.validateField?.('title')
}
function buildDto() {
  const child = isChild.value
  const hasDateRange = child ? parentHasDateRange.value : (form.isPerformanceRelated || form.hasDateRange)
  const start = hasDateRange ? form.startDate : (form.startDate || formatServerDate(serverNow()))
  const end = hasDateRange ? form.endDate : start
  return {
    id: Number(form.id || 0),
    parentId: form.parentId || null,
    title: form.title.trim(),
    itemKind: child ? 1 : Number(form.itemKind || 1),
    periodType: Number(form.periodType || 3),
    startDate: start,
    endDate: end,
    dueTime: null,
    finishDate: child && hasDateRange ? (form.finishDate || end) : null,
    priority: Number(form.priority || 4),
    ownerUserId: form.ownerUserId || null,
    ownerName: form.ownerName || null,
    progressMode: 1,
    metricCode: null,
    targetValue: child && parentIsPerformance.value
      ? Number(Number(form.targetMode) === 2 ? form.qtyValue : form.amountValue)
      : 0,
    currentValue: 0,
    currentAuxValue: 0,
    autoComplete: true,
    reminderTime: null,
    sortNo: 0,
    remark: form.remark || null,
    hasDateRange: hasDateRange ? 1 : 0,
    isPerformanceRelated: child ? (parentIsPerformance.value ? 1 : 0) : (form.isPerformanceRelated ? 1 : 0),
    targetMode: child && parentIsPerformance.value ? Number(form.targetMode || 1) : 1,
    categoryId: child && parentIsPerformance.value ? Number(form.categoryId) : null,
    scopes: [],
    saveAsTemplate: canSaveTemplate.value && !!form.saveAsTemplate,
    templateName: form.templateName || form.title
  }
}
async function submit() {
  if (submitting.value) return
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  if (!isChild.value && (form.isPerformanceRelated || form.hasDateRange) && form.endDate < form.startDate) {
    ElMessage.warning('结束日期不能早于开始日期')
    return
  }
  if (isChild.value && parentIsPerformance.value) {
    if (!Number(form.categoryId)) {
      ElMessage.warning('关联业绩的子目标必须选择款式分类')
      return
    }
    if (Number(form.targetMode) === 2) {
      if (!Number.isInteger(Number(form.qtyValue)) || Number(form.qtyValue) <= 0) {
        ElMessage.warning('目标件数必须输入大于 0 的整数')
        return
      }
    } else if (Number(form.amountValue) <= 0) {
      ElMessage.warning('目标金额必须大于 0')
      return
    }
  }
  if (isChild.value && parentHasDateRange.value && form.finishDate && (form.finishDate < form.startDate || form.finishDate > form.endDate)) {
    ElMessage.warning('子目标完成日期必须在父待办时间区间内')
    return
  }
  submitting.value = true
  try {
    await saveWorkItem(buildDto())
    ElMessage.success(Number(form.id) > 0 ? '已保存修改' : '已创建')
    emit('success')
  } catch (error) {
    ElMessage.error(error?.message || '保存失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}
function handleClosed() {
  parentContext.value = null
  Object.assign(form, emptyForm())
}
watch(visible, async (value) => {
  if (!value) return
  parentContext.value = null
  if (Number(props.parentId || 0) > 0) await loadParent()
  reset()
  if (Number(props.itemId || 0) > 0) await loadDetail()
})
</script>

<style scoped>
.work-editor-dialog :deep(.el-dialog) { border-radius: 14px; overflow: hidden; box-shadow: 0 24px 70px rgba(76, 48, 55, .16); }
.work-editor-dialog :deep(.el-dialog__header) { margin: 0; padding: 17px 22px 15px; border-bottom: 1px solid #efe3e5; background: #fffafb; }
.work-editor-dialog :deep(.el-dialog__title) { color: #352c2f; font-size: 18px; font-weight: 650; }
.work-editor-dialog :deep(.el-dialog__body) { padding: 0; background: #fcf9f9; }
.work-editor-dialog :deep(.el-dialog__footer) { padding: 12px 22px; border-top: 1px solid #efe3e5; background: #fff; }
.editor-shell { max-height: 72vh; overflow-y: auto; padding: 16px 20px 20px; }
.parent-strip { display: flex; align-items: center; gap: 10px; padding: 10px 12px; margin-bottom: 12px; border: 1px solid #efdce0; border-radius: 9px; background: #fff7f8; }
.parent-mark { width: 30px; height: 30px; display: grid; place-items: center; border-radius: 8px; background: #f8e4e8; color: #b85869; }
.parent-strip > div { min-width: 0; }
.parent-strip span { display: block; color: #a8888e; font-size: 12px; }
.parent-strip b { display: block; margin-top: 2px; color: #4b373b; font-size: 14px; font-weight: 600; }
.parent-strip em { margin-left: auto; color: #8e7479; font-size: 12px; font-style: normal; white-space: nowrap; }
.editor-form { display: flex; flex-direction: column; gap: 12px; }
.form-block { padding: 15px 16px 3px; border: 1px solid #eee1e3; border-radius: 11px; background: #fff; }
.form-block.compact-block { padding-bottom: 1px; }
.block-head { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; margin-bottom: 13px; }
.block-head b { color: #413236; font-size: 14px; font-weight: 650; }
.block-head span { color: #a18b8f; font-size: 12px; }
.editor-form :deep(.el-form-item) { margin-bottom: 13px; }
.editor-form :deep(.el-form-item__label) { padding-bottom: 5px; color: #6f5c61; font-size: 13px; line-height: 18px; }
.editor-form :deep(.el-input__wrapper), .editor-form :deep(.el-textarea__inner) { border-radius: 7px; box-shadow: 0 0 0 1px #e8dade inset; }
.editor-form :deep(.el-input__wrapper.is-focus), .editor-form :deep(.el-textarea__inner:focus) { box-shadow: 0 0 0 1px #d77b8b inset; }
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 0 16px; }
.priority-grid { width: 100%; display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
.priority-option { display: grid; grid-template-columns: 9px 1fr; grid-template-rows: auto auto; column-gap: 8px; align-items: center; padding: 9px 11px; border: 1px solid #eadde0; border-radius: 8px; background: #fff; text-align: left; cursor: pointer; transition: border-color .15s, background .15s, box-shadow .15s; }
.priority-option i { grid-row: 1 / 3; width: 8px; height: 8px; border-radius: 50%; background: #b7a6a9; }
.priority-option b { color: #59464a; font-size: 13px; font-weight: 600; }
.priority-option span { color: #a18c90; font-size: 11px; }
.priority-option.active { border-color: #d77b8b; background: #fff5f6; box-shadow: 0 0 0 2px rgba(215, 123, 139, .08); }
.priority-option.q4 i { background: #cf5267; }
.priority-option.q3 i { background: #d99a52; }
.priority-option.q2 i { background: #8d80bd; }
.priority-option.q1 i { background: #9ca7b1; }
.switch-row { display: flex; align-items: center; justify-content: space-between; gap: 18px; padding: 10px 12px; margin-bottom: 12px; border: 1px solid #f0e4e6; border-radius: 8px; background: #fdfafb; }
.switch-row.performance-row { background: #fff8f9; }
.switch-copy b { display: block; color: #5c464b; font-size: 13px; font-weight: 600; }
.switch-copy span { display: block; margin-top: 2px; color: #a08a8f; font-size: 12px; }
.date-row { margin-top: 2px; }
.target-mode { display: inline-flex; padding: 2px; margin-bottom: 12px; border-radius: 7px; background: #f5edef; }
.target-mode button { min-width: 78px; padding: 6px 13px; border: 0; border-radius: 6px; background: transparent; color: #8e777c; font-size: 13px; cursor: pointer; }
.target-mode button.active { background: #fff; color: #a44354; box-shadow: 0 1px 4px rgba(80, 50, 55, .1); font-weight: 600; }
.plain-child-note { display: flex; gap: 8px; padding: 11px 12px; margin-bottom: 13px; border-radius: 8px; background: #fbf8fa; color: #806f74; font-size: 12px; line-height: 1.65; }
.target-value-row { gap: 0 12px; }
.target-value-row .is-inactive :deep(.el-input__wrapper) { background: #f7f3f4; }
.target-value-row .is-inactive :deep(.el-input__inner) { color: #b8aaad; }
.target-mode-tip { margin: -2px 0 12px; color: #9f898e; font-size: 11.5px; line-height: 1.6; }
.plain-child-note .el-icon { flex: none; margin-top: 2px; color: #bc7181; }
.template-save { display: flex; align-items: center; gap: 12px; padding: 8px 3px 12px; }
.template-save .el-input { max-width: 320px; }
.editor-footer { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.editor-footer > span { color: #a28d91; font-size: 12px; }
@media (max-width: 760px) {
  .work-editor-dialog :deep(.el-dialog) { width: 94vw !important; }
  .editor-shell { padding: 12px; }
  .priority-grid, .form-row { grid-template-columns: 1fr; }
  .block-head { flex-direction: column; gap: 2px; }
  .parent-strip em { display: none; }
  .editor-footer > span { display: none; }
  .template-save { align-items: flex-start; flex-direction: column; }
}
</style>