<template>
  <div class="material-doc">
    <header class="page-hero">
      <div>
        <span class="eyebrow">{{ isInbound ? 'MATERIAL INBOUND' : 'MATERIAL OUTBOUND' }}</span>
        <h1>{{ isInbound ? '物料入库' : '物料出库' }}</h1>
        <p>{{ isInbound ? '采购、退料与旧料按克入库；确认后按移动平均法更新库存成本。' : '领用、损耗与调拨按克出库；确认时校验可用重量，不允许负库存。' }}</p>
      </div>
      <div class="hero-actions">
        <el-button plain :loading="loading" @click="refreshAll"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button v-hasPermi="['jingyue:materialdoc:add']" type="primary" @click="openCreate"><el-icon><Plus /></el-icon>新建{{ isInbound ? '入库' : '出库' }}单</el-button>
      </div>
      <i class="hero-glow"></i>
    </header>

    <section class="metric-board" v-loading="dashboardLoading">
      <article class="metric-feature"><span>待确认草稿</span><strong>{{ dashboard.draftCount || 0 }}<em> 单</em></strong><p>确认前可以继续修改明细</p><div class="metric-foot"><span>今日确认 {{ dashboard.confirmedToday || 0 }} 单</span><b>待办优先</b></div></article>
      <article><span class="metric-label">今日入库重量</span><strong>{{ weight(dashboard.todayInWeight) }}<em> g</em></strong><small>已确认单据合计</small><i class="metric-icon"><el-icon><Bottom /></el-icon></i></article>
      <article><span class="metric-label">今日出库重量</span><strong>{{ weight(dashboard.todayOutWeight) }}<em> g</em></strong><small>已确认单据合计</small><i class="metric-icon gold"><el-icon><Top /></el-icon></i></article>
      <article><span class="metric-label">待处理单据</span><strong>{{ dashboard.pending?.length || 0 }}<em> 单</em></strong><small>最近等待确认的物料单</small><i class="metric-icon"><el-icon><Tickets /></el-icon></i></article>
    </section>

    <section class="list-panel">
      <div class="panel-head">
        <div><span class="section-kicker">DOCUMENT QUEUE</span><h2>{{ isInbound ? '入库单' : '出库单' }}</h2><p>草稿优先排列，点击单据进入重量明细工作台</p></div>
        <div class="filters">
          <el-input v-model="query.keyword" clearable placeholder="单号或备注" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
          <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="onDateChange" />
          <el-select v-model="query.status" clearable placeholder="全部状态" @change="search"><el-option label="待确认" :value="0" /><el-option label="已确认" :value="1" /></el-select>
          <el-button @click="search">查询</el-button>
        </div>
      </div>
      <div v-loading="loading" class="doc-list">
        <template v-if="rows.length">
          <article v-for="row in rows" :key="row.id" class="doc-row" :class="{ drafting: row.status === 0 }">
            <button type="button" class="doc-main" @click="openEdit(row)">
              <span class="doc-sign"><el-icon><component :is="isInbound ? Bottom : Top" /></el-icon></span>
              <span class="doc-copy"><b>{{ row.docNo }}</b><small><span v-if="row.supplierName" class="supplier-chip">{{ row.supplierName }}</span>{{ row.bizTypeText }} · {{ row.remark || '未填写备注' }}</small></span>
              <span class="doc-stat"><b>{{ weight(row.totalWeight) }} <em>g</em></b><small>合计重量</small></span>
              <span class="doc-stat"><b>¥{{ money(row.totalAmount) }}</b><small>{{ isInbound ? '入库金额' : '出库成本' }}</small></span>
              <span class="doc-date"><b>{{ dateText(row.docTime) }}</b><small>{{ row.createBy || '操作人' }}</small></span>
              <el-tag :type="row.status === 0 ? 'warning' : 'success'" effect="plain" round>{{ row.statusText }}</el-tag>
            </button>
            <div class="doc-actions">
              <el-button link type="primary" @click="openEdit(row)">{{ row.status === 0 ? '编辑' : '查看' }}</el-button>
              <el-button v-if="row.status === 0" v-hasPermi="['jingyue:materialdoc:confirm']" link type="success" @click="confirmRow(row)">确认</el-button>
              <el-button v-if="row.status === 0" v-hasPermi="['jingyue:materialdoc:remove']" link type="danger" @click="removeRow(row)">删除</el-button>
            </div>
          </article>
        </template>
        <div v-else class="empty-panel"><el-icon><Document /></el-icon><h3>还没有{{ isInbound ? '入库' : '出库' }}单</h3><p>{{ isInbound ? '从一张采购单开始登记物料重量。' : '需要领用或损耗时，从这里新建出库单。' }}</p><el-button v-hasPermi="['jingyue:materialdoc:add']" type="primary" @click="openCreate">新建第一张单</el-button></div>
      </div>
      <div class="list-foot"><span>共 {{ total }} 张单据</span><pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" /></div>
    </section>

    <el-drawer v-model="editorOpen" size="690px" append-to-body :with-header="false">
      <div class="editor-wrap">
        <div class="drawer-heading"><span>{{ form.id ? 'EDIT DOCUMENT' : 'NEW DOCUMENT' }}</span><b>{{ readOnly ? '查看' : '编辑' }}{{ isInbound ? '物料入库单' : '物料出库单' }}</b><button type="button" @click="editorOpen = false"><el-icon><Close /></el-icon></button></div>
        <template v-if="editorLoading"><div class="editor-loading"><el-icon class="is-loading"><Loading /></el-icon>正在读取单据</div></template>
        <template v-else>
          <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="doc-form" :disabled="readOnly">
            <div class="form-section"><div class="form-caption"><span>01</span><b>单据信息</b></div>
              <div class="form-grid three">
                <el-form-item v-if="isInbound" label="供应商" prop="supplierId"><el-select v-model="form.supplierId" filterable placeholder="请选择供应商"><el-option v-for="item in options.suppliers" :key="item.id" :label="item.name" :value="item.id" /><template #empty><span class="supplier-empty">暂无启用供应商，请先在静悦基础维护供应商档案</span></template></el-select></el-form-item>
                <el-form-item label="业务类型" prop="bizType"><el-select v-model="form.bizType"><el-option v-for="item in bizTypes" :key="item.value" :label="item.label" :value="item.value" /></el-select></el-form-item>
                <el-form-item label="业务日期"><el-date-picker v-model="form.docTime" type="date" value-format="YYYY-MM-DD" placeholder="选择日期" style="width:100%" /></el-form-item>
              </div>
              <el-form-item label="备注"><el-input v-model="form.remark" maxlength="500" show-word-limit placeholder="选填：到货批次、用途或损耗说明" /></el-form-item>
            </div>
            <div class="form-section"><div class="form-caption"><span>02</span><b>物料明细</b><el-button v-if="!readOnly" link type="primary" @click="addLine"><el-icon><Plus /></el-icon>加入物料</el-button></div>
              <div class="line-hint"><el-icon><InfoFilled /></el-icon><span>每一行填写本次重量的克数；同一物料在一张单中只能出现一次。</span></div>
              <div v-for="(line, index) in form.lines" :key="line.key" class="line-card">
                <div class="line-head"><span class="line-no">{{ index + 1 }}</span><div class="line-select"><el-select v-model="line.materialId" filterable placeholder="选择物料" @change="onMaterialChange(line)"><el-option v-for="item in materialOptions" :key="item.id" :label="item.materialCode + ' · ' + item.materialName" :value="item.id" /></el-select></div><button v-if="!readOnly" type="button" class="line-remove" @click="removeLine(index)"><el-icon><Delete /></el-icon></button></div>
                <p v-if="line.stockText" class="line-stock">{{ line.stockText }}</p>
                <div class="line-fields">
                  <label><span>本次重量（克）</span><el-input-number v-model="line.weight" :min="0.0001" :precision="4" :step="1" :controls="false" placeholder="0.0000" /></label>
                  <label><span>{{ isInbound ? '成本价（元/克）' : '出库成本（元/克）' }}</span><el-input-number v-model="line.unitCost" :min="0" :precision="4" :step="1" :controls="false" :disabled="!isInbound" /></label>
                  <div class="line-total"><span>本行金额</span><b>¥{{ money(lineAmount(line)) }}</b></div>
                </div>
                <el-input v-model="line.remark" maxlength="200" placeholder="行备注（选填）" class="line-remark" />
              </div>
              <div v-if="!form.lines.length" class="state-box"><span>还没有物料明细</span><el-button v-if="!readOnly" link type="primary" @click="addLine">加入第一项物料</el-button></div>
            </div>
          </el-form>
          <div class="doc-summary"><div><span>合计重量</span><b>{{ weight(totalWeight) }} <em>g</em></b></div><div><span>合计金额</span><b>¥{{ money(totalAmount) }}</b></div><div><span>明细行数</span><b>{{ form.lines.length }} <em>行</em></b></div></div>
          <div class="editor-actions"><el-button @click="editorOpen = false">{{ readOnly ? '关闭' : '取消' }}</el-button><template v-if="!readOnly"><el-button v-hasPermi="['jingyue:materialdoc:add','jingyue:materialdoc:edit']" plain :loading="saving" @click="save(false)">保存草稿</el-button><el-button v-hasPermi="['jingyue:materialdoc:confirm']" type="primary" :loading="saving" @click="save(true)">保存并确认</el-button></template></div>
        </template>
      </div>
    </el-drawer>
  </div>
</template>

<script setup name="JyMaterialDoc">
import { useRoute } from 'vue-router'
import { Search, Plus, Refresh, Close, Bottom, Top, Tickets, Document, InfoFilled, Delete, Loading } from '@element-plus/icons-vue'
import { listMaterialDocs, getMaterialDoc, getMaterialDocDashboard, getMaterialOptions, getAvailableMaterials, saveMaterialDoc, deleteMaterialDoc, confirmMaterialDoc } from '@/api/jingyue/material'

const route = useRoute()
const { proxy } = getCurrentInstance()
const isInbound = computed(() => String(route.path).includes('material-inbound'))
const inboundTypes = [{ value: 'purchase', label: '采购入库' }, { value: 'return', label: '退回入库' }, { value: 'old_material', label: '旧料入库' }, { value: 'other_in', label: '其他入库' }]
const outboundTypes = [{ value: 'consume', label: '领用出库' }, { value: 'loss', label: '损耗出库' }, { value: 'transfer', label: '调拨出库' }, { value: 'other_out', label: '其他出库' }]
const bizTypes = computed(() => isInbound.value ? inboundTypes : outboundTypes)
const loading = ref(false)
const dashboardLoading = ref(false)
const editorLoading = ref(false)
const saving = ref(false)
const readOnly = ref(false)
const rows = ref([])
const total = ref(0)
const dateRange = ref(null)
const options = reactive({ suppliers: [], categories: [], metals: [] })
const materialOptions = ref([])
const dashboard = reactive({ draftCount: 0, confirmedToday: 0, todayInWeight: 0, todayOutWeight: 0, pending: [] })
const query = reactive({ pageNum: 1, pageSize: 12, keyword: '', direction: undefined, status: undefined, beginTime: undefined, endTime: undefined })
const editorOpen = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const rules = { supplierId: [{ required: true, message: '请选择供应商', trigger: 'change' }], bizType: [{ required: true, message: '请选择业务类型', trigger: 'change' }] }
let lineKey = 0

function emptyLine() { return { key: 'l' + Date.now() + (lineKey++), id: 0, materialId: undefined, weight: undefined, unitCost: 0, remark: '', stockText: '' } }
function emptyForm() { return { id: 0, storeId: null, direction: 1, bizType: 'purchase', supplierId: undefined, docTime: new Date().toISOString().slice(0, 10), remark: '', lines: [] } }
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const weight = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
const dateText = (value) => value ? String(value).slice(0, 10) : '—'
const lineAmount = (line) => Number(line.weight || 0) * Number(line.unitCost || 0)
const totalWeight = computed(() => form.lines.reduce((sum, line) => sum + Number(line.weight || 0), 0))
const totalAmount = computed(() => form.lines.reduce((sum, line) => sum + lineAmount(line), 0))

function loadList() {
  loading.value = true
  listMaterialDocs({ ...query, direction: isInbound.value ? 1 : 2 }).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function loadDashboard() {
  dashboardLoading.value = true
  getMaterialDocDashboard().then((res) => Object.assign(dashboard, res.data || {})).finally(() => { dashboardLoading.value = false })
}
function refreshAll() { loadList(); loadDashboard() }
function search() { query.pageNum = 1; loadList() }
function onDateChange(value) { query.beginTime = value?.[0] || undefined; query.endTime = value?.[1] || undefined; search() }
function loadOptions() {
  getMaterialOptions().then((res) => Object.assign(options, res.data || {}))
  getAvailableMaterials().then((res) => { materialOptions.value = res.data || [] })
}
function openCreate() {
  readOnly.value = false
  Object.assign(form, emptyForm())
  form.direction = isInbound.value ? 1 : 2
  form.bizType = isInbound.value ? 'purchase' : 'consume'
  form.lines = [emptyLine()]
  editorOpen.value = true
}
function openEdit(row) {
  editorLoading.value = true
  readOnly.value = row.status !== 0
  Object.assign(form, emptyForm())
  editorOpen.value = true
  getMaterialDoc(row.id).then((res) => {
    const data = res.data || {}
    readOnly.value = Number(data.status) !== 0
    Object.assign(form, {
      id: data.id, storeId: data.storeId, direction: data.direction, bizType: data.bizType,
      supplierId: data.supplierId || undefined, docTime: dateText(data.docTime), remark: data.remark || '',
      lines: (data.lines || []).map((line) => Object.assign(emptyLine(), {
        id: line.id, materialId: line.materialId, weight: Number(line.weight || 0),
        unitCost: Number(line.unitCost || 0), remark: line.remark || ''
      }))
    })
  }).finally(() => { editorLoading.value = false })
}
function addLine() { form.lines.push(emptyLine()) }
function removeLine(index) { form.lines.splice(index, 1) }
function onMaterialChange(line) {
  const material = materialOptions.value.find((it) => it.id === line.materialId)
  if (!material) return
  if (isInbound.value) line.unitCost = Number(material.avgCostPerGram || 0)
  else line.unitCost = Number(material.avgCostPerGram || 0)
  line.stockText = '当前可用 ' + weight(material.availableWeight) + ' g'
}
function save(confirmAfter) {
  formRef.value?.validate().then(async () => {
    if (!form.lines.length) return proxy.$modal.msgWarning('请至少加入一项物料')
    if (form.lines.some((line) => !line.materialId || !(Number(line.weight) > 0))) return proxy.$modal.msgWarning('请补全物料和本次重量')
    if (new Set(form.lines.map((line) => line.materialId)).size !== form.lines.length) return proxy.$modal.msgWarning('同一物料在一张单中只能出现一次')
    if (isInbound.value && !form.supplierId) return proxy.$modal.msgWarning('物料入库必须选择供应商')
    saving.value = true
    try {
      if (confirmAfter) {
        try {
          await proxy.$modal.confirm('确认后将更新物料库存与重量流水，单据不能再修改。', '确认单据')
        } catch (cancel) {
          saving.value = false
          return
        }
      }
      const payload = { ...form, direction: isInbound.value ? 1 : 2, lines: form.lines.map((line) => ({ id: line.id, materialId: line.materialId, weight: Number(line.weight), unitCost: Number(line.unitCost), remark: line.remark })) }
      const res = await saveMaterialDoc(payload)
      const id = res.data
      if (confirmAfter) {
        await confirmMaterialDoc(id)
      }
      proxy.$modal.msgSuccess(confirmAfter ? '单据已确认，库存已更新' : '草稿已保存')
      editorOpen.value = false
      refreshAll()
    } finally { saving.value = false }
  }).catch(() => {})
}
function confirmRow(row) {
  proxy.$modal.confirm('确认「' + row.docNo + '」？确认后将更新库存与流水，不能再修改。', '确认物料单').then(() => {
    saving.value = true
    return confirmMaterialDoc(row.id)
  }).then(() => { proxy.$modal.msgSuccess('单据已确认'); refreshAll() }).catch(() => {}).finally(() => { saving.value = false })
}
function removeRow(row) {
  proxy.$modal.confirm('删除草稿「' + row.docNo + '」？').then(() => deleteMaterialDoc(row.id)).then(() => { proxy.$modal.msgSuccess('草稿已删除'); refreshAll() }).catch(() => {})
}

loadOptions()
refreshAll()
</script>

<style scoped>
.material-doc { display: flex; flex-direction: column; gap: 16px; min-height: calc(100vh - 132px); color: #4d373e; }
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
.list-panel { display: flex; flex: 1; min-height: 400px; flex-direction: column; padding: 17px 19px 11px; border: 1px solid #f2e4e5; border-radius: 16px; background: #fff; box-shadow: 0 7px 22px rgba(106,68,77,.045); }
.panel-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; padding: 0 2px 13px; }
.panel-head h2 { margin: 3px 0 2px; color: #573e45; font-size: 19px; font-weight: 600; }
.panel-head p { margin: 0; color: #b09ba0; font-size: 11px; }
.filters { display: flex; align-items: center; gap: 7px; }
.filters .el-input { width: 160px; }.filters .el-select { width: 116px; }.filters .el-date-editor { width: 230px; }.filters .el-button { margin-left: 0; }
.doc-list { flex: 1; }
.doc-row { display: flex; align-items: stretch; gap: 5px; margin-bottom: 7px; border: 1px solid #f3e8e9; border-radius: 10px; background: #fff; transition: border-color .2s, box-shadow .2s; }
.doc-row:hover { border-color: #eccfd2; box-shadow: 0 5px 16px rgba(124,79,85,.055); }.doc-row.drafting { background: #fffdf9; border-color: #f3e7d9; }
.doc-main { display: grid; flex: 1; grid-template-columns: 34px minmax(180px,1.5fr) minmax(100px,.8fr) minmax(100px,.8fr) 100px 70px; align-items: center; gap: 13px; min-width: 0; padding: 12px 10px 12px 13px; border: 0; background: transparent; color: inherit; text-align: left; cursor: pointer; }
.doc-sign { display: grid; place-items: center; width: 29px; height: 29px; border-radius: 8px; background: #fdf1ef; color: #b8777e; }
.doc-copy { min-width: 0; }.doc-copy b { display: block; overflow: hidden; color: #60474e; font-size: 12.5px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.doc-copy small { display: block; overflow: hidden; margin-top: 4px; color: #b09aa0; font-size: 10.5px; text-overflow: ellipsis; white-space: nowrap; }
.supplier-chip { margin-right: 6px; padding: 1px 5px; border-radius: 3px; background: #fcf2f1; color: #ad737a; }
.doc-stat b, .doc-date b { display: block; color: #624950; font-size: 12.5px; font-weight: 500; font-variant-numeric: tabular-nums; }.doc-stat b em { color: #b59b9f; font-size: 10px; font-style: normal; }.doc-stat small, .doc-date small { display: block; margin-top: 3px; color: #b19ba0; font-size: 10px; }
.doc-actions { display: flex; flex: none; align-items: center; padding: 0 9px 0 3px; border-left: 1px solid #f5ebec; }.doc-actions .el-button { margin-left: 7px; }
.list-foot { display: flex; align-items: center; justify-content: space-between; padding-top: 10px; color: #b19ba0; font-size: 11px; }
.empty-panel { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 280px; color: #ac959a; }.empty-panel .el-icon { font-size: 26px; color: #c79599; }.empty-panel h3 { margin: 11px 0 4px; color: #694f56; font-size: 16px; font-weight: 500; }.empty-panel p { margin: 0 0 15px; font-size: 12px; }
.editor-wrap { padding: 2px 3px 4px; color: #523b42; }.drawer-heading { display: flex; align-items: center; gap: 10px; padding-bottom: 15px; border-bottom: 1px solid #f1e2e4; }.drawer-heading > span { color: #bfa080; font-size: 11px; letter-spacing: 1.9px; }.drawer-heading > b { color: #5b4048; font-size: 16px; font-weight: 500; }.drawer-heading > button { display: grid; place-items: center; width: 27px; height: 27px; margin-left: auto; padding: 0; color: #9c838a; background: #fff; border: 1px solid #f0e1e3; border-radius: 50%; cursor: pointer; }
.editor-loading { display: flex; align-items: center; justify-content: center; gap: 8px; min-height: 260px; color: #a8898f; font-size: 13px; }
.doc-form { margin-top: 18px; }.form-section { margin-bottom: 18px; padding-bottom: 5px; border-bottom: 1px solid #f6eded; }.form-caption { display: flex; align-items: center; gap: 8px; margin-bottom: 13px; color: #654a51; font-size: 13px; }.form-caption span { color: #bfa080; font-size: 10px; letter-spacing: 1px; }.form-caption b { font-weight: 500; }.form-caption .el-button { margin-left: auto; }.form-grid.three { display: grid; grid-template-columns: 1.2fr 1fr .9fr; gap: 0 13px; }
.line-hint { display: flex; align-items: center; gap: 6px; margin-bottom: 10px; padding: 8px 10px; color: #a08187; font-size: 10.5px; background: #fdf8f5; border-radius: 6px; }.line-hint .el-icon { color: #bb8b90; }
.supplier-empty { display: block; padding: 12px; color: #a8898f; font-size: 12px; line-height: 1.5; }
.line-stock { margin: 7px 0 0; color: #9b757c; font-size: 10.5px; }
.line-card { margin-bottom: 9px; padding: 11px; border: 1px solid #f1e3e5; border-radius: 9px; background: #fffdfc; }.line-head { display: flex; align-items: center; gap: 8px; }.line-no { display: grid; place-items: center; width: 20px; height: 20px; border-radius: 50%; background: #f7e9e7; color: #ae727b; font-size: 10px; }.line-select { flex: 1; }.line-remove { display: grid; place-items: center; width: 25px; height: 25px; border: 0; border-radius: 50%; background: #fdf2f2; color: #b77b82; cursor: pointer; }
.line-fields { display: grid; grid-template-columns: 1fr 1fr .85fr; gap: 10px; margin-top: 9px; }.line-fields label > span, .line-total > span { display: block; margin-bottom: 5px; color: #aa9297; font-size: 10.5px; }.line-total { display: flex; flex-direction: column; justify-content: flex-end; }.line-total b { display: flex; min-height: 32px; align-items: center; color: #a86770; font-family: "Songti SC","STSong",serif; font-size: 14px; }.line-remark { margin-top: 7px; }
.state-box { display: flex; align-items: center; justify-content: center; gap: 10px; min-height: 70px; color: #b09aa0; font-size: 12px; border: 1px dashed #eedfe1; border-radius: 8px; }
.doc-summary { display: grid; grid-template-columns: repeat(3,1fr); gap: 1px; margin-bottom: 16px; overflow: hidden; border: 1px solid #f2e6e7; border-radius: 8px; background: #f2e6e7; }.doc-summary div { padding: 10px 13px; background: #fffaf8; }.doc-summary span { display: block; color: #ac959a; font-size: 10.5px; }.doc-summary b { display: block; margin-top: 4px; color: #624950; font-size: 15px; font-weight: 500; }.doc-summary b em { color: #b29ba0; font-size: 10px; font-style: normal; }.editor-actions { display: flex; justify-content: flex-end; gap: 8px; padding-top: 15px; border-top: 1px solid #f3e7e8; }
@media (max-width: 1180px) { .metric-board { grid-template-columns: 1fr 1fr; }.doc-main { grid-template-columns: 30px minmax(160px,1.5fr) 1fr 1fr 74px; }.doc-date { display: none; }.panel-head { align-items: flex-start; flex-direction: column; }.filters { flex-wrap: wrap; } }
@media (max-width: 760px) { .page-hero { align-items: flex-start; flex-direction: column; }.hero-actions { align-self: flex-start; }.metric-board { grid-template-columns: 1fr; }.form-grid.three,.line-fields { grid-template-columns: 1fr; }.doc-main { grid-template-columns: 28px 1fr auto; }.doc-stat,.doc-main > .el-tag { display: none; }.filters .el-input,.filters .el-select,.filters .el-date-editor { width: 100%; }.doc-row { flex-direction: column; }.doc-actions { justify-content: flex-end; padding: 0 10px 8px; border-left: 0; } }
</style>
