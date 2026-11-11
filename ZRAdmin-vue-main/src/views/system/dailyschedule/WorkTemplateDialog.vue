<template>
  <el-dialog v-model="visible" title="目标模板" width="900px" top="6vh" append-to-body destroy-on-close class="template-dialog" @closed="resetDialog">
    <div class="template-layout">
      <aside class="template-list">
        <div class="list-head">
          <div><b>可用模板</b><span>{{ templates.length }} 个</span></div>
          <el-button text circle :icon="Refresh" :loading="loading" @click="loadTemplates" />
        </div>
        <div class="template-scroll" v-loading="loading">
          <button v-for="template in templates" :key="template.id" type="button" class="template-item" :class="{ active: selectedId === template.id }" @click="selectTemplate(template)">
            <span class="template-icon"><el-icon><Files /></el-icon></span>
            <span class="template-copy">
              <b>{{ template.templateName }}</b>
              <small>{{ template.itemCount }} 个目标 · {{ formatDate(template.createTime) }}</small>
            </span>
            <el-icon class="template-arrow"><ArrowRight /></el-icon>
          </button>
          <el-empty v-if="!templates.length && !loading" description="还没有保存过目标模板" :image-size="68" />
        </div>
        <div class="list-tip"><el-icon><InfoFilled /></el-icon>新建事项时勾选“保存为模板”，即可沉淀成可复用模板。</div>
      </aside>

      <main class="template-detail" v-loading="detailLoading">
        <template v-if="selected">
          <header class="detail-head">
            <div>
              <span>模板预览</span>
              <h3>{{ selected.templateName }}</h3>
              <p>{{ selected.description || '暂无模板说明' }}</p>
            </div>
            <el-button v-hasPermi="['jingyue:workitem:template']" text type="danger" :icon="Delete" @click="removeTemplate">删除</el-button>
          </header>

          <div class="template-items">
            <div v-for="(item, index) in selected.items" :key="item.id || index" class="template-row">
              <span class="row-index">{{ String(index + 1).padStart(2, '0') }}</span>
              <div class="row-main">
                <div class="row-title"><b>{{ item.title }}</b><el-tag size="small" effect="plain">{{ kindName(item.itemKind) }}</el-tag></div>
                <div class="row-meta"><span>{{ metricName(item.metricCode) || '手工维护' }}</span><span v-if="item.categoryName">{{ item.categoryName }}</span><span v-if="item.targetValue > 0">目标 {{ number(item.targetValue) }} {{ Number(item.targetMode) === 2 ? '件' : '元' }}</span><span>{{ periodName(item.periodType) }}</span></div>
              </div>
              <el-tag :type="priorityTagType(item.priority)" size="small" effect="light">{{ priorityName(item.priority) }}</el-tag>
            </div>
          </div>

          <section class="apply-panel">
            <div class="apply-head"><b>应用为新目标组</b><span>生成后仍可逐项修改</span></div>
            <el-form :model="applyForm" label-position="top" class="apply-form">
              <el-form-item label="目标组名称" class="apply-title">
                <el-input v-model="applyForm.title" maxlength="200" :placeholder="selected.templateName" />
              </el-form-item>
              <el-form-item label="周期">
                <el-select v-model="applyForm.periodType" style="width: 100%" @change="handlePeriodChange">
                  <el-option v-for="item in periodOptions" :key="item.value" :label="item.label" :value="item.value" />
                </el-select>
              </el-form-item>
              <el-form-item label="开始日期">
                <el-date-picker v-model="applyForm.startDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" @change="handleStartChange" />
              </el-form-item>
              <el-form-item label="结束日期">
                <el-date-picker v-model="applyForm.endDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
              </el-form-item>
              <el-form-item label="优先级覆盖">
                <el-select v-model="applyForm.priority" clearable style="width: 100%" placeholder="沿用模板优先级">
                  <el-option label="重要且紧急" :value="4" /><el-option label="重要不紧急" :value="3" /><el-option label="不重要但紧急" :value="2" /><el-option label="不重要不紧急" :value="1" />
                </el-select>
              </el-form-item>
              <el-form-item label="负责人覆盖">
                <el-select v-model="applyForm.ownerUserId" clearable filterable style="width: 100%" placeholder="不指定">
                  <el-option v-for="owner in owners" :key="owner.userId" :label="owner.nickName || owner.userName" :value="owner.userId" />
                </el-select>
              </el-form-item>
            </el-form>
            <div class="apply-footer">
              <span>日期按服务器时间口径。</span>
              <el-button v-hasPermi="['jingyue:workitem:add']" type="primary" :loading="applying" @click="applyTemplate">应用模板</el-button>
            </div>
          </section>
        </template>
        <div v-else class="detail-empty">
          <span class="empty-icon"><el-icon><CopyDocument /></el-icon></span>
          <h3>选择一个目标模板</h3>
          <p>预览目标内容，设置周期与负责人后快速生成一整套目标。</p>
        </div>
      </main>
    </div>
  </el-dialog>
</template>

<script setup name="WorkTemplateDialog">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowRight, CopyDocument, Delete, Files, InfoFilled, Refresh } from '@element-plus/icons-vue'
import { applyWorkTemplate, deleteWorkTemplate, getWorkTemplate, listWorkTemplates } from '@/api/jingyue/workitem'
import { formatServerDate, parseServerDateTime, serverNow } from '@/utils/serverTime'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  options: { type: Object, default: () => ({}) }
})
const emit = defineEmits(['update:modelValue', 'applied'])
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})
const loading = ref(false)
const detailLoading = ref(false)
const applying = ref(false)
const templates = ref([])
const selectedId = ref(0)
const selected = ref(null)
const applyForm = reactive({ title: '', periodType: 3, startDate: '', endDate: '', priority: undefined, ownerUserId: undefined, ownerName: '' })
const owners = computed(() => props.options?.owners || [])
const periodOptions = [
  { value: 1, label: '日' }, { value: 2, label: '周' }, { value: 3, label: '月' },
  { value: 4, label: '季度' }, { value: 5, label: '年' }, { value: 6, label: '自定义' }
]

function pad(value) { return String(value).padStart(2, '0') }
function formatDate(value) {
  const date = parseServerDateTime(value)
  return date ? `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}` : ''
}
function monthEnd(value) {
  const date = parseServerDateTime(value) || serverNow()
  return formatServerDate(new Date(date.getFullYear(), date.getMonth() + 1, 0))
}
function addDays(value, days) {
  const date = parseServerDateTime(value) || serverNow()
  date.setDate(date.getDate() + days)
  return formatServerDate(date)
}
function periodEnd(value, periodType) {
  const date = parseServerDateTime(value) || serverNow()
  if (Number(periodType) === 1) return formatServerDate(date)
  if (Number(periodType) === 2) return addDays(value, 6)
  if (Number(periodType) === 3) return monthEnd(value)
  if (Number(periodType) === 4) return formatServerDate(new Date(date.getFullYear(), Math.floor(date.getMonth() / 3) * 3 + 3, 0))
  if (Number(periodType) === 5) return `${date.getFullYear()}-12-31`
  return applyForm.endDate
}
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function kindName(value) { return ({ 1: '普通待办', 2: '客户跟进', 3: '品类销售', 4: '期间销售', 5: '自定义量化' })[value] || '待办' }
function metricName(value) {
  return ({ sales_net_amount: '销售净额', category_sales_amount: '品类销售净额', customer_follow_count: '客户跟进人数', customer_deal_count: '成交客户数', custom_value: '自定义量化' })[value] || ''
}
function priorityName(value) {
  return ({ 4: '重要且紧急', 3: '重要不紧急', 2: '不重要但紧急', 1: '不重要不紧急' })[Number(value)] || '未设置'
}
function priorityTagType(value) {
  return ({ 4: 'danger', 3: 'warning', 2: 'primary', 1: 'info' })[Number(value)] || 'info'
}
function periodName(value) { return ({ 1: '日', 2: '周', 3: '月', 4: '季度', 5: '年', 6: '自定义' })[value] || '月' }

async function loadTemplates() {
  loading.value = true
  try {
    const res = await listWorkTemplates({ pageNum: 1, pageSize: 100 })
    templates.value = res.data?.result || []
    if (!templates.value.length) { selectedId.value = 0; selected.value = null }
    else if (!templates.value.some((it) => it.id === selectedId.value)) await selectTemplate(templates.value[0])
  } finally { loading.value = false }
}
async function selectTemplate(row) {
  selectedId.value = row.id
  detailLoading.value = true
  try {
    const res = await getWorkTemplate(row.id)
    selected.value = res.data || null
    const today = formatServerDate(serverNow())
    Object.assign(applyForm, { title: selected.value?.templateName || '', periodType: 3, startDate: today, endDate: monthEnd(today), priority: undefined, ownerUserId: undefined, ownerName: '' })
  } finally { detailLoading.value = false }
}
function handlePeriodChange(value) { if (Number(value) !== 6) applyForm.endDate = periodEnd(applyForm.startDate, value) }
function handleStartChange(value) { if (value) applyForm.endDate = periodEnd(value, applyForm.periodType) }
function handleOwnerChange(value) {
  const owner = owners.value.find((item) => Number(item.userId) === Number(value))
  applyForm.ownerName = owner ? (owner.nickName || owner.userName) : ''
}
async function applyTemplate() {
  if (!selected.value) return
  if (!applyForm.startDate || !applyForm.endDate) { ElMessage.warning('请选择开始和结束日期'); return }
  if (applyForm.endDate < applyForm.startDate) { ElMessage.warning('结束日期不能早于开始日期'); return }
  handleOwnerChange(applyForm.ownerUserId)
  applying.value = true
  try {
    const res = await applyWorkTemplate(selected.value.id, {
      title: applyForm.title || null,
      startDate: applyForm.startDate,
      endDate: applyForm.endDate,
      periodType: Number(applyForm.periodType || 3),
      priority: applyForm.priority || null,
      ownerUserId: applyForm.ownerUserId || null,
      ownerName: applyForm.ownerName || null,
      hasDateRange: 1,
      isPerformanceRelated: selected.value.items?.some((it) => it.categoryId || it.metricCode) ? 1 : 0
    })
    const count = res.data?.itemCount || selected.value.items?.length || 0
    ElMessage.success(`已应用模板，生成 ${count} 个子目标`)
    emit('applied', res.data)
  } finally { applying.value = false }
}
async function removeTemplate() {
  if (!selected.value) return
  try {
    await ElMessageBox.confirm(`确认删除模板「${selected.value.templateName}」？已应用的目标不受影响。`, '删除模板', { type: 'warning', confirmButtonText: '删除', confirmButtonClass: 'el-button--danger' })
  } catch (error) { return }
  await deleteWorkTemplate(selected.value.id)
  ElMessage.success('模板已删除')
  await loadTemplates()
}
function resetDialog() {
  templates.value = []
  selectedId.value = 0
  selected.value = null
}
watch(visible, (value) => { if (value) loadTemplates() })
</script>

<style scoped>
.template-dialog :deep(.el-dialog) { border-radius: 10px; overflow: hidden; box-shadow: 0 16px 48px rgba(66, 41, 46, .12); }
.template-dialog :deep(.el-dialog__header) { margin: 0; padding: 18px 22px; border-bottom: 1px solid #eee3e4; background: #fffafb; }
.template-dialog :deep(.el-dialog__title) { font-size: 18px; font-weight: 650; color: #3b3032; }
.template-dialog :deep(.el-dialog__body) { padding: 0; background: #fbf9f9; }
.template-layout { display: grid; grid-template-columns: 268px minmax(0, 1fr); min-height: 590px; max-height: 75vh; }
.template-list { display: flex; flex-direction: column; border-right: 1px solid #eee3e4; background: #fff; }
.list-head { display: flex; align-items: center; justify-content: space-between; padding: 16px 16px 11px; }
.list-head b { display: block; color: #554447; font-size: 14px; font-weight: 600; }
.list-head span { color: #a38b90; font-size: 12px; }
.template-scroll { flex: 1; min-height: 0; overflow-y: auto; padding: 2px 9px 10px; }
.template-item { display: flex; align-items: center; gap: 10px; width: 100%; padding: 11px 10px; margin-bottom: 5px; border: 1px solid transparent; border-radius: 8px; background: transparent; text-align: left; cursor: pointer; }
.template-item:hover { background: #fff7f8; }
.template-item.active { border-color: #e8cdd2; background: #fff2f4; }
.template-icon { flex: none; width: 32px; height: 32px; display: grid; place-items: center; border-radius: 7px; background: #f8e8eb; color: #b55c6d; font-size: 15px; }
.template-copy { flex: 1; min-width: 0; }
.template-copy b { display: block; overflow: hidden; color: #564346; font-size: 14px; font-weight: 550; text-overflow: ellipsis; white-space: nowrap; }
.template-copy small { display: block; margin-top: 4px; color: #9f888d; font-size: 12px; }
.template-arrow { color: #ceb3b8; font-size: 13px; }
.list-tip { display: flex; gap: 7px; padding: 11px 13px; margin: 0 9px 12px; border-radius: 7px; background: #fff8f2; color: #8c6d5b; font-size: 12px; line-height: 1.65; }
.list-tip .el-icon { flex: none; margin-top: 2px; }
.template-detail { min-width: 0; padding: 20px 22px; overflow-y: auto; }
.detail-head { display: flex; justify-content: space-between; gap: 14px; margin-bottom: 16px; }
.detail-head > div > span { color: #a78188; font-size: 11px; font-weight: 600; letter-spacing: 1px; }
.detail-head h3 { margin: 6px 0 5px; color: #3c3032; font-size: 20px; font-weight: 650; }
.detail-head p { margin: 0; color: #877277; font-size: 13px; line-height: 1.6; }
.template-items { display: flex; flex-direction: column; padding: 8px 13px; border: 1px solid #eee3e4; border-radius: 9px; background: #fff; }
.template-row { display: flex; align-items: center; gap: 11px; min-height: 58px; padding: 9px 5px; border-bottom: 1px solid #f4ecee; }
.template-row:last-child { border-bottom: 0; }
.row-index { flex: none; width: 22px; color: #b99da2; font-size: 11px; font-variant-numeric: tabular-nums; }
.row-main { flex: 1; min-width: 0; }
.row-title { display: flex; align-items: center; gap: 8px; }
.row-title b { overflow: hidden; color: #514043; font-size: 14px; font-weight: 550; text-overflow: ellipsis; white-space: nowrap; }
.row-meta { display: flex; flex-wrap: wrap; gap: 4px 12px; margin-top: 5px; color: #957f84; font-size: 12px; }
.apply-panel { margin-top: 14px; padding: 16px; border: 1px solid #ead9dc; border-radius: 9px; background: #fffafb; }
.apply-head { display: flex; align-items: baseline; gap: 9px; margin-bottom: 13px; }
.apply-head b { color: #514043; font-size: 14px; font-weight: 600; }
.apply-head span { color: #9d858a; font-size: 12px; }
.apply-form { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 0 14px; }
.apply-form .apply-title { grid-column: 1 / -1; }
.apply-form :deep(.el-form-item) { margin-bottom: 11px; }
.apply-form :deep(.el-form-item__label) { padding-bottom: 4px; color: #766166; font-size: 12px; line-height: 18px; }
.apply-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding-top: 5px; }
.apply-footer > span { color: #9c858a; font-size: 12px; }
.detail-empty { height: 100%; min-height: 480px; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #998489; }
.empty-icon { width: 56px; height: 56px; display: grid; place-items: center; margin-bottom: 13px; border-radius: 10px; background: #fbebee; color: #c67686; font-size: 25px; }
.detail-empty h3 { margin: 0 0 7px; color: #5e4b4f; font-size: 15px; }
.detail-empty p { margin: 0; font-size: 12px; }
@media (max-width: 800px) {
  .template-dialog :deep(.el-dialog) { width: 94vw !important; }
  .template-layout { grid-template-columns: 1fr; max-height: 78vh; }
  .template-list { max-height: 200px; border-right: 0; border-bottom: 1px solid #eee3e4; }
  .apply-form { grid-template-columns: 1fr; }
  .apply-form .apply-title { grid-column: auto; }
}
</style>