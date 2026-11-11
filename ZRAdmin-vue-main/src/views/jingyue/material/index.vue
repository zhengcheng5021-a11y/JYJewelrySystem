<template>
  <div class="material-page">
    <header class="page-hero">
      <div>
        <span class="eyebrow">MATERIAL ATELIER</span>
        <h1>物料档案</h1>
        <p>按克管理银料、金料与辅料。库存只由确认后的物料单据改变，档案本身不直接改余额。</p>
      </div>
      <div class="hero-actions">
        <el-button plain :loading="loading" @click="refreshAll"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button v-hasPermi="['jingyue:material:add']" type="primary" @click="openEditor()"><el-icon><Plus /></el-icon>新增物料</el-button>
      </div>
      <i class="hero-glow"></i>
    </header>

    <section class="metric-board" v-loading="summaryLoading">
      <article class="metric-feature">
        <span>当前物料种类</span>
        <strong>{{ summary.materialCount || 0 }}<em> 种</em></strong>
        <p>已启用并参与库存核算的物料</p>
        <div class="metric-foot"><span>零库存 {{ summary.lowStockCount || 0 }} 种</span><b>按克核算</b></div>
      </article>
      <article>
        <span class="metric-label">物料库存总重</span>
        <strong>{{ weight(summary.totalStockWeight) }}<em> g</em></strong>
        <small>所有启用物料库存合计</small>
        <i class="metric-icon"><el-icon><Coin /></el-icon></i>
      </article>
      <article>
        <span class="metric-label">库存移动平均成本</span>
        <strong class="money">¥{{ money(summary.totalStockCost) }}</strong>
        <small>按当前平均成本估算</small>
        <i class="metric-icon gold"><el-icon><Money /></el-icon></i>
      </article>
      <article class="recent-card">
        <span class="metric-label">最近建档</span>
        <div v-if="summary.recent?.length" class="recent-list">
          <button v-for="row in summary.recent.slice(0, 3)" v-hasPermi="['jingyue:material:edit']" :key="row.id" type="button" @click="openEditor(row)">
            <b>{{ row.materialName }}</b><span>{{ row.materialCode }}</span>
          </button>
        </div>
        <small v-else>还没有物料档案</small>
      </article>
    </section>

    <section class="list-panel">
      <div class="panel-head">
        <div><span class="section-kicker">MATERIAL REGISTER</span><h2>物料明细</h2><p>编码、分类、成色和默认供应商集中维护</p></div>
        <div class="filters">
          <el-input v-model="query.keyword" clearable placeholder="编码或名称" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
          <el-select v-model="query.categoryItemId" clearable filterable placeholder="全部分类" @change="search">
            <el-option v-for="item in options.categories" :key="item.id" :label="optionLabel(item)" :value="item.id" />
          </el-select>
          <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
            <el-option label="启用" :value="0" /><el-option label="停用" :value="1" />
          </el-select>
          <el-button @click="search">查询</el-button>
        </div>
      </div>

      <el-table v-loading="loading" :data="rows" height="100%" row-key="id" class="material-table">
        <el-table-column label="物料" min-width="210">
          <template #default="{ row }">
            <div class="material-cell"><span class="material-mark">{{ row.materialName.slice(0, 1) }}</span><div><b>{{ row.materialName }}</b><small>{{ row.materialCode }}</small></div></div>
          </template>
        </el-table-column>
        <el-table-column label="分类 / 成色" min-width="160">
          <template #default="{ row }"><div class="stack"><span>{{ row.categoryName || '未分类' }}</span><small>{{ row.metalName || '未设成色' }}</small></div></template>
        </el-table-column>
        <el-table-column label="当前库存" width="140" align="right">
          <template #default="{ row }"><div class="stock-cell"><b>{{ weight(row.stockWeight) }}</b><small>g</small><em v-if="Number(row.lockedWeight) > 0">锁定 {{ weight(row.lockedWeight) }} g</em></div></template>
        </el-table-column>
        <el-table-column label="平均成本 / 克" width="145" align="right"><template #default="{ row }">¥{{ money(row.avgCostPerGram) }}</template></el-table-column>
        <el-table-column label="销售参考 / 克" width="145" align="right"><template #default="{ row }">¥{{ money(row.salePricePerGram) }}</template></el-table-column>
        <el-table-column label="默认供应商" min-width="140"><template #default="{ row }">{{ row.supplierName || '未设置' }}</template></el-table-column>
        <el-table-column label="状态" width="86"><template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'" effect="plain">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template></el-table-column>
        <el-table-column label="操作" width="142" fixed="right">
          <template #default="{ row }"><el-button v-hasPermi="['jingyue:material:edit']" link type="primary" @click="openEditor(row)">编辑</el-button><el-button v-hasPermi="['jingyue:material:remove']" link type="danger" @click="remove(row)">删除</el-button></template>
        </el-table-column>
        <template #empty><div class="empty-panel"><el-icon><Box /></el-icon><h3>还没有物料档案</h3><p>先建立银料、金料或辅料档案，再到物料入库登记重量。</p><el-button v-hasPermi="['jingyue:material:add']" type="primary" @click="openEditor()">新增第一项物料</el-button></div></template>
      </el-table>
      <div class="list-foot"><span>共 {{ total }} 项物料</span><pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" /></div>
    </section>

    <el-drawer v-model="editorOpen" size="520px" append-to-body :with-header="false">
      <div class="editor-wrap">
        <div class="drawer-heading"><span>{{ form.id ? 'EDIT MATERIAL' : 'NEW MATERIAL' }}</span><b>{{ form.id ? '编辑物料档案' : '新增物料档案' }}</b><button type="button" @click="editorOpen = false"><el-icon><Close /></el-icon></button></div>
        <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="material-form">
          <div class="form-grid two">
            <el-form-item label="物料编码" prop="materialCode"><el-input v-model="form.materialCode" maxlength="32" placeholder="例如 SILVER-001" /></el-form-item>
            <el-form-item label="物料名称" prop="materialName"><el-input v-model="form.materialName" maxlength="100" placeholder="例如 足银板料" /></el-form-item>
            <el-form-item label="物料分类"><el-select v-model="form.categoryItemId" clearable filterable placeholder="选择分类"><el-option v-for="item in options.categories" :key="item.id" :label="optionLabel(item)" :value="item.id" /></el-select></el-form-item>
            <el-form-item label="金属 / 成色"><el-select v-model="form.metalItemId" clearable filterable placeholder="选择成色"><el-option v-for="item in options.metals" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
            <el-form-item label="默认供应商"><el-select v-model="form.supplierId" clearable filterable placeholder="可不选"><el-option v-for="item in options.suppliers" :key="item.id" :label="item.name" :value="item.id" /></el-select></el-form-item>
            <el-form-item label="销售参考价 / 克"><el-input-number v-model="form.salePricePerGram" :min="0" :precision="2" :step="1" controls-position="right" style="width:100%" /></el-form-item>
          </div>
          <el-form-item label="状态"><el-radio-group v-model="form.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group><p class="field-hint">停用后不能继续出入库，但历史单据和流水仍保留。</p></el-form-item>
          <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="3" maxlength="500" show-word-limit placeholder="规格、产地或内部说明" /></el-form-item>
        </el-form>
        <div class="editor-note"><el-icon><InfoFilled /></el-icon><span>库存重量与平均成本由物料单据确认后自动计算，不能在这里手工改写。</span></div>
        <div class="editor-actions"><el-button @click="editorOpen = false">取消</el-button><el-button type="primary" :loading="saving" @click="submit">保存物料</el-button></div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup name="JyMaterial">
import { Search, Plus, Refresh, Close, Box, Coin, Money, InfoFilled } from '@element-plus/icons-vue'
import { listMaterial, getMaterialSummary, getMaterialOptions, saveMaterial, deleteMaterial } from '@/api/jingyue/material'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const summaryLoading = ref(false)
const saving = ref(false)
const rows = ref([])
const total = ref(0)
const options = reactive({ categories: [], metals: [], suppliers: [] })
const summary = reactive({ materialCount: 0, totalStockWeight: 0, totalStockCost: 0, lowStockCount: 0, recent: [] })
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', categoryItemId: undefined, metalItemId: undefined, supplierId: undefined, status: undefined })
const editorOpen = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const rules = {
  materialCode: [{ required: true, message: '请输入物料编码', trigger: 'blur' }],
  materialName: [{ required: true, message: '请输入物料名称', trigger: 'blur' }]
}

function emptyForm() {
  return { id: 0, storeId: null, materialCode: '', materialName: '', categoryItemId: undefined, metalItemId: undefined, supplierId: undefined, salePricePerGram: 0, status: 0, remark: '' }
}
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const weight = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
const optionLabel = (item) => item.parentId ? '　└ ' + item.itemName : item.itemName

function loadList() {
  loading.value = true
  listMaterial(query).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function loadSummary() {
  summaryLoading.value = true
  getMaterialSummary().then((res) => Object.assign(summary, res.data || {})).finally(() => { summaryLoading.value = false })
}
function loadOptions() {
  getMaterialOptions().then((res) => {
    options.categories = res.data?.categories || []
    options.metals = res.data?.metals || []
    options.suppliers = res.data?.suppliers || []
  })
}
function refreshAll() { loadSummary(); loadList() }
function search() { query.pageNum = 1; loadList() }
function openEditor(row) {
  Object.assign(form, emptyForm())
  if (row) Object.assign(form, {
    id: row.id, storeId: row.storeId, materialCode: row.materialCode, materialName: row.materialName,
    categoryItemId: row.categoryItemId || undefined, metalItemId: row.metalItemId || undefined,
    supplierId: row.supplierId || undefined, salePricePerGram: Number(row.salePricePerGram || 0),
    status: row.status, remark: row.remark || ''
  })
  editorOpen.value = true
}
function submit() {
  formRef.value?.validate().then(() => {
    saving.value = true
    saveMaterial({ ...form }).then(() => {
      proxy.$modal.msgSuccess(form.id ? '物料已更新' : '物料已建档')
      editorOpen.value = false
      refreshAll()
    }).finally(() => { saving.value = false })
  }).catch(() => {})
}
function remove(row) {
  proxy.$modal.confirm('删除「' + row.materialName + '」？有库存、单据或流水的物料只能停用。').then(() => deleteMaterial(row.id)).then(() => {
    proxy.$modal.msgSuccess('物料已删除')
    refreshAll()
  }).catch(() => {})
}

loadOptions()
refreshAll()
</script>

<style scoped>
.material-page { display: flex; flex-direction: column; gap: 16px; min-height: calc(100vh - 132px); color: #4d373e; }
.page-hero { position: relative; display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; overflow: hidden; padding: 24px 28px; border: 1px solid #f1e1e3; border-radius: 18px; background: linear-gradient(120deg,#fffaf7 0%,#fdf1ee 48%,#f7e8e7 100%); box-shadow: 0 9px 28px rgba(111,74,81,.06); }
.page-hero > div:first-child { position: relative; z-index: 1; }
.eyebrow, .section-kicker { display: block; color: #b06e7a; font-size: 11px; font-weight: 700; letter-spacing: 2.1px; }
.page-hero h1 { margin: 7px 0 5px; color: #4f363e; font-family: "Songti SC","STSong",Georgia,serif; font-size: 31px; font-weight: 600; }
.page-hero p { max-width: 680px; margin: 0; color: #9b8188; font-size: 13px; line-height: 1.65; }
.hero-actions { position: relative; z-index: 1; display: flex; gap: 8px; flex: none; }
.hero-glow { position: absolute; right: -50px; bottom: -90px; width: 260px; height: 260px; border-radius: 50%; background: radial-gradient(circle,rgba(231,177,163,.34),transparent 68%); pointer-events: none; }
.metric-board { display: grid; grid-template-columns: 1.15fr 1fr 1fr 1fr; gap: 12px; }
.metric-board article { position: relative; min-height: 126px; padding: 18px 19px; overflow: hidden; border: 1px solid #f2e4e5; border-radius: 14px; background: #fff; box-shadow: 0 6px 20px rgba(110,74,80,.045); }
.metric-feature { background: linear-gradient(140deg,#fdf7f4,#f9ece9); }
.metric-feature > span, .metric-label { display: block; color: #a78b91; font-size: 12px; }
.metric-feature strong, .metric-board article > strong { display: block; margin-top: 13px; color: #553b43; font-family: "Songti SC","STSong",Georgia,serif; font-size: 28px; font-weight: 600; }
.metric-feature strong em, .metric-board article > strong em { color: #ac8a8f; font-family: inherit; font-size: 12px; font-style: normal; font-weight: 400; }
.metric-feature p, .metric-board article > small { display: block; margin: 4px 0 0; color: #b09ba0; font-size: 11px; }
.metric-foot { display: flex; justify-content: space-between; margin-top: 13px; padding-top: 10px; border-top: 1px dashed #eed8d8; color: #a4878d; font-size: 11px; }
.metric-foot b { color: #a86672; font-weight: 600; }
.metric-icon { position: absolute; right: 17px; top: 17px; display: grid; place-items: center; width: 30px; height: 30px; border-radius: 50%; background: #fdf2f1; color: #bc7e84; font-style: normal; }
.metric-icon.gold { color: #b29360; background: #fcf7ed; }
.recent-list { display: flex; flex-direction: column; gap: 5px; margin-top: 9px; }
.recent-list button { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; min-width: 0; padding: 0; color: #6c4e54; text-align: left; border: 0; background: transparent; cursor: pointer; }
.recent-list b { overflow: hidden; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.recent-list span { color: #b49ca0; font-size: 10px; white-space: nowrap; }
.list-panel { display: flex; flex: 1; min-height: 400px; flex-direction: column; padding: 17px 19px 11px; border: 1px solid #f2e4e5; border-radius: 16px; background: #fff; box-shadow: 0 7px 22px rgba(106,68,77,.045); }
.panel-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; padding: 0 2px 13px; }
.panel-head h2 { margin: 3px 0 2px; color: #573e45; font-size: 19px; font-weight: 600; }
.panel-head p { margin: 0; color: #b09ba0; font-size: 11px; }
.filters { display: flex; align-items: center; gap: 7px; }
.filters .el-input { width: 180px; }
.filters .el-select { width: 126px; }
.filters .el-button { margin-left: 0; }
.material-table { flex: 1; }
.material-cell { display: flex; align-items: center; gap: 10px; }
.material-mark { display: grid; flex: none; place-items: center; width: 31px; height: 31px; border-radius: 9px; background: #fdf0ee; color: #b5707b; font-family: "Songti SC","STSong",serif; font-size: 16px; }
.material-cell b, .stack span { display: block; color: #62484f; font-size: 12.5px; font-weight: 500; }
.material-cell small, .stack small { display: block; margin-top: 2px; color: #b09aa0; font-size: 10.5px; }
.stack { line-height: 1.4; }
.stock-cell b { color: #5b4148; font-size: 14px; font-weight: 600; font-variant-numeric: tabular-nums; }
.stock-cell small { margin-left: 2px; color: #b0989d; font-size: 10px; }
.stock-cell em { display: block; color: #bd8389; font-size: 10px; font-style: normal; }
.list-foot { display: flex; align-items: center; justify-content: space-between; padding-top: 10px; color: #b19ba0; font-size: 11px; }
.empty-panel { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 280px; color: #ac959a; }
.empty-panel .el-icon { font-size: 26px; color: #c79599; }
.empty-panel h3 { margin: 11px 0 4px; color: #694f56; font-size: 16px; font-weight: 500; }
.empty-panel p { margin: 0 0 15px; font-size: 12px; }
.editor-wrap { padding: 2px 3px 4px; color: #523b42; }
.drawer-heading { display: flex; align-items: center; gap: 10px; padding-bottom: 15px; border-bottom: 1px solid #f1e2e4; }
.drawer-heading > span { color: #bfa080; font-size: 11px; letter-spacing: 1.9px; }
.drawer-heading > b { color: #5b4048; font-size: 16px; font-weight: 500; }
.drawer-heading > button { display: grid; place-items: center; width: 27px; height: 27px; margin-left: auto; padding: 0; color: #9c838a; background: #fff; border: 1px solid #f0e1e3; border-radius: 50%; cursor: pointer; }
.material-form { margin-top: 18px; }
.form-grid.two { display: grid; grid-template-columns: 1fr 1fr; gap: 0 14px; }
.field-hint { margin: 5px 0 0; color: #b19ba0; font-size: 11px; line-height: 1.45; }
.editor-note { display: flex; align-items: flex-start; gap: 8px; margin: 0 0 16px; padding: 11px 12px; color: #9e7c82; font-size: 11.5px; line-height: 1.5; background: #fdf6f4; border: 1px solid #f5e7e4; border-radius: 8px; }
.editor-note .el-icon { flex: none; margin-top: 1px; color: #bb878c; }
.editor-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 15px; border-top: 1px solid #f3e7e8; }
@media (max-width: 1100px) { .metric-board { grid-template-columns: 1fr 1fr; }.panel-head { align-items: flex-start; flex-direction: column; }.filters { flex-wrap: wrap; } }
@media (max-width: 720px) { .page-hero { align-items: flex-start; flex-direction: column; }.hero-actions { align-self: flex-start; }.metric-board { grid-template-columns: 1fr; }.form-grid.two { grid-template-columns: 1fr; }.filters .el-input,.filters .el-select { width: 100%; } }
</style>
