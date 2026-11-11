<template>
  <div class="combo-page">
    <header class="page-head">
      <div>
        <span class="kicker">COMBO LIBRARY</span>
        <h1>联动组合配置</h1>
        <p>组合只选一级款式分类；首页显示近 7 天两个分类的销售净额合计占比，停用后不再出现在首页。</p>
      </div>
      <div class="head-actions">
        <el-button :icon="Right" @click="$router.push('/reports/combo-report')">查看分析</el-button>
        <el-button type="primary" :icon="Plus" @click="openEditor()">新增组合</el-button>
      </div>
    </header>

    <section class="combo-summary">
      <div><span>组合总数</span><b>{{ rows.length }}</b><small>含停用</small></div>
      <div><span>启用中</span><b class="accent">{{ enabledCount }}</b><small>首页可见</small></div>
      <div><span>当前口径</span><b class="range">近 7 天</b><small>{{ rangeLabel }}</small></div>
      <p>占比 = 两个分类销售净额合计 ÷ 全部款式销售净额；退货按退款金额冲减。</p>
    </section>

    <section class="list-panel" v-loading="loading">
      <div class="list-head">
        <span>组合与搭配</span><span>适用人群 / 场景</span><span>近 7 天占比</span><span>状态</span><span>操作</span>
      </div>
      <div v-if="rows.length" class="combo-rows">
        <article v-for="row in rows" :key="row.id" class="combo-row" :class="{ disabled: row.status === 1 }">
          <div class="combo-main">
            <div class="combo-name"><b>{{ row.name }}</b><el-tag v-if="row.status === 1" size="small" type="info" effect="plain">已停用</el-tag></div>
            <div class="category-pair"><span>{{ row.categories?.[0] }}</span><i>+</i><span>{{ row.categories?.[1] }}</span></div>
            <small>{{ row.amount ? '成交 ¥' + money(row.amount) : '所选周期暂无成交' }}</small>
          </div>
          <div class="combo-fit"><p>{{ row.fit || '未填写适用人群' }}</p><small>{{ row.scene || '未填写使用场景' }}</small></div>
          <div class="combo-percent"><b>{{ decimal(row.percent, 2) }}%</b><small>{{ row.basis }}</small></div>
          <div><el-switch :model-value="row.status === 0" :loading="statusLoading === row.id" @change="toggleStatus(row, $event)" /></div>
          <div class="row-actions">
            <el-button link type="primary" @click="openEditor(row)">编辑</el-button>
            <el-button link type="danger" @click="remove(row)">删除</el-button>
          </div>
        </article>
      </div>
      <el-empty v-else description="还没有联动组合" :image-size="72">
        <el-button type="primary" @click="openEditor()">新增第一条组合</el-button>
      </el-empty>
    </section>

    <el-dialog v-model="editorOpen" :title="form.id ? '编辑联动组合' : '新增联动组合'" width="520px" append-to-body :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="combo-form">
        <el-form-item label="组合名称" prop="comboName">
          <el-input v-model="form.comboName" maxlength="100" show-word-limit placeholder="例如：银饰 + 手串日常叠戴" />
        </el-form-item>
        <div class="pair-fields">
          <el-form-item label="款式分类 1" prop="categoryId1">
            <el-select v-model="form.categoryId1" filterable placeholder="选择一级分类">
              <el-option v-for="item in categories" :key="item.id" :label="item.itemName" :value="item.id" :disabled="item.id === form.categoryId2" />
            </el-select>
          </el-form-item>
          <span class="pair-plus">+</span>
          <el-form-item label="款式分类 2" prop="categoryId2">
            <el-select v-model="form.categoryId2" filterable placeholder="选择一级分类">
              <el-option v-for="item in categories" :key="item.id" :label="item.itemName" :value="item.id" :disabled="item.id === form.categoryId1" />
            </el-select>
          </el-form-item>
        </div>
        <el-form-item label="适用人群">
          <el-input v-model="form.fitCrowd" maxlength="200" show-word-limit placeholder="例如：喜欢层次感、每天换款的人" />
        </el-form-item>
        <el-form-item label="使用场景">
          <el-input v-model="form.useScene" maxlength="200" show-word-limit placeholder="例如：通勤、逛街拍照" />
        </el-form-item>
        <div class="form-foot-fields">
          <el-form-item label="排序"><el-input-number v-model="form.sortNo" :min="0" :max="9999" /></el-form-item>
          <el-form-item label="启用"><el-switch v-model="form.enabled" /></el-form-item>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="editorOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存组合</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="JyComboSetting">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Right } from '@element-plus/icons-vue'
import { listCombos, saveCombo, setComboStatus, deleteCombo } from '@/api/jingyue/report'
import { lookupOptions } from '@/api/jingyue/lookup'
import { serverRange } from '@/utils/serverTime'

const loading = ref(false)
const saving = ref(false)
const statusLoading = ref(0)
const editorOpen = ref(false)
const rows = ref([])
const categories = ref([])
const formRef = ref()
const range = serverRange(6)
const form = reactive(emptyForm())
const rules = {
  comboName: [{ required: true, message: '请输入组合名称', trigger: 'blur' }],
  categoryId1: [{ required: true, message: '请选择款式分类 1', trigger: 'change' }],
  categoryId2: [{ required: true, message: '请选择款式分类 2', trigger: 'change' }]
}

const enabledCount = computed(() => rows.value.filter((it) => it.status === 0).length)
const rangeLabel = computed(() => `${range[0]} 至 ${range[1]}`)

function emptyForm() {
  return { id: 0, comboName: '', categoryId1: null, categoryId2: null, fitCrowd: '', useScene: '', enabled: true, sortNo: 0 }
}
function money(value) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function decimal(value, digits = 2) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits })
}

async function load() {
  loading.value = true
  try {
    const [comboRes, catRes] = await Promise.all([
      listCombos({ home: false }),
      lookupOptions('style')
    ])
    rows.value = comboRes.data || []
    categories.value = (catRes.data || []).filter((it) => !it.parentId)
  } catch (error) {
    ElMessage.error(error?.message || '联动组合加载失败')
  } finally {
    loading.value = false
  }
}

function openEditor(row) {
  Object.assign(form, emptyForm())
  if (row) {
    Object.assign(form, {
      id: row.id,
      comboName: row.name,
      categoryId1: row.categoryId1 || null,
      categoryId2: row.categoryId2 || null,
      fitCrowd: row.fit || '',
      useScene: row.scene || '',
      enabled: row.status === 0,
      sortNo: row.sortNo || 0
    })
  }
  editorOpen.value = true
}

async function save() {
  await formRef.value?.validate()
  if (form.categoryId1 === form.categoryId2) {
    ElMessage.warning('两个款式分类不能相同')
    return
  }
  saving.value = true
  try {
    await saveCombo({
      id: form.id,
      comboName: form.comboName,
      categoryId1: form.categoryId1,
      categoryId2: form.categoryId2,
      fitCrowd: form.fitCrowd,
      useScene: form.useScene,
      status: form.enabled ? 0 : 1,
      sortNo: form.sortNo
    })
    ElMessage.success('组合已保存，首页按启用状态展示')
    editorOpen.value = false
    await load()
  } catch (error) {
    ElMessage.error(error?.message || '组合保存失败')
  } finally {
    saving.value = false
  }
}

async function toggleStatus(row, enabled) {
  statusLoading.value = row.id
  try {
    await setComboStatus(row.id, enabled ? 0 : 1)
    row.status = enabled ? 0 : 1
    ElMessage.success(enabled ? '组合已启用' : '组合已停用，首页不再显示')
  } catch (error) {
    ElMessage.error(error?.message || '状态更新失败')
  } finally {
    statusLoading.value = 0
  }
}

async function remove(row) {
  try {
    await ElMessageBox.confirm(`确认删除「${row.name}」？删除后首页不再显示，历史销售数据不受影响。`, '删除组合', { type: 'warning' })
    await deleteCombo(row.id)
    ElMessage.success('组合已删除')
    await load()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') ElMessage.error(error?.message || '删除失败')
  }
}

onMounted(load)
</script>

<style scoped>
.combo-page { min-height: 100%; padding: 16px 20px 24px; background: #fbf7f8; color: #3d3236; display: flex; flex-direction: column; gap: 12px; }
.page-head { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.kicker { color: #b16d7d; font-size: 11px; letter-spacing: .14em; font-weight: 700; }
.page-head h1 { margin: 4px 0 3px; font-size: 23px; font-weight: 680; }
.page-head p { margin: 0; color: #938087; font-size: 12px; }
.head-actions { display: flex; gap: 8px; flex: none; }
.combo-summary { display: grid; grid-template-columns: 150px 150px 210px minmax(220px, 1fr); align-items: center; gap: 16px; padding: 13px 16px; border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; }
.combo-summary div { display: flex; flex-direction: column; gap: 3px; }
.combo-summary span { color: #9b858c; font-size: 11px; }
.combo-summary b { color: #5d474f; font-size: 22px; font-weight: 680; }
.combo-summary b.accent { color: #a44e63; }
.combo-summary b.range { font-size: 15px; }
.combo-summary small { color: #ab969c; font-size: 11px; }
.combo-summary p { margin: 0; color: #9a858b; font-size: 11px; line-height: 1.6; }
.list-panel { border: 1px solid #f0dfe3; border-radius: 8px; background: #fff; overflow: hidden; }
.list-head, .combo-row { display: grid; grid-template-columns: minmax(220px, 1.25fr) minmax(180px, .95fr) 165px 56px 100px; gap: 16px; align-items: center; }
.list-head { padding: 9px 16px; background: #fdf8f9; border-bottom: 1px solid #f3e6e9; color: #9b848b; font-size: 11px; }
.combo-rows { display: flex; flex-direction: column; }
.combo-row { padding: 13px 16px; border-bottom: 1px solid #f7edef; }
.combo-row:last-child { border-bottom: 0; }
.combo-row.disabled { background: #fcfbfb; }
.combo-main { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.combo-name { display: flex; align-items: center; gap: 6px; }
.combo-name b { overflow: hidden; font-size: 13.5px; font-weight: 650; text-overflow: ellipsis; white-space: nowrap; }
.category-pair { display: flex; align-items: center; gap: 5px; color: #8a5966; font-size: 12px; }
.category-pair i { color: #b9909b; font-style: normal; }
.combo-main small, .combo-fit small, .combo-percent small { color: #ab969c; font-size: 11px; }
.combo-fit p { margin: 0 0 3px; color: #745d64; font-size: 12px; }
.combo-percent { display: flex; flex-direction: column; gap: 3px; }
.combo-percent b { color: #a44e63; font-size: 16px; font-weight: 680; }
.row-actions { display: flex; justify-content: flex-end; }
.combo-form { display: flex; flex-direction: column; }
.pair-fields { display: grid; grid-template-columns: 1fr auto 1fr; gap: 8px; align-items: center; }
.pair-plus { padding-top: 18px; color: #b98c98; }
.pair-fields :deep(.el-select) { width: 100%; }
.form-foot-fields { display: flex; gap: 22px; }
@media (max-width: 1080px) {
  .combo-summary { grid-template-columns: 1fr 1fr 1fr; }
  .combo-summary p { grid-column: 1 / -1; }
  .list-head { display: none; }
  .combo-row { grid-template-columns: 1fr 1fr; gap: 10px 16px; }
  .combo-percent { grid-column: 1; }
}
@media (max-width: 700px) {
  .combo-page { padding: 12px; }
  .page-head { align-items: flex-start; flex-direction: column; }
  .combo-summary { grid-template-columns: 1fr 1fr; }
  .combo-row { grid-template-columns: 1fr; }
  .row-actions { justify-content: flex-start; }
  .pair-fields { grid-template-columns: 1fr; }
  .pair-plus { display: none; }
}
</style>
