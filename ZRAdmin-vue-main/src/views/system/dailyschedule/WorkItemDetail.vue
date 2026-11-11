<template>
  <el-drawer v-model="visible" size="620px" :with-header="false" append-to-body destroy-on-close class="work-detail-drawer">
    <div class="detail-shell" v-loading="loading">
      <template v-if="item">
        <header class="detail-head">
          <div class="head-top">
            <span class="detail-kicker">{{ item.itemNo || 'WORK ITEM' }}</span>
            <el-button text circle :icon="Close" @click="visible = false" />
          </div>
          <div class="title-line">
            <span class="status-dot" :class="{ done: item.status === 1 }"></span>
            <h2>{{ item.title }}</h2>
          </div>
          <div class="tag-line">
            <el-tag :type="priorityTagType(item.priority)" effect="light" size="small">{{ item.priorityName }}</el-tag>
            <el-tag effect="plain" size="small">{{ item.itemKindName }}</el-tag>
            <span v-if="item.isOverdue" class="overdue-chip"><el-icon><Clock /></el-icon>已逾期</span>
            <span v-else-if="item.status === 1" class="done-chip"><el-icon><CircleCheck /></el-icon>已完成</span>
            <span v-else class="progress-chip">进行中</span>
          </div>
          <p v-if="item.remark" class="detail-remark">{{ item.remark }}</p>
        </header>

        <section class="progress-hero">
          <div class="hero-number"><strong>{{ progress(item) }}<em>%</em></strong><span>当前进度</span></div>
          <div class="hero-track"><i :style="{ width: progress(item) + '%' }"></i></div>
          <div class="hero-info">
            <span v-if="item.progressMode === 2">{{ metricSummary(item) }}</span>
            <span v-else-if="item.childCount">{{ item.doneChildCount }} / {{ item.childCount }} 个子目标已完成</span>
            <span v-else>{{ number(item.currentValue) }} / {{ number(item.targetValue) || '—' }}</span>
            <span v-if="item.status === 1">完成于 {{ formatTime(item.finishTime) }}</span>
            <span v-else-if="Number(item.hasDateRange) === 1">截止 {{ formatTime(item.dueTime) || formatDate(item.endDate) }}</span>
            <span v-else>未设置时间区间</span>
          </div>
        </section>

        <section class="detail-section facts-section">
          <h3>事项信息</h3>
          <div class="fact-grid">
            <div><span>负责人</span><b>{{ item.ownerName || '未指定' }}</b></div>
            <div><span>目标周期</span><b>{{ periodText(item) }}</b></div>
            <div><span>进度方式</span><b>{{ modeText(item.progressMode) }}</b></div>
            <div><span>创建时间</span><b>{{ formatTime(item.createTime) }}</b></div>
          </div>
          <div v-if="item.scopes?.length" class="scope-line">
            <el-icon><CollectionTag /></el-icon>
            <span v-for="scope in item.scopes" :key="scope.scopeType + '-' + scope.scopeId" class="scope-chip">{{ scope.snapshotName || scopeName(scope) }}</span>
          </div>
        </section>

        <section v-if="item.children?.length" class="detail-section">
          <div class="section-title"><h3>子目标</h3><span>{{ item.doneChildCount }}/{{ item.childCount }} 完成</span></div>
          <div class="child-stack">
            <button v-for="child in item.children" :key="child.id" type="button" class="detail-child" :class="{ done: child.status === 1, overdue: child.isOverdue }" @click="showChild(child)">
              <span class="child-state"><el-icon v-if="child.status === 1"><Check /></el-icon></span>
              <span class="child-main">
                <b>{{ child.title }}</b>
                <small>{{ childMeta(child) }}</small>
              </span>
              <span class="child-value">{{ progress(child) }}%</span>
            </button>
          </div>
        </section>

        <section v-if="canUpdateProgress" class="detail-section progress-edit-section">
          <div class="section-title"><h3>更新进度</h3><span>仅手工目标可修改</span></div>
          <div class="progress-form">
            <el-input-number v-model="progressForm.currentValue" :min="0" :precision="2" :controls="false" placeholder="当前值" />
            <el-input v-model="progressForm.remark" maxlength="500" placeholder="本次进展说明（可选）" />
            <el-button type="primary" :loading="savingProgress" @click="saveProgress">记录进度</el-button>
          </div>
        </section>

        <section class="detail-section timeline-section">
          <div class="section-title"><h3>进度流水</h3><span>最近 {{ logs.length }} 条</span></div>
          <div v-if="logs.length" class="timeline">
            <div v-for="log in logs" :key="log.id" class="timeline-row">
              <i></i>
              <div class="timeline-main">
                <div><b>{{ log.remark || sourceText(log.sourceType) }}</b><span>{{ numeric(log.changeValue) }}</span></div>
                <small>{{ formatTime(log.createTime) }}<template v-if="log.businessDate"> · 业务日期 {{ formatDate(log.businessDate) }}</template></small>
              </div>
            </div>
          </div>
          <el-empty v-else description="暂无进度流水" :image-size="54" />
        </section>

        <section v-if="item.itemKind === 2 && !item.childCount" class="detail-section follow-section">
          <div class="section-title"><h3>客户跟进</h3><span>{{ follows.length }} 条记录</span></div>
          <div v-if="follows.length" class="follow-list">
            <div v-for="follow in follows" :key="follow.id" class="follow-row">
              <div class="follow-avatar">{{ (follow.memberName || '客').slice(0, 1) }}</div>
              <div>
                <b>{{ follow.memberName || '客户' }} <small>{{ follow.phone }}</small></b>
                <p>{{ follow.content }}</p>
                <span>{{ formatTime(follow.followTime) }}<template v-if="follow.result"> · {{ follow.result }}</template></span>
              </div>
            </div>
          </div>
          <el-empty v-else description="还没有跟进记录" :image-size="54" />
        </section>

        <footer class="detail-actions">
          <el-button v-hasPermi="['jingyue:workitem:edit']" :icon="EditPen" @click="emit('edit', item)">编辑</el-button>
          <el-button v-if="!item.parentId" v-hasPermi="['jingyue:workitem:add']" :icon="Plus" @click="emit('add-child', item)">添加子目标</el-button>
          <el-button v-if="item.itemKind === 2 && !item.childCount" v-hasPermi="['jingyue:workitem:follow']" type="primary" :icon="ChatDotRound" @click="emit('follow', item)">记录客户跟进</el-button>
          <el-button v-else-if="item.progressMode === 2 && !item.childCount" v-hasPermi="['jingyue:workitem:edit']" :icon="Refresh" :loading="recalculating" @click="recalculate">按业务数据重算</el-button>
        </footer>
      </template>
      <el-empty v-else-if="!loading" description="事项不存在或已删除" />
    </div>
  </el-drawer>
</template>

<script setup name="WorkItemDetail">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { ChatDotRound, Check, CircleCheck, Clock, Close, CollectionTag, EditPen, Plus, Refresh } from '@element-plus/icons-vue'
import { getWorkItemEvidence, recalculateWorkItem, updateWorkItemProgress } from '@/api/jingyue/workitem'
import { parseServerDateTime } from '@/utils/serverTime'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  itemId: { type: [Number, String], default: 0 },
  options: { type: Object, default: () => ({}) }
})
const emit = defineEmits(['update:modelValue', 'edit', 'add-child', 'changed', 'follow'])
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})
const loading = ref(false)
const recalculating = ref(false)
const savingProgress = ref(false)
const item = ref(null)
const logs = ref([])
const follows = ref([])
const progressForm = reactive({ currentValue: 0, remark: '' })
const canUpdateProgress = computed(() => item.value?.progressMode === 1 && !item.value?.childCount)

function pad(value) { return String(value).padStart(2, '0') }
function formatDate(value) {
  const date = parseServerDateTime(value)
  if (!date) return ''
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}
function formatTime(value) {
  const date = parseServerDateTime(value)
  if (!date) return ''
  return `${formatDate(date)} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function numeric(value) {
  const n = Number(value || 0)
  return `${n > 0 ? '+' : ''}${number(n)}`
}
function progress(row) { return Math.max(0, Math.min(100, Math.round(Number(row?.progressPercent || 0)))) }
function priorityTagType(value) {
  return ({ 4: 'danger', 3: 'warning', 2: 'primary', 1: 'info' })[Number(value)] || 'info'
}
function periodText(row) {
  if (Number(row?.hasDateRange) !== 1) return '未设置时间区间'
  return `${formatDate(row.startDate)} 至 ${formatDate(row.endDate)}`
}
function modeText(mode) { return ({ 1: '手工维护', 2: '业务数据自动统计', 3: '子目标平均汇总' })[mode] || '手工维护' }
function sourceText(type) { return ({ manual: '手工更新进度', business: '业务数据自动重算' })[type] || '进度更新' }
function scopeName(scope) {
  if (scope.scopeType === 2) return props.options?.categories?.find((it) => Number(it.id) === Number(scope.scopeId))?.itemName || `分类 #${scope.scopeId}`
  return props.options?.members?.find((it) => Number(it.id) === Number(scope.scopeId))?.memberName || `客户 #${scope.scopeId}`
}
function metricSummary(row) {
  if (row.progressMode === 2) {
    if (row.metricCode === 'customer_follow_count') return `${number(row.currentValue)} 位客户 · ${number(row.currentAuxValue)} 次跟进`
    if (row.metricCode === 'customer_deal_count') return `${number(row.currentValue)} 位成交客户`
    if (row.metricCode === 'sales_net_amount' || row.metricCode === 'category_sales_amount') return `¥${number(row.currentValue)} / ¥${number(row.targetValue)}`
    return `${number(row.currentValue)} / ${number(row.targetValue)}`
  }
  if (Number(row.isPerformanceRelated) === 1) {
    return Number(row.targetMode) === 2
      ? `${number(row.currentValue)} / ${number(row.targetValue)} 件`
      : `¥${number(row.currentValue)} / ¥${number(row.targetValue)}`
  }
  return Number(row.targetValue) > 0
    ? `${number(row.currentValue)} / ${number(row.targetValue)}`
    : '手工进度'
}
function childMeta(row) {
  return [
    row.priorityName,
    row.categoryName,
    metricSummary(row),
    Number(row.hasDateRange) === 1 && row.targetFinishDate ? `计划 ${formatDate(row.targetFinishDate)}` : '',
    row.ownerName || '未分配'
  ].filter(Boolean).join(' · ')
}
function showChild(child) { item.value = child; loadEvidence(child.id) }
async function loadEvidence(id) {
  if (!id) return
  loading.value = true
  try {
    const res = await getWorkItemEvidence(id)
    item.value = res.data?.item || null
    logs.value = res.data?.logs || []
    follows.value = res.data?.follows || []
    progressForm.currentValue = Number(item.value?.currentValue || 0)
    progressForm.remark = ''
  } finally {
    loading.value = false
  }
}
async function saveProgress() {
  if (!item.value) return
  savingProgress.value = true
  try {
    await updateWorkItemProgress(item.value.id, { currentValue: Number(progressForm.currentValue || 0), currentAuxValue: Number(item.value.currentAuxValue || 0), remark: progressForm.remark || null })
    ElMessage.success('进度已记录')
    await loadEvidence(item.value.id)
    emit('changed')
  } finally {
    savingProgress.value = false
  }
}
async function recalculate() {
  if (!item.value) return
  recalculating.value = true
  try {
    await recalculateWorkItem(item.value.id)
    ElMessage.success('已按最新业务数据重算')
    await loadEvidence(item.value.id)
    emit('changed')
  } finally {
    recalculating.value = false
  }
}
watch(() => props.modelValue, (value) => {
  if (!value) return
  item.value = null
  logs.value = []
  follows.value = []
  loadEvidence(props.itemId)
})
</script>

<style scoped>
.work-detail-drawer :deep(.el-drawer__body) {
  display: flex;
  padding: 0;
  overflow: hidden;
  background: #fbf8f8;
}
.detail-shell {
  flex: 1;
  min-height: 100%;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}
.detail-head {
  flex: none;
  padding: 22px 24px 18px;
  border-bottom: 1px solid #eee3e5;
  background: #fffafb;
}
.head-top { display: flex; justify-content: space-between; align-items: center; }
.detail-kicker { color: #af8b92; font-size: 11px; font-weight: 650; letter-spacing: 1px; }
.title-line { display: flex; align-items: flex-start; gap: 9px; margin-top: 11px; }
.status-dot { flex: none; width: 8px; height: 8px; margin-top: 9px; border-radius: 50%; background: #d99355; box-shadow: 0 0 0 4px #fbefe3; }
.status-dot.done { background: #5f9c77; box-shadow: 0 0 0 4px #edf7f0; }
.title-line h2 { margin: 0; color: #342b2d; font-size: 21px; line-height: 1.45; font-weight: 650; }
.tag-line { display: flex; flex-wrap: wrap; align-items: center; gap: 7px; margin: 11px 0 0 17px; }
.overdue-chip, .done-chip, .progress-chip { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; }
.overdue-chip { color: #bd4456; }
.done-chip { color: #4e8c67; }
.progress-chip { color: #9b7350; }
.detail-remark { margin: 12px 0 0 17px; color: #736267; font-size: 13px; line-height: 1.7; white-space: pre-wrap; }
.progress-hero {
  flex: none;
  margin: 14px 20px 0;
  padding: 16px 18px;
  border: 1px solid #eddfe2;
  border-radius: 10px;
  background: #fff;
  box-shadow: 0 2px 8px rgba(73, 48, 52, .025);
}
.hero-number { display: flex; align-items: baseline; gap: 8px; }
.hero-number strong { color: #3b2f31; font-size: 30px; line-height: 1; font-weight: 650; letter-spacing: -.8px; }
.hero-number em { margin-left: 1px; color: #b55d6d; font-size: 13px; font-style: normal; }
.hero-number span { color: #9f878c; font-size: 12px; }
.hero-track { height: 5px; margin-top: 13px; overflow: hidden; border-radius: 99px; background: #f4e9eb; }
.hero-track i { display: block; height: 100%; border-radius: inherit; background: #d76778; transition: width .25s ease; }
.hero-info { display: flex; justify-content: space-between; gap: 12px; margin-top: 9px; color: #937d82; font-size: 12px; }
.detail-section {
  flex: none;
  margin: 12px 20px 0;
  padding: 16px 17px;
  border: 1px solid #eee3e5;
  border-radius: 10px;
  background: #fff;
}
.detail-section h3 { margin: 0; color: #413537; font-size: 14px; font-weight: 650; }
.section-title { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.section-title > span { color: #9b858a; font-size: 11px; }
.fact-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px 20px; }
.fact-grid div { min-width: 0; }
.fact-grid span { display: block; margin-bottom: 3px; color: #a58f93; font-size: 11px; }
.fact-grid b { display: block; overflow: hidden; color: #59494c; font-size: 13px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.scope-line { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; margin-top: 13px; padding-top: 12px; border-top: 1px solid #f4eaec; color: #a78f94; }
.scope-chip { padding: 3px 8px; border-radius: 5px; background: #fff3f5; color: #925d67; font-size: 11px; }
.child-stack { display: flex; flex-direction: column; gap: 7px; }
.detail-child {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 10px 11px;
  border: 1px solid #eee5e7;
  border-radius: 8px;
  background: #fdfbfb;
  text-align: left;
  cursor: pointer;
  transition: border-color .16s, background .16s;
}
.detail-child:hover { border-color: #e5c8cd; background: #fff8f8; }
.detail-child.overdue { border-left: 2px solid #d76778; }
.detail-child.done { opacity: .7; }
.child-state { flex: none; width: 17px; height: 17px; border: 1px solid #ddc7ca; border-radius: 50%; display: grid; place-items: center; color: #fff; font-size: 10px; }
.detail-child.done .child-state { border-color: #76a98a; background: #76a98a; }
.child-main { min-width: 0; flex: 1; }
.child-main b { display: block; overflow: hidden; color: #544346; font-size: 13px; font-weight: 550; text-overflow: ellipsis; white-space: nowrap; }
.child-main small { display: block; overflow: hidden; margin-top: 3px; color: #9b878b; font-size: 11px; text-overflow: ellipsis; white-space: nowrap; }
.child-main i { margin: 0 4px; font-style: normal; }
.child-value { flex: none; color: #9c5763; font-size: 13px; font-weight: 600; }
.progress-form { display: grid; grid-template-columns: 120px minmax(0, 1fr) auto; gap: 8px; }
.timeline { position: relative; padding-left: 3px; }
.timeline-row { display: flex; gap: 11px; position: relative; padding-bottom: 14px; }
.timeline-row:last-child { padding-bottom: 0; }
.timeline-row::before { content: ''; position: absolute; top: 8px; bottom: -2px; left: 4px; width: 1px; background: #eee1e3; }
.timeline-row:last-child::before { display: none; }
.timeline-row > i { z-index: 1; flex: none; width: 9px; height: 9px; margin-top: 4px; border: 2px solid #fff; border-radius: 50%; background: #d6818f; box-shadow: 0 0 0 1px #e9c8ce; }
.timeline-main { flex: 1; min-width: 0; }
.timeline-main > div { display: flex; justify-content: space-between; gap: 10px; }
.timeline-main b { overflow: hidden; color: #5f4d51; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.timeline-main span { flex: none; color: #b65b6b; font-size: 11px; }
.timeline-main small { display: block; margin-top: 3px; color: #a58f93; font-size: 11px; }
.follow-list { display: flex; flex-direction: column; gap: 13px; }
.follow-row { display: flex; gap: 10px; }
.follow-avatar { flex: none; width: 28px; height: 28px; display: grid; place-items: center; border-radius: 50%; background: #f7e5e9; color: #a24f5e; font-size: 12px; }
.follow-row > div:last-child { min-width: 0; }
.follow-row b { color: #59474a; font-size: 12px; font-weight: 550; }
.follow-row b small { margin-left: 5px; color: #a28c90; font-size: 11px; font-weight: 400; }
.follow-row p { margin: 4px 0; color: #726166; font-size: 12px; line-height: 1.6; }
.follow-row span { color: #a58f93; font-size: 11px; }
.detail-actions {
  position: sticky;
  bottom: 0;
  z-index: 5;
  width: 100%;
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  padding: 13px 20px;
  margin-top: 14px;
  border-top: 1px solid #eee3e5;
  background: rgba(255, 255, 255, .97);
  backdrop-filter: blur(8px);
  box-sizing: border-box;
}
@media (max-width: 700px) {
  .work-detail-drawer :deep(.el-drawer) { width: 100% !important; }
  .detail-head, .detail-section { padding-left: 16px; padding-right: 16px; }
  .progress-hero { margin-left: 14px; margin-right: 14px; }
  .detail-section { margin-left: 14px; margin-right: 14px; }
  .progress-form { grid-template-columns: 1fr; }
  .detail-actions { padding: 12px 14px; flex-wrap: wrap; }
  .hero-info { flex-direction: column; gap: 4px; }
}
</style>