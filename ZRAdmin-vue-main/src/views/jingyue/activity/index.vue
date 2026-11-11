<template>
  <div class="jy-activity">
    <header class="activity-hero">
      <div class="hero-copy">
        <span class="eyebrow">JEWELLERY CAMPAIGNS</span>
        <h1>营销雅集</h1>
        <p>把折扣、满减、赠礼与会员专享安排在有意义的时令里，让每一次活动都有节奏、有预算、有回响。</p>
        <div class="hero-actions">
          <el-button v-hasPermi="['jingyue:activity:add']" type="primary" @click="openEditor()"><el-icon><Plus /></el-icon>策划新活动</el-button>
          <el-button plain @click="loadAll"><el-icon><Refresh /></el-icon>刷新看板</el-button>
        </div>
      </div>
      <div class="hero-calendar">
        <span>{{ currentMonth }}</span>
        <strong>{{ dashboard.runningCount || 0 }}</strong>
        <small>场活动正在进行</small>
        <div class="calendar-lines"><i></i><i></i><i></i><i></i><i></i></div>
      </div>
      <i class="hero-glow glow-one"></i><i class="hero-glow glow-two"></i>
    </header>
    <section class="metric-board" v-loading="dashboardLoading">
      <article class="metric-feature">
        <div class="metric-head"><span>正在进行</span><el-icon><MagicStick /></el-icon></div>
        <strong>{{ dashboard.runningCount || 0 }}<em> 场</em></strong>
        <p>此刻正在影响柜台成交的活动</p>
        <div class="metric-foot"><span>即将开始 {{ dashboard.upcomingCount || 0 }} 场</span><b>实时</b></div>
      </article>
      <article>
        <span class="metric-label">本月优惠让利</span>
        <strong class="money">¥{{ money(dashboard.monthBenefit) }}</strong>
        <small>活动实际优惠金额合计</small>
        <div class="metric-icon rose"><el-icon><Discount /></el-icon></div>
      </article>
      <article>
        <span class="metric-label">本月参与人次</span>
        <strong>{{ number(dashboard.joinCount) }}<em> 次</em></strong>
        <small>会员活动核销记录</small>
        <div class="metric-icon gold"><el-icon><Tickets /></el-icon></div>
      </article>
      <article>
        <span class="metric-label">活动总数</span>
        <strong>{{ total }}<em> 场</em></strong>
        <small>当前筛选范围</small>
        <div class="metric-icon blush"><el-icon><Present /></el-icon></div>
      </article>
    </section>

    <section class="campaign-panel">
      <div class="panel-head">
        <div><span class="section-kicker">CAMPAIGN CANVAS</span><h2>活动编排</h2><p>按时间与状态查看，优先处理即将结束的活动</p></div>
        <div class="filters">
          <el-input v-model="query.keyword" clearable placeholder="搜索活动编号或名称" :prefix-icon="Search" @keyup.enter="search" />
          <el-select v-model="query.activityType" clearable placeholder="全部类型" @change="search"><el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select>
          <el-select v-model="query.status" clearable placeholder="全部状态" @change="search"><el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select>
          <el-button @click="search">查询</el-button>
        </div>
      </div>

      <div class="status-tabs">
        <button v-for="tab in statusTabs" :key="tab.key" type="button" :class="{ active: activeTab === tab.key }" @click="activeTab = tab.key">{{ tab.label }}<small>{{ tabCount(tab.key) }}</small></button>
      </div>

      <div v-loading="loading" class="campaign-grid">
        <template v-if="filteredRows.length">
          <article v-for="row in filteredRows" :key="row.id" class="campaign-card" :class="'status-' + row.status" @click="openDetail(row)">
            <div class="card-ribbon" :class="'type-' + row.activityType"><el-icon><component :is="typeIcon(row.activityType)" /></el-icon><span>{{ typeText(row.activityType) }}</span></div>
            <div class="card-head">
              <span class="campaign-no">{{ row.activityNo }}</span>
              <span class="status-chip" :class="'chip-' + row.status">{{ statusText(row.status) }}</span>
            </div>
            <h3>{{ row.activityName }}</h3>
            <p class="campaign-offer">{{ offerText(row) }}</p>
            <div class="campaign-period"><el-icon><Calendar /></el-icon><span>{{ shortDate(row.startTime) }} - {{ shortDate(row.endTime) }}</span><small>{{ periodText(row) }}</small></div>
            <div class="campaign-scope"><span>{{ row.scopeType === 1 ? '全场通用' : row.styleItemName || '指定分类' }}</span><span v-if="row.memberLevelName">{{ row.memberLevelName }}</span><span v-else>所有会员</span></div>
            <div class="campaign-progress">
              <div class="progress-label"><span>预算使用</span><b>{{ budgetPercent(row) }}%</b></div>
              <div class="progress-track"><i :style="{ width: budgetPercent(row) + '%' }"></i></div>
              <div class="progress-foot"><span>已让利 ¥{{ money(row.usedAmount) }}</span><span>{{ row.budgetAmount > 0 ? '预算 ¥' + money(row.budgetAmount) : '未设预算' }}</span></div>
            </div>
            <footer>
              <div class="join-info"><span>参与</span><b>{{ number(row.joinCount) }}</b><small v-if="row.joinLimit > 0">/ {{ row.joinLimit }} 次上限</small></div>
              <el-dropdown trigger="click" @click.stop>
                <button class="more-btn" type="button" @click.stop><el-icon><MoreFilled /></el-icon></button>
                <template #dropdown><el-dropdown-menu>
                  <el-dropdown-item v-hasPermi="['jingyue:activity:edit']" @click="openEditor(row)">编辑活动</el-dropdown-item>
                  <el-dropdown-item v-hasPermi="['jingyue:activity:status']" @click="toggleStatus(row)">{{ row.status === 1 ? '暂停活动' : '启用活动' }}</el-dropdown-item>
                  <el-dropdown-item v-hasPermi="['jingyue:activity:remove']" divided @click="removeRow(row)">删除活动</el-dropdown-item>
                </el-dropdown-menu></template>
              </el-dropdown>
            </footer>
          </article>
        </template>
        <div v-else class="empty-campaign">
          <div class="empty-icon"><el-icon><MagicStick /></el-icon></div>
          <h3>还没有安排活动</h3>
          <p>从一个清晰的优惠目标开始，给柜台一次值得期待的相遇。</p>
          <el-button v-hasPermi="['jingyue:activity:add']" type="primary" @click="openEditor()">策划第一场活动</el-button>
        </div>
      </div>
      <div class="list-foot"><span>活动优惠在销售确认时核销，预算与参与次数实时累计</span><pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" /></div>
    </section>
    <el-drawer v-model="detailOpen" size="540px" append-to-body :with-header="false">
      <div v-loading="detailLoading" class="detail-wrap">
        <template v-if="detail.id">
          <div class="drawer-heading"><span>CAMPAIGN BRIEF</span><b>活动详情</b><button type="button" @click="detailOpen = false"><el-icon><Close /></el-icon></button></div>
          <section class="detail-hero" :class="'type-' + detail.activityType">
            <div class="detail-icon"><el-icon><component :is="typeIcon(detail.activityType)" /></el-icon></div>
            <div><span>{{ typeText(detail.activityType) }} · {{ detail.activityNo }}</span><h2>{{ detail.activityName }}</h2><p>{{ statusText(detail.status) }} · {{ shortDate(detail.startTime) }} 至 {{ shortDate(detail.endTime) }}</p></div>
          </section>
          <div class="detail-offer">{{ offerText(detail) }}</div>
          <section class="detail-stats">
            <div><span>已让利</span><strong>¥{{ money(detail.usedAmount) }}</strong></div>
            <div><span>活动预算</span><strong>{{ detail.budgetAmount > 0 ? '¥' + money(detail.budgetAmount) : '不限' }}</strong></div>
            <div><span>参与次数</span><strong>{{ number(detail.joinCount) }}</strong></div>
          </section>
          <section class="detail-section">
            <div class="section-caption"><span>DETAILS</span><h3>活动规则</h3></div>
            <div class="info-grid">
              <div><span>适用范围</span><b>{{ detail.scopeType === 1 ? '全场通用' : detail.styleItemName || '指定款式分类' }}</b></div>
              <div><span>会员条件</span><b>{{ detail.memberLevelName || '所有会员' }}</b></div>
              <div><span>参与次数上限</span><b>{{ detail.joinLimit > 0 ? detail.joinLimit + ' 次 / 会员' : '不限制' }}</b></div>
              <div><span>创建时间</span><b>{{ dateTime(detail.createTime) }}</b></div>
            </div>
            <div v-if="detail.remark" class="soft-note"><span>活动备注</span><p>{{ detail.remark }}</p></div>
          </section>
          <div class="detail-actions">
            <el-button v-hasPermi="['jingyue:activity:edit']" plain @click="openEditor(detail)">编辑活动</el-button>
            <el-button v-hasPermi="['jingyue:activity:status']" type="primary" @click="toggleStatus(detail)">{{ detail.status === 1 ? '暂停活动' : '启用活动' }}</el-button>
          </div>
        </template>
      </div>
    </el-drawer>

    <el-drawer v-model="editorOpen" size="610px" append-to-body :with-header="false">
      <div class="editor-wrap">
        <div class="drawer-heading"><span>{{ form.id ? 'EDIT CAMPAIGN' : 'NEW CAMPAIGN' }}</span><b>{{ form.id ? '编辑营销活动' : '策划新活动' }}</b><button type="button" @click="editorOpen = false"><el-icon><Close /></el-icon></button></div>
        <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="activity-form">
          <div class="form-section"><div class="form-caption"><span>01</span><b>活动主题</b></div>
            <div class="form-grid two">
              <el-form-item label="活动名称" prop="activityName" class="full"><el-input v-model="form.activityName" maxlength="100" placeholder="如：仲夏银饰雅集" /></el-form-item>
              <el-form-item label="活动类型"><el-select v-model="form.activityType" @change="onTypeChange"><el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>
              <el-form-item label="活动状态"><el-select v-model="form.status"><el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>
              <el-form-item label="开始时间" prop="startTime"><el-date-picker v-model="form.startTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="选择开始时间" style="width: 100%" /></el-form-item>
              <el-form-item label="结束时间" prop="endTime"><el-date-picker v-model="form.endTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="选择结束时间" style="width: 100%" /></el-form-item>
            </div>
          </div>
          <div class="form-section"><div class="form-caption"><span>02</span><b>优惠内容</b></div>
            <div class="form-grid two">
              <el-form-item v-if="form.activityType === 1 || form.activityType === 5" label="折扣率"><el-input-number v-model="form.discountRate" :min="0.01" :max="1" :step="0.01" :precision="2" controls-position="right" style="width: 100%" /><p class="field-hint">0.95 表示 95 折，0.88 表示 88 折</p></el-form-item>
              <el-form-item v-if="form.activityType === 2" label="满减金额"><el-input-number v-model="form.reduceAmount" :min="0" :precision="2" controls-position="right" style="width: 100%" /><p class="field-hint">达到活动门槛后直接减免的金额</p></el-form-item>
              <el-form-item v-if="form.activityType === 3" label="赠品名称"><el-input v-model="form.giftName" maxlength="200" placeholder="如：檀木手串、银质书签" /></el-form-item>
              <el-form-item v-if="form.activityType === 4" label="积分倍数"><el-input-number v-model="form.pointsMultiple" :min="1" :step="0.5" :precision="1" controls-position="right" style="width: 100%" /></el-form-item>
              <el-form-item label="活动预算"><el-input-number v-model="form.budgetAmount" :min="0" :precision="2" controls-position="right" style="width: 100%" /><p class="field-hint">0 表示不限制预算，达到上限后销售端自动停止核销</p></el-form-item>
              <el-form-item label="每人参与上限"><el-input-number v-model="form.joinLimit" :min="0" :precision="0" controls-position="right" style="width: 100%" /><p class="field-hint">0 表示不限制参与次数</p></el-form-item>
            </div>
          </div>
          <div class="form-section"><div class="form-caption"><span>03</span><b>适用范围</b></div>
            <div class="form-grid two">
              <el-form-item label="活动范围"><el-radio-group v-model="form.scopeType" @change="onScopeChange"><el-radio :value="1">全场通用</el-radio><el-radio :value="2">指定款式分类</el-radio></el-radio-group></el-form-item>
              <el-form-item v-if="form.scopeType === 2" label="款式分类" prop="styleItemId"><el-select v-model="form.styleItemId" filterable placeholder="选择款式分类"><el-option v-for="item in categories" :key="item.id" :label="categoryLabel(item)" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="会员等级"><el-select v-model="form.memberLevelId" clearable placeholder="不限会员等级"><el-option v-for="level in levels" :key="level.id" :label="level.levelName" :value="level.id" /></el-select><p class="field-hint">会员专享活动必须选择等级</p></el-form-item>
            </div>
          </div>
          <div class="form-section"><div class="form-caption"><span>04</span><b>活动备注</b></div>
            <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="活动话术、陈列要求或内部提醒" /></el-form-item>
          </div>
        </el-form>
        <div class="editor-actions"><el-button @click="editorOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submit">保存活动</el-button></div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup name="JyActivity">
import { Search, Plus, Refresh, Calendar, MagicStick, Discount, Tickets, Present, MoreFilled, Close, Star } from '@element-plus/icons-vue'
import { listActivity, getActivity, getActivityOptions, getActivityDashboard, saveActivity, setActivityStatus, deleteActivity } from '@/api/jingyue/activity'
import { serverNow, parseServerDateTime } from '@/utils/serverTime'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const dashboardLoading = ref(false)
const detailLoading = ref(false)
const saving = ref(false)
const rows = ref([])
const total = ref(0)
const levels = ref([])
const categories = ref([])
const activeTab = ref('all')
const query = reactive({ pageNum: 1, pageSize: 12, keyword: '', activityType: undefined, status: undefined })
const dashboard = reactive({ runningCount: 0, upcomingCount: 0, monthBenefit: 0, joinCount: 0, running: [], upcoming: [] })
const detailOpen = ref(false)
const detail = reactive({})
const editorOpen = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const typeOptions = [{ value: 1, label: '折扣活动' }, { value: 2, label: '满减活动' }, { value: 3, label: '赠礼活动' }, { value: 4, label: '积分倍数' }, { value: 5, label: '会员专享' }]
const statusOptions = [{ value: 0, label: '草稿' }, { value: 1, label: '进行中' }, { value: 2, label: '已结束' }, { value: 3, label: '已停用' }]
const statusTabs = [{ key: 'all', label: '全部活动' }, { key: 'running', label: '进行中' }, { key: 'upcoming', label: '即将开始' }, { key: 'history', label: '历史活动' }]
const rules = {
  activityName: [{ required: true, message: '请输入活动名称', trigger: 'blur' }],
  startTime: [{ required: true, message: '请选择开始时间', trigger: 'change' }],
  endTime: [{ required: true, message: '请选择结束时间', trigger: 'change' }],
  styleItemId: [{ required: true, message: '请选择款式分类', trigger: 'change' }]
}
const currentMonth = computed(() => { const now = serverNow(); return now.getFullYear() + ' 年 ' + (now.getMonth() + 1) + ' 月' })
const filteredRows = computed(() => {
  if (activeTab.value === 'all') return rows.value
  const now = serverNow()
  if (activeTab.value === 'running') return rows.value.filter((it) => it.status === 1 && parseServerDateTime(it.startTime) <= now && parseServerDateTime(it.endTime) >= now)
  if (activeTab.value === 'upcoming') return rows.value.filter((it) => it.status === 1 && parseServerDateTime(it.startTime) > now)
  return rows.value.filter((it) => it.status === 2 || it.status === 3 || (it.status === 1 && parseServerDateTime(it.endTime) < now))
})
function emptyForm() {
  const start = serverNow(); start.setHours(start.getHours() + 1, 0, 0, 0)
  const end = new Date(start); end.setDate(end.getDate() + 7)
  return { id: 0, activityName: '', activityType: 1, startTime: formatDate(start), endTime: formatDate(end), scopeType: 1, styleItemId: undefined, discountRate: 0.95, reduceAmount: 0, giftName: '', pointsMultiple: 1, memberLevelId: undefined, joinLimit: 0, budgetAmount: 0, status: 0, remark: '' }
}
function formatDate(date) { const pad = (n) => String(n).padStart(2, '0'); return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) + ' ' + pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':00' }
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function typeText(value) { return typeOptions.find((it) => it.value === value)?.label || '营销活动' }
function typeIcon(value) { return value === 1 ? Discount : value === 2 ? Tickets : value === 3 ? Present : value === 4 ? Star : MagicStick }
function statusText(value) { return statusOptions.find((it) => it.value === value)?.label || '未知' }
function shortDate(value) { return value ? String(value).slice(5, 10).replace('-', '.') : '--' }
function dateTime(value) { return value ? String(value).replace('T', ' ').slice(0, 16) : '' }
function categoryLabel(item) { return item.parentId ? '　└ ' + item.itemName : item.itemName }
function offerText(row) {
  if (row.activityType === 1) return (Number(row.discountRate || 1) * 10).toFixed(1).replace(/\.0$/, '') + ' 折优惠'
  if (row.activityType === 2) return '满额立减 ¥' + money(row.reduceAmount)
  if (row.activityType === 3) return '赠礼 · ' + (row.giftName || '精选好礼')
  if (row.activityType === 4) return '积分 ' + Number(row.pointsMultiple || 1) + ' 倍'
  return '会员专享 ' + (Number(row.discountRate || 1) * 10).toFixed(1).replace(/\.0$/, '') + ' 折'
}
function budgetPercent(row) { if (!row.budgetAmount || row.budgetAmount <= 0) return 0; return Math.min(100, Math.round((Number(row.usedAmount || 0) / Number(row.budgetAmount)) * 100)) }
function periodText(row) { const now = serverNow(); const start = parseServerDateTime(row.startTime); const end = parseServerDateTime(row.endTime); if (row.status === 0) return '待发布'; if (start > now) return '未开始'; if (end < now) return '已结束'; const days = Math.ceil((end - now) / 86400000); return days <= 1 ? '今日结束' : '剩 ' + days + ' 天' }
function tabCount(key) { if (key === 'all') return total.value; const now = serverNow(); if (key === 'running') return rows.value.filter((it) => it.status === 1 && parseServerDateTime(it.startTime) <= now && parseServerDateTime(it.endTime) >= now).length; if (key === 'upcoming') return rows.value.filter((it) => it.status === 1 && parseServerDateTime(it.startTime) > now).length; return rows.value.filter((it) => it.status === 2 || it.status === 3).length }
function loadList() {
  loading.value = true
  listActivity(query).then((res) => { rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 }).finally(() => { loading.value = false })
}
function loadDashboard() {
  dashboardLoading.value = true
  getActivityDashboard().then((res) => Object.assign(dashboard, res.data || {})).finally(() => { dashboardLoading.value = false })
}
function loadOptions() {
  return getActivityOptions().then((res) => { levels.value = res.data?.levels || []; categories.value = res.data?.categories || [] })
}
function loadAll() { loadOptions(); loadDashboard(); loadList() }
function search() { query.pageNum = 1; loadList() }
function openDetail(row) {
  detailOpen.value = true; detailLoading.value = true
  getActivity(row.id).then((res) => { Object.keys(detail).forEach((key) => delete detail[key]); Object.assign(detail, res.data || {}) }).finally(() => { detailLoading.value = false })
}
function openEditor(row) {
  loadOptions().then(() => {
    if (row && row.id) {
      getActivity(row.id).then((res) => { fillForm(res.data); editorOpen.value = true })
    } else {
      Object.assign(form, emptyForm()); editorOpen.value = true
    }
  })
}
function fillForm(data) {
  Object.assign(form, emptyForm(), {
    id: data.id, activityName: data.activityName || '', activityType: data.activityType || 1,
    startTime: data.startTime ? String(data.startTime).replace('T', ' ').slice(0, 19) : '',
    endTime: data.endTime ? String(data.endTime).replace('T', ' ').slice(0, 19) : '',
    scopeType: data.scopeType || 1, styleItemId: data.styleItemId || undefined,
    discountRate: data.discountRate ?? 0.95, reduceAmount: data.reduceAmount || 0,
    giftName: data.giftName || '', pointsMultiple: data.pointsMultiple || 1,
    memberLevelId: data.memberLevelId || undefined, joinLimit: data.joinLimit || 0,
    budgetAmount: data.budgetAmount || 0, status: data.status ?? 0, remark: data.remark || ''
  })
}
function onTypeChange(value) {
  if (value === 1 || value === 5) form.discountRate = form.discountRate || 0.95
  if (value === 2) form.reduceAmount = form.reduceAmount || 0
  if (value === 4) form.pointsMultiple = form.pointsMultiple || 1
}
function onScopeChange(value) { if (value === 1) form.styleItemId = undefined }
function submit() {
  formRef.value?.validate((valid) => {
    if (!valid) return
    if (parseServerDateTime(form.startTime) >= parseServerDateTime(form.endTime)) return proxy.$modal.msgWarning('结束时间必须晚于开始时间')
    saving.value = true
    saveActivity({
      ...form, styleItemId: form.styleItemId || null, memberLevelId: form.memberLevelId || null,
      discountRate: form.activityType === 1 || form.activityType === 5 ? form.discountRate : null,
      reduceAmount: form.activityType === 2 ? form.reduceAmount : 0,
      giftName: form.activityType === 3 ? form.giftName : null,
      pointsMultiple: form.activityType === 4 ? form.pointsMultiple : 1
    }).then(() => { proxy.$modal.msgSuccess('活动已保存'); editorOpen.value = false; loadAll() }).finally(() => { saving.value = false })
  })
}
function toggleStatus(row) {
  const next = row.status === 1 ? 3 : 1
  const text = next === 1 ? '启用' : '暂停'
  proxy.$modal.confirm('确认' + text + '活动「' + row.activityName + '」？').then(() => setActivityStatus({ id: row.id, status: next })).then(() => {
    proxy.$modal.msgSuccess('活动已' + text); detailOpen.value = false; loadAll()
  }).catch(() => {})
}
function removeRow(row) {
  proxy.$modal.confirm('删除活动「' + row.activityName + '」？已有参与记录或优惠金额时系统会阻止删除。').then(() => deleteActivity(row.id)).then(() => {
    proxy.$modal.msgSuccess('已删除'); detailOpen.value = false; loadAll()
  }).catch(() => {})
}
onMounted(loadAll)
</script>
<style scoped lang="scss">
.jy-activity { color: #3e3035; }
.activity-hero { position: relative; display: flex; align-items: center; justify-content: space-between; min-height: 108px; padding: 15px 24px; overflow: hidden; background: linear-gradient(112deg, #fffafb 0%, #fdf3f5 58%, #f8e7eb 100%); border: 1px solid #f0dee3; border-radius: 16px; box-shadow: 0 7px 20px rgba(111,66,77,.05); }
.hero-copy { position: relative; z-index: 2; max-width: 650px; }.eyebrow { color: #b76e79; font-size:12px; font-weight: 700; letter-spacing: 3px; }.hero-copy h1 { margin: 4px 0 3px; color: #4a333a; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 23px; font-weight: 500; letter-spacing: 2px; }.hero-copy p { margin: 0; color: #92737d; font-size: 11px; line-height: 1.55; }.hero-actions { display: flex; gap: 8px; margin-top: 9px; }.hero-actions .el-button { margin: 0; border-radius: 8px; }
.hero-calendar { position: relative; z-index: 2; display: flex; flex-direction: column; align-items: center; justify-content: center; width: 132px; height: 88px; margin-right: 16px; background: rgba(255,255,255,.68); border: 1px solid #ead3d9; border-radius: 12px; box-shadow: 0 5px 15px rgba(126,81,90,.06); }.hero-calendar > span { color: #b0857d; font-size:11.5px; letter-spacing: 1.3px; }.hero-calendar > strong { margin: 3px 0 1px; color: #a9636e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 26px; font-weight: 400; line-height: 1; }.hero-calendar > small { color: #a68d93; font-size:11px; }.calendar-lines { display: flex; align-items: flex-end; gap: 4px; width: 76px; height: 17px; margin-top: 5px; }.calendar-lines i { display: block; width: 11px; background: #e5c4c7; border-radius: 3px 3px 0 0; }.calendar-lines i:nth-child(1) { height: 9px; }.calendar-lines i:nth-child(2) { height: 15px; }.calendar-lines i:nth-child(3) { height: 12px; background: #c68791; }.calendar-lines i:nth-child(4) { height: 20px; }.calendar-lines i:nth-child(5) { height: 16px; background: #d8aeb3; }
.hero-glow { position: absolute; border-radius: 50%; }.glow-one { top: -70px; right: 26%; width: 190px; height: 190px; background: rgba(255,255,255,.5); }.glow-two { right: 40px; bottom: -86px; width: 160px; height: 160px; background: rgba(213,168,171,.16); }
.metric-board { display: grid; grid-template-columns: 1.25fr 1fr 1fr 1fr; margin: 12px 0 14px; background: #fff; border: 1px solid #f2e4e6; border-radius: 16px; box-shadow: 0 10px 26px rgba(106,68,77,.06); }.metric-board article { position: relative; min-height: 130px; padding: 20px 23px; overflow: hidden; border-left: 1px solid #f5eaec; }.metric-board article:first-child { border-left: 0; }.metric-head { display: flex; align-items: center; justify-content: space-between; color: #a98f96; font-size:12px; }.metric-head .el-icon { color: #c39a83; font-size: 17px; }.metric-board strong { display: block; margin: 11px 0 4px; color: #533c43; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 27px; font-weight: 500; }.metric-board strong em { color: #a88c93; font-family: inherit; font-size: 11px; font-style: normal; }.metric-board p { margin: 0; color: #b09ba1; font-size:11.5px; }.metric-foot { display: flex; justify-content: space-between; margin-top: 13px; color: #a98790; font-size:11.5px; }.metric-foot b { color: #b76e79; font-weight: 500; }.metric-label { color: #a98f96; font-size:12px; }.metric-board article small { display: block; margin-top: 5px; color: #b4a0a5; font-size:11.5px; }.metric-icon { position: absolute; right: 20px; bottom: 17px; display: grid; place-items: center; width: 31px; height: 31px; border-radius: 50%; }.metric-icon .el-icon { font-size: 15px; }.metric-icon.rose { color: #b76e79; background: #fbf0f1; }.metric-icon.gold { color: #b59664; background: #fbf6eb; }.metric-icon.blush { color: #ba7f82; background: #fdf1ed; }
.campaign-panel { padding: 18px 20px 14px; background: #fff; border: 1px solid #f2e4e6; border-radius: 18px; box-shadow: 0 7px 22px rgba(106,68,77,.045); }.panel-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; padding: 0 3px 14px; }.section-kicker { color: #c0a080; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }.panel-head h2 { margin: 3px 0 2px; color: #523a41; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }.panel-head p { margin: 0; color: #b09ba1; font-size:11.5px; }.filters { display: flex; align-items: center; gap: 7px; }.filters .el-input { width: 190px; }.filters .el-select { width: 116px; }.filters .el-button { margin-left: 0; }
.status-tabs { display: flex; gap: 5px; padding: 0 3px 13px; border-bottom: 1px solid #f6ecee; }.status-tabs button { display: flex; align-items: center; gap: 5px; padding: 6px 11px; color: #9b858b; font-size:12px; background: transparent; border: 0; border-radius: 6px; cursor: pointer; }.status-tabs button:hover { color: #a7606b; background: #fdf7f7; }.status-tabs button.active { color: #965662; background: #fbefef; }.status-tabs small { color: #bca7ac; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; }.status-tabs button.active small { color: #b47c84; }

.campaign-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; min-height: 260px; padding: 16px 1px 4px; }
.campaign-card { position: relative; min-width: 0; padding: 19px 18px 14px; overflow: hidden; background: linear-gradient(160deg, #fff 0%, #fffafa 100%); border: 1px solid #f1e2e4; border-radius: 14px; cursor: pointer; transition: transform .22s ease, box-shadow .22s ease, border-color .22s ease; }
.campaign-card::after { position: absolute; top: 0; right: 0; width: 76px; height: 76px; content: ''; background: radial-gradient(circle at 100% 0, rgba(215,175,179,.17), transparent 68%); pointer-events: none; }
.campaign-card:hover { border-color: #e3c3c7; box-shadow: 0 15px 30px rgba(121,73,82,.11); transform: translateY(-3px); }
.campaign-card.status-3 { opacity: .76; }
.campaign-card.status-3:hover { opacity: 1; }
.card-ribbon { display: inline-flex; align-items: center; gap: 5px; height: 23px; padding: 0 9px; color: #a9636e; font-size:11.5px; background: #fbf0f1; border-radius: 5px; }
.card-ribbon .el-icon { font-size: 12px; }
.card-ribbon.type-2 { color: #9e7747; background: #fbf5e9; }
.card-ribbon.type-3 { color: #9b6d61; background: #faf0eb; }
.card-ribbon.type-4 { color: #85704f; background: #f8f4e8; }
.card-ribbon.type-5 { color: #a65f76; background: #f9edf2; }
.card-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-top: 12px; }
.campaign-no { overflow: hidden; color: #b29ca1; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; letter-spacing: .6px; text-overflow: ellipsis; white-space: nowrap; }
.status-chip { flex: 0 0 auto; padding: 3px 7px; font-size:11px; border-radius: 10px; }
.chip-0 { color: #9d8a8f; background: #f4eff0; }
.chip-1 { color: #5f8a71; background: #edf6f0; }
.chip-2 { color: #a98b69; background: #f8f2e8; }
.chip-3 { color: #aa8f95; background: #f5eff0; }
.campaign-card h3 { margin: 8px 0 5px; overflow: hidden; color: #4e373e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; letter-spacing: .5px; text-overflow: ellipsis; white-space: nowrap; }
.campaign-offer { min-height: 20px; margin: 0 0 13px; color: #b76e79; font-size: 12px; font-weight: 600; letter-spacing: .2px; }
.campaign-period { display: flex; align-items: center; gap: 5px; padding-bottom: 10px; color: #927a81; font-size:11.5px; border-bottom: 1px dashed #f0e1e3; }
.campaign-period .el-icon { color: #c29a8f; font-size: 12px; }
.campaign-period small { margin-left: auto; color: #b59a93; }
.campaign-scope { display: flex; gap: 5px; margin: 10px 0 12px; }
.campaign-scope span { max-width: 55%; overflow: hidden; padding: 3px 6px; color: #9f858c; font-size:11px; background: #fbf6f6; border-radius: 4px; text-overflow: ellipsis; white-space: nowrap; }
.campaign-progress { padding: 9px 10px 8px; background: #fdf9f8; border: 1px solid #f7ecec; border-radius: 7px; }
.progress-label, .progress-foot { display: flex; justify-content: space-between; color: #ad9399; font-size:11px; }
.progress-label b { color: #b16d78; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-weight: 500; }
.progress-track { height: 3px; margin: 6px 0; overflow: hidden; background: #f2e4e4; border-radius: 3px; }
.progress-track i { display: block; height: 100%; background: linear-gradient(90deg, #d7a5ab, #b76e79); border-radius: inherit; transition: width .4s ease; }
.campaign-card footer { display: flex; align-items: center; justify-content: space-between; margin-top: 11px; }
.join-info { display: flex; align-items: baseline; gap: 4px; color: #ae969b; font-size:11px; }
.join-info b { color: #5d434a; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.join-info small { color: #b6a2a6; font-size:11px; }
.more-btn { display: grid; place-items: center; width: 25px; height: 25px; padding: 0; color: #a88e94; background: #fff; border: 1px solid #f1e3e4; border-radius: 6px; cursor: pointer; }
.more-btn:hover { color: #a6606b; background: #fdf5f5; }
.empty-campaign { grid-column: 1 / -1; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 270px; color: #a99096; text-align: center; }
.empty-icon { display: grid; place-items: center; width: 52px; height: 52px; color: #c09191; background: #fcf1f0; border-radius: 50%; }
.empty-icon .el-icon { font-size: 23px; }
.empty-campaign h3 { margin: 13px 0 5px; color: #62464e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.empty-campaign p { margin: 0 0 16px; font-size:12px; }
.list-foot { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 12px 2px 2px; color: #b29ca1; font-size:11.5px; }

.detail-wrap, .editor-wrap { color: #4e373e; }
.drawer-heading { display: flex; align-items: center; gap: 10px; padding-bottom: 15px; border-bottom: 1px solid #f1e2e4; }
.drawer-heading > span { color: #c0a080; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }
.drawer-heading > b { color: #5a3f47; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 500; }
.drawer-heading > button { display: grid; place-items: center; width: 27px; height: 27px; margin-left: auto; padding: 0; color: #9c838a; background: #fff; border: 1px solid #f0e1e3; border-radius: 50%; cursor: pointer; }
.drawer-heading > button:hover { color: #a65e6a; background: #fdf5f5; }
.detail-hero { display: flex; align-items: center; gap: 14px; margin-top: 18px; padding: 18px; background: linear-gradient(120deg, #fdf4f3, #fffaf6); border: 1px solid #f3e2e1; border-radius: 12px; }
.detail-hero.type-2 { background: linear-gradient(120deg, #fbf6eb, #fffaf6); border-color: #f0e6d4; }
.detail-hero.type-3 { background: linear-gradient(120deg, #fcf1ec, #fffaf8); border-color: #f2e2da; }
.detail-hero.type-4 { background: linear-gradient(120deg, #f8f4e8, #fffaf5); border-color: #eee7d5; }
.detail-hero.type-5 { background: linear-gradient(120deg, #fbf0f4, #fffafb); border-color: #f1dfe6; }
.detail-icon { display: grid; flex: 0 0 auto; place-items: center; width: 47px; height: 47px; color: #b76e79; background: rgba(255,255,255,.75); border-radius: 50%; }
.detail-icon .el-icon { font-size: 22px; }
.detail-hero > div:last-child { min-width: 0; }
.detail-hero span { color: #aa8d92; font-size:11.5px; }
.detail-hero h2 { margin: 5px 0 4px; overflow: hidden; color: #513840; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.detail-hero p { margin: 0; color: #a4898f; font-size:11.5px; }
.detail-offer { margin: 12px 0; padding: 11px 14px; color: #a8616c; font-size: 13px; font-weight: 600; background: #fdf7f7; border-left: 2px solid #c98791; border-radius: 0 6px 6px 0; }
.detail-stats { display: grid; grid-template-columns: repeat(3, 1fr); margin-bottom: 22px; background: #fff; border: 1px solid #f1e3e4; border-radius: 10px; }
.detail-stats div { padding: 13px 14px; border-left: 1px solid #f5eaec; }
.detail-stats div:first-child { border-left: 0; }
.detail-stats span { display: block; margin-bottom: 4px; color: #af989e; font-size:11.5px; }
.detail-stats strong { color: #5d4148; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 500; }
.detail-section { padding-top: 2px; }
.section-caption { display: flex; align-items: baseline; gap: 8px; margin-bottom: 12px; }
.section-caption span { color: #c1a081; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 1.5px; }
.section-caption h3 { margin: 0; color: #5b4048; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 15px; font-weight: 500; }
.info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1px; overflow: hidden; background: #f3e6e8; border: 1px solid #f3e6e8; border-radius: 8px; }
.info-grid div { padding: 12px 13px; background: #fff; }
.info-grid span { display: block; margin-bottom: 4px; color: #ad969b; font-size:11px; }
.info-grid b { color: #674b53; font-size:12px; font-weight: 500; }
.soft-note { margin-top: 13px; padding: 12px 14px; background: #fdf8f6; border: 1px solid #f4e9e5; border-radius: 8px; }
.soft-note span { color: #b1917d; font-size:11.5px; }
.soft-note p { margin: 5px 0 0; color: #8e737a; font-size:12px; line-height: 1.8; white-space: pre-wrap; }
.detail-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 23px; padding-top: 14px; border-top: 1px solid #f1e3e4; }
.detail-actions .el-button { margin-left: 0; }

.activity-form { padding-top: 18px; }
.form-section { margin-bottom: 21px; }
.form-caption { display: flex; align-items: center; gap: 8px; margin-bottom: 13px; padding-bottom: 9px; border-bottom: 1px solid #f2e4e6; }
.form-caption span { display: grid; place-items: center; width: 20px; height: 20px; color: #b76e79; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; background: #fbf0f1; border-radius: 50%; }
.form-caption b { color: #5a4048; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 500; }
.form-grid { display: grid; gap: 0 14px; }
.form-grid.two { grid-template-columns: 1fr 1fr; }
.form-grid .full { grid-column: 1 / -1; }
.activity-form :deep(.el-form-item) { margin-bottom: 14px; }
.activity-form :deep(.el-form-item__label) { height: auto; padding-bottom: 4px; color: #967d84; font-size:11.5px; }
.activity-form :deep(.el-input__wrapper), .activity-form :deep(.el-textarea__inner), .activity-form :deep(.el-select__wrapper) { border-radius: 7px; box-shadow: 0 0 0 1px #efdfe2 inset; }
.activity-form :deep(.el-input__wrapper.is-focus), .activity-form :deep(.el-select__wrapper.is-focused) { box-shadow: 0 0 0 1px #c17a86 inset; }
.activity-form :deep(.el-radio-group) { gap: 12px; }
.field-hint { margin: 3px 0 0; color: #b29ca1; font-size:11px; line-height: 1.5; }
.editor-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 14px; border-top: 1px solid #f2e4e6; }
.editor-actions .el-button { margin-left: 0; }
@media (max-width: 1400px) { .campaign-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } .panel-head { align-items: flex-start; flex-direction: column; } .filters { width: 100%; } .filters .el-input { flex: 1; width: auto; } }
@media (max-width: 900px) { .activity-hero { align-items: flex-start; flex-direction: column; padding: 24px; } .hero-calendar { display: none; } .metric-board { grid-template-columns: 1fr 1fr; margin: -12px 8px 14px; } .metric-board article { min-height: 115px; padding: 16px; } .metric-board article:nth-child(3) { border-top: 1px solid #f5eaec; border-left: 0; } .metric-board article:nth-child(4) { border-top: 1px solid #f5eaec; } .campaign-panel { padding: 14px 12px 10px; } .filters { flex-wrap: wrap; } .filters .el-input { flex: 1 1 100%; } .filters .el-select { flex: 1; width: auto; } .campaign-grid { grid-template-columns: 1fr; } .list-foot { align-items: flex-start; flex-direction: column; } .form-grid.two { grid-template-columns: 1fr; } .form-grid .full { grid-column: auto; } }
</style>

