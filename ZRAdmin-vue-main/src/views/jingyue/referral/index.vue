<template>
  <div class="jy-referral">
    <header class="ref-head">
      <div class="head-copy">
        <span class="eyebrow">REFERRAL</span>
        <h1>转介绍奖励</h1>
        <p>客户 A 介绍 B，建档时绑定推荐人。B 每笔成交按款式分类返点自动生成返现台账，永久留痕；金额可逐单修改，也可按日期区间一次性结清。</p>
      </div>
      <div class="head-actions">
        <el-button v-hasPermi="['jingyue:referral:export']" plain @click="exportCsv"><el-icon><Download /></el-icon>导出</el-button>
        <el-button v-hasPermi="['jingyue:referral:rate']" plain @click="openRates"><el-icon><Setting /></el-icon>返点设置</el-button>
        <el-button v-hasPermi="['jingyue:referral:payout']" type="primary" @click="openSettle"><el-icon><Wallet /></el-icon>返现结算</el-button>
      </div>
    </header>

    <section class="metric-row" v-loading="summaryLoading">
      <article class="primary">
        <span>累计奖励</span>
        <strong>¥{{ money(summary.totalAmount) }}</strong>
        <small>{{ summary.totalCount }} 条 · 含已返现与待返现</small>
      </article>
      <article>
        <span>待返现</span>
        <strong class="warn">¥{{ money(summary.pendingAmount) }}</strong>
        <small>{{ summary.pendingCount }} 条待处理 · 本月新增 ¥{{ money(summary.monthPendingAmount) }}</small>
      </article>
      <article>
        <span>已返现</span>
        <strong class="ok">¥{{ money(summary.paidAmount) }}</strong>
        <small>{{ summary.paidCount }} 条已结清</small>
      </article>
      <article>
        <span>已作废</span>
        <strong class="muted">¥{{ money(summary.voidAmount) }}</strong>
        <small>{{ summary.voidCount }} 条（来源销售单作废）</small>
      </article>
    </section>

    <section class="ref-body">
      <div class="ref-main">
        <div class="toolbar">
          <el-input v-model="query.keyword" clearable placeholder="推荐人 / 被推荐人 / 手机号 / 微信号 / 单号" :prefix-icon="Search" style="width: 300px" @keyup.enter="search" />
          <el-select v-model="query.status" clearable placeholder="全部状态" style="width: 120px" @change="search">
            <el-option label="待返现" :value="0" />
            <el-option label="已返现" :value="1" />
            <el-option label="已作废" :value="2" />
          </el-select>
          <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" style="width: 236px" @change="search" />
          <el-button type="primary" plain @click="search">查询</el-button>
          <el-button text @click="resetQuery">重置</el-button>
        </div>

        <el-table v-loading="loading" :data="rows" row-key="id" class="ref-table" @selection-change="onSelectionChange">
          <el-table-column type="selection" width="42" :selectable="selectable" />
          <el-table-column label="推荐人" min-width="140">
            <template #default="{ row }">
              <button type="button" class="link-name" @click="openGraph(row.referrerMemberId)">{{ row.referrerName || '—' }}</button>
              <div class="sub-line">{{ row.referrerWechatNo || row.referrerPhone || '无联系方式' }}</div>
            </template>
          </el-table-column>
          <el-table-column label="被推荐人" min-width="110">
            <template #default="{ row }">
              <div class="cell-strong">{{ row.referralName || '—' }}</div>
              <div class="sub-line">{{ row.referralPhone || '—' }}</div>
            </template>
          </el-table-column>
          <el-table-column label="来源单据" min-width="142">
            <template #default="{ row }">
              <div class="cell-strong">{{ row.saleOrderNo }}</div>
              <div class="sub-line">{{ dateText(row.bizTime) }} · 实付 ¥{{ money(row.orderAmount) }}</div>
            </template>
          </el-table-column>
          <el-table-column label="返点构成" min-width="190">
            <template #default="{ row }">
              <div class="style-summary">{{ row.styleSummary || '未匹配款式分类' }}</div>
            </template>
          </el-table-column>
          <el-table-column label="返现金额" width="132" align="right">
            <template #default="{ row }">
              <b class="reward">¥{{ money(row.rewardAmount) }}</b>
              <div v-if="row.isAdjusted === 1" class="sub-line">已调整 · 原 ¥{{ money(row.originalAmount) }}</div>
              <div v-else-if="row.rewardRate" class="sub-line">综合返点 {{ row.rewardRate }}%</div>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="98" align="center">
            <template #default="{ row }"><span class="status-tag" :class="statusClass(row.status)">{{ statusText(row.status) }}</span></template>
          </el-table-column>
          <el-table-column label="结算信息" min-width="150">
            <template #default="{ row }">
              <template v-if="row.status === 1">
                <div class="sub-line">{{ dateTime(row.payoutTime) }} · {{ row.payoutBy || '—' }}</div>
                <div class="sub-line">{{ row.settleBatchNo ? '批次 ' + row.settleBatchNo : '—' }}</div>
              </template>
              <span v-else class="sub-line">未结算</span>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="118" align="right">
            <template #default="{ row }">
              <el-button v-if="row.status === 0" v-hasPermi="['jingyue:referral:payout']" link type="primary" @click="editAmount(row)">调整金额</el-button>
              <el-button v-else-if="row.status === 1" v-hasPermi="['jingyue:referral:payout']" link type="primary" @click="cancelOne(row)">撤销结算</el-button>
              <span v-else class="sub-line">—</span>
            </template>
          </el-table-column>
          <template #empty>
            <div class="empty-block"><span>暂无转介绍返现记录</span><small>客户建档时填写推荐人微信后，成交即会自动生成台账。</small></div>
          </template>
        </el-table>

        <div class="list-foot">
          <span>记录永久保留，作废与撤销只改状态</span>
          <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
        </div>
      </div>

      <aside class="graph-panel">
        <div class="graph-head">
          <div><span>RELATION MAP</span><h2>推荐关系网</h2></div>
          <el-button v-if="graph && graph.focus" link type="primary" @click="graph = null">清空</el-button>
        </div>
        <div v-if="!graph" class="graph-empty">
          <el-icon><Share /></el-icon>
          <p>点击左侧推荐人姓名，查看上级介绍链、下挂客户和成交摘要。</p>
        </div>
        <div v-else v-loading="graphLoading" class="graph-body">
          <div v-if="graph.upChain.length" class="chain up">
            <div v-for="node in graph.upChain" :key="node.memberId" class="chain-node">
              <span class="chain-tag">上级 {{ node.depth }}</span>
              <b>{{ node.memberName }}</b>
              <small>{{ node.wechatNo || node.phone || '无联系方式' }}</small>
            </div>
            <i class="chain-line"></i>
          </div>
          <article class="focus-node">
            <div class="focus-main">
              <span class="focus-badge">当前客户</span>
              <b>{{ graph.focus.memberName }}</b>
              <small>{{ graph.focus.memberNo }} · {{ graph.focus.wechatNo || graph.focus.phone || '无联系方式' }}</small>
            </div>
            <div class="focus-metrics">
              <div><span>累计消费</span><strong>¥{{ money(graph.focus.totalAmount) }}</strong></div>
              <div><span>累计奖励</span><strong class="reward">¥{{ money(graph.focus.rewardTotal) }}</strong></div>
              <div><span>待返现</span><strong class="warn">¥{{ money(graph.focus.rewardPending) }}</strong></div>
            </div>
            <div v-if="graph.focus.orders.length" class="focus-orders">
              <div class="orders-title">最近成交</div>
              <div v-for="order in graph.focus.orders.slice(0, 4)" :key="order.orderId" class="order-line">
                <span>{{ order.orderNo }}</span>
                <small>{{ dateText(order.dealTime) }}</small>
                <b>¥{{ money(order.payAmount) }}</b>
              </div>
            </div>
          </article>
          <div class="down-block">
            <div class="down-title">下挂被推荐人 <b>{{ graph.downList.length }}</b></div>
            <div v-if="!graph.downList.length" class="graph-empty compact"><p>还没有被推荐的客户。</p></div>
            <div v-for="node in graph.downList" :key="node.memberId" class="down-row" :style="{ marginLeft: (node.depth - 1) * 12 + 'px' }">
              <div class="down-main">
                <b>{{ node.memberName }}</b>
                <small>{{ node.wechatNo || node.phone || '无联系方式' }} · L{{ node.depth }}</small>
              </div>
              <div class="down-values">
                <span>消费 ¥{{ money(node.totalAmount) }}</span>
                <span class="reward">返现 ¥{{ money(node.rewardTotal) }}</span>
              </div>
              <el-button link type="primary" @click="openGraph(node.memberId)">展开</el-button>
            </div>
          </div>
        </div>
      </aside>
    </section>

    <!-- 返点设置 -->
    <el-dialog v-model="rateOpen" title="按款式分类设置返点" width="640px" append-to-body :close-on-click-modal="false">
      <p class="dialog-tip">按一级款式分类设置返点百分比。生成返现台账时，按明细成交额占整单实付的比例分别计算，未配置的分类不产生返现。</p>
      <el-table :data="rateRows" v-loading="rateLoading" size="small" class="rate-table" max-height="420">
        <el-table-column label="一级款式分类" min-width="150">
          <template #default="{ row }"><span class="cell-strong">{{ row.styleName }}</span></template>
        </el-table-column>
        <el-table-column label="当前在库" width="90" align="right">
          <template #default="{ row }"><span class="sub-line">{{ number(row.stockQty) }} 件</span></template>
        </el-table-column>
        <el-table-column label="返点比例" width="150" align="center">
          <template #default="{ row }">
            <div class="rate-input">
              <el-input-number v-model="row.rate" :min="0" :max="100" :precision="2" :step="0.5" controls-position="right" size="small" />
              <span>%</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="备注" min-width="150">
          <template #default="{ row }"><el-input v-model="row.remark" size="small" maxlength="200" placeholder="选填" /></template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="rateOpen = false">取消</el-button>
        <el-button type="primary" :loading="rateSaving" @click="saveRates">保存返点设置</el-button>
      </template>
    </el-dialog>

    <!-- 区间结算 -->
    <el-dialog v-model="settleOpen" title="返现结算" width="820px" append-to-body :close-on-click-modal="false">
      <div class="settle-head">
        <div class="settle-range">
          <span>结算区间</span>
          <el-date-picker v-model="settleRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" style="width: 250px" @change="loadPending" />
        </div>
        <div class="settle-quick">
          <button v-for="item in quickRanges" :key="item.label" type="button" @click="applyQuick(item.days)">{{ item.label }}</button>
        </div>
      </div>
      <p class="dialog-tip">区间内所有待返现记录会一次结清，不要求当天发放。金额可直接修改，系统会保留原始值用于对账。</p>
      <el-table :data="pendingRows" v-loading="pendingLoading" size="small" max-height="360" class="settle-table">
        <el-table-column label="推荐人 / 被推荐人" min-width="180">
          <template #default="{ row }">
            <div class="cell-strong">{{ row.referrerName }} → {{ row.referralName }}</div>
            <div class="sub-line">{{ row.saleOrderNo }} · {{ dateText(row.bizTime) }}</div>
          </template>
        </el-table-column>
        <el-table-column label="返点构成" min-width="180">
          <template #default="{ row }"><span class="style-summary">{{ row.styleSummary || '—' }}</span></template>
        </el-table-column>
        <el-table-column label="系统金额" width="96" align="right">
          <template #default="{ row }"><span class="sub-line">¥{{ money(row.originalAmount || row.rewardAmount) }}</span></template>
        </el-table-column>
        <el-table-column label="本次返现" width="140" align="right">
          <template #default="{ row }">
            <el-input-number v-model="row.rewardAmount" :min="0" :max="1000000" :precision="2" :step="1" controls-position="right" size="small" />
          </template>
        </el-table-column>
      </el-table>
      <div class="settle-total">
        <span>共 {{ pendingRows.length }} 条</span>
        <b>本次合计 ¥{{ money(settleAmount) }}</b>
      </div>
      <el-form label-position="top" class="settle-form">
        <el-form-item label="发放说明"><el-input v-model="payoutRemark" type="textarea" :rows="2" maxlength="200" show-word-limit placeholder="如：2026-10-26 微信转账结清 10 月转介绍返现" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="settleOpen = false">取消</el-button>
        <el-button type="primary" :loading="payoutSaving" :disabled="!pendingRows.length" @click="submitSettle">确认结算（{{ pendingRows.length }} 条）</el-button>
      </template>
    </el-dialog>

    <!-- 单条改金额 -->
    <el-dialog v-model="amountOpen" title="调整返现金额" width="440px" append-to-body :close-on-click-modal="false">
      <p class="dialog-tip">{{ amountRow.referrerName }} 的这笔返现，系统按规则计算为 <b>¥{{ money(amountRow.originalAmount) }}</b>。</p>
      <el-form label-position="top">
        <el-form-item label="实际返现金额">
          <el-input-number v-model="amountRow.rewardAmount" :min="0" :max="1000000" :precision="2" :step="1" controls-position="right" style="width: 100%" />
        </el-form-item>
        <el-form-item label="调整说明"><el-input v-model="amountRemark" maxlength="200" placeholder="选填，如：抹零后按 198 元结清" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="amountOpen = false">取消</el-button>
        <el-button type="primary" @click="saveAmount">保存金额</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="JyReferral">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  listReferralRewards, getReferralSummary, payoutReferralRewards, cancelReferralPayout,
  getReferralGraph, listReferralRates, saveReferralRates, adjustReferralAmount
} from '@/api/jingyue/referral'

const loading = ref(false)
const summaryLoading = ref(false)
const graphLoading = ref(false)
const payoutSaving = ref(false)
const rateLoading = ref(false)
const rateSaving = ref(false)
const pendingLoading = ref(false)

const rows = ref([])
const total = ref(0)
const selection = ref([])
const dateRange = ref([])
const graph = ref(null)
const summary = reactive({ totalCount: 0, totalAmount: 0, pendingCount: 0, pendingAmount: 0, paidCount: 0, paidAmount: 0, voidCount: 0, voidAmount: 0, monthPendingAmount: 0 })
const query = reactive({ pageNum: 1, pageSize: 15, keyword: '', status: undefined })

const rateOpen = ref(false)
const rateRows = ref([])
const settleOpen = ref(false)
const settleRange = ref([])
const pendingRows = ref([])
const payoutRemark = ref('')
const amountOpen = ref(false)
const amountRow = reactive({ id: 0, referrerName: '', originalAmount: 0, rewardAmount: 0 })
const amountRemark = ref('')

const quickRanges = [
  { label: '今天', days: 1 },
  { label: '近 7 天', days: 7 },
  { label: '近 30 天', days: 30 },
  { label: '本月', days: 0 }
]

const selectionAmount = computed(() => selection.value.reduce((sum, row) => sum + Number(row.rewardAmount || 0), 0))
const settleAmount = computed(() => pendingRows.value.reduce((sum, row) => sum + Number(row.rewardAmount || 0), 0))

function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function dateText(value) { return value ? String(value).slice(0, 10) : '—' }
function dateTime(value) { return value ? String(value).replace('T', ' ').slice(0, 16) : '—' }
function statusText(value) { return value === 0 ? '待返现' : value === 1 ? '已返现' : value === 2 ? '已作废' : '未知' }
function statusClass(value) { return value === 0 ? 'pending' : value === 1 ? 'paid' : 'void' }
function selectable(row) { return row.status === 0 }

function fmtDate(d) {
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function buildQuery() {
  return {
    pageNum: query.pageNum,
    pageSize: query.pageSize,
    keyword: query.keyword?.trim() || undefined,
    status: query.status,
    beginTime: dateRange.value?.[0] || undefined,
    endTime: dateRange.value?.[1] || undefined
  }
}

function loadList() {
  loading.value = true
  listReferralRewards(buildQuery()).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function loadSummary() {
  summaryLoading.value = true
  getReferralSummary(buildQuery()).then((res) => Object.assign(summary, res.data || {})).finally(() => { summaryLoading.value = false })
}
function refresh() { loadList(); loadSummary() }
function search() { query.pageNum = 1; refresh() }
function resetQuery() {
  query.keyword = ''
  query.status = undefined
  dateRange.value = []
  query.pageNum = 1
  refresh()
}
function onSelectionChange(list) { selection.value = list }

/* ---------------- 返点设置 ---------------- */
function openRates() {
  rateOpen.value = true
  rateLoading.value = true
  listReferralRates().then((res) => {
    rateRows.value = (res.data || []).map((it) => ({ ...it, rate: Number(it.rate || 0), remark: it.remark || '' }))
  }).finally(() => { rateLoading.value = false })
}
function saveRates() {
  const items = rateRows.value.map((it) => ({ styleItemId: it.styleItemId, rate: Number(it.rate || 0), remark: it.remark || null }))
  rateSaving.value = true
  saveReferralRates({ items }).then(() => {
    ElMessage.success('返点设置已保存')
    rateOpen.value = false
    refresh()
  }).finally(() => { rateSaving.value = false })
}

/* ---------------- 区间结算 ---------------- */
function applyQuick(days) {
  const end = new Date()
  let begin = new Date()
  if (days === 0) begin = new Date(end.getFullYear(), end.getMonth(), 1)
  else begin.setDate(end.getDate() - (days - 1))
  settleRange.value = [fmtDate(begin), fmtDate(end)]
  loadPending()
}
function openSettle() {
  settleOpen.value = true
  payoutRemark.value = ''
  if (!settleRange.value?.length) applyQuick(30)
  else loadPending()
}
function loadPending() {
  if (!settleRange.value?.length) { pendingRows.value = []; return }
  pendingLoading.value = true
  listReferralRewards({
    pageNum: 1,
    pageSize: 500,
    status: 0,
    beginTime: settleRange.value[0],
    endTime: settleRange.value[1]
  }).then((res) => {
    pendingRows.value = (res.data?.result || []).map((it) => ({ ...it, rewardAmount: Number(it.rewardAmount || 0) }))
  }).finally(() => { pendingLoading.value = false })
}
function submitSettle() {
  if (!pendingRows.value.length) return ElMessage.warning('当前区间没有待返现记录')
  payoutSaving.value = true
  payoutReferralRewards({
    beginTime: settleRange.value[0],
    endTime: settleRange.value[1],
    payoutRemark: payoutRemark.value,
    adjusts: pendingRows.value.map((it) => ({ id: it.id, rewardAmount: Number(it.rewardAmount || 0), remark: it.remark || null }))
  }).then((res) => {
    const data = res.data || {}
    ElMessage.success(`已结算 ${data.count || 0} 条，合计 ¥${money(data.amount)}`)
    settleOpen.value = false
    selection.value = []
    refresh()
  }).finally(() => { payoutSaving.value = false })
}
function editAmount(row) {
  amountRow.id = row.id
  amountRow.referrerName = row.referrerName || ''
  amountRow.originalAmount = Number(row.originalAmount || row.rewardAmount || 0)
  amountRow.rewardAmount = Number(row.rewardAmount || 0)
  amountRemark.value = ''
  amountOpen.value = true
}
function saveAmount() {
  const amount = Number(amountRow.rewardAmount || 0)
  if (amount !== Number(amountRow.originalAmount || 0)) {
    ElMessageBox.confirm(`这笔返现系统按规则是 ¥${money(amountRow.originalAmount)}，当前改为 ¥${money(amount)}。调整后仍保持「待返现」，可在结算时一起结清。`, '确认调整金额')
      .then(() => adjustReferralAmount(amountRow.id, { rewardAmount: amount, remark: amountRemark.value || null }))
      .then(() => {
        ElMessage.success('金额已调整，仍待结算')
        amountOpen.value = false
        refresh()
      })
      .catch(() => {})
    return
  }
  adjustReferralAmount(amountRow.id, { rewardAmount: amount, remark: amountRemark.value || null }).then(() => {
    ElMessage.success('金额未变化，已保存说明')
    amountOpen.value = false
    refresh()
  })
}
function cancelOne(row) {
  ElMessageBox.confirm('撤销这条结算？记录会回到「待返现」，不会删除，金额保留。', '撤销结算')
    .then(() => cancelReferralPayout(row.id))
    .then(() => { ElMessage.success('已撤销'); refresh() })
    .catch(() => {})
}

function openGraph(memberId) {
  if (!memberId) return
  graphLoading.value = true
  getReferralGraph(memberId).then((res) => { graph.value = res.data || null }).finally(() => { graphLoading.value = false })
}

function exportCsv() {
  if (!rows.value.length) return ElMessage.warning('当前没有可导出的记录')
  const header = ['推荐人', '推荐人微信', '被推荐人', '被推荐人手机', '来源单号', '成交金额', '返点构成', '综合返点(%)', '系统返现', '实际返现', '是否调整', '状态', '业务时间', '结算批次', '结算时间', '结算人', '发放说明']
  const lines = rows.value.map((row) => [
    row.referrerName || '', row.referrerWechatNo || '', row.referralName || '', row.referralPhone || '',
    row.saleOrderNo || '', row.orderAmount ?? 0, row.styleSummary || '', row.rewardRate ?? 0,
    row.originalAmount ?? 0, row.rewardAmount ?? 0, row.isAdjusted === 1 ? '是' : '否',
    statusText(row.status), dateTime(row.bizTime), row.settleBatchNo || '',
    row.payoutTime ? dateTime(row.payoutTime) : '', row.payoutBy || '', row.payoutRemark || ''
  ])
  const content = [header, ...lines]
    .map((line) => line.map((cell) => `"${String(cell).replace(/"/g, '""')}"`).join(','))
    .join('\r\n')
  const blob = new Blob(['\uFEFF' + content], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `静悦转介绍奖励_${new Date().toISOString().slice(0, 10)}.csv`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

onMounted(refresh)
</script>

<style scoped lang="scss">
.jy-referral { color: #3f3136; }
.ref-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 20px; padding: 18px 22px; background: linear-gradient(120deg, #fdf3f5 0%, #fffaf8 62%, #f7e7ea 100%); border: 1px solid #f2e1e4; border-radius: 12px; }
.eyebrow { color: #b76e79; font-size: 11px; font-weight: 700; letter-spacing: 2.4px; }
.head-copy h1 { margin: 6px 0 5px; color: #4a333a; font-size: 22px; font-weight: 600; }
.head-copy p { max-width: 760px; margin: 0; color: #8f767d; font-size: 12.5px; line-height: 1.8; }
.head-actions { display: flex; flex: none; gap: 8px; }

.metric-row { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 10px; margin: 12px 0 14px; }
.metric-row article { padding: 13px 16px; background: #fff; border: 1px solid #f3e6e8; border-radius: 10px; }
.metric-row article.primary { background: linear-gradient(135deg, #fdf0f3, #fff); border-color: #f0d9de; }
.metric-row span { display: block; color: #ab949a; font-size: 11.5px; }
.metric-row strong { display: block; margin: 5px 0 3px; color: #543a42; font-size: 21px; font-weight: 600; font-variant-numeric: tabular-nums; }
.metric-row strong.warn { color: #b0752f; }
.metric-row strong.ok { color: #5d8169; }
.metric-row strong.muted { color: #9c8b90; }
.metric-row small { color: #b09ba0; font-size: 11px; line-height: 1.5; }

.ref-body { display: grid; grid-template-columns: minmax(0, 1fr) 330px; gap: 12px; align-items: start; }
.ref-main { min-width: 0; padding: 14px 16px; background: #fff; border: 1px solid #f0e2e5; border-radius: 10px; }
.toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; margin-bottom: 10px; }
.ref-table { width: 100%; --el-table-border-color: #f7edef; --el-table-header-bg-color: #fdf8f9; --el-table-row-hover-bg-color: #fffafb; }
.ref-table :deep(th) { height: 36px; color: #9c8289; font-size: 12px; font-weight: 600; }
.ref-table :deep(td) { padding: 8px 0; color: #6c565d; font-size: 12.5px; }
.cell-strong { color: #544149; font-size: 12.5px; }
.sub-line { color: #ab9499; font-size: 11px; line-height: 1.6; }
.money { color: #6f5a61; font-variant-numeric: tabular-nums; }
.reward { color: #a75f6e; font-variant-numeric: tabular-nums; }
.style-summary { color: #8a7379; font-size: 11.5px; line-height: 1.6; }
.link-name { padding: 0; color: #a75f6e; font-size: 12.5px; font-weight: 600; background: none; border: 0; cursor: pointer; }
.link-name:hover { text-decoration: underline; }
.status-tag { display: inline-block; padding: 2px 9px; font-size: 11.5px; border-radius: 99px; }
.status-tag.pending { color: #a4703a; background: #fbf3e8; }
.status-tag.paid { color: #5f7f6c; background: #eef5f0; }
.status-tag.void { color: #9b8f92; background: #f5f2f3; }
.list-foot { display: flex; align-items: center; justify-content: space-between; margin-top: 10px; color: #b09ba0; font-size: 11.5px; }
.empty-block { padding: 26px 0; text-align: center; }
.empty-block span { display: block; color: #8d757c; font-size: 13px; }
.empty-block small { display: block; margin-top: 5px; color: #b3a0a5; font-size: 11.5px; }

.graph-panel { padding: 14px 15px; background: #fff; border: 1px solid #f0e2e5; border-radius: 10px; }
.graph-head { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 10px; }
.graph-head span { color: #c5949e; font-size: 10.5px; font-weight: 700; letter-spacing: 1.6px; }
.graph-head h2 { margin: 3px 0 0; color: #4a383e; font-size: 15px; font-weight: 700; }
.graph-empty { padding: 24px 12px; color: #b09a9f; font-size: 12px; text-align: center; }
.graph-empty .el-icon { color: #d8bfc5; font-size: 24px; }
.graph-empty p { margin: 8px 0 0; line-height: 1.7; }
.graph-empty.compact { padding: 10px 0; text-align: left; }
.chain.up { position: relative; padding-left: 12px; margin-bottom: 9px; }
.chain-node { display: flex; align-items: baseline; gap: 8px; padding: 6px 9px; margin-bottom: 5px; background: #fbf6f7; border: 1px dashed #f0dde1; border-radius: 6px; }
.chain-tag { color: #b98d95; font-size: 10.5px; }
.chain-node b { color: #594449; font-size: 12.5px; }
.chain-node small { color: #ab9499; font-size: 11px; }
.chain-line { position: absolute; top: 12px; bottom: 14px; left: 2px; width: 1px; background: #f0dde1; }
.focus-node { padding: 12px 13px; background: linear-gradient(135deg, #fdf3f5, #fff); border: 1px solid #f1dfe3; border-radius: 8px; }
.focus-badge { display: inline-block; padding: 1px 7px; margin-bottom: 4px; color: #b76e79; font-size: 10.5px; background: #fbecf0; border-radius: 3px; }
.focus-main b { display: block; color: #4a333a; font-size: 15px; }
.focus-main small { color: #a9929a; font-size: 11px; }
.focus-metrics { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px; margin-top: 9px; }
.focus-metrics div { display: flex; flex-direction: column; gap: 2px; }
.focus-metrics span { color: #ab9499; font-size: 10.5px; }
.focus-metrics strong { color: #5b464d; font-size: 13px; font-variant-numeric: tabular-nums; }
.focus-orders { margin-top: 10px; padding-top: 8px; border-top: 1px dashed #f0dee2; }
.orders-title { margin-bottom: 4px; color: #b18f96; font-size: 10.5px; }
.order-line { display: flex; justify-content: space-between; gap: 8px; padding: 3px 0; font-size: 11.5px; }
.order-line span { color: #6b555b; }
.order-line small { flex: 1; color: #b09ba0; text-align: center; }
.order-line b { color: #a75f6e; font-variant-numeric: tabular-nums; }
.down-block { margin-top: 12px; }
.down-title { margin-bottom: 6px; color: #8d757c; font-size: 12px; }
.down-title b { color: #b76e79; }
.down-row { display: flex; align-items: center; gap: 8px; padding: 6px 9px; margin-bottom: 5px; background: #fdfafb; border: 1px solid #f5eaec; border-radius: 6px; }
.down-main { flex: 1; min-width: 0; }
.down-main b { display: block; color: #594449; font-size: 12.5px; }
.down-main small { color: #ab9499; font-size: 10.5px; }
.down-values { display: flex; flex-direction: column; align-items: flex-end; gap: 1px; color: #a18b91; font-size: 10.5px; white-space: nowrap; }

.dialog-tip { margin: 0 0 10px; color: #8d767d; font-size: 12.5px; line-height: 1.75; }
.dialog-tip b { color: #a75f6e; }
.rate-table :deep(th), .settle-table :deep(th) { color: #9c8289; font-size: 12px; background: #fdf8f9; }
.rate-input { display: flex; align-items: center; gap: 4px; }
.rate-input span { color: #a18b91; font-size: 12px; }
.settle-head { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 10px; margin-bottom: 10px; }
.settle-range { display: flex; align-items: center; gap: 8px; color: #8d767d; font-size: 12.5px; }
.settle-quick { display: flex; gap: 4px; }
.settle-quick button { padding: 4px 10px; color: #a06874; font-size: 11.5px; background: #fdf4f6; border: 1px solid #f1dde2; border-radius: 5px; cursor: pointer; }
.settle-quick button:hover { background: #fbeaee; }
.settle-total { display: flex; align-items: center; justify-content: space-between; padding: 9px 4px 4px; color: #a18b91; font-size: 12px; }
.settle-total b { color: #a75f6e; font-size: 16px; }
.settle-form { margin-top: 4px; }

@media (max-width: 1400px) { .ref-body { grid-template-columns: 1fr; } }
@media (max-width: 900px) {
  .metric-row { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .ref-head { flex-direction: column; }
  .head-actions { flex-wrap: wrap; }
}
</style>
