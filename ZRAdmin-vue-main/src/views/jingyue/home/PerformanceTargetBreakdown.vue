<template>
  <div class="breakdown-page" v-loading="loading">
    <header class="page-head">
      <div class="head-copy">
        <p class="eyebrow">PERFORMANCE TARGET BREAKDOWN</p>
        <h1>业绩目标拆解</h1>
        <p class="sub">先验证目标是否合理，再决定每个品类卖多少、今天做什么、找哪些客户。</p>
      </div>
      <div class="head-tools">
        <el-date-picker v-model="month" type="month" value-format="YYYY-MM" :clearable="false" :editable="false" @change="load" />
        <el-button :icon="Refresh" :loading="loading" @click="load">刷新</el-button>
        <el-button type="primary" :icon="EditPen" @click="$router.push('/business/goals')">维护月目标</el-button>
      </div>
    </header>

    <section class="summary-band">
      <div class="summary-lead">
        <span class="month-chip">{{ monthLabel }}</span>
        <div class="summary-number"><strong>{{ money(report.targetCheck?.targetAmount || 0) }}</strong><small>月目标</small></div>
        <div class="summary-number done"><strong>{{ money(totalDone) }}</strong><small>已达成</small></div>
      </div>
      <div class="summary-progress">
        <div class="progress-label"><span>整体达成</span><b>{{ decimal(totalPercent) }}%</b></div>
        <div class="progress-track"><i :style="{ width: Math.min(100, totalPercent) + '%' }" :class="toneClass(totalPercent)"></i></div>
        <p>{{ report.statusLine }}</p>
      </div>
      <dl class="summary-facts">
        <div><dt>剩余缺口</dt><dd>{{ money(Math.max(0, (report.targetCheck?.targetAmount || 0) - totalDone)) }}</dd></div>
        <div><dt>剩余天数</dt><dd>{{ report.restDays || 0 }}<em>天</em></dd></div>
        <div><dt>今日优先</dt><dd>{{ report.todayActions?.length || 0 }}<em>项</em></dd></div>
        <div><dt>待回访客户</dt><dd>{{ dueVisits }}<em>位</em></dd></div>
      </dl>
    </section>

    <div v-if="errorText" class="error-bar">{{ errorText }}</div>

    <section class="report-grid">
      <article class="panel target-panel">
        <header class="panel-head">
          <div><span class="section-no">01</span><h2>目标合理性</h2></div>
          <span class="panel-note">按近 3 月销售与库存反推上限</span>
        </header>
        <div class="target-compare">
          <div class="compare-column"><span>你定的目标</span><strong>{{ money(report.targetCheck?.targetAmount || 0) }}</strong></div>
          <div class="compare-arrow" :class="{ danger: report.targetCheck?.isHigh }"><el-icon><Right /></el-icon></div>
          <div class="compare-column"><span>数据支撑上限</span><strong>{{ money(report.targetCheck?.ceiling || 0) }}</strong></div>
        </div>
        <div class="target-metrics">
          <div><span>近 3 月月均</span><b>{{ money(report.targetCheck?.average3Month || 0) }}</b></div>
          <div><span>月销售趋势</span><b :class="(report.targetCheck?.trend || 0) >= 0 ? 'good' : 'low'">{{ signedMoney(report.targetCheck?.trend || 0) }}</b></div>
          <div><span>库存可支撑</span><b>{{ money(report.targetCheck?.stockSupport || 0) }}</b></div>
        </div>
        <p class="target-advice" :class="{ warn: report.targetCheck?.isHigh }">{{ report.targetCheck?.suggestion }}</p>
      </article>

      <article class="panel weight-panel">
        <header class="panel-head">
          <div><span class="section-no">02</span><h2>品类权重</h2></div>
          <el-button text :icon="Setting" @click="openWeight">调整权重</el-button>
        </header>
        <div class="weight-formula">
          <span>销售占比 {{ weights.salesWeight }}%</span><i>+</i>
          <span>库存压力 {{ weights.stockWeight }}%</span><i>+</i>
          <span>利润率 {{ weights.marginWeight }}%</span>
        </div>
        <div class="weight-list">
          <div v-for="row in topWeights" :key="row.categoryId" class="weight-row">
            <span class="weight-name">{{ row.categoryName }}</span>
            <div class="weight-bar"><i :style="{ width: Math.min(100, row.percent) + '%' }"></i></div>
            <b>{{ decimal(row.percent, 0) }}%</b>
            <small>{{ row.basis }}</small>
          </div>
        </div>
      </article>
    </section>

    <section class="panel week-panel">
      <header class="panel-head">
        <div><span class="section-no">03</span><h2>周目标</h2></div>
        <span class="panel-note">把剩余缺口均分到本周与下周，避免月初松、月底赶</span>
      </header>
      <div class="week-track">
        <div v-for="(week, index) in report.weeks || []" :key="index" class="week-item">
          <span class="week-index">{{ index === 0 ? 'NOW' : 'NEXT' }}</span>
          <div class="week-main">
            <div><h3>{{ week.weekLabel }}</h3><strong>{{ money(week.targetAmount) }}</strong></div>
            <p>主推：{{ week.focusCategories || '暂无主推品类' }}</p>
            <small>{{ week.keyAction }}</small>
          </div>
        </div>
      </div>
    </section>

    <section class="panel actions-panel">
      <header class="panel-head">
        <div>
          <span class="section-no">04</span><h2>今日优先动作</h2>
          <el-tooltip placement="top-start" effect="dark" popper-class="jy-guide-tip" :show-after="180">
            <template #content>
              <div class="guide-title">今日优先动作</div>
              <p><b>作用：</b>按今天最该处理的顺序，给出客户回访、销售补口、联动组合和库存处理动作。</p>
              <p><b>来源：</b>会员购买与回访记录、销售目标缺口、在库与超过 60 天库存、已启用联动组合。</p>
              <p><b>排序：</b>客户到期优先，其次按今日需补金额和压货数量排序，最多展示 5 条可执行动作。</p>
              <p><b>说明：</b>接待客次、加微信和流失统计目前由人工录入，只用于当天复盘，不参与这里的自动排序。</p>
            </template>
            <span class="info-dot" aria-label="查看今日优先动作说明"><el-icon><InfoFilled /></el-icon></span>
          </el-tooltip>
        </div>
        <span class="panel-note">客户到期、目标缺口、压货风险，按可执行性排序</span>
      </header>
      <div v-if="!(report.todayActions || []).length" class="empty-state">先补齐品类目标和经营参数，这里会自动生成今天能执行的动作。</div>
      <div v-else class="action-list">
        <article v-for="(action, index) in report.todayActions" :key="index" class="action-item" :class="priorityClass(action.priority)">
          <div class="action-rank">{{ String(index + 1).padStart(2, '0') }}</div>
          <div class="action-body">
            <div class="action-title">
              <span class="priority-pill">{{ action.priority }}</span>
              <b>{{ action.categoryName }}</b>
              <em>{{ action.goal }}</em>
              <span v-if="action.actionType" class="action-type">{{ action.actionType }}</span>
            </div>
            <p>{{ action.actionText }}</p><small>{{ action.basis }}</small>
            <div v-if="action.quantifiedGoal || action.sourceLabel" class="action-meta">
              <span v-if="action.quantifiedGoal" class="action-quantity">{{ action.quantifiedGoal }}</span>
              <span v-if="action.sourceLabel">数据来源：{{ action.sourceLabel }}</span>
            </div>
          </div>
          <el-button :icon="DocumentAdd" :loading="todoLoading === index" @click="createTodo(action, index)">生成待办</el-button>
        </article>
      </div>
    </section>

    <section class="panel category-action-panel">
      <header class="panel-head">
        <div>
          <span class="section-no">05</span><h2>每个品类今天的动作</h2>
          <el-tooltip placement="top-start" effect="dark" popper-class="jy-guide-tip" :show-after="180">
            <template #content>
              <div class="guide-title">每个品类今天的动作</div>
              <p><b>作用：</b>逐个品类判断今天该补目标、去库存、补货还是观察，方便快速分配柜台主推和员工动作。</p>
              <p><b>来源：</b>品类月目标与实际销售、在库件数、超过 60 天库存、近 3 月毛利和会员偏好。</p>
              <p><b>规则：</b>只有出现目标缺口、压货、库存不足或客户机会时才生成动作；每个品类最多一条主动作和一条备选动作。</p>
              <p><b>说明：</b>没有触发条件的品类显示“观察”，不强行安排动作；手工客流数据不会直接分摊到具体品类。</p>
            </template>
            <span class="info-dot" aria-label="查看品类动作说明"><el-icon><InfoFilled /></el-icon></span>
          </el-tooltip>
        </div>
        <span class="panel-note">先补目标缺口，再处理压货；无触发条件不硬排动作</span>
      </header>
      <div class="category-actions">
        <article v-for="row in actionRows" :key="row.categoryId" class="category-action">
          <header>
            <div>
              <span class="category-name"><b>{{ row.categoryName }}</b><i :class="actionStateClass(row)">{{ actionState(row) }}</i></span>
              <small>完成 {{ money(row.doneAmount) }} / 目标 {{ money(row.targetAmount) }} · 进度 {{ decimal(row.percent, 0) }}%</small>
            </div>
            <el-button text :icon="EditPen" @click="openPlanEditor(row)">改动作</el-button>
          </header>
          <div class="category-metrics">
            <span><em>今日需补</em><b>{{ money(gapDailyNeed(row)) }}</b></span>
            <span><em>在库</em><b>{{ decimal(row.stockQty, 0) }} 件</b></span>
            <span><em>超 60 天</em><b :class="{ warn: Number(row.staleQty || 0) > 0 }">{{ decimal(row.staleQty, 0) }} 件</b></span>
            <span><em>毛利</em><b>{{ decimal(row.grossRate, 0) }}%</b></span>
          </div>
          <ol>
            <li v-for="level in actionLevels" v-show="level.key !== 'low' || row.actionLow" :key="level.key" :class="level.key">
              <span>{{ level.label }}</span><p>{{ actionText(row, level.key) }}</p>
            </li>
          </ol>
          <small class="action-basis">{{ row.autoActionBasis }}</small>
        </article>
      </div>
    </section>

    <section class="report-grid visit-grid">
      <article class="panel combo-panel">
        <header class="panel-head">
          <div><span class="section-no">06</span><h2>联动组合</h2></div>
          <div class="combo-tools">
            <span class="panel-note">近 7 天销售净额占比</span>
            <el-button text :icon="Setting" @click="$router.push('/personal/combo-setting')">配置组合</el-button>
            <el-button text :icon="Right" @click="$router.push('/reports/combo-report')">查看详情</el-button>
          </div>
        </header>
        <div v-if="!(report.combos || []).length" class="combo-empty">
          <span>还没有启用的联动组合</span>
          <el-button text type="primary" @click="$router.push('/personal/combo-setting')">去配置</el-button>
        </div>
        <div v-else class="combo-list">
          <div v-for="combo in report.combos" :key="combo.id || combo.name" class="combo-row">
            <div class="combo-title"><b>{{ combo.name }}</b><span>{{ decimal(combo.percent, 2) }}%</span></div>
            <p>{{ (combo.categories || []).join(' + ') }}<template v-if="combo.fit"> · {{ combo.fit }}</template><template v-if="combo.scene"> · {{ combo.scene }}</template></p>
            <div class="combo-bar"><i :style="{ width: Math.min(100, Number(combo.percent || 0)) + '%' }"></i></div>
            <small>{{ combo.basis }}<template v-if="combo.amount"> · 成交 {{ money(combo.amount) }}</template></small>
          </div>
        </div>
      </article>
      <article class="panel visit-panel">
        <header class="panel-head"><div><span class="section-no">07</span><h2>客户回访</h2></div><el-button text @click="$router.push('/business/member')">会员档案</el-button></header>
        <div v-if="!(report.visits || []).length" class="empty-state compact">暂无可回访客户，先积累成交记录。</div>
        <div v-else class="visit-list">
          <div v-for="visit in report.visits.slice(0, 5)" :key="visit.memberId" class="visit-row">
            <div class="visit-person"><b>{{ visit.memberName }}</b><small>{{ visit.preferredCategory }} · {{ visit.phone || '未留电话' }}</small></div>
            <div class="visit-due" :class="{ due: visit.dueDays <= 0 }">{{ visit.cycleDays ? (visit.dueDays <= 0 ? '已到期' : '还有 ' + visit.dueDays + ' 天') : '首单客户' }}</div>
            <p>{{ visit.suggest }}</p>
          </div>
        </div>
      </article>
    </section>

    <section class="panel plan-panel">
      <header class="panel-head plan-head">
        <div class="plan-title">
          <div class="title-line"><span class="section-no">08</span><h2>按品类拆解</h2></div>
          <p class="panel-note">改占比自动算销售额，改销售额自动反推占比</p>
        </div>
        <el-button class="save-plan-btn" type="primary" :icon="Check" :loading="saving" @click="savePlans">保存拆解</el-button>
      </header>
      <el-table :data="editRows" row-key="categoryId" size="small" class="plan-table" max-height="520">
        <el-table-column label="品类" min-width="130" fixed>
          <template #default="{ row }"><div class="plan-category"><b>{{ row.categoryName }}</b><small v-if="row.planRatio > 40" class="over-ratio">占比超过 40%</small></div></template>
        </el-table-column>
        <el-table-column label="分配占比" width="118" align="right"><template #default="{ row }"><el-input-number v-model="row.planRatio" :min="0" :max="100" :precision="1" :controls="false" size="small" @change="ratioChanged(row)" /></template></el-table-column>
        <el-table-column label="目标销售额" width="150" align="right"><template #default="{ row }"><el-input-number v-model="row.targetAmount" :min="0" :precision="2" :controls="false" size="small" @change="amountChanged(row)" /></template></el-table-column>
        <el-table-column label="毛利率" width="110" align="right"><template #default="{ row }"><el-input-number v-model="row.grossMarginRate" :min="0" :max="100" :precision="1" :controls="false" size="small" /></template></el-table-column>
        <el-table-column label="预估毛利" width="120" align="right"><template #default="{ row }"><span class="money-cell">{{ money((row.targetAmount || 0) * (row.grossMarginRate || 0) / 100) }}</span></template></el-table-column>
        <el-table-column label="客单价" width="118" align="right"><template #default="{ row }"><el-input-number v-model="row.unitPrice" :min="0" :precision="2" :controls="false" size="small" /></template></el-table-column>
        <el-table-column label="预估件数" width="94" align="right"><template #default="{ row }">{{ row.unitPrice > 0 ? Math.ceil(row.targetAmount / row.unitPrice) : '—' }}</template></el-table-column>
        <el-table-column label="已达成" width="116" align="right"><template #default="{ row }"><b class="money-cell done">{{ money(row.doneAmount) }}</b></template></el-table-column>
        <el-table-column label="达成率" width="110" align="right"><template #default="{ row }"><span :class="toneClass(row.percent)">{{ row.targetAmount > 0 ? decimal(row.percent) + '%' : '—' }}</span></template></el-table-column>
      </el-table>
      <div class="plan-footer">
        <span>占比合计 <b :class="Math.abs(planRatioSum - 100) > 0.1 ? 'low' : 'good'">{{ decimal(planRatioSum) }}%</b></span>
        <span>目标合计 <b>{{ money(planTargetSum) }}</b></span><span>预估毛利 <b>{{ money(planGrossSum) }}</b></span>
      </div>
    </section>

    <section class="panel customer-panel">
      <header class="panel-head"><div><span class="section-no">09</span><h2>分品类客户动作</h2></div><span class="panel-note">由品类目标反推成交单数、意向客户数和每日联系量</span></header>
      <div class="customer-table-head"><span>品类</span><span>成交单数</span><span>意向客户</span><span>新客咨询</span><span>老客回访</span><span>每日新客</span><span>每日老客</span><span>工作量</span></div>
      <div v-for="plan in customerPlans" :key="plan.categoryId" class="customer-table-row">
        <span><b>{{ plan.categoryName }}</b><small>{{ money(plan.targetAmount) }} ÷ {{ money(plan.unitPrice) }}</small></span>
        <span>{{ plan.orders }}</span><span>{{ plan.intentCustomers }}</span><span>{{ plan.newConsult }}</span><span>{{ plan.oldVisit }}</span>
        <span>{{ plan.perDayNew }}</span><span>{{ plan.perDayOld }}</span>
        <span><em :class="{ heavy: plan.tooHeavy }">{{ plan.tooHeavy ? '任务过重' : '可执行' }}</em></span>
      </div>
      <p class="formula-note">成交单数 = 目标 ÷ 客单价；意向客户 = 成交单数 ÷ 转化率；新客与老客按新客占比拆分，再按 {{ customerPlans[0]?.workdays || 0 }} 个工作日均分。单品类每日联系超过 8 人标记任务过重。</p>
    </section>

    <el-dialog v-model="weightDialog" title="调整品类权重" width="430px" append-to-body :close-on-click-modal="false" class="jy-goal-dialog">
      <div class="weight-dialog">
        <label><span>销售占比</span><el-input-number v-model="weightDraft.salesWeight" :min="0" :max="100" :precision="0" /></label>
        <label><span>库存压力</span><el-input-number v-model="weightDraft.stockWeight" :min="0" :max="100" :precision="0" /></label>
        <label><span>利润率</span><el-input-number v-model="weightDraft.marginWeight" :min="0" :max="100" :precision="0" /></label>
      </div>
      <p class="dialog-tip" :class="{ low: weightDraftTotal !== 100 }">三项合计 {{ weightDraftTotal }}%，必须等于 100%。</p>
      <template #footer><el-button @click="weightDialog = false">取消</el-button><el-button type="primary" :loading="savingWeight" @click="saveWeights">保存权重</el-button></template>
    </el-dialog>

    <el-dialog v-model="planDialog" :title="'调整 ' + editingCategory + ' 的动作'" width="560px" append-to-body :close-on-click-modal="false" class="jy-goal-dialog">
      <div class="action-edit-form">
        <p class="edit-tip">留空时使用系统根据目标缺口、库存和毛利自动生成的动作；“恢复自动”会清空人工改写。</p>
        <label><span>主动作</span><el-input v-model="actionDraft.high" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="填写今天最先执行的一条动作" /></label>
        <label><span>备选动作</span><el-input v-model="actionDraft.medium" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="主推受阻时再执行，可留空" /></label>
        <label><span>补充说明（可选）</span><el-input v-model="actionDraft.low" type="textarea" :rows="2" maxlength="500" show-word-limit placeholder="记录交接或注意事项，不占卡片主展示区" /></label>
      </div>
      <template #footer><el-button @click="planDialog = false">取消</el-button><el-button :icon="RefreshLeft" @click="resetActionDraft">恢复自动</el-button><el-button type="primary" :loading="saving" @click="savePlans">保存</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyPerformanceTargetBreakdown">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { Refresh, EditPen, Setting, Check, Right, DocumentAdd, RefreshLeft, InfoFilled } from '@element-plus/icons-vue'
import { getOpsBreakdown, saveGoalWeights, saveGoalPlans } from '@/api/jingyue/report'
import { saveWorkItem } from '@/api/jingyue/workitem'
import { serverNow } from '@/utils/serverTime'

const now = serverNow()
const month = ref(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`)
const loading = ref(false)
const saving = ref(false)
const savingWeight = ref(false)
const errorText = ref('')
const report = reactive(emptyReport())
const editRows = ref([])
const weightDialog = ref(false)
const weightDraft = reactive({ salesWeight: 60, stockWeight: 20, marginWeight: 20 })
const planDialog = ref(false)
const editingCategory = ref('')
const editingCategoryId = ref(0)
const actionDraft = reactive({ high: '', medium: '', low: '' })
const todoLoading = ref(-1)
const actionLevels = [
  { key: 'high', label: '主' },
  { key: 'medium', label: '备' },
  { key: 'low', label: '补' }
]

function emptyReport() {
  return { date: '', month: '', restDays: 0, statusLine: '', targetCheck: {}, weights: {}, categoryWeights: [], weeks: [], todayActions: [], combos: [], visits: [], planRows: [], customerPlans: [] }
}
const monthLabel = computed(() => {
  const [year, monthNo] = month.value.split('-')
  return `${year} 年 ${Number(monthNo)} 月`
})
const weights = computed(() => report.weights || {})
const totalDone = computed(() => editRows.value.reduce((sum, row) => sum + Number(row.doneAmount || 0), 0))
const totalPercent = computed(() => {
  const target = Number(report.targetCheck?.targetAmount || 0)
  return target > 0 ? (totalDone.value * 100) / target : 0
})
const topWeights = computed(() => (report.categoryWeights || []).filter((it) => it.percent > 0 || it.salesAmount > 0).slice(0, 6))
const actionRows = computed(() => editRows.value.slice(0, 12))
const customerPlans = computed(() => report.customerPlans || [])
const dueVisits = computed(() => (report.visits || []).filter((it) => it.dueDays <= 7).length)
const weightDraftTotal = computed(() => Number(weightDraft.salesWeight || 0) + Number(weightDraft.stockWeight || 0) + Number(weightDraft.marginWeight || 0))
const planRatioSum = computed(() => editRows.value.reduce((sum, row) => sum + Number(row.planRatio || 0), 0))
const planTargetSum = computed(() => editRows.value.reduce((sum, row) => sum + Number(row.targetAmount || 0), 0))
const planGrossSum = computed(() => editRows.value.reduce((sum, row) => sum + (Number(row.targetAmount || 0) * Number(row.grossMarginRate || 0)) / 100, 0))

function money(value) {
  return '¥' + Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function signedMoney(value) {
  const n = Number(value || 0)
  return (n >= 0 ? '+' : '-') + money(Math.abs(n)).slice(1)
}
function decimal(value, digits = 1) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits })
}
function toneClass(value) {
  const n = Number(value || 0)
  return n >= 100 ? 'good' : n >= 60 ? 'normal' : 'low'
}
function priorityClass(priority) {
  return priority === '高' ? 'is-high' : priority === '中' ? 'is-mid' : 'is-low'
}
function dailyNeed(row) {
  return Number(report.restDays || 1) > 0 ? Number(row.targetAmount || 0) / Number(report.restDays) : 0
}
function gapDailyNeed(row) {
  const gap = Math.max(0, Number(row.targetAmount || 0) - Number(row.doneAmount || 0))
  return gap / Math.max(1, Number(report.restDays || 1))
}
function actionState(row) {
  const gap = Number(row.targetAmount || 0) - Number(row.doneAmount || 0)
  if (gap > 0 && Number(row.staleQty || 0) > 0) return '补目标 · 去库存'
  if (gap > 0) return '补销售目标'
  if (Number(row.staleQty || 0) > 0) return '去库存'
  return '观察'
}
function actionStateClass(row) {
  const state = actionState(row)
  return state === '观察' ? 'is-observe' : state.includes('库存') ? 'is-stock' : 'is-sales'
}
function actionText(row, level) {
  const manual = level === 'high' ? row.actionHigh : level === 'medium' ? row.actionMedium : row.actionLow
  if (manual) return manual
  if (level === 'high') return row.autoActionMain || '当前没有触发动作条件，今天观察。'
  if (level === 'medium') return row.autoActionAlt || '暂无备选动作。'
  return ''
}
function ratioChanged(row) {
  const total = Number(report.targetCheck?.targetAmount || 0)
  row.targetAmount = Math.round(total * Number(row.planRatio || 0)) / 100
}
function amountChanged(row) {
  const total = planTargetSum.value || Number(report.targetCheck?.targetAmount || 0)
  row.planRatio = total > 0 ? Math.round((Number(row.targetAmount || 0) * 10000) / total) / 100 : 0
}

async function load() {
  loading.value = true
  errorText.value = ''
  try {
    const [year, monthNo] = month.value.split('-').map(Number)
    const lastDay = new Date(year, monthNo, 0).getDate()
    const res = await getOpsBreakdown({ month: month.value, date: `${month.value}-${String(Math.min(now.getDate(), lastDay)).padStart(2, '0')}` })
    Object.assign(report, emptyReport(), res.data || {})
    Object.assign(weightDraft, report.weights || {})
    editRows.value = (report.planRows || []).map((row) => ({ ...row }))
  } catch (error) {
    errorText.value = error?.message || '目标拆解数据加载失败，请检查目标与款式分类配置。'
  } finally {
    loading.value = false
  }
}
function openWeight() {
  Object.assign(weightDraft, report.weights || {})
  weightDialog.value = true
}
async function saveWeights() {
  if (weightDraftTotal.value !== 100) {
    ElMessage.warning('销售占比、库存压力、利润率三项合计必须等于 100%')
    return
  }
  savingWeight.value = true
  try {
    await saveGoalWeights({
      goalYear: Number(month.value.slice(0, 4)),
      goalMonth: Number(month.value.slice(5, 7)),
      salesWeight: weightDraft.salesWeight,
      stockWeight: weightDraft.stockWeight,
      marginWeight: weightDraft.marginWeight
    })
    ElMessage.success('权重已保存，品类排序会按新权重重算')
    weightDialog.value = false
    await load()
  } catch (error) {
    ElMessage.error(error?.message || '权重保存失败')
  } finally {
    savingWeight.value = false
  }
}
function openPlanEditor(row) {
  editingCategory.value = row.categoryName
  editingCategoryId.value = row.categoryId
  Object.assign(actionDraft, { high: row.actionHigh || '', medium: row.actionMedium || '', low: row.actionLow || '' })
  planDialog.value = true
}
function resetActionDraft() {
  Object.assign(actionDraft, { high: '', medium: '', low: '' })
}
async function savePlans() {
  if (Math.abs(planRatioSum.value - 100) > 0.1) {
    ElMessage.warning(`品类占比合计必须为 100%，当前 ${decimal(planRatioSum.value)}%`)
    return
  }
  if (planDialog.value && editingCategoryId.value > 0) {
    const row = editRows.value.find((it) => it.categoryId === editingCategoryId.value)
    if (row) Object.assign(row, { actionHigh: actionDraft.high, actionMedium: actionDraft.medium, actionLow: actionDraft.low })
  }
  saving.value = true
  try {
    await saveGoalPlans({
      goalYear: Number(month.value.slice(0, 4)),
      goalMonth: Number(month.value.slice(5, 7)),
      rows: editRows.value.map((row) => ({
        categoryId: row.categoryId,
        planRatio: row.planRatio,
        targetAmount: row.targetAmount,
        grossMarginRate: row.grossMarginRate,
        unitPrice: row.unitPrice,
        conversionRate: row.conversionRate,
        newCustomerRate: row.newCustomerRate,
        actionHigh: row.actionHigh,
        actionMedium: row.actionMedium,
        actionLow: row.actionLow
      }))
    })
    ElMessage.success('品类拆解和动作已保存')
    planDialog.value = false
    await load()
  } catch (error) {
    ElMessage.error(error?.message || '拆解保存失败')
  } finally {
    saving.value = false
  }
}
async function createTodo(action, index) {
  todoLoading.value = index
  try {
    const endDate = `${month.value}-${String(new Date(Number(month.value.slice(0, 4)), Number(month.value.slice(5, 7)), 0).getDate()).padStart(2, '0')}`
    await saveWorkItem({
      title: `[目标拆解] ${action.categoryName} · ${String(action.actionText).slice(0, 36)}`,
      itemKind: 1,
      periodType: 3,
      startDate: report.date,
      endDate,
      dueTime: `${endDate} 23:59:59`,
      priority: action.priority === '高' ? 4 : action.priority === '中' ? 3 : 2,
      hasDateRange: 1,
      isPerformanceRelated: 0,
      progressMode: 1,
      remark: `${action.actionText}\n依据：${action.basis}`
    })
    ElMessage.success('已生成到待办与目标')
  } catch (error) {
    ElMessage.error(error?.message || '生成待办失败，请检查待办权限')
  } finally {
    todoLoading.value = -1
  }
}
onMounted(load)
</script>

<style scoped>
.breakdown-page { padding: 2px 0 16px; color: #3f3438; font-family: "Microsoft YaHei", "PingFang SC", sans-serif; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; margin-bottom: 14px; }
.head-copy { min-width: 0; }
.eyebrow { margin: 0 0 4px; color: #b17a86; font-size: 11.5px; font-weight: 700; letter-spacing: .14em; }
.head-copy h1 { margin: 0; color: #3b2c31; font-size: 26px; font-weight: 700; line-height: 1.25; }
.head-copy .sub { margin: 5px 0 0; color: #927e84; font-size: 13.5px; }
.head-tools { display: flex; align-items: center; gap: 8px; flex: none; }
.head-tools :deep(.el-button) { border-color: #ead8dd; color: #8c5965; background: #fffdfd; }
.head-tools :deep(.el-button--primary) { color: #fff; background: #b76f7f; border-color: #b76f7f; }
.summary-band { display: grid; grid-template-columns: auto minmax(220px, .9fr) minmax(400px, 1.35fr); align-items: center; gap: 26px; padding: 16px 20px; margin-bottom: 12px; background: #fffdfd; border: 1px solid #eddfe2; border-left: 3px solid #b76f7f; border-radius: 6px; }
.summary-lead { display: flex; align-items: center; gap: 20px; }
.month-chip { padding: 4px 8px; color: #9d5f6d; font-size: 12px; background: #fbefF2; border-radius: 3px; white-space: nowrap; }
.summary-number { display: flex; flex-direction: column; gap: 1px; }
.summary-number strong { color: #443237; font-size: 23px; font-weight: 700; line-height: 1.15; font-variant-numeric: tabular-nums; }
.summary-number.done strong { color: #a55d6d; }
.summary-number small { color: #a38e94; font-size: 11.5px; }
.summary-progress { align-self: stretch; display: flex; flex-direction: column; justify-content: center; padding-right: 8px; border-right: 1px solid #f0e4e7; }
.progress-label { display: flex; justify-content: space-between; margin-bottom: 5px; color: #8e7a80; font-size: 12px; }
.progress-label b { color: #9b5765; font-size: 13px; }
.progress-track { height: 5px; overflow: hidden; border-radius: 99px; background: #f4e7ea; }
.progress-track i { display: block; height: 100%; border-radius: 99px; background: #b76f7f; }
.progress-track i.good { background: #779681; }
.progress-track i.normal { background: #b99861; }
.summary-progress p { margin: 6px 0 0; color: #ad989e; font-size: 11.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.summary-facts { display: grid; grid-template-columns: repeat(4, minmax(76px, 1fr)); gap: 0; margin: 0; }
.summary-facts div { padding: 0 11px; border-right: 1px solid #f2e5e8; }
.summary-facts div:last-child { border-right: 0; }
.summary-facts dt { color: #a38d93; font-size: 11.5px; }
.summary-facts dd { margin: 3px 0 0; color: #55434a; font-size: 15px; font-weight: 700; font-variant-numeric: tabular-nums; white-space: nowrap; }
.summary-facts dd em { margin-left: 2px; color: #a18b91; font-size: 11px; font-style: normal; font-weight: 400; }
.report-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.08fr); gap: 12px; margin-bottom: 12px; }
.panel { padding: 15px 17px; background: #fff; border: 1px solid #eddfe2; border-radius: 6px; }
.panel-head { display: flex; align-items: center; justify-content: space-between; gap: 14px; margin-bottom: 12px; }
.panel-head > div { display: flex; align-items: center; gap: 8px; min-width: 0; }
.section-no { color: #c5949e; font-size: 11px; font-weight: 700; font-variant-numeric: tabular-nums; }
.panel-head h2 { margin: 0; color: #4a383e; font-size: 15px; font-weight: 700; white-space: nowrap; }
.panel-note { overflow: hidden; color: #a58e94; font-size: 11.5px; text-overflow: ellipsis; white-space: nowrap; }
.info-dot { display: inline-flex; align-items: center; justify-content: center; width: 17px; height: 17px; color: #b98b96; cursor: help; border: 1px solid #eddfe2; border-radius: 50%; transition: color .18s ease, border-color .18s ease, background .18s ease; }
.info-dot:hover { color: #a35f6e; background: #fdf5f7; border-color: #dfbdc5; }
.info-dot .el-icon { font-size: 12px; }
.panel-head :deep(.el-button) { padding: 0; color: #a4606f; font-size: 12px; }
.target-compare { display: grid; grid-template-columns: 1fr 30px 1fr; align-items: center; gap: 6px; padding: 11px 13px; background: #fdf8f9; border-radius: 4px; }
.compare-column { display: flex; flex-direction: column; gap: 3px; }
.compare-column span { color: #9e878d; font-size: 11.5px; }
.compare-column strong { color: #4a383e; font-size: 20px; font-weight: 700; font-variant-numeric: tabular-nums; }
.compare-arrow { color: #c3959f; text-align: center; }
.compare-arrow.danger { color: #b75e54; }
.target-metrics { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-top: 10px; }
.target-metrics div { display: flex; flex-direction: column; gap: 2px; }
.target-metrics span { color: #a18b91; font-size: 11.5px; }
.target-metrics b { color: #625057; font-size: 13px; font-variant-numeric: tabular-nums; }
.target-advice { margin: 11px 0 0; padding-top: 9px; color: #7f6a70; font-size: 12px; line-height: 1.65; border-top: 1px dashed #f0e2e5; }
.target-advice.warn { color: #9b5b4d; }
.weight-formula { display: flex; align-items: center; gap: 7px; margin-bottom: 9px; color: #8f747b; font-size: 11.5px; }
.weight-formula span { padding: 3px 7px; background: #fbf1f3; border-radius: 3px; white-space: nowrap; }
.weight-formula i { color: #c5aab0; font-style: normal; }
.weight-list { display: flex; flex-direction: column; gap: 6px; }
.weight-row { display: grid; grid-template-columns: 52px minmax(40px, 1fr) 32px; align-items: center; gap: 7px; }
.weight-name { color: #5f4b51; font-size: 12px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.weight-bar { height: 4px; overflow: hidden; background: #f6e9ec; border-radius: 99px; }
.weight-bar i { display: block; height: 100%; background: linear-gradient(90deg, #d6a4ae, #b66f7f); border-radius: 99px; }
.weight-row b { color: #9a5c69; font-size: 12px; text-align: right; font-variant-numeric: tabular-nums; }
.weight-row small { grid-column: 1 / -1; margin: -2px 0 1px; color: #b09a9f; font-size: 10.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.week-panel { margin-bottom: 12px; }
.week-track { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
.week-item { display: flex; gap: 11px; padding: 12px 14px; background: #fdf9fa; border: 1px solid #f2e5e8; border-radius: 4px; }
.week-index { align-self: flex-start; padding: 3px 5px; color: #aa6c79; font-size: 10px; font-weight: 700; background: #f9e9ec; border-radius: 2px; }
.week-main { min-width: 0; flex: 1; }
.week-main > div { display: flex; justify-content: space-between; gap: 10px; }
.week-main h3 { margin: 0; color: #5d484f; font-size: 13px; font-weight: 700; }
.week-main strong { color: #a75f6e; font-size: 16px; font-variant-numeric: tabular-nums; }
.week-main p { margin: 6px 0 2px; color: #897279; font-size: 12px; }
.week-main small { color: #ae979d; font-size: 11px; line-height: 1.5; }
.actions-panel { margin-bottom: 12px; }
.action-list { display: flex; flex-direction: column; gap: 7px; }
.action-item { display: grid; grid-template-columns: 32px minmax(0, 1fr) auto; align-items: center; gap: 12px; padding: 10px 12px; border: 1px solid #efe3e5; border-left: 2px solid #c88d99; border-radius: 4px; }
.action-item.is-high { border-left-color: #b45468; background: #fffafb; }
.action-item.is-mid { border-left-color: #c5a05d; }
.action-item.is-low { border-left-color: #b9a7aa; }
.action-rank { color: #d2b2b9; font-size: 18px; font-weight: 700; font-variant-numeric: tabular-nums; }
.action-body { min-width: 0; }
.action-title { display: flex; align-items: center; gap: 7px; flex-wrap: wrap; }
.action-title b { color: #504046; font-size: 13px; }
.action-title em { color: #a1878e; font-size: 11px; font-style: normal; }
.priority-pill { padding: 2px 5px; color: #a35e6c; font-size: 10.5px; background: #f9e9ed; border-radius: 2px; }
.action-type { padding: 2px 6px; color: #8b6b72; font-size: 10.5px; background: #f8f2f3; border: 1px solid #f0e3e6; border-radius: 2px; }
.action-body p { margin: 4px 0 2px; color: #665157; font-size: 12.5px; line-height: 1.6; }
.action-body small { color: #ac969c; font-size: 11px; }
.action-meta { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 5px; color: #b09a9f; font-size: 10.5px; }
.action-quantity { padding: 1px 6px; color: #96626d; background: #fcf3f5; border-radius: 2px; }
.action-item :deep(.el-button) { color: #93606b; border-color: #ead9dd; background: #fff; font-size: 12px; }
.category-action-panel { margin-bottom: 12px; }
.category-actions { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 9px; }
.category-action { padding: 11px 12px; background: #fffdfd; border: 1px solid #f0e3e6; border-radius: 4px; }
.category-action header { display: flex; align-items: flex-start; justify-content: space-between; gap: 5px; padding-bottom: 7px; border-bottom: 1px solid #f7edeF; }
.category-action header div { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.category-action header b { overflow: hidden; color: #554249; font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.category-action header small { color: #aa9399; font-size: 11px; }
.category-action header :deep(.el-button) { flex: none; padding: 0; color: #a36b77; font-size: 11px; }
.category-name { display: flex; align-items: center; gap: 6px; min-width: 0; }
.category-name i { flex: none; padding: 1px 5px; color: #9b6170; font-size: 10px; font-style: normal; background: #fbf0f3; border-radius: 2px; }
.category-name i.is-observe { color: #938287; background: #f5f2f3; }
.category-name i.is-stock { color: #95713c; background: #fbf5e9; }
.category-metrics { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 4px; margin-top: 8px; }
.category-metrics span { display: flex; flex-direction: column; gap: 1px; padding: 5px 6px; background: #fdfafa; border: 1px solid #f7eef0; border-radius: 3px; }
.category-metrics em { color: #af999f; font-size: 10px; font-style: normal; }
.category-metrics b { overflow: hidden; color: #675158; font-size: 11.5px; text-overflow: ellipsis; white-space: nowrap; font-variant-numeric: tabular-nums; }
.category-metrics b.warn { color: #ad6356; }
.category-action ol { display: flex; flex-direction: column; gap: 6px; margin: 7px 0 0; padding: 0; list-style: none; }
.category-action li { display: grid; grid-template-columns: 18px minmax(0, 1fr); gap: 6px; }
.category-action li > span { height: 16px; padding-top: 1px; color: #fff; font-size: 10px; text-align: center; background: #b46c7b; border-radius: 2px; }
.category-action li.medium > span { background: #b99a63; }
.category-action li.low > span { background: #bba9ad; }
.category-action li p { margin: 0; color: #79656b; font-size: 11.5px; line-height: 1.55; }
.category-action li.low:has(p:empty) { display: none; }
.action-basis { display: block; margin-top: 7px; padding-top: 6px; color: #b09a9f; font-size: 10.5px; line-height: 1.5; border-top: 1px dashed #f3e8ea; }
.visit-grid { grid-template-columns: minmax(0, .9fr) minmax(0, 1.2fr); }
.combo-list, .visit-list { display: flex; flex-direction: column; gap: 0; }
.combo-row, .visit-row { padding: 8px 0; border-bottom: 1px solid #f7eff1; }
.combo-row:last-child, .visit-row:last-child { border-bottom: 0; padding-bottom: 0; }
.combo-title { display: flex; justify-content: space-between; gap: 8px; }
.combo-title b { color: #5d484f; font-size: 12.5px; }
.combo-title span { color: #aa6876; font-size: 12px; font-weight: 700; }
.combo-row p { margin: 3px 0; color: #887177; font-size: 11.5px; }
.combo-bar { height: 3px; margin: 5px 0 4px; overflow: hidden; background: #f7e8ec; }
.combo-bar i { display: block; height: 100%; background: linear-gradient(90deg, #e6bcc6, #aa556a); transition: width .3s ease; }
.combo-row small { color: #b19ba0; font-size: 10.5px; }
.combo-tools { display: flex; align-items: center; gap: 2px; flex-wrap: wrap; justify-content: flex-end; }
.combo-tools :deep(.el-button) { padding: 2px 4px; color: #98616e; font-size: 11px; }
.combo-tools .panel-note { margin-right: 3px; }
.combo-empty { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 18px 0 6px; color: #a28d93; font-size: 12px; }
.combo-empty :deep(.el-button) { padding: 0; font-size: 12px; }
.visit-row { display: grid; grid-template-columns: 112px 60px minmax(0, 1fr); align-items: center; gap: 6px 9px; }
.visit-person { display: flex; flex-direction: column; min-width: 0; }
.visit-person b { color: #56434a; font-size: 12.5px; }
.visit-person small { overflow: hidden; color: #ad969c; font-size: 10.5px; text-overflow: ellipsis; white-space: nowrap; }
.visit-due { padding: 2px 5px; color: #a9878e; font-size: 10.5px; text-align: center; background: #f7f0f1; border-radius: 2px; white-space: nowrap; }
.visit-due.due { color: #a95a54; background: #fcf0ed; }
.visit-row p { margin: 0; color: #7b666c; font-size: 11.5px; line-height: 1.55; }
.plan-panel { margin-bottom: 12px; padding-bottom: 12px; }
.head-actions { display: flex; align-items: center; gap: 12px; }
.head-actions :deep(.el-button--primary) { color: #fff; background: #b76f7f; border-color: #b76f7f; font-size: 12px; }
.plan-table { width: 100%; --el-table-border-color: #f7edef; --el-table-header-bg-color: #fdf8f9; --el-table-row-hover-bg-color: #fffafb; }
.plan-table :deep(.el-table__header th) { height: 34px; padding: 0; color: #9c8289; font-size: 11.5px; font-weight: 600; }
.plan-table :deep(.el-table__body td) { height: 38px; padding: 0 3px; color: #68545a; font-size: 11.5px; border-bottom-color: #faf3f5; }
.plan-table :deep(.el-input-number) { width: 100%; }
.plan-table :deep(.el-input__wrapper) { padding: 0 5px; background: #fffdfd; box-shadow: 0 0 0 1px #f2e5e8 inset; }
.plan-table :deep(.el-input__inner) { color: #68545a; font-size: 11.5px; text-align: right; }
.plan-category { display: flex; align-items: center; gap: 6px; min-width: 0; }
.plan-category b { overflow: hidden; color: #544149; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.over-ratio { flex: none; padding: 1px 4px; color: #a9644d; font-size: 10px; background: #fbf0e9; border-radius: 2px; }
.money-cell { color: #746168; font-variant-numeric: tabular-nums; }
.money-cell.done { color: #9f5e6c; }
.plan-footer { display: flex; gap: 24px; padding: 9px 2px 0; color: #9b858b; font-size: 11.5px; }
.plan-footer b { margin-left: 3px; color: #5b484f; font-size: 12.5px; }
.plan-footer b.good { color: #668671; }
.plan-footer b.low { color: #b25f54; }
.customer-panel { padding-bottom: 13px; }
.customer-table-head, .customer-table-row { display: grid; grid-template-columns: 1.7fr repeat(6, .72fr) .9fr; align-items: center; gap: 5px; padding: 0 8px; }
.customer-table-head { height: 32px; color: #9c858b; font-size: 11.5px; background: #fdf8f9; border-bottom: 1px solid #f2e5e8; }
.customer-table-row { min-height: 41px; color: #6e5a60; font-size: 12px; border-bottom: 1px solid #faf3f4; }
.customer-table-row:last-of-type { border-bottom: 0; }
.customer-table-row > span:first-child { display: flex; flex-direction: column; gap: 1px; }
.customer-table-row > span:first-child b { color: #544148; font-size: 12px; }
.customer-table-row > span:first-child small { color: #ae989e; font-size: 10.5px; }
.customer-table-row em { color: #6e8a77; font-size: 11px; font-style: normal; }
.customer-table-row em.heavy { color: #b45e52; }
.formula-note { margin: 9px 8px 0; color: #ad979d; font-size: 10.5px; line-height: 1.6; }
.error-bar { margin-bottom: 10px; padding: 9px 12px; color: #a5574f; font-size: 12.5px; background: #fdf1ef; border: 1px solid #f3d8d3; border-radius: 4px; }
.empty-state { padding: 18px 12px; color: #b09a9f; font-size: 12px; text-align: center; background: #fdfafb; border-radius: 4px; }
.empty-state.compact { padding: 10px; }
.good { color: #63826f; }
.normal { color: #a17b45; }
.low { color: #b05b50; }
.weight-dialog { display: flex; flex-direction: column; gap: 11px; }
.weight-dialog label { display: flex; align-items: center; justify-content: space-between; gap: 14px; color: #665158; font-size: 13px; }
.dialog-tip { margin: 10px 0 0; color: #9c878d; font-size: 12px; }
.dialog-tip.low { color: #b15d51; }
.action-edit-form { display: flex; flex-direction: column; gap: 12px; }
.edit-tip { margin: 0; padding: 8px 10px; color: #967e85; font-size: 12px; line-height: 1.6; background: #fdf8f9; border-radius: 4px; }
.action-edit-form label { display: flex; flex-direction: column; gap: 5px; color: #665158; font-size: 13px; }
@media (max-width: 1320px) {
  .summary-band { grid-template-columns: auto minmax(200px, 1fr); }
  .summary-facts { grid-column: 1 / -1; padding-top: 11px; border-top: 1px solid #f3e5e8; }
  .summary-progress { border-right: 0; }
  .category-actions { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 1000px) {
  .page-head { align-items: flex-start; flex-direction: column; gap: 10px; }
  .head-tools { width: 100%; }
  .head-tools :deep(.el-date-editor) { flex: 1; }
  .report-grid, .visit-grid { grid-template-columns: 1fr; }
  .summary-band { grid-template-columns: 1fr; gap: 12px; }
  .summary-facts { border-top: 1px solid #f3e5e8; padding-top: 10px; }
  .week-track { grid-template-columns: 1fr; }
  .customer-panel { overflow-x: auto; }
  .customer-table-head, .customer-table-row { min-width: 720px; }
}
@media (max-width: 700px) {
  .summary-lead { flex-wrap: wrap; gap: 10px 16px; }
  .summary-facts { grid-template-columns: repeat(2, 1fr); row-gap: 10px; }
  .summary-facts div:nth-child(2) { border-right: 0; }
  .target-metrics { grid-template-columns: 1fr; }
  .category-actions { grid-template-columns: 1fr; }
  .action-item { grid-template-columns: 24px minmax(0, 1fr); }
  .action-item > .el-button { grid-column: 2; justify-self: start; }
  .visit-row { grid-template-columns: 1fr auto; }
  .visit-row p { grid-column: 1 / -1; }
  .head-actions { align-items: flex-end; flex-direction: column; gap: 6px; }
  .panel-note { display: none; }
}

:global(.jy-guide-tip.el-popper) { max-width: 360px; padding: 12px 14px; font-family: "Microsoft YaHei", "PingFang SC", sans-serif; border: 1px solid #e9d5da; border-radius: 6px; }
:global(.jy-guide-tip .guide-title) { margin-bottom: 7px; color: #fff; font-size: 13px; font-weight: 700; }
:global(.jy-guide-tip p) { margin: 0 0 5px; color: #eadde0; font-size: 12px; line-height: 1.65; }
:global(.jy-guide-tip p:last-child) { margin-bottom: 0; }
:global(.jy-guide-tip b) { color: #fff; font-weight: 700; }

/* ---- 按品类拆解：头部与保存按钮 ---- */
.panel-head.plan-head { align-items: flex-start; }
.plan-head > .plan-title { display: flex; flex-direction: column; align-items: flex-start; gap: 2px; flex: 1 1 auto; min-width: 0; }
.plan-head .title-line { display: flex; align-items: center; gap: 8px; }
.plan-head .panel-note { display: block; overflow: visible; white-space: normal; line-height: 1.5; text-overflow: clip; }
.panel-head :deep(.el-button.save-plan-btn) { flex: 0 0 auto; height: 32px; min-width: 104px; padding: 0 16px; color: #fff; font-size: 13px; white-space: nowrap; background: #b76f7f; border-color: #b76f7f; border-radius: 5px; }
.panel-head :deep(.el-button.save-plan-btn:hover) { background: #c07f8d; border-color: #c07f8d; }
.panel-head :deep(.el-button.save-plan-btn:focus-visible) { outline: 2px solid rgba(183, 111, 127, .35); outline-offset: 2px; }

/* ---- 弹窗：避免被带 transform 的容器裁剪 ---- */
:global(.el-dialog.jy-goal-dialog) { max-width: calc(100vw - 32px); margin-top: 8vh; border-radius: 8px; }
:global(.el-dialog.jy-goal-dialog .el-dialog__header) { margin-right: 0; padding-bottom: 14px; border-bottom: 1px solid #f2e5e8; }
:global(.el-dialog.jy-goal-dialog .el-dialog__title) { color: #4a383e; font-size: 16px; font-weight: 700; }
:global(.el-dialog.jy-goal-dialog .el-dialog__body) { max-height: 64vh; padding-top: 18px; overflow-y: auto; }
:global(.el-dialog.jy-goal-dialog .el-dialog__footer) { padding-top: 12px; border-top: 1px solid #f2e5e8; }

@media (max-width: 700px) {
  .panel-head.plan-head { flex-direction: column; align-items: stretch; gap: 10px; }
  .panel-head :deep(.el-button.save-plan-btn) { align-self: flex-end; }
}
</style>


