<template>
  <div class="jy-custom">
    <div class="page-bar"><h1>定金退回</h1><el-button plain @click="router.push('/business/sale-custom')">返回销售定制</el-button></div>
    <section class="filter-line">
      <el-input v-model="query.keyword" clearable placeholder="单号 / 客户 / 手机 / 货品" @keyup.enter="search" @clear="search" />
      <el-button type="primary" plain @click="search">查询</el-button>
      <span class="count">按商品逐件退款；已被未完成销售单占用的金额不可退</span>
    </section>
    <section class="sheet">
      <el-table v-loading="loading" :data="rows" height="100%" empty-text="没有可退定金">
        <el-table-column label="定制单号" min-width="150"><template #default="{ row }"><b>{{ row.customNo }}</b><div class="sub">{{ dateText(row.customDate) }}</div></template></el-table-column>
        <el-table-column label="客户" min-width="145"><template #default="{ row }"><div class="stack"><b>{{ row.customerName }}</b><small>{{ row.customerPhone || '无手机号' }}</small></div></template></el-table-column>
        <el-table-column label="商品" min-width="240"><template #default="{ row }"><div class="item-lines"><div v-for="item in refundableItems(row)" :key="item.id" class="item-line"><div><b>{{ item.goodsName }}</b><small>{{ item.arrivalStatus === 1 ? '已到货' : '待到货' }} · 定金 ¥{{ money(item.depositAmount) }}</small></div><div class="item-line-action"><span class="remain">可退 ¥{{ money(item.availableAmount) }}</span><el-button v-hasPermi="['jingyue:custom:refund']" link type="primary" @click="openRefund(row, item)">退这件</el-button></div></div><span v-if="!refundableItems(row).length" class="sub">没有可退金额</span></div></template></el-table-column>
        <el-table-column label="整单剩余" width="110" align="right"><template #default="{ row }"><b class="remain">¥{{ money(row.remainAmount) }}</b></template></el-table-column>
      </el-table>
      <div class="pager"><el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[10, 20, 50]" layout="total, sizes, prev, pager, next" background small @size-change="load" @current-change="load" /></div>
    </section>

    <el-dialog v-model="dialog" title="退这件商品的定金" width="440px" append-to-body destroy-on-close>
      <div class="refund-head"><b>{{ currentOrder?.customNo }} · {{ currentItem?.goodsName }}</b><span>{{ currentOrder?.customerName }} · {{ currentOrder?.customerPhone }}</span><span>定金 ¥{{ money(currentItem?.depositAmount) }} · 已冲 ¥{{ money(currentItem?.offsetAmount) }} · 已退 ¥{{ money(currentItem?.refundAmount) }}</span></div>
      <el-form label-width="82px"><el-form-item label="退款金额"><el-input-number v-model="amount" :min="0.01" :max="maxAmount" :precision="2" controls-position="right" /></el-form-item><el-form-item label="退款备注"><el-input v-model="remark" maxlength="200" placeholder="例如：客户取消这件定制" /></el-form-item></el-form>
      <p class="hint">只影响当前商品，其他商品的定金、到货和取货状态保持不变。</p>
      <template #footer><el-button @click="dialog = false">取消</el-button><el-button type="danger" :loading="saving" @click="submit">确认退定金</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyCustomRefund">
import { onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { listCustom, refundCustom } from '@/api/jingyue/custom'

const router = useRouter()
const loading = ref(false)
const saving = ref(false)
const dialog = ref(false)
const rows = ref([])
const total = ref(0)
const currentOrder = ref(null)
const currentItem = ref(null)
const amount = ref(0)
const maxAmount = ref(0)
const remark = ref('')
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', remainOnly: 1 })

function money(v) { return Number(v || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function dateText(v) { return v ? String(v).slice(0, 10) : '' }
function refundableItems(row) { return (row.items || []).filter((item) => Number(item.availableAmount || 0) > 0) }
async function load() {
  loading.value = true
  try { const res = await listCustom(query); rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 } finally { loading.value = false }
}
function search() { query.pageNum = 1; load() }
function openRefund(order, item) {
  currentOrder.value = order
  currentItem.value = item
  maxAmount.value = Number(item.availableAmount || 0)
  amount.value = maxAmount.value
  remark.value = ''
  dialog.value = true
}
async function submit() {
  if (!currentOrder.value || !currentItem.value || amount.value <= 0) return
  saving.value = true
  try {
    await refundCustom({ id: currentOrder.value.id, itemId: currentItem.value.id, amount: amount.value, remark: remark.value })
    ElMessage.success('该件商品的定金已退回')
    dialog.value = false
    load()
  } finally { saving.value = false }
}
onMounted(load)
</script>

<style scoped>
.jy-custom { height: 100%; min-height: 100%; display: flex; flex-direction: column; gap: 8px; padding: 10px 12px 12px; background: #fbf7f8; color: #3d3236; }
.page-bar, .filter-line, .sheet { background: #fff; border: 1px solid #f0e1e5; border-radius: 10px; }
.page-bar { display: flex; align-items: center; justify-content: space-between; padding: 8px 12px; }
.page-bar h1 { margin: 0; font-size: 16px; font-weight: 650; }
.filter-line { display: flex; align-items: center; gap: 8px; padding: 8px 10px; }
.filter-line .el-input { width: 280px; }
.count { margin-left: auto; color: #8d737b; font-size: 12px; }
.sheet { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.sheet .el-table { flex: 1; }
.pager { display: flex; justify-content: flex-end; padding: 8px 10px; }
.stack, .item-line > div:first-child { display: flex; flex-direction: column; line-height: 1.35; }
.stack small, .item-line small, .sub, .hint { color: #8d737b; font-size: 12px; }
.remain { color: #a85c6e; }
.item-lines { display: flex; flex-direction: column; gap: 6px; }
.item-line { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 5px 7px; border-radius: 6px; background: #fcf7f8; }
.item-line-action { display: flex; align-items: center; gap: 8px; white-space: nowrap; }
.refund-head { display: flex; flex-direction: column; gap: 4px; margin: -2px 0 14px; padding: 10px 12px; border-radius: 8px; background: #fbf5f7; }
.refund-head span { color: #806c72; font-size: 12px; }
.hint { margin: 0; }
</style>
