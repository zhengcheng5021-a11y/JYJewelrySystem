<template>
  <div class="script-page">
    <header class="page-head">
      <div>
        <span class="kicker">SALES PLAYBOOK</span>
        <h1>话术模板库</h1>
        <p>按接待阶段整理常用表达，搜索后直接复制，也能按款式与材质沉淀经验。</p>
      </div>
      <el-button v-hasPermi="['jingyue:script:add']" type="primary" :icon="Plus" @click="openCreate">新增话术</el-button>
    </header>

    <section class="filter-panel">
      <div class="type-tabs">
        <button type="button" :class="{ active: activeType === '全部' }" @click="selectType('全部')">全部</button>
        <button v-for="item in types" :key="item" type="button" :class="{ active: activeType === item }" @click="selectType(item)">{{ item }}</button>
      </div>
      <div class="search-row">
        <el-input v-model="query.keyword" clearable :prefix-icon="Search" placeholder="搜索标题或话术内容" @keyup.enter="search" @clear="search" />
        <el-select v-model="query.styleItemId" clearable filterable placeholder="全部款式" @change="search">
          <el-option v-for="item in styleOptions" :key="item.id" :label="item.fullName || item.itemName" :value="item.id" />
        </el-select>
        <el-button :icon="Search" @click="search">搜索</el-button>
        <el-button v-if="hasFilter" text @click="resetFilter">清除筛选</el-button>
        <span class="result-count">共 {{ total }} 条话术</span>
      </div>
    </section>

    <section v-loading="loading" class="card-grid">
      <article v-for="row in rows" :key="row.id" class="script-card">
        <div class="card-top">
          <span class="card-meta">
            <span class="type-pill">{{ row.scriptType || '其他' }}</span>
            <el-rate :model-value="row.sortNo || 3" disabled class="card-rate" />
          </span>
          <span class="card-actions">
            <el-button v-hasPermi="['jingyue:script:edit']" text circle :icon="EditPen" @click="openEdit(row)" />
            <el-button text circle :icon="CopyDocument" @click="copyContent(row)" />
            <el-button v-hasPermi="['jingyue:script:remove']" text circle type="danger" :icon="Delete" @click="remove(row)" />
          </span>
        </div>
        <h2>{{ row.title }}</h2>
        <p class="content">{{ row.content }}</p>
        <div class="card-foot">
          <span>{{ row.styleName || '全款式' }}</span>
          <span>{{ row.materialName || '不限材质' }}</span>
          <span>{{ row.metalName || '不限成色' }}</span>
        </div>
      </article>
      <div v-if="!loading && !rows.length" class="empty-card">
        <el-icon><ChatDotRound /></el-icon>
        <h3>{{ hasFilter ? '没有匹配的话术' : '还没有话术模板' }}</h3>
        <p>{{ hasFilter ? '换个关键词或类型试试。' : '把门店里验证过的表达沉淀下来，新人也能快速上手。' }}</p>
        <el-button v-if="hasFilter" plain @click="resetFilter">清除筛选</el-button>
        <el-button v-else v-hasPermi="['jingyue:script:add']" type="primary" @click="openCreate">新增第一条话术</el-button>
      </div>
    </section>

    <div v-if="total > query.pageSize" class="pager-row">
      <el-pagination v-model:current-page="query.pageNum" v-model:page-size="query.pageSize" :total="total" :page-sizes="[6, 12, 24]" layout="total, sizes, prev, pager, next" background small @size-change="reload" @current-change="load" />
    </div>

    <el-dialog v-model="editorOpen" :title="form.id ? '修改话术' : '新增话术'" width="560px" append-to-body>
      <div class="editor-form">
        <label><span>话术标题</span><el-input v-model="form.title" maxlength="100" placeholder="例如：新客接待开场" /></label>
        <label><span>话术类型</span><el-select v-model="form.scriptType" placeholder="选择类型"><el-option v-for="item in types" :key="item" :label="item" :value="item" /></el-select></label>
        <label><span>话术星级</span><el-rate v-model="form.sortNo" :max="5" show-score score-template="{value} 星" /></label>
        <div class="editor-pair">
          <label><span>适用款式</span><el-select v-model="form.styleItemId" clearable filterable placeholder="不限"><el-option v-for="item in styleOptions" :key="item.id" :label="item.fullName || item.itemName" :value="item.id" /></el-select></label>
          <label><span>适用材质</span><el-select v-model="form.materialItemId" clearable filterable placeholder="不限"><el-option v-for="item in materialOptions" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></label>
        </div>
        <label><span>话术内容</span><el-input v-model="form.content" type="textarea" :rows="7" maxlength="4000" show-word-limit placeholder="把想保存的销售表达写在这里" /></label>
      </div>
      <template #footer>
        <el-button @click="editorOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="submit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="JySalesScript">
import { computed, onMounted, reactive, ref } from 'vue'
import { Plus, Search, EditPen, Delete, CopyDocument, ChatDotRound } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { listScript, saveScript, deleteScript } from '@/api/jingyue/script'
import { lookupOptions } from '@/api/jingyue/lookup'

const types = ['接待开场', '产品介绍', '异议处理', '成交推进', '售后维护', '其他']
const loading = ref(false)
const saving = ref(false)
const editorOpen = ref(false)
const activeType = ref('全部')
const rows = ref([])
const total = ref(0)
const styleOptions = ref([])
const materialOptions = ref([])
const query = reactive({ pageNum: 1, pageSize: 6, keyword: '', scriptType: '', styleItemId: null, status: 0 })
const form = reactive(emptyForm())
const hasFilter = computed(() => !!query.keyword.trim() || activeType.value !== '全部' || !!query.styleItemId)

function emptyForm() {
  return { id: 0, title: '', scriptType: '接待开场', styleItemId: null, materialItemId: null, metalItemId: null, content: '', status: 0, sortNo: 3, remark: '' }
}
function selectType(value) { activeType.value = value; query.pageNum = 1; load() }
function search() { query.pageNum = 1; load() }
function resetFilter() { query.keyword = ''; query.styleItemId = null; activeType.value = '全部'; query.pageNum = 1; load() }
function reload() { query.pageNum = 1; load() }
async function load() {
  loading.value = true
  try {
    const res = await listScript({ ...query, scriptType: activeType.value === '全部' ? '' : activeType.value })
    rows.value = res.data?.result || []
    total.value = Number(res.data?.totalNum || 0)
  } finally { loading.value = false }
}
function openCreate() { Object.assign(form, emptyForm()); editorOpen.value = true }
function openEdit(row) {
  Object.assign(form, emptyForm(), row, { scriptType: row.scriptType || '其他', sortNo: row.sortNo || 3 })
  editorOpen.value = true
}
async function submit() {
  if (!form.title.trim()) return ElMessage.warning('请填写话术标题')
  if (!form.content.trim()) return ElMessage.warning('请填写话术内容')
  saving.value = true
  try {
    await saveScript({ ...form, styleItemId: form.styleItemId || null, materialItemId: form.materialItemId || null, metalItemId: form.metalItemId || null })
    ElMessage.success('话术已保存')
    editorOpen.value = false
    await load()
  } finally { saving.value = false }
}
async function remove(row) {
  try {
    await ElMessageBox.confirm('确定删除「' + row.title + '」吗？', '删除话术', { type: 'warning' })
    await deleteScript(row.id)
    ElMessage.success('话术已删除')
    await load()
  } catch {}
}
async function copyContent(row) {
  try {
    await navigator.clipboard.writeText(row.content || '')
    ElMessage.success('已复制话术')
  } catch { ElMessage.warning('浏览器未授予剪贴板权限') }
}
onMounted(async () => {
  const [styles, materials] = await Promise.all([lookupOptions('style'), lookupOptions('material')])
  styleOptions.value = styles.data || []
  materialOptions.value = materials.data || []
  await load()
})
</script>

<style scoped>
.script-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size:12px; letter-spacing: .16em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.filter-panel { display: flex; flex-direction: column; gap: 10px; padding: 11px 13px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.type-tabs { display: flex; flex-wrap: wrap; gap: 4px; }
.type-tabs button { height: 29px; padding: 0 13px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: #8d7780; font-size: 12px; cursor: pointer; }
.type-tabs button:hover { background: #fdf7f8; }
.type-tabs button.active { border-color: #eccbd3; background: #faeef1; color: #9f4357; font-weight: 650; }
.search-row { display: flex; align-items: center; gap: 8px; }
.search-row :deep(.el-input) { width: 270px; }
.search-row :deep(.el-select) { width: 180px; }
.result-count { margin-left: auto; color: #a28e93; font-size: 11px; }
.card-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; align-content: start; min-height: 280px; }
.script-card { display: flex; flex-direction: column; gap: 9px; min-height: 195px; padding: 13px 14px 12px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; transition: box-shadow .2s, transform .2s; }
.script-card:hover { box-shadow: 0 6px 18px rgba(123, 73, 86, .08); transform: translateY(-1px); }
.card-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.card-meta { display: flex; align-items: center; gap: 7px; min-width: 0; }
.card-rate { --el-rate-icon-size: 14px; height: 18px; }
.card-rate :deep(.el-rate__item) { margin-right: 1px; }
.card-rate :deep(.el-rate__icon) { margin-right: 0; }
.type-pill { padding: 2px 7px; border: 1px solid #efd3da; border-radius: 3px; color: #a45163; background: #fff8f9; font-size:12px; }
.card-actions { display: flex; align-items: center; gap: 0; opacity: .72; transition: opacity .2s; }
.script-card:hover .card-actions { opacity: 1; }
.script-card h2 { margin: 0; font-size: 14px; font-weight: 650; line-height: 1.4; }
.content { display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; overflow: hidden; margin: 0; color: #79666c; font-size: 12px; line-height: 1.68; white-space: pre-wrap; }
.card-foot { display: flex; flex-wrap: wrap; gap: 5px 10px; margin-top: auto; padding-top: 8px; border-top: 1px solid #f7edef; color: #aa959b; font-size:12px; }
.empty-card { grid-column: span 3; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 7px; min-height: 280px; padding: 24px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; text-align: center; }
.empty-card .el-icon { color: #c79da8; font-size: 28px; }
.empty-card h3 { margin: 0; font-size: 14px; font-weight: 650; }
.empty-card p { margin: 0 0 4px; color: #a38e94; font-size: 11.5px; }
.pager-row { display: flex; justify-content: flex-end; }
.editor-form { display: flex; flex-direction: column; gap: 13px; }
.editor-form label { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.editor-form label > span { color: #806970; font-size: 12px; }
.editor-form :deep(.el-select) { width: 100%; }
.editor-pair { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
@media (max-width: 1150px) { .card-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } .empty-card { grid-column: span 2; } }
@media (max-width: 700px) {
  .script-page { padding: 12px; }
  .page-head { align-items: flex-start; flex-direction: column; gap: 10px; }
  .search-row { align-items: stretch; flex-wrap: wrap; }
  .search-row :deep(.el-input), .search-row :deep(.el-select) { width: 100%; }
  .result-count { margin-left: 0; }
  .card-grid { grid-template-columns: 1fr; }
  .empty-card { grid-column: span 1; }
  .editor-pair { grid-template-columns: 1fr; }
}
</style>
