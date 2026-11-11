<template>
  <div class="jy-custom">
    <div class="page-bar">
      <h1>销售定制</h1>
      <div class="bar-actions">
        <el-button plain @click="router.push('/business/custom-refund')">定金退回</el-button>
        <el-button v-hasPermi="['jingyue:custom:add']" type="primary" @click="openEditor()">收取定金</el-button>
      </div>
    </div>

    <section class="filter-line">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="search" />
      <el-input v-model="query.keyword" clearable placeholder="单号 / 客户 / 手机 / 货品" @keyup.enter="search" @clear="search" />
      <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
        <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
      </el-select>
      <el-select v-model="query.arrivalStatus" clearable placeholder="到货状态" @change="search">
        <el-option label="有商品待到货" :value="0" />
        <el-option label="已有商品到货" :value="1" />
      </el-select>
      <el-checkbox v-model="query.contactPending" :true-value="1" :false-value="null" @change="search">待联系客户</el-checkbox>
      <el-button type="primary" plain @click="search">查询</el-button>
      <span class="count">{{ total }} 张</span>
    </section>

    <section class="sheet">
      <el-table v-loading="loading" :data="rows" height="100%" empty-text="没有定制单" @row-click="openDetail">
        <el-table-column label="定制单号" min-width="145"><template #default="{ row }"><b>{{ row.customNo }}</b><div class="sub">{{ dateText(row.customDate) }}</div></template></el-table-column>
        <el-table-column label="客户" min-width="130"><template #default="{ row }"><div class="stack"><b>{{ row.customerName || '未留姓名' }}</b><small>{{ row.customerPhone || '无手机号' }}</small></div></template></el-table-column>
        <el-table-column label="商品" min-width="220"><template #default="{ row }"><div class="stack"><b>{{ row.goodsName }}</b><small>{{ row.itemCount }} 件 · 已到 {{ row.arrivedItemCount }} · 已取 {{ row.pickedItemCount }}</small><small v-if="row.pickupDate" :class="{ 'pickup-warn': isPickupSoon(row) }">约定取货 {{ dateText(row.pickupDate) }}</small></div></template></el-table-column>
        <el-table-column label="定金" width="92" align="right"><template #default="{ row }">¥{{ money(row.depositAmount) }}</template></el-table-column>
        <el-table-column label="已对冲" width="92" align="right"><template #default="{ row }">¥{{ money(row.offsetAmount) }}</template></el-table-column>
        <el-table-column label="已退" width="86" align="right"><template #default="{ row }">¥{{ money(row.refundAmount) }}</template></el-table-column>
        <el-table-column label="剩余" width="96" align="right"><template #default="{ row }"><b class="remain">¥{{ money(row.remainAmount) }}</b></template></el-table-column>
        <el-table-column label="状态" width="92" align="center"><template #default="{ row }"><el-tag size="small" effect="plain" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag></template></el-table-column>
        <el-table-column label="操作" width="145" align="center">
          <template #default="{ row }">
            <el-button link type="primary" @click.stop="openDetail(row)">明细</el-button>
            <el-button v-if="canEdit(row)" link type="primary" @click.stop="openEditor(row)">修改</el-button>
            <el-button v-if="canDelete(row)" v-hasPermi="['jingyue:custom:remove']" link type="danger" @click.stop="removeRow(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pager">
        <el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="load" @current-change="load" />
      </div>
    </section>

    <el-dialog v-model="editor" :title="form.id ? '修改定制单' : '收取定金'" width="1080px" top="4vh" append-to-body destroy-on-close>
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="form-scroll">
        <div class="customer-grid">
          <el-form-item label="客户姓名" prop="customerName"><el-input v-model="form.customerName" maxlength="50" placeholder="回车识别会员" @keyup.enter="identify" /></el-form-item>
          <el-form-item label="手机号码" prop="customerPhone"><el-input v-model="form.customerPhone" maxlength="30" placeholder="回车识别会员" @keyup.enter="identify" /></el-form-item>
          <el-form-item label="性别"><el-radio-group v-model="form.customerSex"><el-radio-button :value="0">未知</el-radio-button><el-radio-button :value="1">先生</el-radio-button><el-radio-button :value="2">女士</el-radio-button></el-radio-group></el-form-item>
          <el-form-item label="客户识别"><el-tag v-if="form.memberId" type="success" effect="plain">库内会员</el-tag><el-tag v-else type="warning" effect="plain">新客户，保存后自动建档</el-tag></el-form-item>
          <el-form-item label="定制日期"><el-date-picker v-model="form.customDate" type="date" value-format="YYYY-MM-DD HH:mm:ss" /></el-form-item>
          <el-form-item label="整单备注" class="span-2"><el-input v-model="form.remark" maxlength="300" placeholder="工期、改款要求或取货约定" /></el-form-item>
        </div>

        <div class="items-head">
          <div><b>商品明细</b><span>{{ form.items.length }} 件，合计定金 ¥{{ money(itemDepositTotal) }}</span></div>
          <el-button type="primary" plain @click="addItem">添加商品</el-button>
        </div>

        <div class="item-list">
          <div v-for="(item, index) in form.items" :key="item.key" class="item-card">
            <div class="item-head">
              <div class="item-title"><b>商品 {{ index + 1 }}</b><el-tag v-if="item.arrivalStatus === 1" size="small" type="success" effect="plain">已到货</el-tag><el-tag v-if="item.pickupStatus === 1" size="small" type="info" effect="plain">已取货</el-tag><span v-if="item.id" class="sub">明细已入账</span></div>
              <el-button v-if="!item.id" link type="danger" @click="removeItem(index)">移除</el-button>
            </div>
            <div class="item-fields">
              <label class="field span-2"><span>绑定在库商品</span><el-select v-model="item.goodsItemId" filterable remote clearable :remote-method="(kw) => searchGoods(kw, item)" :loading="item.goodsLoading" placeholder="输入条码 / 货号 / 款式搜索；定制件可不绑定" @change="applyBoundGoods(item)" @clear="clearBoundGoods(item)"><el-option v-for="goods in item.goodsOptions" :key="goods.id" :label="goodsLabel(goods)" :value="goods.id"><div class="goods-option"><b>{{ goods.goodsNo }}</b><span>{{ goods.styleName }} · {{ goods.styleItemName || '未分类' }} · ¥{{ money(goods.salePrice) }}</span></div></el-option></el-select></label>
              <label class="field span-2"><span>定制货品名称 *</span><el-input v-model="item.goodsName" maxlength="100" placeholder="例如：红宝石手链，按客样改圈口" /></label>
              <label class="field"><span>款式分类</span><el-select v-model="item.styleItemId" clearable filterable placeholder="选择款式分类"><el-option v-for="lookup in styles" :key="lookup.id" :label="categoryLabel(lookup)" :value="lookup.id" /></el-select></label>
              <label class="field"><span>材质</span><el-select v-model="item.materialItemId" clearable placeholder="选择材质"><el-option v-for="lookup in materials" :key="lookup.id" :label="lookup.itemName" :value="lookup.id" /></el-select></label>
              <label class="field"><span>金属成色</span><el-select v-model="item.metalItemId" clearable placeholder="选择成色"><el-option v-for="lookup in metals" :key="lookup.id" :label="lookup.itemName" :value="lookup.id" /></el-select></label>
              <label class="field"><span>克重</span><el-input-number v-model="item.weight" :min="0" :precision="3" controls-position="right" /></label>
              <label class="field"><span>预计售价</span><el-input-number v-model="item.expectPrice" :min="0" :precision="2" controls-position="right" /></label>
              <label class="field deposit-field"><span>本件定金 *</span><el-input-number v-model="item.depositAmount" :min="0.01" :precision="2" :disabled="!!item.id" controls-position="right" /></label>
              <label class="field"><span>约定取货日</span><el-date-picker v-model="item.pickupDate" type="date" value-format="YYYY-MM-DD" placeholder="可不填" clearable /></label>
              <label class="field span-2"><span>本件备注</span><el-input v-model="item.remark" maxlength="200" placeholder="材质要求、改圈口等" /></label>
            </div>
            <div v-if="item.id && (item.arrivalStatus === 1 || item.pickupStatus === 1)" class="item-progress">
              <span v-if="item.arrivalTime">到货：{{ dateTime(item.arrivalTime) }}</span>
              <span v-if="item.pickupTime">取货：{{ dateTime(item.pickupTime) }}</span>
            </div>
          </div>
          <el-empty v-if="!form.items.length" description="请添加至少一件定制商品" :image-size="64" />
        </div>
      </el-form>
      <template #footer>
        <div class="dialog-footer"><span>共 {{ form.items.length }} 件 · 定金合计 <b>¥{{ money(itemDepositTotal) }}</b></span><div><el-button @click="editor = false">取消</el-button><el-button type="primary" :loading="saving" @click="submit">{{ form.id ? '保存修改' : '保存并收定金' }}</el-button></div></div>
      </template>
    </el-dialog>

    <el-drawer v-model="detailOpen" title="定金单明细" size="620px">
      <template v-if="detail">
        <div class="detail-head"><div><b>{{ detail.customNo }}</b><span>{{ detail.customerName }} · {{ detail.customerPhone }}</span></div><div class="detail-money"><span>定金 ¥{{ money(detail.depositAmount) }}</span><span>剩余 <b>¥{{ money(detail.remainAmount) }}</b></span></div></div>
        <div class="detail-items">
          <div v-for="item in detail.items || []" :key="item.id" class="detail-item">
            <div class="detail-item-head"><div><b>{{ item.goodsName }}</b><span>{{ metaText(item) }}</span><span v-if="item.goodsNo || item.barcode" class="bind-code">{{ codeText(item) }}</span></div><div class="detail-tags"><el-tag size="small" :type="item.arrivalStatus === 1 ? 'success' : 'info'" effect="plain">{{ item.arrivalStatus === 1 ? '已到货' : '待到货' }}</el-tag><el-tag v-if="item.pickupStatus === 1" size="small" type="warning" effect="plain">已取货</el-tag></div></div>
            <div class="detail-numbers"><span>定金 ¥{{ money(item.depositAmount) }}</span><span>已冲 ¥{{ money(item.offsetAmount) }}</span><span>已退 ¥{{ money(item.refundAmount) }}</span><span class="remain">可退 ¥{{ money(item.availableAmount) }}</span></div>
            <div class="item-actions">
              <el-button v-if="item.arrivalStatus !== 1" size="small" type="primary" plain @click="openArrival(item)">登记到货</el-button>
              <el-button v-if="item.arrivalStatus === 1 && item.pickupStatus !== 1" size="small" type="success" plain @click="pickupItem(item)">客户取货</el-button>
              <el-button v-if="item.availableAmount > 0" size="small" type="danger" plain @click="openRefund(item)">退定金</el-button>
              <span v-if="item.arrivalRemark" class="sub">{{ item.arrivalRemark }}</span>
            </div>
          </div>
        </div>
        <div class="ledger-title">定金流水</div>
        <el-timeline>
          <el-timeline-item v-for="item in detail.ledgers || []" :key="item.id" :timestamp="dateTime(item.bizTime)">
            <b>{{ ledgerText(item.changeType) }} {{ money(item.amount) }}</b>
            <p>{{ item.goodsName ? item.goodsName + ' · ' : '' }}{{ item.remark || '无备注' }} · 余额 ¥{{ money(item.balanceAfter) }}</p>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-if="!(detail.ledgers || []).length" description="暂无流水" :image-size="56" />
      </template>
    </el-drawer>

    <el-dialog v-model="arrivalOpen" title="登记到货" width="460px" append-to-body destroy-on-close>
      <div v-if="arrivalItem" class="arrival-head"><b>{{ detail?.customNo }} · {{ arrivalItem.goodsName }}</b><span>{{ detail?.customerName }} · {{ detail?.customerPhone }}</span><small v-if="arrivalItem.barcode">原绑定 {{ codeText(arrivalItem) }}</small></div>
      <el-form label-width="82px" @submit.prevent>
        <el-form-item label="货品条码" required><el-input v-model="arrivalForm.barcode" maxlength="64" autofocus clearable placeholder="扫描或输入货品唯一条码" @keyup.enter="submitArrival" /></el-form-item>
        <el-form-item label="到货备注"><el-input v-model="arrivalForm.remark" type="textarea" :rows="2" maxlength="500" placeholder="例如：已到店，已通知客户取货" /></el-form-item>
      </el-form>
      <template #footer><el-button @click="arrivalOpen = false">取消</el-button><el-button type="primary" :loading="arrivalSaving" :disabled="!arrivalForm.barcode.trim()" @click="submitArrival">确认到货</el-button></template>
    </el-dialog>

    <el-dialog v-model="refundOpen" title="退定金" width="420px" append-to-body destroy-on-close>
      <div v-if="refundItem" class="refund-head"><b>{{ detail?.customNo }} · {{ refundItem.goodsName }}</b><span>当前可退 ¥{{ money(refundMax) }}</span></div>
      <el-form label-width="80px"><el-form-item label="退款金额"><el-input-number v-model="refundAmount" :min="0.01" :max="refundMax" :precision="2" controls-position="right" /></el-form-item><el-form-item label="退款备注"><el-input v-model="refundRemark" maxlength="200" placeholder="例如：客户取消这件定制" /></el-form-item></el-form>
      <template #footer><el-button @click="refundOpen = false">取消</el-button><el-button type="danger" :loading="refundSaving" @click="submitRefund">确认退定金</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyCustom">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { arriveCustom, deleteCustom, getCustom, listCustom, pickupCustom, refundCustom, saveCustom } from '@/api/jingyue/custom'
import { lookupOptions } from '@/api/jingyue/lookup'
import { findSaleGoods, matchSaleCustomer } from '@/api/jingyue/sale'
import { serverTodayText } from '@/utils/serverTime'

const router = useRouter()
const loading = ref(false)
const saving = ref(false)
const editor = ref(false)
const detailOpen = ref(false)
const rows = ref([])
const total = ref(0)
const detail = ref(null)
const dateRange = ref([])
const styles = ref([])
const materials = ref([])
const metals = ref([])
const formRef = ref()
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', status: null, arrivalStatus: null, contactPending: null, beginTime: null, endTime: null })
const form = reactive(blankForm())
const arrivalOpen = ref(false)
const arrivalSaving = ref(false)
const arrivalItem = ref(null)
const arrivalForm = reactive({ barcode: '', remark: '' })
const refundOpen = ref(false)
const refundSaving = ref(false)
const refundItem = ref(null)
const refundAmount = ref(0)
const refundRemark = ref('')
const rules = {
  customerName: [{ required: true, message: '请填写客户姓名', trigger: 'blur' }],
  customerPhone: [{ required: true, message: '请填写客户手机号', trigger: 'blur' }]
}
const statusOptions = [{ value: 1, label: '已收定金' }, { value: 2, label: '部分处理' }, { value: 3, label: '已对冲完' }, { value: 8, label: '已退清' }, { value: 9, label: '作废' }]
const itemDepositTotal = computed(() => form.items.reduce((sum, row) => sum + Number(row.depositAmount || 0), 0))
const refundMax = computed(() => Number(refundItem.value?.availableAmount || 0))
const categoryLabel = (item) => item.parentId ? '　└ ' + item.itemName : item.itemName

let keySeed = 1
function newItem() {
  return { key: 'new-' + keySeed++, id: 0, goodsItemId: null, goodsNo: '', barcode: '', goodsName: '', styleItemId: null, materialItemId: null, metalItemId: null, weight: null, expectPrice: 0, depositAmount: null, pickupDate: '', remark: '', arrivalStatus: 0, pickupStatus: 0, goodsOptions: [], goodsLoading: false, availableAmount: 0 }
}
function blankForm() {
  return { id: 0, memberId: null, customerName: '', customerPhone: '', customerSex: 0, customDate: today(), remark: '', items: [] }
}
function today() { return serverTodayText() + ' 00:00:00' }
function money(v) { return Number(v || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function dateText(v) { return v ? String(v).slice(0, 10) : '' }
function dateTime(v) { return v ? String(v).replace('T', ' ').slice(0, 16) : '' }
function statusText(v) { return statusOptions.find((it) => it.value === v)?.label || '草稿' }
function statusType(v) { return v === 1 ? 'success' : v === 2 ? 'warning' : v === 8 || v === 9 ? 'info' : '' }
function ledgerText(v) { return { 1: '收定金', 2: '销售对冲', 3: '作废退回', 4: '退定金', 5: '到货登记', 6: '客户取货' }[v] || '变动' }
function metaText(row) { return [row.styleItemName, row.materialName, row.metalName].filter(Boolean).join(' · ') || '未填写属性' }
function codeText(row) { return [row.goodsNo, row.barcode].filter(Boolean).filter((v, i, a) => a.indexOf(v) === i).join(' / ') }
function goodsLabel(item) { return [item.goodsNo, item.styleName, item.styleItemName].filter(Boolean).join(' · ') }
function canDelete(row) { return Number(row.offsetAmount || 0) <= 0 && Number(row.remainAmount || 0) <= 0 && Number(row.status) !== 9 }
function canEdit(row) { return Number(row.offsetAmount || 0) <= 0 && Number(row.refundAmount || 0) <= 0 && ![3, 8, 9].includes(Number(row.status)) }
function isPickupSoon(row) { return row.pickupDate && Number(row.arrivedItemCount || 0) < Number(row.itemCount || 0) && String(row.pickupDate).slice(0, 10) <= serverTodayText() }

async function loadOptions() {
  const [styleRes, materialRes, metalRes] = await Promise.all([lookupOptions('style'), lookupOptions('material'), lookupOptions('metal')])
  styles.value = styleRes.data || []
  materials.value = (materialRes.data || []).filter((item) => !item.parentId)
  metals.value = (metalRes.data || []).filter((item) => !item.parentId)
}
async function load() {
  loading.value = true
  try {
    query.beginTime = dateRange.value?.[0] || null
    query.endTime = dateRange.value?.[1] || null
    const res = await listCustom(query)
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  } finally { loading.value = false }
}
function search() { query.pageNum = 1; load() }
function addItem() { form.items.push(newItem()) }
function removeItem(index) { form.items.splice(index, 1) }
async function openEditor(row) {
  Object.assign(form, blankForm())
  if (row?.id) {
    const res = await getCustom(row.id)
    const data = res.data
    Object.assign(form, { id: data.id, memberId: data.memberId, customerName: data.customerName, customerPhone: data.customerPhone, customerSex: data.customerSex, customDate: data.customDate, remark: data.remark || '' })
    form.items = (data.items || []).map((item) => ({ ...newItem(), ...item, key: 'saved-' + item.id, goodsOptions: item.goodsNo ? [{ id: item.goodsItemId, goodsNo: item.goodsNo, barcode: item.barcode, styleName: item.goodsName }] : [] }))
  }
  if (!form.items.length) addItem()
  editor.value = true
}
async function identify() {
  const keyword = (form.customerPhone || form.customerName || '').trim()
  if (!keyword) return
  const res = await matchSaleCustomer(keyword)
  const data = res.data || {}
  const one = data.memberId ? data : (data.candidates || [])[0]
  if (!one?.memberId && !data.exists) { form.memberId = null; ElMessage.info('未找到会员，将按新客户同步建档'); return }
  const member = data.memberId ? data : one
  form.memberId = member.memberId
  form.customerName = member.memberName || form.customerName
  form.customerPhone = member.phone || form.customerPhone
  form.customerSex = member.sex ?? form.customerSex
  ElMessage.success('已识别库内会员')
}
async function searchGoods(keyword, item) {
  const text = String(keyword || '').trim()
  if (!text) { item.goodsOptions = []; return }
  item.goodsLoading = true
  try { item.goodsOptions = (await findSaleGoods(text)).data || [] } finally { item.goodsLoading = false }
}
function applyBoundGoods(item) {
  const goods = item.goodsOptions.find((it) => it.id === item.goodsItemId)
  if (!goods) { item.goodsNo = ''; item.barcode = ''; return }
  item.goodsNo = goods.goodsNo || ''
  item.barcode = goods.barcode || ''
  if (!item.goodsName) item.goodsName = goods.styleName || ''
  if (item.weight == null) item.weight = goods.weight ?? null
  if (!item.expectPrice) item.expectPrice = goods.salePrice || 0
  item.styleItemId = item.styleItemId || goods.styleItemId || null
  item.materialItemId = item.materialItemId || goods.materialItemId || null
  item.metalItemId = item.metalItemId || goods.metalItemId || null
}
function clearBoundGoods(item) { item.goodsItemId = null; item.goodsNo = ''; item.barcode = '' }
function itemPayload(item) {
  return { id: item.id || 0, goodsItemId: item.goodsItemId || null, goodsNo: item.goodsNo || null, barcode: item.barcode || null, goodsName: item.goodsName || null, styleItemId: item.styleItemId || null, materialItemId: item.materialItemId || null, metalItemId: item.metalItemId || null, weight: item.weight ?? null, expectPrice: Number(item.expectPrice || 0), depositAmount: Number(item.depositAmount || 0), pickupDate: item.pickupDate || null, remark: item.remark || null }
}
async function submit() {
  await formRef.value?.validate()
  if (!form.items.length) { ElMessage.warning('请至少添加一件商品'); return }
  for (let i = 0; i < form.items.length; i++) {
    const item = form.items[i]
    if (!String(item.goodsName || '').trim()) { ElMessage.warning(`请填写第 ${i + 1} 件商品名称`); return }
    if (!item.id && Number(item.depositAmount || 0) <= 0) { ElMessage.warning(`请填写第 ${i + 1} 件商品的定金`); return }
  }
  saving.value = true
  try {
    await saveCustom({ id: form.id, memberId: form.memberId || null, customerName: form.customerName, customerPhone: form.customerPhone, customerSex: form.customerSex, customDate: form.customDate, remark: form.remark, items: form.items.map(itemPayload) })
    ElMessage.success(form.id ? '定制单已修改' : '定金已入账，可按商品分别到货和取货')
    editor.value = false
    search()
  } finally { saving.value = false }
}
async function openDetail(row) {
  const res = await getCustom(row.id)
  detail.value = res.data
  detailOpen.value = true
}
function openArrival(item) { arrivalItem.value = item; Object.assign(arrivalForm, { barcode: item.barcode || '', remark: '' }); arrivalOpen.value = true }
async function submitArrival() {
  const barcode = arrivalForm.barcode.trim()
  if (!detail.value?.id || !arrivalItem.value?.id || !barcode) return
  arrivalSaving.value = true
  try {
    await arriveCustom(detail.value.id, { itemId: arrivalItem.value.id, barcode, remark: arrivalForm.remark })
    ElMessage.success('已登记到货，请及时联系客户')
    arrivalOpen.value = false
    await refreshDetail()
  } finally { arrivalSaving.value = false }
}
async function pickupItem(item) {
  try {
    await ElMessageBox.confirm(`确认客户已取走「${item.goodsName}」？其他商品不受影响。`, '登记取货', { type: 'warning', confirmButtonText: '确认取货' })
  } catch { return }
  await pickupCustom(detail.value.id, { itemId: item.id })
  ElMessage.success('已登记取货')
  await refreshDetail()
}
function openRefund(item) { refundItem.value = item; refundAmount.value = Number(item.availableAmount || 0); refundRemark.value = ''; refundOpen.value = true }
async function submitRefund() {
  if (!detail.value?.id || !refundItem.value?.id || refundAmount.value <= 0) return
  refundSaving.value = true
  try {
    await refundCustom({ id: detail.value.id, itemId: refundItem.value.id, amount: refundAmount.value, remark: refundRemark.value })
    ElMessage.success('该件商品的定金已退回')
    refundOpen.value = false
    await refreshDetail()
  } finally { refundSaving.value = false }
}
async function refreshDetail() {
  const res = await getCustom(detail.value.id)
  detail.value = res.data
  load()
}
async function removeRow(row) {
  const reason = Number(row.remainAmount || 0) > 0 ? `还有 ¥${money(row.remainAmount)} 定金未退清，请先按商品办理退定金。` : '删除后列表不再显示，定金流水仍保留；如在库商品仍由本单锁定，会同时解除。'
  try { await ElMessageBox.confirm(reason, '删除定制单 ' + row.customNo, { type: Number(row.remainAmount || 0) > 0 ? 'info' : 'warning', confirmButtonText: Number(row.remainAmount || 0) > 0 ? '知道了' : '确认删除' }) } catch { return }
  if (Number(row.remainAmount || 0) > 0) return
  await deleteCustom(row.id)
  ElMessage.success('定制单已删除')
  load()
}
onMounted(() => { loadOptions().catch(() => {}); load() })
</script>

<style scoped>
.jy-custom { height: 100%; min-height: 100%; display: flex; flex-direction: column; gap: 8px; padding: 10px 12px 12px; background: #fbf7f8; color: #3d3236; }
.page-bar, .filter-line, .sheet { background: #fff; border: 1px solid #f0e1e5; border-radius: 10px; }
.page-bar { display: flex; align-items: center; justify-content: space-between; padding: 8px 12px; }
.page-bar h1 { margin: 0; font-size: 16px; font-weight: 650; }
.bar-actions, .filter-line { display: flex; align-items: center; gap: 8px; }
.filter-line { padding: 8px 10px; }
.filter-line .el-input, .filter-line .el-select { width: 190px; }
.count { margin-left: auto; color: #8d737b; font-size: 12px; }
.sheet { flex: 1; min-height: 0; display: flex; flex-direction: column; padding: 0 0 8px; }
.sheet .el-table { flex: 1; }
.pager { display: flex; justify-content: flex-end; padding: 8px 10px 0; }
.stack { display: flex; flex-direction: column; line-height: 1.35; }
.stack small, .sub { color: #8d737b; font-size: 11px; }
.remain, .bind-code { color: #a85c6e; }
.pickup-warn { color: #b15b4c; font-weight: 600; }
.form-scroll { max-height: 70vh; overflow-y: auto; padding-right: 4px; }
.customer-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0 12px; padding: 10px 12px 2px; border: 1px solid #f0e1e5; border-radius: 8px; background: #fffafb; }
.span-2 { grid-column: span 2; }
.customer-grid :deep(.el-input), .customer-grid :deep(.el-select), .customer-grid :deep(.el-date-editor) { width: 100%; }
.items-head { display: flex; align-items: center; justify-content: space-between; margin: 14px 0 8px; }
.items-head div { display: flex; align-items: baseline; gap: 10px; }
.items-head span { color: #8d737b; font-size: 12px; }
.item-list { display: flex; flex-direction: column; gap: 8px; }
.item-card { padding: 9px 11px 10px; border: 1px solid #ecdde1; border-radius: 8px; background: #fff; }
.item-head, .item-title, .item-progress { display: flex; align-items: center; gap: 8px; }
.item-head { justify-content: space-between; margin-bottom: 7px; }
.item-fields { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 7px 9px; }
.field { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.field > span { color: #806c72; font-size: 11px; }
.field :deep(.el-input), .field :deep(.el-select), .field :deep(.el-date-editor), .field :deep(.el-input-number) { width: 100%; }
.deposit-field :deep(.el-input__wrapper) { box-shadow: 0 0 0 1px #d9a3af inset; }
.item-progress { margin-top: 7px; color: #7b9b86; font-size: 11px; }
.dialog-footer { display: flex; align-items: center; justify-content: space-between; }
.dialog-footer > span { color: #806c72; font-size: 12px; }
.dialog-footer b { color: #a85c6e; font-size: 15px; }
.goods-option { display: flex; flex-direction: column; line-height: 1.35; }
.goods-option span { color: #8d737b; font-size: 12px; }
.detail-head { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 12px; padding: 10px 12px; border-radius: 8px; background: #fbf5f7; }
.detail-head > div { display: flex; flex-direction: column; gap: 3px; }
.detail-head span { color: #806c72; font-size: 12px; }
.detail-money { align-items: flex-end; }
.detail-money b { color: #a85c6e; font-size: 17px; }
.detail-items { display: flex; flex-direction: column; gap: 8px; }
.detail-item { padding: 10px; border: 1px solid #f0e1e5; border-radius: 8px; }
.detail-item-head { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; }
.detail-item-head > div:first-child { display: flex; flex-direction: column; gap: 2px; }
.detail-item-head span { color: #8d737b; font-size: 11px; }
.detail-tags, .item-actions, .detail-numbers { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.detail-numbers { margin-top: 8px; color: #69575c; font-size: 12px; }
.item-actions { margin-top: 8px; }
.ledger-title { margin: 18px 0 12px; font-weight: 650; }
.el-timeline p { margin: 3px 0 0; color: #8d737b; font-size: 12px; }
.arrival-head, .refund-head { display: flex; flex-direction: column; gap: 4px; margin: -2px 0 16px; padding: 10px 12px; border-radius: 8px; background: #fbf5f7; }
.arrival-head span, .arrival-head small, .refund-head span { color: #806c72; font-size: 12px; }
@media (max-width: 900px) {
  .filter-line { flex-wrap: wrap; }
  .customer-grid { grid-template-columns: 1fr 1fr; }
  .item-fields { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
