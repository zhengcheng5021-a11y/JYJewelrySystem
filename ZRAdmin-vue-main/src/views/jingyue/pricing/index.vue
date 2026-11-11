<template>
  <div class="jy-pricing">
    <header class="pricing-head">
      <div>
        <span class="eyebrow">PRICING ENGINE</span>
        <h1>价格与折扣</h1>
        <p>维护成色金价与会员折扣；销售按有效金价结算并保留成交快照。</p>
      </div>
      <div class="head-stats">
        <div><span>当前有效金价</span><b>{{ currentMetals.length }}</b></div>
        <div><span>启用折扣规则</span><b>{{ activeRuleCount }}</b></div>
      </div>
    </header>

    <el-tabs v-model="activeTab" class="pricing-tabs">
      <el-tab-pane label="金价维护" name="metal">
        <div class="tab-toolbar">
          <el-select v-model="metalQuery.metalItemId" clearable filterable placeholder="全部金属成色" @change="loadMetals">
            <el-option v-for="item in options.metals" :key="item.id" :label="item.itemName" :value="item.id" />
          </el-select>
          <el-select v-model="metalQuery.status" clearable placeholder="全部状态" @change="loadMetals">
            <el-option label="启用" :value="0" />
            <el-option label="停用" :value="1" />
          </el-select>
          <el-button :loading="metalLoading" plain @click="loadMetals">刷新</el-button>
          <span class="toolbar-note">销售只取生效中且时间有效的最新一条，旧单据保留成交快照。</span>
          <el-button v-hasPermi="['jingyue:pricing:add']" type="primary" @click="openMetal()">新增金价</el-button>
        </div>
        <el-table v-loading="metalLoading" :data="metalRows" class="pricing-table">
          <el-table-column prop="metalName" label="金属成色" min-width="140" />
          <el-table-column label="每克价格" width="150" align="right">
            <template #default="{ row }"><b class="money">¥{{ money(row.pricePerGram) }}</b></template>
          </el-table-column>
          <el-table-column label="生效时间" width="170"><template #default="{ row }">{{ dateText(row.effectiveTime) }}</template></el-table-column>
          <el-table-column label="失效时间" width="170"><template #default="{ row }">{{ row.expireTime ? dateText(row.expireTime) : '长期有效' }}</template></el-table-column>
          <el-table-column label="状态" width="110">
            <template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'" effect="plain">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template>
          </el-table-column>
          <el-table-column label="当前适用" width="110">
            <template #default="{ row }"><span :class="['current-flag', { on: row.isCurrent }]">{{ row.isCurrent ? '当前' : '历史' }}</span></template>
          </el-table-column>
          <el-table-column prop="remark" label="备注" min-width="180" show-overflow-tooltip />
          <el-table-column label="操作" width="140" fixed="right">
            <template #default="{ row }">
              <el-button v-hasPermi="['jingyue:pricing:edit']" link type="primary" @click="openMetal(row)">编辑</el-button>
              <el-button v-hasPermi="['jingyue:pricing:remove']" link type="danger" @click="removeMetal(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <pagination v-show="metalTotal > 0" :total="metalTotal" v-model:page="metalQuery.pageNum" v-model:limit="metalQuery.pageSize" @pagination="loadMetals" />
      </el-tab-pane>

      <el-tab-pane label="会员折扣矩阵" name="discount">
        <div class="tab-toolbar">
          <el-select v-model="ruleQuery.memberLevelId" clearable placeholder="全部会员等级" @change="loadRules">
            <el-option v-for="item in options.levels" :key="item.id" :label="item.levelName" :value="item.id" />
          </el-select>
          <el-select v-model="ruleQuery.pricingMode" clearable placeholder="全部计价方式" @change="loadRules">
            <el-option label="按件" :value="1" />
            <el-option label="按克" :value="2" />
          </el-select>
          <el-button :loading="ruleLoading" plain @click="loadRules">刷新</el-button>
          <span class="toolbar-note">精确匹配：二级分类优先于一级分类，最后回退等级默认折扣；会员折扣与活动优惠不叠加。</span>
          <el-button v-hasPermi="['jingyue:pricing:add']" type="primary" @click="openRule()">新增规则</el-button>
        </div>
        <el-table v-loading="ruleLoading" :data="ruleRows" class="pricing-table">
          <el-table-column prop="memberLevelName" label="会员等级" width="140" />
          <el-table-column label="适用分类" min-width="170">
            <template #default="{ row }">{{ row.styleParentName ? row.styleParentName + ' / ' : '' }}{{ row.styleItemName || '全部分类' }}</template>
          </el-table-column>
          <el-table-column label="计价方式" width="110"><template #default="{ row }">{{ pricingText(row.pricingMode) }}</template></el-table-column>
          <el-table-column label="折扣" width="110" align="right"><template #default="{ row }"><b class="money">{{ rateText(row.discountRate) }}</b></template></el-table-column>
          <el-table-column label="折扣基数" width="150"><template #default="{ row }">{{ row.discountBase === 2 ? '金料 + 工费' : '仅金料/商品' }}</template></el-table-column>
          <el-table-column prop="priority" label="优先级" width="90" align="center" />
          <el-table-column label="状态" width="100"><template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'" effect="plain">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template></el-table-column>
          <el-table-column label="操作" width="140" fixed="right">
            <template #default="{ row }">
              <el-button v-hasPermi="['jingyue:pricing:edit']" link type="primary" @click="openRule(row)">编辑</el-button>
              <el-button v-hasPermi="['jingyue:pricing:remove']" link type="danger" @click="removeRule(row)">删除</el-button>
            </template>
          </el-table-column>
        </el-table>
        <pagination v-show="ruleTotal > 0" :total="ruleTotal" v-model:page="ruleQuery.pageNum" v-model:limit="ruleQuery.pageSize" @pagination="loadRules" />
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="metalOpen" :title="metalForm.id ? '编辑金价' : '新增金价'" width="560px" append-to-body :close-on-click-modal="false">
      <el-form ref="metalFormRef" :model="metalForm" :rules="metalRules" label-position="top">
        <el-form-item label="金属成色" prop="metalItemId">
          <el-select v-model="metalForm.metalItemId" filterable placeholder="选择金属成色" style="width: 100%">
            <el-option v-for="item in options.metals" :key="item.id" :label="item.itemName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="每克价格" prop="pricePerGram"><el-input-number v-model="metalForm.pricePerGram" :min="0.01" :max="1000000" :precision="2" :step="10" controls-position="right" style="width: 100%" /></el-form-item>
        <el-form-item label="生效时间" prop="effectiveTime"><el-date-picker v-model="metalForm.effectiveTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width: 100%" /></el-form-item>
        <el-form-item label="失效时间"><el-date-picker v-model="metalForm.expireTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" clearable placeholder="留空表示长期有效" style="width: 100%" /></el-form-item>
        <el-form-item label="状态"><el-radio-group v-model="metalForm.status"><el-radio-button :value="0">启用</el-radio-button><el-radio-button :value="1">停用</el-radio-button></el-radio-group></el-form-item>
        <el-form-item label="备注"><el-input v-model="metalForm.remark" type="textarea" :rows="2" maxlength="500" show-word-limit /></el-form-item>
      </el-form>
      <template #footer><el-button @click="metalOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submitMetal">保存</el-button></template>
    </el-dialog>

    <el-dialog v-model="ruleOpen" :title="ruleForm.id ? '编辑折扣规则' : '新增折扣规则'" width="620px" append-to-body :close-on-click-modal="false">
      <el-form ref="ruleFormRef" :model="ruleForm" :rules="ruleRules" label-position="top">
        <div class="form-grid">
          <el-form-item label="会员等级" prop="memberLevelId"><el-select v-model="ruleForm.memberLevelId" filterable placeholder="选择会员等级"><el-option v-for="item in options.levels" :key="item.id" :label="item.levelName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="计价方式"><el-select v-model="ruleForm.pricingMode" clearable placeholder="全部"><el-option label="按件" :value="1" /><el-option label="按克" :value="2" /></el-select></el-form-item>
        </div>
        <div class="form-grid">
          <el-form-item label="款式大类"><el-select v-model="ruleForm.styleParentId" clearable filterable placeholder="全部分类" @change="onRuleParentChange"><el-option v-for="item in styleParents" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="款式分类"><el-select v-model="ruleForm.styleItemId" clearable filterable :disabled="!ruleForm.styleParentId" placeholder="全部子类"><el-option v-for="item in styleChildren" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
        </div>
        <div class="form-grid">
          <el-form-item label="折扣率" prop="discountRate"><el-input-number v-model="ruleForm.discountRate" :min="0.01" :max="1" :precision="2" :step="0.01" controls-position="right" /><small class="field-hint">0.85 表示 8.5 折</small></el-form-item>
          <el-form-item label="折扣基数"><el-select v-model="ruleForm.discountBase"><el-option :value="1" label="仅金料 / 商品金额" /><el-option :value="2" label="金料 + 工费" /></el-select></el-form-item>
        </div>
        <div class="form-grid">
          <el-form-item label="优先级"><el-input-number v-model="ruleForm.priority" :min="0" controls-position="right" /></el-form-item>
          <el-form-item label="状态"><el-radio-group v-model="ruleForm.status"><el-radio-button :value="0">启用</el-radio-button><el-radio-button :value="1">停用</el-radio-button></el-radio-group></el-form-item>
        </div>
        <div class="form-grid">
          <el-form-item label="生效时间"><el-date-picker v-model="ruleForm.effectiveTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" clearable placeholder="留空表示立即生效" style="width: 100%" /></el-form-item>
          <el-form-item label="失效时间"><el-date-picker v-model="ruleForm.expireTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" clearable placeholder="留空表示长期有效" style="width: 100%" /></el-form-item>
        </div>
        <el-form-item label="备注"><el-input v-model="ruleForm.remark" type="textarea" :rows="2" maxlength="500" show-word-limit /></el-form-item>
      </el-form>
      <template #footer><el-button @click="ruleOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submitRule">保存</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyPricing">
import { ref, reactive, computed, onMounted } from 'vue'
import { ElMessageBox } from 'element-plus'
import { getCurrentInstance } from 'vue'
import { listMetalPrices, saveMetalPrice, deleteMetalPrice, listDiscountRules, saveDiscountRule, deleteDiscountRule, getPricingOptions } from '@/api/jingyue/pricing'

const { proxy } = getCurrentInstance()
const activeTab = ref('metal')
const saving = ref(false)
const options = reactive({ metals: [], categories: [], levels: [] })
const metalLoading = ref(false)
const ruleLoading = ref(false)
const metalRows = ref([])
const ruleRows = ref([])
const metalTotal = ref(0)
const ruleTotal = ref(0)
const metalQuery = reactive({ pageNum: 1, pageSize: 20, metalItemId: undefined, status: undefined })
const ruleQuery = reactive({ pageNum: 1, pageSize: 20, memberLevelId: undefined, pricingMode: undefined })
const metalOpen = ref(false)
const ruleOpen = ref(false)
const metalFormRef = ref()
const ruleFormRef = ref()
const metalForm = reactive(emptyMetal())
const ruleForm = reactive(emptyRule())
const metalRules = {
  metalItemId: [{ required: true, message: '请选择金属成色', trigger: 'change' }],
  pricePerGram: [{ required: true, message: '请输入每克价格', trigger: 'blur' }],
  effectiveTime: [{ required: true, message: '请选择生效时间', trigger: 'change' }]
}
const ruleRules = {
  memberLevelId: [{ required: true, message: '请选择会员等级', trigger: 'change' }],
  discountRate: [{ required: true, message: '请输入折扣率', trigger: 'blur' }]
}
const styleParents = computed(() => options.categories.filter((item) => item.level === 1))
const styleChildren = computed(() => options.categories.filter((item) => item.level === 2 && Number(item.parentId) === Number(ruleForm.styleParentId)))
const currentMetals = computed(() => metalRows.value.filter((row) => row.isCurrent))
const activeRuleCount = computed(() => ruleRows.value.filter((row) => row.status === 0).length)

function emptyMetal() {
  return { id: 0, metalItemId: undefined, pricePerGram: 0, effectiveTime: nowText(), expireTime: null, status: 0, remark: '' }
}
function emptyRule() {
  return { id: 0, memberLevelId: undefined, styleParentId: undefined, styleItemId: undefined, pricingMode: undefined, discountRate: 1, discountBase: 1, priority: 0, effectiveTime: null, expireTime: null, status: 0, remark: '' }
}
function nowText() {
  const d = new Date()
  const p = (v) => String(v).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function dateText(value) { return value ? String(value).replace('T', ' ').slice(0, 16) : '-' }
function rateText(value) { const rate = Number(value || 1); return rate > 0 && rate < 1 ? (rate * 10).toFixed(1).replace(/\.0$/, '') + ' 折' : '不打折' }
function pricingText(value) { return value === 1 ? '按件' : value === 2 ? '按克' : '全部' }
function onRuleParentChange() { ruleForm.styleItemId = undefined }

async function loadOptions() {
  const res = await getPricingOptions()
  const data = res.data || {}
  options.metals = data.metals || []
  options.categories = data.categories || []
  options.levels = data.levels || []
}
function loadMetals() {
  metalLoading.value = true
  listMetalPrices(metalQuery).then((res) => {
    metalRows.value = res.data?.result || []
    metalTotal.value = res.data?.totalNum || 0
  }).finally(() => { metalLoading.value = false })
}
function loadRules() {
  ruleLoading.value = true
  listDiscountRules(ruleQuery).then((res) => {
    ruleRows.value = res.data?.result || []
    ruleTotal.value = res.data?.totalNum || 0
  }).finally(() => { ruleLoading.value = false })
}
function openMetal(row) {
  Object.assign(metalForm, emptyMetal(), row ? {
    id: row.id, metalItemId: row.metalItemId, pricePerGram: Number(row.pricePerGram || 0),
    effectiveTime: row.effectiveTime ? String(row.effectiveTime).replace(' ', 'T').slice(0, 19) : nowText(),
    expireTime: row.expireTime ? String(row.expireTime).replace(' ', 'T').slice(0, 19) : null,
    status: row.status, remark: row.remark || ''
  } : {})
  metalOpen.value = true
}
function openRule(row) {
  const styleId = row?.styleItemId || undefined
  const parent = options.categories.find((item) => Number(item.id) === Number(styleId))?.parentId || undefined
  Object.assign(ruleForm, emptyRule(), row ? {
    id: row.id, memberLevelId: row.memberLevelId, styleParentId: parent, styleItemId: styleId,
    pricingMode: row.pricingMode ?? undefined, discountRate: Number(row.discountRate || 1),
    discountBase: row.discountBase || 1, priority: row.priority || 0,
    effectiveTime: row.effectiveTime ? String(row.effectiveTime).replace(' ', 'T').slice(0, 19) : null,
    expireTime: row.expireTime ? String(row.expireTime).replace(' ', 'T').slice(0, 19) : null,
    status: row.status, remark: row.remark || ''
  } : {})
  ruleOpen.value = true
}
function submitMetal() {
  metalFormRef.value?.validate(async (valid) => {
    if (!valid) return
    saving.value = true
    try {
      await saveMetalPrice({ ...metalForm, metalItemId: metalForm.metalItemId, expireTime: metalForm.expireTime || null })
      proxy.$modal.msgSuccess('金价已保存')
      metalOpen.value = false
      loadMetals()
    } finally { saving.value = false }
  })
}
function submitRule() {
  ruleFormRef.value?.validate(async (valid) => {
    if (!valid) return
    saving.value = true
    try {
      await saveDiscountRule({
        ...ruleForm,
        styleItemId: ruleForm.styleItemId || null,
        pricingMode: ruleForm.pricingMode ?? null,
        effectiveTime: ruleForm.effectiveTime || null,
        expireTime: ruleForm.expireTime || null
      })
      proxy.$modal.msgSuccess('折扣规则已保存')
      ruleOpen.value = false
      loadRules()
    } finally { saving.value = false }
  })
}
async function removeMetal(row) {
  try {
    await ElMessageBox.confirm('删除这条金价记录？已被销售单引用的记录不能删除。', '删除金价', { type: 'warning' })
    await deleteMetalPrice(row.id)
    proxy.$modal.msgSuccess('已删除')
    loadMetals()
  } catch (error) {}
}
async function removeRule(row) {
  try {
    await ElMessageBox.confirm('删除这条折扣规则？', '删除规则', { type: 'warning' })
    await deleteDiscountRule(row.id)
    proxy.$modal.msgSuccess('已删除')
    loadRules()
  } catch (error) {}
}
onMounted(async () => {
  await loadOptions()
  loadMetals()
  loadRules()
})
</script>

<style scoped>
.jy-pricing { min-height: 100%; padding: 20px; color: #3d3236; background: linear-gradient(180deg, #fdf7f8 0%, #fbf3f5 100%); }
.pricing-head { display: flex; align-items: center; justify-content: space-between; gap: 24px; padding: 20px 22px; background: #fff; border: 1px solid #f0dfe3; border-radius: 12px; }
.eyebrow { color: #c4788a; font-size: 11px; font-weight: 700; letter-spacing: .18em; }
.pricing-head h1 { margin: 5px 0 5px; font-size: 23px; font-weight: 700; }
.pricing-head p { margin: 0; color: #8b7a80; font-size: 12.5px; line-height: 1.6; }
.head-stats { display: flex; gap: 28px; flex: none; }
.head-stats > div { display: flex; flex-direction: column; gap: 4px; }
.head-stats span { color: #8b7a80; font-size: 11px; }
.head-stats b { color: #a85c6e; font-size: 22px; }
.pricing-tabs { margin-top: 14px; padding: 0 18px 18px; background: #fff; border: 1px solid #f0dfe3; border-radius: 12px; }
.tab-toolbar { display: flex; align-items: center; flex-wrap: wrap; gap: 10px; margin: 4px 0 14px; }
.tab-toolbar :deep(.el-select) { width: 180px; }
.toolbar-note { flex: 1 1 260px; color: #9a858b; font-size: 11.5px; line-height: 1.5; }
.pricing-table :deep(th.el-table__cell) { background: #fdf6f8; color: #7c6c72; font-size: 11.5px; }
.money { color: #a25464; font-size: 13px; }
.current-flag { padding: 2px 7px; color: #9a858b; background: #f5eff0; border-radius: 4px; font-size: 11px; }
.current-flag.on { color: #3d8b6d; background: #edf8f2; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 16px; }
.form-grid :deep(.el-select), .form-grid :deep(.el-input-number) { width: 100%; }
.field-hint { display: block; margin-top: 4px; color: #9a858b; font-size: 11px; }
@media (max-width: 820px) { .jy-pricing { padding: 12px; } .pricing-head { align-items: flex-start; flex-direction: column; } .head-stats { width: 100%; justify-content: space-between; } .pricing-tabs { padding: 0 10px 12px; } .tab-toolbar :deep(.el-select) { width: 100%; } .toolbar-note { order: 5; } .form-grid { grid-template-columns: 1fr; } }
</style>
