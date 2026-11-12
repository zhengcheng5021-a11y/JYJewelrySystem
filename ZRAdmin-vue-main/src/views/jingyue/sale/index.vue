<template>
  <div class="jy-sale">
    <header class="sale-topbar">
      <div class="topbar-copy">
        <span class="eyebrow">JEWELLERY COUNTER</span>
        <div class="title-line">
          <h1>柜台销售</h1>
          <el-tag v-if="form.id" :type="form.status === 0 ? 'warning' : 'success'" effect="plain" round>{{ form.status === 0 ? '草稿' : '已完成' }}</el-tag>
        </div>
              </div>
      <div class="topbar-stats">
        <div><span>今日成交</span><b>¥{{ money(dashboard.todayAmount) }}</b></div>
        <div><span>今日订单</span><b>{{ dashboard.todayOrders || 0 }}<em>单</em></b></div>
        <div><span>客单价</span><b>¥{{ money(dashboard.avgTicket) }}</b></div>
      </div>
      <div class="topbar-actions">
        <el-button plain @click="router.push('/business/sale-records')"><el-icon><Tickets /></el-icon>开单记录</el-button>
        <el-button type="primary" @click="newSale()"><el-icon><Plus /></el-icon>新开一单</el-button>
      </div>
    </header>

    <div class="sale-workspace">
      <main class="sale-main">
        <section class="summary-card">
          <div class="summary-fields">
            <label class="field customer-field">
              <span>客户姓名 <em class="enter-hint">Enter 识别</em></span>
              <el-input v-model="form.customerName" :prefix-icon="User" clearable maxlength="50" placeholder="输入姓名后回车" @keyup.enter="identifyCustomer('name')" @input="onCustomerInput" />
            </label>
            <label class="field customer-field">
              <span>手机号码 <em class="enter-hint">Enter 识别</em></span>
              <el-input v-model="form.customerPhone" :prefix-icon="Phone" clearable maxlength="30" placeholder="输入手机号后回车" @keyup.enter="identifyCustomer('phone')" @input="onCustomerInput" />
            </label>
            <label class="field customer-field">
              <span>本人微信号 <em class="enter-hint">Enter 识别</em></span>
              <el-input v-model="form.customerWechatNo" :prefix-icon="ChatDotRound" clearable maxlength="64" placeholder="输入微信号后回车" @keyup.enter="identifyCustomer('wechat')" @input="onCustomerInput" />
            </label>
            <label v-if="customerState === 'member'" class="field customer-field referrer-field">
              <span>本次推荐人 <em class="enter-hint">选填 · 不选不生成返点</em></span>
              <el-select v-model="form.referrerMemberId" clearable filterable placeholder="可选，不选则不生成返点" @change="onReferrerChange">
                <el-option v-for="item in memberReferrers" :key="item.referrerMemberId"
                  :label="(item.referrerName || item.referrerWechatNo) + (item.isPrimary === 1 ? '（默认）' : '')"
                  :value="item.referrerMemberId" />
              </el-select>
            </label>
            <label v-else-if="customerState === 'new'" class="field customer-field">
              <span>推荐人微信 <em class="enter-hint">选填 · 不填不生成返点</em></span>
              <el-input v-model="form.referrerWechatNo" :prefix-icon="Share" clearable maxlength="64" placeholder="介绍人微信，不填则不生成返点" />
            </label>
            <label class="field birthday-field">
              <span>生日</span>
              <el-input v-model="form.birthdayText" clearable maxlength="10" placeholder="1988-05-20 或 05-20" @change="parseBirthday" @keyup.enter="parseBirthday" />
            </label>
            <label class="field sex-field">
              <span>性别</span>
              <el-radio-group v-model="form.customerSex" size="small">
                <el-radio-button :value="0">未知</el-radio-button>
                <el-radio-button :value="1">先生</el-radio-button>
                <el-radio-button :value="2">女士</el-radio-button>
              </el-radio-group>
            </label>
            <label class="field remark-field">
              <span>单据备注</span>
              <el-input v-model="form.remark" maxlength="200" placeholder="例如：婚嫁套装，客户自取" />
            </label>
          </div>

          <div class="summary-side">
            <div class="summary-meta">
              <div><span>单据编号:<b>{{ form.orderNo || '保存后生成' }}</b></span><span>开单时间:<b>{{ form.dealTime }}</b></span></div>
            </div>
            <div class="identity-chip" :class="customerState">
            <el-icon><WarnTriangleFilled /></el-icon>
              <b>{{ identityText }}</b>
              <small v-if="memberInfo">{{ memberInfo.levelName || '未分级' }} · 积分 {{ number(memberInfo.pointsBalance) }}</small>
              <small v-else-if="customerState === 'new'">确认收款后写入会员名册</small>
              <small v-else>姓名或手机号回车识别</small>
            </div>
            <p class="identity-hint" :class="{ warn: !customerReady }">
              <!-- <el-icon><WarnTriangleFilled /></el-icon> -->
              <!-- <span>{{ customerReady ? '客户已确认，可录入商品' : '必须先识别客户，才能扫码录入商品' }}</span> -->
            </p>
          </div>
        </section>
        <section class="scan-bar">
          <el-input ref="scanInputRef" v-model="scanKeyword" size="large" clearable :disabled="!customerReady" :placeholder="customerReady ? '扫描商品条码 / 输入货品编码、名称后回车' : '请先识别客户，再扫描商品'" :prefix-icon="Search" @keyup.enter="handleScan" />
          <!-- <div class="scan-tip"><el-icon><InfoFilled /></el-icon>条码或货品编码精确命中会直接加入明细；多条候选会弹出选择。</div> -->
        </section>

        <section class="lines-card">
          <div class="lines-head">
            <div class="lines-title"><span class="section-kicker">ITEMS</span><h2>商品明细<em>{{ lines.length }} 件</em></h2></div>
            <label class="more-attributes-control"><span>更多属性</span><el-switch v-model="showMoreAttributes" aria-label="显示更多属性" /></label>
          </div>
          <div class="line-table-wrap">
          <el-table v-loading="loading" :data="lines" class="line-table" height="100%" row-key="goodsItemId" empty-text="扫描条码开始录入本单商品">
            <el-table-column type="index" label="#" width="48" align="center" fixed="left" />
            <el-table-column label="货品" width="250" fixed="left">
              <template #default="{ row }">
                <div class="goods-cell">
                  <span class="goods-gem"><el-icon><Gem /></el-icon></span>
                  <div><b>{{ row.styleName || row.goodsName }}</b><small>{{ codeText(row) }}</small></div>
                </div>
              </template>
            </el-table-column>
            <el-table-column label="材质 / 分类 / 成色" min-width="176">
              <template #default="{ row }">
                <div class="meta-cell"><span>{{ materialGroupText(row.materialGroup) }} · {{ row.styleItemName || '未分类' }}</span><small>{{ row.materialName || '未设材质' }} · {{ row.metalName || '未设成色' }}</small></div>
              </template>
            </el-table-column>
            <el-table-column label="计价 / 工费" width="150" align="center">
              <template #default="{ row }">
                <div class="pricing-cell"><b>{{ pricingLabel(row) }}</b><small>{{ laborText(row) }} · 包装 {{ money(row.packagingFee) }}/件</small></div>
              </template>
            </el-table-column>
            <el-table-column label="销售维护价/克" width="136" align="center">
              <template #default="{ row }"><el-input-number v-if="Number(row.pricingMode) === 2" v-model="row.marketMetalPricePerGram" :min="0" :precision="2" size="small" controls-position="right" @change="onMarketPriceChange(row)" /><span v-else class="muted-price">¥{{ money(row.salePrice) }}/件</span></template>
            </el-table-column>
            <el-table-column v-if="showMoreAttributes" label="销售数量 / 单位" width="154" align="center">
              <template #default="{ row }">
                <div class="sale-quantity-cell"><b>1</b><el-select v-model="row.unit" class="line-unit-select" size="small" filterable allow-create default-first-option @change="schedulePreview">
                  <el-option v-for="unit in UNIT_OPTIONS" :key="unit" :label="unit" :value="unit" />
                </el-select></div>
              </template>
            </el-table-column>
            <el-table-column v-if="showMoreAttributes" label="库存属性数量" width="132" align="center">
              <template #default="{ row }">
                <el-input-number v-model="row.stockQty" :min="0.01" :max="100000" :precision="2" :step="1" size="small" controls-position="right" />
              </template>
            </el-table-column>
            <el-table-column label="重量 (g)" width="120" align="center">
              <template #default="{ row }">
                <el-input-number v-model="row.weight" class="line-weight-input" :min="0" :precision="2" :step="0.1" size="small" controls-position="right" @change="schedulePreview" />
              </template>
            </el-table-column>
            <el-table-column label="实际成交金价/克" width="152" align="center">
              <template #default="{ row }">
                <el-input-number v-model="row.dealPrice" :min="0" :precision="2" size="small" controls-position="right" :disabled="row.isGift === 1 || Number(row.pricingMode) !== 2" @change="onDealPriceChange(row)" />
                <small v-if="Number(row.pricingMode) !== 2" class="muted-price">¥{{ money(row.dealPrice) }}/件</small>
              </template>
            </el-table-column>
            <el-table-column label="标价" width="110" align="right">
              <template #default="{ row }"><span class="line-amount">¥{{ money(rowCalc(row).listAmount) }}</span></template>
            </el-table-column>
            <el-table-column label="会员折扣" width="100" align="right">
              <template #default="{ row }"><span class="line-discount">-¥{{ money(rowCalc(row).memberDiscountAmount || 0) }}</span></template>
            </el-table-column>
            <el-table-column label="行实付" width="110" align="right">
              <template #default="{ row }"><b class="line-pay">¥{{ money(rowCalc(row).payAmount || lineAmount(row)) }}</b></template>
            </el-table-column>
            <el-table-column label="赠品" width="66" align="center">
              <template #default="{ row }"><el-switch v-model="row.isGift" :active-value="1" :inactive-value="0" size="small" @change="onGiftChange(row)" /></template>
            </el-table-column>
            <el-table-column label="操作" width="76" align="center" fixed="right">
              <template #default="{ row, $index }">
                <el-button link type="danger" title="移除" @click="removeLine($index)"><el-icon><Delete /></el-icon></el-button>
              </template>
            </el-table-column>
          </el-table>
          </div>
        </section>
      </main>

      <aside class="settle-rail">
        <section class="settle-card">
          <div class="settle-head"><span class="section-kicker">CHECKOUT</span><h2>结算核对</h2></div>
          <div class="amount-list">
            <div><span>商品成交价合计</span><b>¥{{ money(calc.totalAmount) }}</b></div>
            <el-checkbox v-if="memberInfo && form.orderType === 1" v-model="form.applyMemberDiscount" class="level-discount-check" :disabled="form.status === 1" @change="onDiscountToggle">按会员等级折扣</el-checkbox>
            <div v-if="calc.memberDiscount > 0" class="discount"><span>会员等级折扣<em>{{ calc.levelName }} {{ rateText(calc.discountRate) }}</em></span><b>-¥{{ money(calc.memberDiscount) }}</b></div>
            <div v-if="calc.activityBenefit > 0" class="discount"><span>活动优惠<em>{{ calc.activityName }}</em></span><b>-¥{{ money(calc.activityBenefit) }}</b></div>
            <div class="base-pay"><span>折后金额</span><b>¥{{ money(calc.basePayAmount) }}</b></div>
          </div>

          <div class="adjust-block">
            <label>
              <span>手工调价</span>
              <el-input-number v-model="form.adjustAmount" :precision="2" :step="10" size="small" controls-position="right" @change="onAmountChange" />
              <small>正数加价，负数减价，单独留痕</small>
            </label>
          </div>

          <div class="offset-block">
            <div class="coupon-head"><span>定金对冲</span><b v-if="offsetTotal">-¥{{ money(offsetTotal) }}</b></div>
            <div v-if="customs.length" class="offset-list">
              <div v-for="row in customs" :key="offsetKey(row)" class="offset-row">
                <div><b>{{ row.customNo }}</b><small>{{ row.goodsName }} · 定金商品 · 可冲 ¥{{ money(row.cap) }}</small><el-tag v-if="row.bound" size="small" type="success" effect="plain">已带商品</el-tag></div>
                <el-input-number v-model="row.offsetAmount" :min="0" :max="Number(row.cap || 0)" :precision="2" :step="100" size="small" controls-position="right" @change="onOffsetChange(row)" />
              </div>
            </div>
            <p v-else>识别客户后，未用完的定制定金会出现在这里，可按本单实付对冲。</p>
          </div>

          <div class="coupon-block">
            <div class="coupon-head"><span>优惠券</span><el-tag type="info" effect="plain" size="small">规则待定</el-tag></div>
            <p>按款式分类自动匹配券种的算法尚未确定，本期暂不开通。</p>
          </div>

          <div class="pay-panel">
            <div class="pay-line"><span>抹零前应收</span><b>¥{{ money(beforeRoundAmount) }}</b></div>
            <div class="pay-line"><span>抹零金额（自动）</span><b class="round-off">-¥{{ money(roundOffAmount) }}</b></div>
            <div class="pay-line receivable"><span>应收金额</span><b>¥{{ money(receivableAmount) }}</b></div>
            <div class="pay-line cash-input-line">
              <span>实收金额</span>
              <el-input-number v-model="form.cashInput" :min="0" :max="receivableAmount" :precision="2" :step="1" size="small" controls-position="right" :disabled="form.status === 1" @change="onCashInputChange" />
            </div>
            <div v-if="offsetTotal > 0" class="pay-line"><span>定金对冲</span><b>-¥{{ money(Math.min(offsetTotal, receivableAmount)) }}</b></div>
            <div class="pay-total">
              <span>本单现金实收</span>
              <strong>¥{{ money(cashAmount) }}</strong>
            </div>
            <small v-if="calc.pointsEarned > 0">按商品实付预计获得 {{ number(calc.pointsEarned) }} 积分</small>
            <small v-else>积分按商品实付计算，定金对冲不另计积分</small>
          </div>

          <div class="settle-actions">
            <el-button plain :loading="saving" :disabled="confirming" @click="saveDraft">保存草稿</el-button>
            <el-button type="primary" :loading="confirming" :disabled="!lines.length || saving" @click="confirmPay">确认收款 <kbd>Ctrl + Enter</kbd></el-button>
          </div>

          <p class="settle-note" :class="{ warn: adjustWarning || offsetOverflow }">
            <el-icon><InfoFilled /></el-icon>
            <span v-if="offsetOverflow">定金对冲不能超过本次实收金额，请先调整对冲金额。</span>
            <span v-else-if="adjustWarning">调价或抹零不为 0，保存时会留痕，请与客户确认。</span>
            <span v-else>确认收款后扣库存并累计积分。定金只冲减现金实收，不改变商品成交额。</span>
          </p>
        </section>
      </aside>
    </div>

    <el-dialog v-model="payDialogOpen" title="确认收款" width="460px" append-to-body class="pay-confirm-dialog" :close-on-click-modal="false">
      <div class="pay-confirm-body">
        <div class="pay-confirm-row"><span>商品成交价合计</span><b>¥{{ money(calc.totalAmount) }}</b></div>
        <div v-if="calc.memberDiscount > 0" class="pay-confirm-row discount-row"><span>会员等级折扣</span><b>-¥{{ money(calc.memberDiscount) }}</b></div>
        <div v-if="calc.activityBenefit > 0" class="pay-confirm-row discount-row"><span>活动优惠</span><b>-¥{{ money(calc.activityBenefit) }}</b></div>
        <div class="pay-confirm-row"><span>折后金额</span><b>¥{{ money(calc.basePayAmount) }}</b></div>
        <div v-if="Number(form.adjustAmount || 0)" class="pay-confirm-row"><span>手工调价</span><b>{{ Number(form.adjustAmount) > 0 ? '+' : '' }}¥{{ money(form.adjustAmount) }}</b></div>
        <div class="pay-confirm-row round-row"><span>抹零金额</span><b>-¥{{ money(roundOffAmount) }}</b></div>
        <div class="pay-confirm-row receivable-row"><span>应收金额</span><b>¥{{ money(receivableAmount) }}</b></div>
        <div class="pay-confirm-row received-row"><span>实收金额</span><b>¥{{ money(form.cashInput) }}</b></div>
        <div v-if="offsetTotal > 0" class="pay-confirm-row deposit-row"><span>定金对冲</span><b>-¥{{ money(Math.min(offsetTotal, receivableAmount)) }}</b></div>
        <div class="pay-confirm-row cash-row"><span>本单现金实收</span><b>¥{{ money(cashAmount) }}</b></div>
        <p v-if="adjustWarning" class="pay-confirm-warning">本单包含调价或抹零，确认后将保留调整记录。</p>
        <p class="pay-confirm-hint">按 Enter 确认收款，按 Esc 返回核对。</p>
      </div>
      <template #footer>
        <el-button :disabled="confirming" @click="payDialogOpen = false">再核对一下</el-button>
        <el-button type="primary" :loading="confirming" @click="submitConfirmedPay">确认收款</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="customerDialog" title="选择会员" width="520px" append-to-body class="candidate-dialog">
      <p class="dialog-hint">找到 {{ customerCandidates.length }} 位可能匹配的会员，请确认是哪一位。</p>
      <div class="candidate-list">
        <button v-for="item in customerCandidates" :key="item.memberId" type="button" class="candidate-row" :class="{ active: customerPick === item.memberId }" @click="customerPick = item.memberId">
          <span class="candidate-avatar"><el-icon><Avatar /></el-icon></span>
          <span class="candidate-copy"><b>{{ item.memberName }}</b><small>{{ item.memberNo }} · {{ item.phone || '无手机号' }}</small></span>
          <em>{{ item.levelName }}</em>
        </button>
      </div>
      <template #footer>
        <el-button plain @click="customerDialog = false">取消</el-button>
        <el-button type="primary" :disabled="!customerPick" @click="confirmCustomerPick">确认为该会员</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="goodsDialog" title="选择商品" width="720px" append-to-body class="candidate-dialog">
      <p class="dialog-hint">找到 {{ goodsCandidates.length }} 件候选商品，点击一行加入明细。</p>
      <div class="goods-candidate-list">
        <button v-for="item in goodsCandidates" :key="item.id" type="button" class="goods-candidate" @click="pickGoods(item)">
          <span class="goods-gem"><el-icon><Gem /></el-icon></span>
          <span class="candidate-copy"><b>{{ item.styleName }}</b><small>{{ codeText(item) }}</small></span>
          <span class="candidate-meta">{{ item.styleItemName || '未分类' }} · {{ item.metalName || '未设成色' }}</span>
          <b class="candidate-price">¥{{ money(item.salePrice) }}</b>
        </button>
      </div>
    </el-dialog>
  </div>
</template>

<script setup name="JySale">
import { ref, reactive, computed, onMounted, onBeforeUnmount, nextTick, watch } from 'vue'
import { useRouter, useRoute, onBeforeRouteLeave } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { getSale, previewSale, saveSale, confirmSale, matchSaleCustomer, findSaleGoods, getSaleDashboard } from '@/api/jingyue/sale'
import { listAvailableCustom } from '@/api/jingyue/custom'
import { getMember } from '@/api/jingyue/member'
import { serverDateTimeText } from '@/utils/serverTime'

const router = useRouter()
const route = useRoute()

const loading = ref(false)
const saving = ref(false)
const confirming = ref(false)
const payDialogOpen = ref(false)
const submitting = computed(() => saving.value || confirming.value)
const scanKeyword = ref('')
const scanInputRef = ref()
const lines = ref([])
const customs = ref([])
const memberInfo = ref(null)
const customerState = ref('idle')
const customerDialog = ref(false)
const customerCandidates = ref([])
const customerPick = ref(0)
const goodsDialog = ref(false)
const goodsCandidates = ref([])
const preview = ref(null)
const showMoreAttributes = ref(false)
const dashboard = reactive({ todayAmount: 0, todayOrders: 0, todayPieces: 0, avgTicket: 0 })
const UNIT_OPTIONS = ['条', '颗', '对', '件', '套']

const form = reactive({
  id: 0,
  orderNo: '',
  status: 0,
  dealTime: serverDateTimeText().slice(0, 16),
  orderType: 1,
  memberId: null,
  customerName: '',
  customerPhone: '',
  customerWechatNo: '',
  referrerWechatNo: '',
  referrerMemberId: null,
  customerSex: 0,
  birthdayText: '',
  birthday: null,
  birthYear: null,
  remark: '',
  adjustAmount: 0,
  roundOffAmount: 0,
  cashInput: 0,
  cashTouched: false,
  applyMemberDiscount: true
})

let previewTimer = null
let previewSeq = 0
let lineStockSeq = 0
let customerSeq = 0
let currentLoadSeq = 0
let draftSeq = 0
let scanBusy = false
// 单据代次：新开单或离开页面后递增，旧请求即使晚返回也不得污染当前单据。
let saleEpoch = 0
const saleDraftKey = 'jy.sale.draft.v1'
let saleDraftTimer = null

const saleHasInput = computed(() => Boolean(
  form.id || form.customerName || form.customerPhone || form.customerWechatNo || form.referrerWechatNo
  || form.remark || scanKeyword.value || lines.value.length
))

function saleDraftSnapshot() {
  return {
    form: JSON.parse(JSON.stringify(form)),
    lines: JSON.parse(JSON.stringify(lines.value)),
    scanKeyword: scanKeyword.value,
    customs: JSON.parse(JSON.stringify(customs.value))
  }
}
function saveLocalSaleDraft() {
  if (!saleHasInput.value || form.status === 1) {
    localStorage.removeItem(saleDraftKey)
    return
  }
  localStorage.setItem(saleDraftKey, JSON.stringify(saleDraftSnapshot()))
}
function scheduleLocalSaleDraft() {
  if (saleDraftTimer) clearTimeout(saleDraftTimer)
  saleDraftTimer = setTimeout(saveLocalSaleDraft, 250)
}
async function restoreLocalSaleDraft() {
  const raw = localStorage.getItem(saleDraftKey)
  if (!raw || route.query.id) return
  let draft
  try { draft = JSON.parse(raw) } catch { localStorage.removeItem(saleDraftKey); return }
  if (!draft?.form || !((draft.lines || []).length || draft.form.customerName || draft.form.customerPhone)) return
  try {
    await ElMessageBox.confirm('发现上次未完成的销售开单，是否恢复？', '恢复销售草稿', { confirmButtonText: '恢复', cancelButtonText: '放弃', type: 'warning' })
    Object.assign(form, draft.form)
    lines.value = Array.isArray(draft.lines) ? draft.lines : []
    scanKeyword.value = draft.scanKeyword || ''
    customs.value = Array.isArray(draft.customs) ? draft.customs : []
    refreshLineStock()
    schedulePreview()
    ElMessage.success('已恢复未完成开单')
  } catch { localStorage.removeItem(saleDraftKey) }
}

function money(value) {
  const num = Number(value || 0)
  return num.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function number(value) {
  return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}
function materialGroupText(value) { return ({ 1: '足金类', 2: '足银类', 3: '其他类' }[Number(value)] || '其他类') }
function rateText(value) {
  const rate = Number(value || 1)
  if (!(rate > 0 && rate < 1)) return ''
  return (rate * 10).toFixed(1).replace(/\.0$/, '') + ' 折'
}
function lineAmount(row) {
  if (row.isGift === 1) return 0
  const material = Number(row.pricingMode) === 2
    ? Number(row.weight || 0) * (Number(row.dealPrice || 0) + Number(row.laborFee || 0))
    : Number(row.dealPrice || 0) * Number(row.qty || 0)
  const labor = Number(row.pricingMode) === 2 ? 0 : Number(row.laborFeeMode) === 2
    ? Number(row.weight || 0) * Number(row.laborFee || 0)
    : Number(row.laborFee || 0) * Number(row.qty || 0)
  return material + labor + Number(row.packagingFee || 0) * Number(row.qty || 0)
}
const previewItemMap = computed(() => {
  const map = new Map()
  const items = preview.value?.items || []
  for (let i = 0; i < items.length; i++) {
    map.set(Number(items[i].goodsItemId), items[i])
  }
  return map
})

function rowCalc(row) {
  const calcItem = previewItemMap.value.get(Number(row.goodsItemId))
  if (calcItem) {
    return {
      listAmount: Number(calcItem.listAmount || 0),
      memberDiscountAmount: Number(calcItem.memberDiscountAmount || 0),
      activityBenefit: Number(calcItem.activityBenefit || 0),
      payAmount: Number(calcItem.payAmount || 0),
      metalPricePerGram: Number(calcItem.metalPricePerGram || 0),
      marketMetalPricePerGram: Number(calcItem.marketMetalPricePerGram || 0),
      packagingAmount: Number(calcItem.packagingAmount || 0),
      dealPrice: Number(calcItem.dealPrice || 0)
    }
  }
  const fallback = lineAmount(row)
  return {
    listAmount: fallback,
    memberDiscountAmount: Number(row.memberDiscountAmount || 0),
    activityBenefit: Number(row.activityBenefit || 0),
    payAmount: Number(row.payAmount || 0) || fallback,
    metalPricePerGram: Number(row.metalPricePerGram || 0),
    marketMetalPricePerGram: Number(row.marketMetalPricePerGram || 0),
    packagingAmount: Number(row.packagingAmount || 0),
    dealPrice: Number(row.dealPrice || 0)
  }
}
function pricingLabel(row) {
  return Number(row.pricingMode) === 2 ? '按克' : '按件'
}
function laborText(row) {
  const fee = Number(row.laborFee || 0)
  if (!fee) return '无工费'
  return Number(row.laborFeeMode) === 2 ? number(fee) + ' 元/克' : number(fee) + ' 元/件'
}

const localTotal = computed(() => lines.value.reduce((sum, row) => sum + lineAmount(row), 0))
const calc = computed(() => {
  if (preview.value) {
    return {
      totalAmount: preview.value.totalAmount || 0,
      memberDiscount: preview.value.memberDiscount || 0,
      activityBenefit: preview.value.activityBenefit || 0,
      basePayAmount: preview.value.basePayAmount || 0,
      payAmount: Math.max(0, Number(preview.value.basePayAmount || 0) + Number(form.adjustAmount || 0) - Number(form.roundOffAmount || 0)),
      pointsEarned: preview.value.pointsEarned || 0,
      levelName: preview.value.memberLevelName || '',
      discountRate: preview.value.discountRate || 1,
      activityName: preview.value.activityName || ''
    }
  }
  const total = localTotal.value
  const base = total
  const pay = Math.max(0, base + Number(form.adjustAmount || 0))
  return {
    totalAmount: total,
    memberDiscount: 0,
    activityBenefit: 0,
    basePayAmount: base,
    payAmount: pay,
    pointsEarned: 0,
    levelName: memberInfo.value?.levelName || '',
    discountRate: memberInfo.value?.discountRate || 1,
    activityName: ''
  }
})
const adjustWarning = computed(() => Number(form.adjustAmount || 0) !== 0 || Number(form.roundOffAmount || 0) !== 0)
const offsetTotal = computed(() => customs.value.reduce((sum, row) => sum + Number(row.offsetAmount || 0), 0))
const beforeRoundAmount = computed(() => {
  const base = Number(calc.value.basePayAmount || 0)
  const adjust = Number(form.adjustAmount || 0)
  return Math.max(0, Number((base + adjust).toFixed(2)))
})
const roundOffAmount = computed(() => {
  if (!form.cashTouched) return 0
  const received = Number(form.cashInput)
  if (!Number.isFinite(received)) return 0
  return Math.max(0, Number((beforeRoundAmount.value - Math.max(0, received)).toFixed(2)))
})
const receivableAmount = computed(() => Math.max(0, Number((beforeRoundAmount.value - roundOffAmount.value).toFixed(2))))
const actualReceived = computed(() => Math.max(0, Number(form.cashInput || 0)))
const cashAmount = computed(() => Math.max(0, actualReceived.value - offsetTotal.value))
// 定金只冲减本单现金实收；实收可手工下调，因此对冲上限也必须跟随实收。
const offsetOverflow = computed(() => offsetTotal.value - actualReceived.value > 0.009)
const identityText = computed(() => customerState.value === 'member' ? '库内会员' : customerState.value === 'new' ? '新客户' : '未识别')
const customerReady = computed(() => customerState.value === 'member' || customerState.value === 'new')
const memberReferrers = ref([])

function codeText(row) {
  const goodsNo = row?.goodsNo || ''
  const barcode = row?.barcode || ''
  if (!barcode || barcode === goodsNo) return goodsNo || '无条码'
  return goodsNo ? `${goodsNo} · ${barcode}` : barcode
}

function pad2(value) {
  return String(value).padStart(2, '0')
}
/** 生日支持 1988-05-20 / 1988/5/20 / 05-20；未知年份按 1900 存，BirthYear 为空 */
function parseBirthday() {
  const raw = String(form.birthdayText || '').trim()
  if (!raw) {
    form.birthday = null
    form.birthYear = null
    return
  }
  const text = raw.replace(/[./年]/g, '-').replace(/月/g, '-').replace(/日/g, '')
  let year = null
  let month = null
  let day = null
  let match = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/)
  if (match) {
    year = Number(match[1]); month = Number(match[2]); day = Number(match[3])
  } else {
    match = text.match(/^(\d{1,2})-(\d{1,2})$/)
    if (match) { month = Number(match[1]); day = Number(match[2]) }
    else match = text.match(/^(\d{4})(\d{2})(\d{2})$/)
    if (!month && match) { year = Number(match[1]); month = Number(match[2]); day = Number(match[3]) }
  }
  if (!month || !day || month > 12 || day > 31) {
    ElMessage.warning('生日格式不正确，请输入 1988-05-20 或 05-20')
    return
  }
  const y = year && year > 1900 ? year : 1900
  form.birthday = `${y}-${pad2(month)}-${pad2(day)}`
  form.birthYear = year && year > 1900 ? year : null
}

function birthdayText(birthday, birthYear) {
  if (!birthday) return ''
  const text = String(birthday).slice(0, 10)
  if (birthYear && Number(birthYear) > 1900) return text
  return text.slice(5)
}

function buildPayload() {
  return {
    id: form.id,
    orderType: form.orderType,
    memberId: form.memberId || null,
    adjustAmount: Number(form.adjustAmount || 0),
    roundOffAmount: Number(roundOffAmount.value || 0),
    applyMemberDiscount: !!form.applyMemberDiscount,
    customerName: form.customerName?.trim() || null,
    customerPhone: form.customerPhone?.trim() || null,
    customerWechatNo: form.customerWechatNo?.trim() || null,
    referrerWechatNo: form.referrerWechatNo?.trim() || null,
    referrerMemberId: form.referrerMemberId || null,
    customerSex: form.customerSex || 0,
    customerBirthday: form.birthday || null,
    customerBirthYear: form.birthYear || null,
    remark: form.remark?.trim() || null,
    customOffsets: customs.value.filter((row) => Number(row.offsetAmount) > 0).map((row) => ({ customOrderId: row.id, customItemId: row.itemId, offsetAmount: Number(row.offsetAmount) })),
    items: lines.value.map((row) => ({
      goodsItemId: row.goodsItemId,
      qty: 1,
      stockQty: row.stockQty === null || row.stockQty === undefined ? null : Number(row.stockQty),
      dealPrice: row.isGift === 1 ? 0 : Number(row.dealPrice || 0),
      marketMetalPricePerGram: Number(row.marketMetalPricePerGram || 0),
      isGift: row.isGift === 1 ? 1 : 0,
      weight: row.weight === null || row.weight === '' || row.weight === undefined ? null : Number(row.weight),
      unit: String(row.unit || '').trim() || null
    }))
  }
}

function cancelPendingPreview() {
  if (previewTimer) clearTimeout(previewTimer)
  previewTimer = null
  previewSeq += 1
}
function schedulePreview() {
  cancelPendingPreview()
  preview.value = null
  const seq = previewSeq
  previewTimer = setTimeout(() => runPreview(seq), 320)
}
function onCheckoutHotkey(event) {
  if (payDialogOpen.value && event.key === 'Enter' && !event.ctrlKey && !event.altKey && !event.metaKey) {
    event.preventDefault()
    submitConfirmedPay()
    return
  }
  if (!event.ctrlKey || event.altKey || event.metaKey || event.key !== 'Enter') return
  if (form.status === 1) return
  const target = event.target
  if (target && ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
    target.blur?.()
  }
  event.preventDefault()
  confirmPay()
}
async function runPreview(seq) {
  const epoch = saleEpoch
  if (!lines.value.length) {
    if (epoch === saleEpoch && seq === previewSeq) preview.value = null
    return
  }
  try {
    const res = await previewSale(buildPayload())
    if (epoch !== saleEpoch || seq !== previewSeq) return
    preview.value = res.data || null
    // 金价维护值与本单实际成交值分开；未手工改价时实际价跟随维护价。
    const previewItems = preview.value?.items || []
    lines.value.forEach((row) => {
      if (Number(row.pricingMode) !== 2 || row.isGift === 1) return
      const item = previewItems.find((it) => Number(it.goodsItemId) === Number(row.goodsItemId))
      if (item) {
        row.marketMetalPricePerGram = Number(item.marketMetalPricePerGram || 0)
        if (!row.dealPriceTouched) row.dealPrice = row.marketMetalPricePerGram
        row.metalPricePerGram = Number(row.dealPrice || 0)
      }
    })
    // 预览返回后，实收默认跟随最新应收；用户改过实收则保留手工金额。
    if (!form.cashTouched) form.cashInput = Number(receivableAmount.value.toFixed(2))
  } catch (error) {
    if (epoch === saleEpoch && seq === previewSeq) preview.value = null
  }
}

function onDiscountToggle() {
  // 等级变化会改变折后金额，按新的折后应收重算抹零；实收保持收银员已输入的值。
  if (form.cashTouched) form.roundOffAmount = Number(roundOffAmount.value.toFixed(2))
  schedulePreview()
}
function onCashInputChange() {
  form.cashTouched = true
  const received = Math.max(0, Number(form.cashInput || 0))
  form.cashInput = Number(Math.min(received, receivableAmount.value).toFixed(2))
  form.roundOffAmount = Number(roundOffAmount.value.toFixed(2))
  // 实收变化会改变抹零，必须重新预览；cashTouched 防止预览返回时覆盖手工实收。
  schedulePreview()
}
function onReferrerChange(value) {
  const match = memberReferrers.value.find((it) => it.referrerMemberId === value)
  form.referrerWechatNo = match?.referrerWechatNo || ''
}
function onAmountChange() {
  if (!form.cashTouched) form.cashInput = Number(receivableAmount.value.toFixed(2))
  schedulePreview()
  if (adjustWarning.value) {
    ElMessage.warning('调价或抹零已生效，请与客户确认实付金额')
  }
}

function onCustomerInput() {
  form.memberId = null
  memberInfo.value = null
  memberReferrers.value = []
  form.referrerMemberId = null
  form.applyMemberDiscount = true
  customerState.value = 'idle'
  customs.value = []
  preview.value = null
}

/** 扫描枪以 Enter 提交，客户识别后继续把焦点交给商品扫描框。 */
function focusScanAfterCustomer() {
  nextTick(() => scanInputRef.value?.focus?.())
}

async function identifyCustomer(source) {
  const keyword = (source === 'phone' ? form.customerPhone : source === 'wechat' ? form.customerWechatNo : form.customerName)?.trim()
  if (!keyword) {
    ElMessage.warning(source === 'phone' ? '请先输入手机号' : source === 'wechat' ? '请先输入微信号' : '请先输入客户姓名')
    return
  }
  const epoch = saleEpoch
  try {
    const res = await matchSaleCustomer(keyword)
    if (epoch !== saleEpoch) return
    const data = res.data || {}
    const candidates = data.candidates || []
    if (data.exists && data.memberId) {
      await applyMember(data.memberId, epoch)
      if (epoch !== saleEpoch) return
      ElMessage.success(`已识别库内会员：${data.memberName || keyword}`)
      focusScanAfterCustomer()
      return
    }
    if (candidates.length === 1) {
      await applyMember(candidates[0].memberId, epoch)
      if (epoch !== saleEpoch) return
      ElMessage.success(`已识别库内会员：${candidates[0].memberName}`)
      focusScanAfterCustomer()
      return
    }
    if (candidates.length > 1) {
      customerCandidates.value = candidates
      customerPick.value = candidates[0].memberId
      customerDialog.value = true
      return
    }
    form.memberId = null
    memberInfo.value = null
    customerState.value = 'new'
    preview.value = null
    await loadCustoms(undefined, epoch)
    if (epoch !== saleEpoch) return
    ElMessage.info('库内未找到该客户，按新客户开单')
    focusScanAfterCustomer()
  } catch (error) {
    if (epoch === saleEpoch) customerState.value = 'idle'
  }
}

async function confirmCustomerPick() {
  if (!customerPick.value) return
  await applyMember(customerPick.value)
  customerDialog.value = false
  ElMessage.success('已绑定会员，自动套用等级折扣')
  focusScanAfterCustomer()
}

async function applyMember(memberId, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (epoch !== saleEpoch || seq !== customerSeq) return
    const member = res.data
    if (!member) throw new Error('会员信息不存在')
    memberInfo.value = member
    customerState.value = 'member'
    form.memberId = member.id
    form.customerName = member.memberName || form.customerName
    form.customerPhone = member.phone || form.customerPhone
    form.customerSex = member.sex ?? form.customerSex
    memberReferrers.value = member.referrers || []
    // 历史推荐关系只作为可选名单展示，本次是否产生返点由收银员明确选择。
    form.referrerMemberId = null
    form.referrerWechatNo = ''
    if (member.solarBirthday) {
      form.birthday = String(member.solarBirthday).slice(0, 10)
      form.birthYear = member.birthYear || null
      form.birthdayText = birthdayText(member.solarBirthday, member.birthYear)
    }
    await loadCustoms(undefined, epoch)
    if (epoch !== saleEpoch || seq !== customerSeq) return
    await autoBindCustomGoods(seq, epoch)
    if (epoch !== saleEpoch || seq !== customerSeq) return
    schedulePreview()
  } catch (error) {
    if (epoch === saleEpoch && seq === customerSeq) customerState.value = 'idle'
  }
}

/** 客户识别后，把已绑定该客户的定制定金对应商品直接带入明细，收款时自动冲抵 */
async function autoBindCustomGoods(seq = customerSeq, epoch = saleEpoch) {
  const pending = customs.value.filter((row) => row.goodsItemId && !lines.value.some((line) => line.goodsItemId === row.goodsItemId))
  if (!pending.length || epoch !== saleEpoch || seq !== customerSeq) return
  for (const row of pending) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (epoch !== saleEpoch || seq !== customerSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) addLine(found, true)
    } catch (error) {
      if (epoch !== saleEpoch || seq !== customerSeq) return
    }
  }
  if (epoch === saleEpoch && seq === customerSeq) {
    ElMessage.info('已带入该客户已付定金的商品，收款时自动冲抵定金')
    focusScanAfterCustomer()
  }
}

async function handleScan() {
  const keyword = scanKeyword.value?.trim()
  if (!keyword || scanBusy) return
  if (!customerReady.value) {
    ElMessage.warning('请先输入客户姓名或手机号并回车识别，再录入商品')
    scanKeyword.value = ''
    return
  }
  const epoch = saleEpoch
  scanBusy = true
  try {
    const res = await findSaleGoods(keyword, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
    if (epoch !== saleEpoch) return
    const rows = res.data || []
    if (!rows.length) {
      ElMessage.warning('没有找到可售商品，请检查条码或编码')
      return
    }
    if (rows.length === 1) {
      addLine(rows[0])
      scanKeyword.value = ''
      nextTick(() => scanInputRef.value?.focus?.())
      return
    }
    goodsCandidates.value = rows
    goodsDialog.value = true
  } finally {
    if (epoch === saleEpoch) scanBusy = false
  }
}

function pickGoods(item) {
  addLine(item)
  goodsDialog.value = false
  scanKeyword.value = ''
  nextTick(() => scanInputRef.value?.focus?.())
}

function addLine(goods, silent) {
  const exist = lines.value.find((row) => row.goodsItemId === goods.id)
  if (exist) {
    if (!silent) ElMessage.warning('该商品已在明细中，请调整库存属性数量、单位、重量或成交价')
    return
  }
  const maxQty = Number(goods.qty || 1)
  lines.value.push({
    goodsItemId: goods.id,
    goodsNo: goods.goodsNo,
    barcode: goods.barcode,
    styleName: goods.styleName,
    goodsName: goods.styleName,
    materialGroup: Number(goods.materialGroup || 3),
    styleItemName: goods.styleItemName,
    materialName: goods.materialName,
    metalName: goods.metalName,
    itemMode: goods.itemMode,
    unit: goods.unit || '件',
    stockQty: Number(goods.qty || 1),
    weight: goods.weight === null || goods.weight === undefined ? null : Number(goods.weight),
    salePrice: Number(goods.salePrice || 0),
    dealPrice: Number(goods.pricingMode) === 2 ? 0 : Number(goods.salePrice || 0),
    dealPriceTouched: false,
    qty: 1,
    maxQty: goods.itemMode === 1 ? 1 : Math.max(1, maxQty),
    isGift: 0,
    pricingMode: Number(goods.pricingMode || 1),
    laborFee: Number(goods.laborFee || 0),
    packagingFee: Number(goods.packagingFee || 0),
    laborFeeMode: Number(goods.laborFeeMode || 1),
    metalPricePerGram: 0,
    marketMetalPricePerGram: 0,
    memberDiscountAmount: 0,
    payAmount: 0
  })
  if (!silent) ElMessage.success(`已加入：${goods.styleName}`)
  schedulePreview()
}

function onGiftChange(row) {
  if (row.isGift === 1) {
    row.dealPrice = 0
    row.dealPriceTouched = false
  } else {
    row.dealPrice = Number(row.pricingMode) === 2 ? Number(row.marketMetalPricePerGram || 0) : Number(row.salePrice || 0)
    row.dealPriceTouched = false
  }
  schedulePreview()
}

function onDealPriceChange(row) {
  row.dealPriceTouched = true
  schedulePreview()
}

function onMarketPriceChange(row) {
  if (!row.dealPriceTouched) row.dealPrice = Number(row.marketMetalPricePerGram || 0)
  schedulePreview()
}

function removeLine(index) {
  lines.value.splice(index, 1)
  if (!lines.value.length) preview.value = null
  else schedulePreview()
}

async function persist(silent, reloadDetail = true) {
  if (!lines.value.length) {
    ElMessage.warning('请先扫描商品再保存')
    return 0
  }
  const epoch = saleEpoch
  const seq = draftSeq
  saving.value = true
  try {
    const res = await saveSale(buildPayload())
    if (epoch !== saleEpoch || seq !== draftSeq) return 0
    const id = Number(res.data || form.id || 0)
    if (!id) throw new Error('保存失败，未返回单据编号')
    form.id = id
    // 收款流程不能回填旧单据：确认成功后页面必须保持“新单”状态，
    // 否则异步详情响应会把已售商品重新写回明细。
    if (reloadDetail) {
      try {
        const detail = await getSale(id)
        if (epoch === saleEpoch && seq === draftSeq) await applyDetail(detail.data, epoch)
      } catch (error) {}
    }
    if (!silent) ElMessage.success('销售单已保存')
    return id
  } catch (error) {
    return 0
  } finally {
    if (epoch === saleEpoch) saving.value = false
  }
}

async function saveDraft() {
  if (submitting.value) return
  if (!customerReady.value) {
    ElMessage.warning('请先识别客户（姓名或手机号回车），再保存单据')
    return
  }
  const id = await persist(false, true)
  if (!id) return
  form.status = 0
  refreshDashboard()
}

async function confirmPay() {
  if (submitting.value) return
  if (payDialogOpen.value) return
  if (!customerReady.value) return ElMessage.warning('请先识别客户（姓名或手机号回车），再确认收款')
  if (!lines.value.length) return ElMessage.warning('请先扫描商品')
  if (Number(receivableAmount.value) < 0) return ElMessage.warning('应收金额不能为负数')
  if (Number(form.cashInput || 0) > Number(receivableAmount.value) + 0.009) return ElMessage.warning('实收金额不能大于应收金额')
  if (offsetOverflow.value) return ElMessage.warning('定金对冲不能超过本次实收金额')
  payDialogOpen.value = true
}

async function submitConfirmedPay() {
  if (!payDialogOpen.value || confirming.value) return
  payDialogOpen.value = false
  const epoch = saleEpoch
  confirming.value = true
  let id = 0
  try {
    id = await persist(true, false)
    if (!id || epoch !== saleEpoch) return
    await confirmSale(id)
    if (epoch !== saleEpoch) return
    // 新开单会递增 saleEpoch，所有旧查询、预览、客户识别和扫码请求同时失效。
    await newSale(true)
    ElMessage.success('收款完成，销售单已完成')
    refreshDashboard()
  } catch (error) {
  } finally {
    if (epoch === saleEpoch) confirming.value = false
  }
}

async function applyDetail(detail, epoch = saleEpoch) {
  if (!detail || epoch !== saleEpoch) return
  form.id = detail.id
  form.orderNo = detail.orderNo
  form.status = detail.status
  form.dealTime = detail.dealTime ? String(detail.dealTime).replace('T', ' ').slice(0, 16) : serverDateTimeText().slice(0, 16)
  form.orderType = detail.orderType || 1
  form.memberId = detail.memberId || null
  form.customerName = detail.customerName || detail.memberName || ''
  form.customerPhone = detail.customerPhone || ''
  form.customerWechatNo = detail.customerWechatNo || ''
  form.referrerMemberId = detail.referrerMemberId || null
  form.referrerWechatNo = detail.referrerWechatNo || ''
  form.customerSex = detail.customerSex ?? 0
  form.birthday = detail.customerBirthday || null
  form.birthYear = detail.customerBirthYear || null
  form.birthdayText = detail.customerBirthdayText || birthdayText(detail.customerBirthday, detail.customerBirthYear)
  form.remark = detail.remark || ''
  form.adjustAmount = Number(detail.adjustAmount || 0)
  form.applyMemberDiscount = !!detail.applyMemberDiscount
  form.roundOffAmount = Number(detail.roundOffAmount || 0)
  form.cashTouched = form.roundOffAmount > 0
  form.cashInput = Math.max(0, Number(detail.payAmount || 0))
  const savedOffsets = new Map((detail.offsets || []).map((item) => [offsetKey(item), Number(item.offsetAmount || 0)]))
  lines.value = (detail.items || []).map((item) => ({
    goodsItemId: item.goodsItemId,
    goodsNo: item.goodsNo,
    barcode: item.barcode,
    styleName: item.styleName || item.goodsName,
    goodsName: item.goodsName,
    styleItemName: item.styleItemName,
    materialGroup: Number(item.materialGroup || 3),
    materialName: item.materialName,
    metalName: item.metalName,
    itemMode: 0,
    unit: item.unit || '件',
    stockQty: item.stockQty === null || item.stockQty === undefined ? null : Number(item.stockQty),
    weight: item.weight === null || item.weight === undefined ? null : Number(item.weight),
    salePrice: Number(item.salePrice || 0),
    dealPrice: Number(item.pricingMode) === 2 ? Number(item.metalPricePerGram || item.dealPrice || 0) : Number(item.dealPrice || 0),
    dealPriceTouched: true,
    qty: 1,
    maxQty: 1,
    isGift: item.isGift || 0,
    pricingMode: Number(item.pricingMode || 1),
    laborFee: Number(item.laborFee || 0),
    packagingFee: Number(item.packagingFee || 0),
    laborFeeMode: Number(item.laborFeeMode || 1),
    metalPricePerGram: Number(item.metalPricePerGram || 0),
    marketMetalPricePerGram: Number(item.marketMetalPricePerGram || item.metalPricePerGram || 0),
    memberDiscountAmount: Number(item.memberDiscountAmount || 0),
    payAmount: Number(item.payAmount || 0)
  }))
  refreshLineStock(epoch)
  if (form.memberId) {
    loadMember(form.memberId, epoch)
  } else if (form.customerName || form.customerPhone) {
    customerState.value = 'new'
  } else {
    customerState.value = 'idle'
  }
  await loadCustoms(savedOffsets, epoch)
  if (epoch !== saleEpoch) return
  schedulePreview()
}

async function loadCustoms(savedMap, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = customerSeq
  const phone = form.customerPhone?.trim() || ''
  const memberId = form.memberId || undefined
  if (!memberId && !phone) {
    if (epoch === saleEpoch && seq === customerSeq) customs.value = []
    return
  }
  try {
    const res = await listAvailableCustom({ memberId, phone })
    if (epoch !== saleEpoch || seq !== customerSeq) return
    const saved = savedMap || new Map(customs.value.map((row) => [offsetKey(row), Number(row.offsetAmount || 0)]))
    customs.value = (res.data || []).map((row) => {
      const key = offsetKey(row)
      const kept = Number(saved.get(key) || 0)
      const cap = Number(row.availableAmount || 0) + kept
      const bound = row.goodsItemId && lines.value.some((line) => line.goodsItemId === row.goodsItemId)
      const preset = saved.has(key) ? kept : (bound ? cap : 0)
      return { ...row, cap, bound: !!bound, offsetAmount: Math.min(preset, cap) }
    })
  } catch (error) {
    if (epoch === saleEpoch && seq === customerSeq) customs.value = []
  }
}

function offsetKey(row) {
  return String(row.customItemId || row.itemId || row.id || '')
}

function onOffsetChange(row) {
  const cap = Number(row.cap || 0)
  if (Number(row.offsetAmount) > cap) row.offsetAmount = cap
  if (Number(row.offsetAmount) < 0) row.offsetAmount = 0
  if (offsetOverflow.value) ElMessage.warning('定金对冲合计不能超过商品实付')
  schedulePreview()
}

async function refreshLineStock(epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++lineStockSeq
  const snapshot = lines.value.slice()
  for (const row of snapshot) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (epoch !== saleEpoch || seq !== lineStockSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) {
        row.maxQty = found.itemMode === 1 ? 1 : Math.max(Number(row.qty || 1), Number(found.qty || 1))
        row.itemMode = found.itemMode
        if (row.stockQty === null || row.stockQty === undefined) row.stockQty = Number(found.qty || 1)
        if (!row.unit) row.unit = found.unit || '件'
        if (row.weight === null || row.weight === undefined) row.weight = found.weight
      }
    } catch (error) {
      if (epoch !== saleEpoch || seq !== lineStockSeq) return
    }
  }
}

async function loadMember(memberId, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (epoch !== saleEpoch || seq !== customerSeq || !res.data) return
    memberInfo.value = res.data
    customerState.value = 'member'
    memberReferrers.value = res.data.referrers || []
    if (!form.id) form.applyMemberDiscount = true
  } catch (error) {}
}

async function refreshDashboard() {
  try {
    const res = await getSaleDashboard()
    const data = res.data || {}
    dashboard.todayAmount = data.todayAmount || 0
    dashboard.todayOrders = data.todayOrders || 0
    dashboard.todayPieces = data.todayPieces || 0
    dashboard.avgTicket = data.avgTicket || 0
  } catch (error) {}
}

function resetForm() {
  saleEpoch += 1
  // 旧请求在代次递增后会停止回写；这里同步清掉旧请求留下的提交状态，
  // 否则收款后自动新开单会让“确认收款/保存草稿”按钮一直 loading。
  saving.value = false
  confirming.value = false
  cancelPendingPreview()
  lineStockSeq += 1
  customerSeq += 1
  currentLoadSeq += 1
  draftSeq += 1
  preview.value = null
  scanBusy = false
  form.id = 0
  form.orderNo = ''
  form.status = 0
  form.dealTime = serverDateTimeText().slice(0, 16)
  form.orderType = 1
  form.memberId = null
  form.customerName = ''
  form.customerPhone = ''
  form.customerWechatNo = ''
  form.referrerWechatNo = ''
  form.referrerMemberId = null
  form.customerSex = 0
  form.birthdayText = ''
  form.birthday = null
  form.birthYear = null
  form.remark = ''
  form.adjustAmount = 0
  form.applyMemberDiscount = true
  form.roundOffAmount = 0
  form.cashInput = 0
  form.cashTouched = false
  lines.value = []
  customs.value = []
  memberInfo.value = null
  memberReferrers.value = []
  customerState.value = 'idle'
  preview.value = null
  scanKeyword.value = ''
  goodsDialog.value = false
  goodsCandidates.value = []
  customerDialog.value = false
  customerCandidates.value = []
  localStorage.removeItem(saleDraftKey)
}

async function newSale(skipConfirm = false) {
  if (!skipConfirm && submitting.value) {
    ElMessage.warning('当前正在保存或收款，请稍候')
    return
  }
  if (!skipConfirm && (form.id || lines.value.length) && form.status === 0) {
    try {
      await ElMessageBox.confirm('当前单据内容将被清空，确定新开一单？', '新开一单', { confirmButtonText: '新开一单', cancelButtonText: '取消', type: 'info' })
    } catch (error) {
      return
    }
  }
  resetForm()
  const { id, ...rest } = route.query
  if (id !== undefined) await router.replace({ query: rest })
  nextTick(() => scanInputRef.value?.focus?.())
}

async function loadCurrent() {
  const id = Number(route.query.id || 0)
  const epoch = saleEpoch
  const seq = ++currentLoadSeq
  if (!id) return
  loading.value = true
  try {
    const res = await getSale(id)
    if (epoch === saleEpoch && seq === currentLoadSeq) await applyDetail(res.data, epoch)
  } catch (error) {
    if (epoch === saleEpoch && seq === currentLoadSeq) ElMessage.error('销售单加载失败')
  } finally {
    if (epoch === saleEpoch && seq === currentLoadSeq) loading.value = false
  }
}

onMounted(() => {
  window.addEventListener('keydown', onCheckoutHotkey)
  refreshDashboard()
  loadCurrent()
  restoreLocalSaleDraft()
  nextTick(() => scanInputRef.value?.focus?.())
})
watch([form, lines, scanKeyword, customs], scheduleLocalSaleDraft, { deep: true })
onBeforeRouteLeave((to, from, next) => {
  if (saleHasInput.value && form.status !== 1) {
    saveLocalSaleDraft()
    ElMessage.info('当前销售开单已自动暂存，返回后可继续录入')
  }
  next()
})
onBeforeUnmount(() => {
  saleEpoch += 1
  if (previewTimer) clearTimeout(previewTimer)
  if (saleDraftTimer) clearTimeout(saleDraftTimer)
  saveLocalSaleDraft()
  window.removeEventListener('keydown', onCheckoutHotkey)
})
</script>

<style scoped>
.jy-sale {
  --rose: #c4788a;
  --rose-deep: #a85c6e;
  --rose-soft: #f8e9ed;
  --rose-mist: #fdf6f7;
  --ink: #3d3236;
  --muted: #8b7a80;
  --line: #f0dfe3;
  --ivory: #fffbfc;
  min-height: 100%;
  padding: 14px 16px 20px;
  background: linear-gradient(180deg, #fdf7f8 0%, #fbf3f5 100%);
  color: var(--ink);
  font-size: 13px;
}

.eyebrow,
.section-kicker {
  display: inline-block;
  font-size:12px;
  letter-spacing: 0.22em;
  color: var(--rose);
  font-weight: 700;
}

/* ---------- top bar ---------- */
.sale-topbar {
  display: flex;
  align-items: center;
  gap: 24px;
  padding: 12px 18px;
  background: var(--ivory);
  border: 1px solid var(--line);
  border-radius: 12px;
  box-shadow: 0 4px 14px rgba(196, 120, 138, 0.055);
  position: relative;
  overflow: hidden;
}
.sale-topbar::before {
  content: "";
  position: absolute;
  left: 0;
  top: 18px;
  bottom: 18px;
  width: 3px;
  border-radius: 3px;
  background: linear-gradient(180deg, var(--rose), #e7b7c2);
}
.topbar-copy { flex: 1 1 auto; min-width: 0; }
.title-line { display: flex; align-items: center; gap: 10px; margin: 4px 0 4px; }
.title-line h1 { margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.04em; }
.topbar-copy p { margin: 0; color: var(--muted); font-size: 12.5px; }
.topbar-stats { display: flex; align-items: stretch; gap: 0; padding: 0 4px; }
.topbar-stats > div {
  padding: 2px 18px;
  border-left: 1px solid var(--line);
  display: flex;
  flex-direction: column;
  gap: 3px;
}
.topbar-stats > div:first-child { border-left: 0; }
.topbar-stats span { font-size: 11px; color: var(--muted); }
.topbar-stats b { font-size: 17px; color: var(--rose-deep); font-weight: 700; }
.topbar-stats b em { font-size: 11px; font-style: normal; color: var(--muted); margin-left: 2px; }
.topbar-actions { display: flex; gap: 10px; }

/* ---------- workspace ---------- */
.sale-workspace {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 336px;
  gap: 14px;
  margin-top: 12px;
  align-items: start;
}
.sale-main { display: flex; flex-direction: column; gap: 12px; min-width: 0; }

/* ---------- summary ---------- */
.summary-card {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 220px;
  gap: 14px;
  padding: 13px 16px;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 12px;
  box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04);
}
.summary-fields {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px 14px;
  align-content: start;
}
.field { display: flex; flex-direction: column; gap: 6px; }
.field > span { font-size: 11.5px; color: var(--muted); font-weight: 600; letter-spacing: 0.02em; }
.field > strong { font-size: 14px; font-weight: 600; color: var(--ink); min-height: 32px; display: flex; align-items: center; }
.enter-hint { font-style: normal; font-size:12px; color: var(--rose); background: var(--rose-soft); border-radius: 4px; padding: 1px 5px; margin-left: 4px; }
.remark-field { grid-column: span 2; }
.remark-field :deep(.el-input__wrapper) { padding: 2px 12px; }

.summary-side { display: flex; flex-direction: column; gap: 8px; padding-left: 12px; border-left: 1px dashed var(--line); min-width: 0; }
.summary-meta { display: grid; grid-template-columns: 1fr; gap: 4px; }
.summary-meta > div { display: flex; align-items: baseline; justify-content: space-between; gap: 6px; min-width: 0; }
.summary-meta span { font-size: 11px; color: var(--muted); }
.summary-meta b { font-size: 11.5px; font-weight: 600; color: var(--ink); text-align: right; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-width: 0; }
.identity-hint { display: flex; align-items: center; gap: 5px; margin: 0; font-size: 11px; line-height: 1.5; color: #7c8a7f; }
.identity-hint .el-icon { font-size: 12px; color: #6f9c7d; flex: 0 0 auto; }
.identity-hint.warn { color: #b56a2e; }
.identity-hint.warn .el-icon { color: #d08a44; }

.member-panel {
  border-radius: 14px;
  padding: 16px;
  background: linear-gradient(150deg, #fdf1f4, #fbf7f8);
  border: 1px solid #f3dde2;
  display: flex;
  flex-direction: column;
  gap: 12px;
  transition: all 0.2s ease;
}
.member-panel.member { background: linear-gradient(150deg, #fbe9ee, #fdf5f7); border-color: #edc9d2; }
.member-panel.new { background: linear-gradient(150deg, #fdf3e9, #fdfaf6); border-color: #f2e0cd; }
.member-head { display: flex; align-items: center; gap: 10px; }
.member-avatar {
  width: 38px; height: 38px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  background: #fff; color: var(--rose); font-size: 18px;
  box-shadow: 0 4px 10px rgba(196, 120, 138, 0.16);
}
.member-avatar.fresh { color: #c58a4a; }
.member-avatar.idle { color: #b7a6ab; }
.member-copy { flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.member-copy b { font-size: 14px; }
.member-copy small { font-size: 11px; color: var(--muted); }
.member-tag { font-style: normal; font-size:12px; padding: 2px 8px; border-radius: 20px; background: #fff; color: var(--muted); }
.member-tag.ok { color: #fff; background: var(--rose); }
.member-tag.fresh { color: #a9702f; background: #fbe7cf; }
.member-tag.idle { color: #9b8b90; background: #f3ebed; }
.member-metrics { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; }
.member-metrics > div { display: flex; flex-direction: column; gap: 2px; padding: 8px 6px; background: rgba(255, 255, 255, 0.72); border-radius: 10px; text-align: center; }
.member-metrics span { font-size:12px; color: var(--muted); }
.member-metrics b { font-size: 14px; color: var(--rose-deep); }
.member-tip { margin: 0; font-size: 11.5px; line-height: 1.6; color: var(--muted); }

/* ---------- scan ---------- */
.scan-bar { display: flex; flex-direction: column; gap: 8px; }
.scan-bar :deep(.el-input__wrapper) {
  border-radius: 12px;
  box-shadow: 0 0 0 1px var(--line) inset, 0 6px 16px rgba(196, 120, 138, 0.06);
  padding: 4px 14px;
  background: #fff;
}
.scan-bar :deep(.el-input__wrapper.is-focus) { box-shadow: 0 0 0 1px var(--rose) inset, 0 8px 18px rgba(196, 120, 138, 0.12); }
.scan-tip { display: flex; align-items: center; gap: 6px; color: var(--muted); font-size: 11.5px; padding-left: 2px; }
.scan-tip .el-icon { color: var(--rose); }

/* ---------- lines ---------- */
.lines-card { background: #fff; border: 1px solid var(--line); border-radius: 12px; box-shadow: 0 4px 12px rgba(196, 120, 138, 0.04); overflow: hidden; display: flex; flex-direction: column; }
.lines-head { display: flex; align-items: center; justify-content: space-between; padding: 16px 20px 12px; border-bottom: 1px solid #f7edef; }
.more-attributes-control { display: inline-flex; align-items: center; gap: 8px; color: var(--muted); font-size: 12px; cursor: pointer; }
.lines-title h2 { margin: 3px 0 0; font-size: 16px; font-weight: 700; display: flex; align-items: baseline; gap: 8px; }
.lines-title h2 em { font-style: normal; font-size: 11.5px; color: var(--muted); font-weight: 500; }
.line-table-wrap { height: 392px; min-height: 0; }
.line-table :deep(th.el-table__cell) { background: #fdf6f8; color: #7c6c72; font-size: 11.5px; font-weight: 600; }
.line-table :deep(td.el-table__cell) { padding: 8px 0; vertical-align: middle; }
.line-table :deep(.cell) { overflow: hidden; text-overflow: ellipsis; }
.line-table :deep(.el-input-number) { width: 100%; }
.line-table :deep(.el-input-number .el-input__inner) { text-align: center; }
.line-table :deep(.el-select) { width: 100%; }
.line-table :deep(.el-table-fixed-column--left),
.line-table :deep(.el-table-fixed-column--right) { background: #fff; }
.line-table :deep(.el-table__header .el-table-fixed-column--left),
.line-table :deep(.el-table__header .el-table-fixed-column--right) { background: #fdf6f8; }
.line-unit-select { width: 86px; }
.sale-quantity-cell { display: flex; align-items: center; justify-content: center; gap: 8px; }
.sale-quantity-cell > b { min-width: 18px; color: var(--ink); font-weight: 700; }
.sale-quantity-cell .line-unit-select { width: 92px; }
.line-weight-input { width: 112px; }
.line-weight-input :deep(.el-input__inner) { text-align: right; }
.goods-cell { display: flex; align-items: center; gap: 8px; min-width: 0; }
.goods-cell > div { min-width: 0; }
.goods-gem { width: 34px; height: 34px; flex: 0 0 34px; border-radius: 10px; display: flex; align-items: center; justify-content: center; background: var(--rose-soft); color: var(--rose); font-size: 16px; }
.goods-cell b { display: block; font-size: 13px; font-weight: 600; color: var(--ink); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.goods-cell small { display: block; font-size: 11px; color: var(--muted); margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.meta-cell { display: flex; flex-direction: column; gap: 2px; }
.meta-cell span { font-size: 12.5px; color: #5c4d52; }
.meta-cell small { font-size: 11px; color: var(--muted); }
.muted-price { color: var(--muted); font-size: 12.5px; text-decoration: line-through; }
.line-amount { color: var(--rose-deep); font-size: 13px; }
.line-discount { color: #b56a7a; font-size: 12.5px; }
.line-pay { color: #a25464; font-size: 13px; font-weight: 700; }
.pricing-cell { display: flex; flex-direction: column; gap: 2px; align-items: center; }
.pricing-cell b { color: #8c5562; font-size: 12px; font-weight: 650; }
.pricing-cell small { color: var(--muted); font-size: 10.5px; }

/* ---------- settle ---------- */
.settle-rail { position: sticky; top: 12px; }
.settle-card { background: #fff; border: 1px solid var(--line); border-radius: 12px; padding: 14px; box-shadow: 0 4px 14px rgba(196, 120, 138, 0.05); display: flex; flex-direction: column; gap: 12px; }
.settle-head h2 { margin: 3px 0 0; font-size: 16px; font-weight: 700; }
.amount-list { display: flex; flex-direction: column; gap: 9px; padding-bottom: 12px; border-bottom: 1px dashed var(--line); }
.level-discount-check { margin: 2px 0 4px; }
.level-discount-check :deep(.el-checkbox__label) { color: #8a6b72; font-size: 11.5px; }
.amount-list > div { display: flex; align-items: center; justify-content: space-between; font-size: 12.5px; color: #5c4d52; }
.amount-list b { font-weight: 600; }
.amount-list .discount { color: var(--rose-deep); }
.amount-list .discount em { font-style: normal; font-size:12px; color: var(--muted); margin-left: 6px; background: var(--rose-soft); padding: 1px 5px; border-radius: 4px; }

.adjust-block { display: flex; flex-direction: column; gap: 12px; }
.adjust-block label { display: grid; grid-template-columns: 72px minmax(0, 1fr); align-items: center; gap: 4px 8px; }
.adjust-block label > span { font-size: 12px; color: #5c4d52; }
.adjust-block label :deep(.el-input-number) { width: 100%; }
.adjust-block label small { grid-column: 2; font-size:12px; color: var(--muted); }

.coupon-block { border-radius: 12px; background: #fdf8f9; border: 1px dashed #edd7dc; padding: 10px 12px; }
.coupon-head { display: flex; align-items: center; justify-content: space-between; font-size: 12px; color: #5c4d52; }
.coupon-block p { margin: 6px 0 0; font-size:12px; line-height: 1.6; color: var(--muted); }

.pay-panel {
  border-radius: 14px;
  padding: 16px;
  background: linear-gradient(135deg, #c4788a, #a95f71);
  color: #fff;
  display: flex;
  flex-direction: column;
  gap: 4px;
  box-shadow: 0 10px 22px rgba(169, 95, 113, 0.28);
}
.pay-panel span { font-size: 12px; opacity: 0.86; }
.pay-panel strong { font-size: 34px; font-weight: 800; letter-spacing: 0.01em; line-height: 1.2; }
.pay-panel small { font-size: 12px; opacity: 0.82; }
.pay-line.receivable { align-items: baseline; padding-bottom: 8px; border-bottom: 1px solid rgba(255,255,255,.2); }
.pay-line.receivable b { font-size: 20px; font-weight: 800; }
.cash-input-line { align-items: center; padding: 6px 0; }
.cash-input-line > span { font-size: 13px; font-weight: 700; }
.cash-input-line :deep(.el-input-number) { width: 150px; }
.cash-input-line :deep(.el-input__inner) { font-size: 17px; font-weight: 800; }
.cash-input-line :deep(.el-input__wrapper) { background: rgba(255,255,255,.96); box-shadow: none; }
.pay-line .round-off { color: #ffe5b8; font-size: 16px; font-weight: 800; }
.pay-total { display: flex; align-items: baseline; justify-content: space-between; gap: 12px; padding-top: 8px; border-top: 1px solid rgba(255,255,255,.2); }
.pay-total span { font-size: 13px; font-weight: 700; }
.pay-total strong { color: #fff; font-size: 38px; }
.settle-actions :deep(kbd) { margin-left: 4px; padding: 1px 4px; color: inherit; background: rgba(255,255,255,.16); border: 1px solid rgba(255,255,255,.32); border-radius: 3px; font-family: inherit; font-size: 9.5px; }

.settle-actions { display: grid; grid-template-columns: 1fr 1.2fr; gap: 10px; }
.settle-actions :deep(.el-button) { width: 100%; height: 38px; border-radius: 10px; }
.settle-note { display: flex; gap: 6px; margin: 0; font-size:12px; line-height: 1.6; color: var(--muted); }
.settle-note .el-icon { color: var(--rose); margin-top: 2px; flex: 0 0 auto; }
.settle-note.warn { color: #b56a2e; }
.settle-note.warn .el-icon { color: #d08a44; }

.pay-confirm-dialog :deep(.el-dialog__header) { padding-bottom: 12px; border-bottom: 1px solid #f0e3e6; }
.pay-confirm-body { display: flex; flex-direction: column; gap: 10px; }
.pay-confirm-row { display: flex; align-items: baseline; justify-content: space-between; gap: 16px; padding: 10px 12px; border-radius: 8px; background: #faf7f8; }
.pay-confirm-row span { color: #7f6a70; font-size: 13px; }
.pay-confirm-row b { color: #513b42; font-size: 19px; font-weight: 700; }
.pay-confirm-row.received-row { background: #eef5ff; border: 1px solid #cfe2ff; }
.pay-confirm-row.received-row span { color: #3d6f9e; font-weight: 600; }
.pay-confirm-row.received-row b { color: #1677ff; font-size: 30px; font-weight: 800; }
.pay-confirm-row.round-row b { color: #b7791f; }
.pay-confirm-row.deposit-row b { color: #3d8b6d; }
.pay-confirm-row.cash-row { background: #f3f0ff; border: 1px solid #ded6ff; }
.pay-confirm-row.cash-row b { color: #5b4bb7; font-size: 25px; font-weight: 800; }
.pay-confirm-warning { margin: 0; padding: 8px 10px; color: #9a5b1d; background: #fff8ed; border-radius: 6px; font-size: 12px; }
.pay-confirm-hint { margin: 2px 0 0; color: #9a858b; font-size: 12px; text-align: center; }

/* ---------- dialogs ---------- */
.dialog-hint { margin: 0 0 12px; font-size: 12px; color: var(--muted); }
.candidate-list { display: flex; flex-direction: column; gap: 8px; max-height: 380px; overflow-y: auto; }
.candidate-row { display: flex; align-items: center; gap: 12px; padding: 10px 14px; border: 1px solid var(--line); border-radius: 12px; background: #fff; cursor: pointer; text-align: left; transition: all 0.16s ease; }
.candidate-row:hover { border-color: #e6bcc6; background: #fdf7f8; }
.candidate-row.active { border-color: var(--rose); background: var(--rose-soft); }
.candidate-avatar { width: 34px; height: 34px; border-radius: 50%; display: flex; align-items: center; justify-content: center; background: #fff; color: var(--rose); }
.candidate-copy { flex: 1 1 auto; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.candidate-copy b { font-size: 13px; }
.candidate-copy small { font-size: 11px; color: var(--muted); }
.candidate-row em { font-style: normal; font-size: 11px; color: var(--rose-deep); }

.goods-candidate-list { display: flex; flex-direction: column; gap: 8px; max-height: 420px; overflow-y: auto; }
.goods-candidate { display: grid; grid-template-columns: 34px minmax(0, 1.4fr) minmax(0, 1fr) 90px; align-items: center; gap: 12px; padding: 10px 14px; border: 1px solid var(--line); border-radius: 12px; background: #fff; cursor: pointer; text-align: left; transition: all 0.16s ease; }
.goods-candidate:hover { border-color: #e6bcc6; background: #fdf7f8; transform: translateX(2px); }
.candidate-meta { font-size: 11.5px; color: var(--muted); }
.candidate-price { text-align: right; color: var(--rose-deep); font-size: 13px; }


.identity-chip { min-height: 32px; display: flex; align-items: center; gap: 8px; padding: 0 10px; border-radius: 8px; background: #faf6f7; border: 1px solid #f0e1e5; }
.identity-chip b { font-size: 13px; font-weight: 600; }
.identity-chip small { color: #8d737b; font-size: 12px; }
.identity-chip.member { background: #fbeff2; border-color: #efd0d7; }
.identity-chip.member b { color: #a85c6e; }
.identity-chip.new b { color: #b56a2e; }
.offset-block { border-radius: 10px; background: #fdf8f9; border: 1px solid #f0e1e5; padding: 8px 10px; }
.offset-block p, .offset-row small { margin: 6px 0 0; color: #8d737b; font-size: 12px; }
.offset-list { display: flex; flex-direction: column; gap: 8px; margin-top: 8px; }
.offset-row { display: grid; grid-template-columns: minmax(0, 1fr) 120px; gap: 8px; align-items: center; }
.offset-row > div { min-width: 0; }
.offset-row .el-tag { margin-top: 4px; }
.offset-row b { display: block; font-size: 12px; }
.pay-line { display: flex; justify-content: space-between; font-size: 12px; opacity: 0.92; }
.settle-note.warn, .settle-note.over { color: #b56a2e; }

@media (max-width: 1280px) {
  .sale-workspace { grid-template-columns: 1fr; }
  .settle-rail { position: static; }
  .summary-card { grid-template-columns: 1fr; }
}
@media (max-width: 900px) {
  .jy-sale { padding: 14px; }
  .sale-topbar { flex-wrap: wrap; gap: 14px; }
  .topbar-stats { order: 3; width: 100%; }
  .summary-fields { grid-template-columns: 1fr 1fr; }
  .remark-field { grid-column: span 2; }
}
.referrer-field :deep(.el-select) { width: 100%; }
</style>

