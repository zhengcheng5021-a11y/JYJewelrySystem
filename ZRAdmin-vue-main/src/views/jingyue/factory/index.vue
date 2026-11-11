<template>
  <div class="jy-factory">
    <header class="head">
      <div class="head-copy">
        <span class="kicker">FACTORY RETURN</span>
        <h1>商品返厂</h1>
        <p>把在库商品退回供应商维修或换货，草稿不动库存，确认后商品转为返厂出库。</p>
      </div>
      <el-button v-hasPermi="['jingyue:factory:add']" type="primary" :icon="Plus" @click="openEditor()">新建返厂单</el-button>
    </header>

    <section class="filters">
      <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable :shortcuts="shortcuts" @change="search" />
      <el-input v-model="query.keyword" clearable placeholder="返厂单号 / 供应商 / 备注" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
      <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
        <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
      </el-select>
      <el-button type="primary" plain @click="search">查询</el-button>
      <span class="count">共 {{ total }} 张</span>
    </section>

    <section class="sheet">
      <el-table v-loading="loading" :data="rows" height="100%" row-key="id" empty-text="没有返厂单" @row-click="openDetail">
        <el-table-column label="返厂单号" min-width="165"><template #default="{ row }"><b class="no">{{ row.returnNo }}</b></template></el-table-column>
        <el-table-column label="返厂日期" width="120" align="center"><template #default="{ row }">{{ dateText(row.returnDate) }}</template></el-table-column>
        <el-table-column label="供应商" min-width="150"><template #default="{ row }">{{ row.supplierName || '未指定' }}</template></el-table-column>
        <el-table-column label="数量" width="90" align="right"><template #default="{ row }">{{ number(row.totalQty) }}</template></el-table-column>
        <el-table-column label="成本" width="120" align="right"><template #default="{ row }">¥{{ money(row.totalCost) }}</template></el-table-column>
        <el-table-column label="备注" min-width="180" show-overflow-tooltip><template #default="{ row }">{{ row.remark || '—' }}</template></el-table-column>
        <el-table-column label="制单" width="110" align="center"><template #default="{ row }">{{ row.createBy || '—' }}</template></el-table-column>
        <el-table-column label="状态" width="100" align="center">
          <template #default="{ row }"><el-tag size="small" effect="plain" :type="statusType(row.status)">{{ statusText(row.status) }}</el-tag></template>
        </el-table-column>
        <el-table-column label="操作" width="190" align="center">
          <template #default="{ row }">
            <el-button v-if="row.status === 0" v-hasPermi="['jingyue:factory:edit']" link type="primary" @click.stop="openEditor(row)">编辑</el-button>
            <el-button v-if="row.status === 0" v-hasPermi="['jingyue:factory:confirm']" link type="success" @click.stop="confirmRow(row)">确认返厂</el-button>
            <el-button v-if="row.status === 0" v-hasPermi="['jingyue:factory:remove']" link type="danger" @click.stop="removeRow(row)">删除</el-button>
            <el-button v-if="row.status !== 0" link type="primary" @click.stop="openDetail(row)">查看</el-button>
          </template>
        </el-table-column>
      </el-table>
      <div class="pager">
        <el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="load" @current-change="load" />
      </div>
    </section>

    <el-dialog v-model="editor" :title="form.id ? '编辑返厂单' : '新建返厂单'" width="960px" append-to-body destroy-on-close top="6vh">
      <el-form inline class="head-form">
        <el-form-item label="返厂日期"><el-date-picker v-model="form.returnDate" type="date" value-format="YYYY-MM-DD" :clearable="false" /></el-form-item>
        <el-form-item label="供应商">
          <el-select v-model="form.supplierId" clearable filterable placeholder="选择供应商">
            <el-option v-for="item in suppliers" :key="item.id" :label="item.supplierName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" maxlength="500" placeholder="返厂原因、收件人等信息" style="width: 320px" /></el-form-item>
      </el-form>

      <div class="scan-line">
        <el-input ref="scanRef" v-model="scanKeyword" clearable placeholder="扫描在库商品条码 / 输入货号后回车加入明细" :prefix-icon="Search" @keyup.enter="handleScan" />
        <span class="scan-tip">只允许在库（状态 1）且未锁定的商品返厂，单件商品数量只能是 1。</span>
      </div>

      <el-table :data="form.items" border size="small" class="line-table" empty-text="扫描条码添加返厂商品">
        <el-table-column type="index" label="#" width="46" align="center" />
        <el-table-column label="货号" min-width="140"><template #default="{ row }"><b>{{ row.goodsNo }}</b></template></el-table-column>
        <el-table-column label="条码" min-width="140"><template #default="{ row }">{{ row.barcode || '—' }}</template></el-table-column>
        <el-table-column label="商品名称" min-width="150"><template #default="{ row }">{{ row.goodsName || row.styleName || '—' }}</template></el-table-column>
        <el-table-column label="数量" width="110" align="center">
          <template #default="{ row }"><el-input-number v-model="row.qty" :min="1" :max="row.maxQty || 1" :precision="0" :disabled="row.maxQty === 1" size="small" controls-position="right" /></template>
        </el-table-column>
        <el-table-column label="成本" width="110" align="right"><template #default="{ row }">¥{{ money(row.costPrice) }}</template></el-table-column>
        <el-table-column label="返厂原因" min-width="170"><template #default="{ row }"><el-input v-model="row.reason" maxlength="200" size="small" placeholder="如：链扣松动" /></template></el-table-column>
        <el-table-column label="" width="56" align="center"><template #default="{ $index }"><el-button link type="danger" :icon="Delete" @click="form.items.splice($index, 1)" /></template></el-table-column>
      </el-table>

      <div class="editor-foot">
        <span>共 {{ form.items.length }} 条明细 · 成本合计 ¥{{ money(totalCost) }}</span>
        <div>
          <el-button plain @click="editor = false">取消</el-button>
          <el-button type="primary" :loading="saving" @click="submit">保存草稿</el-button>
        </div>
      </div>
    </el-dialog>

    <el-drawer v-model="detailOpen" size="640px" append-to-body>
      <template #header><div class="drawer-head"><span>RETURN SHEET</span><b>{{ detail.returnNo || '返厂单' }}</b></div></template>
      <div v-if="detail.id" class="detail">
        <div class="detail-meta">
          <div><span>返厂日期</span><b>{{ dateText(detail.returnDate) }}</b></div>
          <div><span>供应商</span><b>{{ detail.supplierName || '未指定' }}</b></div>
          <div><span>状态</span><b>{{ statusText(detail.status) }}</b></div>
          <div><span>数量 / 成本</span><b>{{ number(detail.totalQty) }} 件 · ¥{{ money(detail.totalCost) }}</b></div>
        </div>
        <p class="detail-remark">{{ detail.remark || '未填写备注' }}</p>
        <el-table :data="detail.items || []" size="small" border>
          <el-table-column label="货号" min-width="130" prop="goodsNo" />
          <el-table-column label="条码" min-width="130" prop="barcode" />
          <el-table-column label="名称" min-width="140" prop="goodsName" />
          <el-table-column label="数量" width="70" align="center" prop="qty" />
          <el-table-column label="成本" width="100" align="right"><template #default="{ row }">¥{{ money(row.costPrice) }}</template></el-table-column>
          <el-table-column label="原因" min-width="140" prop="reason" />
        </el-table>
        <div class="detail-foot">
          <span>制单 {{ detail.createBy || '—' }} · {{ dateTime(detail.createTime) }}</span>
          <el-button v-if="detail.status === 0" v-hasPermi="['jingyue:factory:edit']" type="primary" plain @click="openEditor(detail)">继续编辑</el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup name="JyFactory">
import { Plus, Search, Delete } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { listFactoryReturn, getFactoryReturn, findFactoryGoods, saveFactoryReturn, confirmFactoryReturn, deleteFactoryReturn } from '@/api/jingyue/factory'
import { listSupplier } from '@/api/jingyue/supplier'
import { serverNow, serverRange, serverTodayText } from '@/utils/serverTime'

const loading = ref(false)
const saving = ref(false)
const rows = ref([])
const total = ref(0)
const dateRange = ref(defaultRange())
const scanKeyword = ref('')
const scanRef = ref()
const editor = ref(false)
const detailOpen = ref(false)
const detail = ref({})
const suppliers = ref([])
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', status: undefined, beginTime: null, endTime: null })
const form = reactive({ id: 0, returnDate: today(), supplierId: null, remark: '', items: [] })

const statusOptions = [
  { value: 0, label: '草稿' },
  { value: 1, label: '已返厂' }
]
const shortcuts = [
  { text: '最近一周', value: () => { const e = serverNow(); const s = new Date(e); s.setDate(s.getDate() - 6); return [s, e] } },
  { text: '本月', value: () => { const e = serverNow(); return [new Date(e.getFullYear(), e.getMonth(), 1), e] } }
]

const totalCost = computed(() => form.items.reduce((sum, row) => sum + Number(row.costPrice || 0) * Number(row.qty || 0), 0))

function today() {
  return serverTodayText()
}
function defaultRange() {
  return serverRange(6)
}
function money(v) { return Number(v || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(v) { return Number(v || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function dateText(v) { return v ? String(v).slice(0, 10) : '—' }
function dateTime(v) { return v ? String(v).replace('T', ' ').slice(0, 16) : '—' }
function statusText(v) { return { 0: '草稿', 1: '已返厂', 9: '作废' }[v] || '未知' }
function statusType(v) { return v === 1 ? 'success' : v === 9 ? 'info' : 'warning' }

async function load() {
  loading.value = true
  try {
    query.beginTime = dateRange.value?.[0] || null
    query.endTime = dateRange.value?.[1] || null
    const res = await listFactoryReturn(query)
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  } finally { loading.value = false }
}
function search() { query.pageNum = 1; load() }

async function loadSuppliers() {
  try {
    const res = await listSupplier({ pageNum: 1, pageSize: 300 })
    suppliers.value = res.data?.result || res.data || []
  } catch (error) { suppliers.value = [] }
}

async function openEditor(row) {
  if (!suppliers.value.length) await loadSuppliers()
  if (row?.id) {
    const res = await getFactoryReturn(row.id)
    const data = res.data || {}
    Object.assign(form, {
      id: data.id,
      returnDate: data.returnDate ? String(data.returnDate).slice(0, 10) : today(),
      supplierId: data.supplierId || null,
      remark: data.remark || '',
      items: (data.items || []).map((it) => ({ ...it, maxQty: Math.max(1, Number(it.qty || 1)) }))
    })
  } else {
    Object.assign(form, { id: 0, returnDate: today(), supplierId: null, remark: '', items: [] })
  }
  scanKeyword.value = ''
  editor.value = true
  nextTick(() => scanRef.value?.focus?.())
}

async function handleScan() {
  const keyword = scanKeyword.value?.trim()
  if (!keyword) return
  try {
    const res = await findFactoryGoods(keyword)
    const list = res.data || []
    if (!list.length) return ElMessage.warning('没有找到可返厂的在库商品')
    const goods = list[0]
    const exist = form.items.find((it) => it.goodsItemId === goods.id)
    if (exist) return ElMessage.warning('该商品已在返厂明细中')
    form.items.push({
      goodsItemId: goods.id,
      goodsNo: goods.goodsNo,
      barcode: goods.barcode,
      goodsName: goods.styleName || goods.goodsName,
      qty: 1,
      maxQty: goods.itemMode === 1 ? 1 : Math.max(1, Number(goods.qty || 1)),
      costPrice: Number(goods.costPrice || 0),
      reason: ''
    })
    scanKeyword.value = ''
    nextTick(() => scanRef.value?.focus?.())
  } catch (error) { }
}

async function submit() {
  if (!form.items.length) return ElMessage.warning('请扫描添加返厂商品')
  saving.value = true
  try {
    await saveFactoryReturn({
      id: form.id,
      returnDate: form.returnDate,
      supplierId: form.supplierId || null,
      remark: form.remark?.trim() || null,
      items: form.items.map((it) => ({ goodsItemId: it.goodsItemId, qty: Number(it.qty || 1), reason: it.reason?.trim() || null }))
    })
    ElMessage.success('返厂单已保存')
    editor.value = false
    load()
  } finally { saving.value = false }
}

async function openDetail(row) {
  const res = await getFactoryReturn(row.id)
  detail.value = res.data || {}
  detailOpen.value = true
}

async function confirmRow(row) {
  try {
    await ElMessageBox.confirm(`确认返厂单 ${row.returnNo}？确认后商品状态变为「返厂出库」，库存直接扣减。`, '确认返厂', { type: 'warning', confirmButtonText: '确认返厂', cancelButtonText: '取消' })
  } catch (error) { return }
  await confirmFactoryReturn(row.id)
  ElMessage.success('已确认返厂')
  load()
}

async function removeRow(row) {
  try {
    await ElMessageBox.confirm(`确认删除返厂单 ${row.returnNo}？`, '删除确认', { type: 'warning' })
  } catch (error) { return }
  await deleteFactoryReturn(row.id)
  ElMessage.success('已删除')
  load()
}

onMounted(() => { load(); loadSuppliers() })
</script>

<style scoped>
.jy-factory {
  --rose: #c2848f;
  --rose-deep: #a5666f;
  --ink: #3f3538;
  --muted: #8e7d82;
  --line: #f2e3e6;
  height: 100%;
  min-height: 100%;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px 16px 16px;
  background: #fcf8f9;
  color: var(--ink);
  font-size: 13px;
}
.head { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.kicker { font-size:12px; letter-spacing: 0.22em; color: var(--rose); font-weight: 700; }
.head-copy h1 { margin: 2px 0; font-size: 19px; font-weight: 650; }
.head-copy p { margin: 0; font-size: 12px; color: var(--muted); }
.filters { display: flex; flex-wrap: wrap; align-items: center; gap: 8px; padding: 8px 10px; background: #fff; border: 1px solid var(--line); border-radius: 10px; }
.filters .el-input { width: 240px; }
.filters .el-select { width: 140px; }
.count { margin-left: auto; font-size: 12px; color: var(--muted); }
.sheet { flex: 1; min-height: 0; display: flex; flex-direction: column; background: #fff; border: 1px solid var(--line); border-radius: 10px; padding-bottom: 8px; }
.sheet .el-table { flex: 1; }
.no { color: var(--rose-deep); font-weight: 600; }
.pager { display: flex; justify-content: flex-end; padding: 8px 10px 0; }
.head-form :deep(.el-form-item) { margin-bottom: 10px; margin-right: 12px; }
.scan-line { display: flex; align-items: center; gap: 10px; margin: 4px 0 10px; }
.scan-line .el-input { max-width: 460px; }
.scan-tip { font-size: 11.5px; color: var(--muted); }
.line-table { margin-bottom: 10px; }
.editor-foot { display: flex; align-items: center; justify-content: space-between; padding-top: 10px; border-top: 1px solid var(--line); font-size: 12px; color: var(--muted); }
.drawer-head { display: flex; flex-direction: column; gap: 2px; }
.drawer-head span { font-size:12px; letter-spacing: 0.2em; color: var(--rose); }
.drawer-head b { font-size: 17px; }
.detail { display: flex; flex-direction: column; gap: 12px; }
.detail-meta { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 14px; padding: 12px 14px; background: #fdf8f9; border: 1px solid var(--line); border-radius: 10px; }
.detail-meta span { display: block; font-size: 11px; color: var(--muted); }
.detail-meta b { font-size: 13px; font-weight: 600; }
.detail-remark { margin: 0; font-size: 12px; color: var(--muted); line-height: 1.7; }
.detail-foot { display: flex; align-items: center; justify-content: space-between; font-size: 11.5px; color: var(--muted); }
</style>