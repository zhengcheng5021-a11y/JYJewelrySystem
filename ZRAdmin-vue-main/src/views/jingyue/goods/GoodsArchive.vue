<template>
  <div class="jy-goods" :class="{ 'is-embedded': embedded }">
    <header v-if="!embedded" class="archive-hero">
      <div class="hero-content">
        <span class="eyebrow">JEWELLERY ARCHIVE</span>
        <h1>商品档案</h1>
        <p>每件货品都来自已完成的采购入库，拥有唯一编码。这里只维护名称、价格、照片、状态与备注，不改动来源与编码。</p>
        <div class="hero-note"><el-icon><Lock /></el-icon><span>档案由入库自动生成，保证账实与编码可追溯</span></div>
      </div>
      <div class="hero-stats">
        <div><span>筛选结果</span><b>{{ total }}<em> 件</em></b></div>
        <div><span>在库可售</span><b>{{ inStockCount }}<em> 件</em></b></div>
        <div><span>库存成本</span><b>¥{{ money(stockCost) }}</b></div>
      </div>
      <div class="hero-rings"><i></i><i></i><i></i></div>
    </header>

    <section class="archive-toolbar">
      <div class="search-area">
        <el-input v-model="query.keyword" clearable placeholder="搜索货品编码、名称或条码" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
        <el-date-picker
          v-model="dateRange"
          type="daterange"
          value-format="YYYY-MM-DD"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          unlink-panels
          clearable
          @change="search"
        />
        <el-select v-model="query.status" clearable placeholder="全部状态" @change="search">
          <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
        </el-select>
        <el-button plain @click="search">查询</el-button>
      </div>
      <div class="view-note"><span class="live-dot"></span><span>显示入库中、在库及出库状态商品</span><el-button link type="primary" :loading="loading" @click="loadList">刷新</el-button></div>
    </section>

    <div v-loading="loading" class="goods-archive">
      <template v-if="rows.length">
        <article v-for="row in rows" :key="row.id" class="archive-card" :class="statusClass(row.status)" @click="openEditor(row)">
          <div class="card-visual">
            <el-image v-if="row.imageUrl" :src="row.imageUrl" fit="cover" :preview-src-list="[row.imageUrl]" preview-teleported @click.stop />
            <div v-else class="visual-placeholder"><el-icon><PictureFilled /></el-icon><span>SILENT JEWELRY</span></div>
            <span class="status-ribbon"><i></i>{{ statusLabel(row.status) }}</span>
            <span class="card-code"><span class="code-text">{{ row.goodsNo }}</span><el-tooltip content="复制商品编码" placement="top"><button type="button" class="copy-code" aria-label="复制商品编码" @click.stop="copyGoodsNo(row.goodsNo)"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
          </div>
          <div class="card-body">
            <div class="card-heading">
              <div><span class="item-label">货品名称</span><h3>{{ row.goodsName }}</h3></div>
              <button type="button" v-if="canEdit" class="edit-button" aria-label="编辑商品" @click.stop="openEditor(row)"><el-icon><EditPen /></el-icon></button>
            </div>
            <div class="card-attributes"><el-tag v-for="tag in attributeTags(row)" :key="tag" size="small" effect="plain" round>{{ tag }}</el-tag></div>
            <div class="unit-line"><span>{{ pricingLabel(row) }}</span><i></i><span>{{ row.weight ? '重量 ' + number(row.weight) + ' g' : '重量未记录' }}</span><i></i><span>{{ laborLabel(row) }}</span><i></i><span>包装 ¥{{ money(row.packagingFee) }}/件</span></div>
            <p class="card-remark">{{ row.remark || '暂未填写尺寸、设计细节或保养说明' }}</p>
            <div class="price-row"><div><span>成本价</span><b>¥{{ money(row.costPrice) }}</b></div><div><span>{{ Number(row.pricingMode) === 2 ? '销售计价' : '销售价' }}</span><b class="sale">{{ Number(row.pricingMode) === 2 ? '按当前金价' : '¥' + money(row.salePrice) }}</b></div></div>
            <footer>
              <span class="source"><el-icon><Document /></el-icon>{{ row.inboundNo || '来源单号缺失' }}</span>
              <span>{{ dateText(row.inboundTime || row.createTime) }}</span>
            </footer>
          </div>
        </article>
      </template>
      <div v-else class="empty-archive">
        <div class="empty-display"><el-icon><Collection /></el-icon></div>
        <span class="eyebrow">EMPTY ARCHIVE</span>
        <h3>档案柜里还没有商品</h3>
        <p>完成采购入库后，商品会自动进入档案，无需手工新建。</p>
        <el-button v-hasPermi="['jingyue:inbound:add']" type="primary" plain @click="handleGoInbound">前往采购入库</el-button>
      </div>
    </div>

    <div class="archive-footer">
      <span>当前展示第 {{ total ? (query.pageNum - 1) * query.pageSize + 1 : 0 }} - {{ Math.min(query.pageNum * query.pageSize, total) }} 件，共 {{ total }} 件</span>
      <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
    </div>

    <el-drawer v-model="editorOpen" size="520px" :close-on-click-modal="false" class="goods-drawer" :before-close="beforeClose">
      <template #header>
        <div class="drawer-heading">
          <span class="eyebrow">ITEM PROFILE</span>
          <h2>维护商品信息</h2>
          <p>编码与入库来源为系统资产，不允许修改。</p>
        </div>
      </template>

      <div v-if="form.goodsNo" class="drawer-code-card">
        <div><span>货品编码</span><strong>{{ form.goodsNo }}<el-tooltip content="复制商品编码" placement="top"><button type="button" class="copy-code" aria-label="复制商品编码" @click.stop="copyGoodsNo(form.goodsNo)"><el-icon><CopyDocument /></el-icon></button></el-tooltip></strong></div>
        <el-tag :type="currentStatusTag" effect="plain" round>{{ statusLabel(form.status) }}</el-tag>
      </div>

      <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="goods-form" :disabled="!canEdit || saving">
        <div class="image-editor">
          <el-upload class="image-uploader" :show-file-list="false" :http-request="uploadImage" :before-upload="beforeImageUpload" accept="image/png,image/jpeg,image/webp" :disabled="!canEdit || saving || imageUploading">
            <div class="image-frame" :class="{ filled: form.imageUrl }"><el-image v-if="form.imageUrl" :src="form.imageUrl" fit="cover" :preview-src-list="[form.imageUrl]" preview-teleported @click.stop /><template v-else><el-icon><PictureFilled /></el-icon><span>{{ imageUploading ? '上传中…' : '上传商品照片' }}</span></template></div>
          </el-upload>
          <div class="image-copy"><h3>商品主图</h3><p>建议使用纯色背景，突出材质、光泽与款式细节。图片会同步展示在库存和销售页面。</p><el-button v-if="form.imageUrl && canEdit" link type="danger" @click="clearImage">移除照片</el-button></div>
        </div>

        <el-form-item label="货品名称" prop="goodsName"><el-input v-model="form.goodsName" maxlength="100" show-word-limit placeholder="请输入货品名称" /></el-form-item>
        <div v-loading="optionsLoading" class="attribute-form-grid">
          <el-form-item label="材质大类"><el-select v-model="form.materialGroup"><el-option :value="1" label="足金类" /><el-option :value="2" label="足银类" /><el-option :value="3" label="其他类" /></el-select></el-form-item>
          <el-form-item label="款式大类"><el-select v-model="form.styleParentId" clearable placeholder="请选择大类" @change="changeStyleParent"><el-option v-for="item in styleParents" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="款式分类" prop="styleItemId"><el-select v-model="form.styleItemId" clearable :disabled="!form.styleParentId" placeholder="请选择二级分类"><el-option v-for="item in styleChildren" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="材质"><el-select v-model="form.materialItemId" clearable placeholder="请选择材质"><el-option v-for="item in options.materials" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="金属成色"><el-select v-model="form.metalItemId" clearable placeholder="请选择金属成色"><el-option v-for="item in options.metals" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
          <el-form-item label="证书类型"><el-select v-model="form.certTypeId" clearable placeholder="请选择证书类型"><el-option v-for="item in options.certTypes" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
        </div>
        <div class="price-fields"><el-form-item label="成本价"><el-input-number v-model="form.costPrice" :min="0" :precision="2" :step="10" controls-position="right" /></el-form-item><el-form-item :label="form.pricingMode === 2 ? '入库金属价（元/克）' : '销售价（元/件）'"><el-input-number v-model="form.salePrice" :min="0" :precision="2" :step="10" controls-position="right" /><small v-if="form.pricingMode === 2" class="field-hint">销售时另取维护金价，可在开单时调整成交价</small></el-form-item></div>
        <div class="unit-fields">
          <el-form-item label="计量单位"><el-select v-model="form.unit" placeholder="选择单位"><el-option v-for="unit in unitOptions" :key="unit" :label="unit" :value="unit" /></el-select></el-form-item>
          <el-form-item label="重量（克）"><el-input-number v-model="form.weight" :min="0" :max="100000" :precision="2" :step="0.1" controls-position="right" placeholder="按克计价必填" /></el-form-item>
        </div>
        <div class="unit-fields">
          <el-form-item label="计价方式"><el-radio-group v-model="form.pricingMode"><el-radio-button :value="1">按件</el-radio-button><el-radio-button :value="2">按克</el-radio-button></el-radio-group></el-form-item>
        </div>
        <div class="price-fields"><el-form-item :label="form.pricingMode === 2 ? '工费（元/克）' : '工费（元/件）'"><el-input-number v-model="form.laborFee" :min="0" :precision="2" :step="5" controls-position="right" /></el-form-item><el-form-item label="包装费（每件）"><el-input-number v-model="form.packagingFee" :min="0" :precision="2" :step="1" controls-position="right" /></el-form-item></div>
        <!-- <el-form-item label="商品状态">
          <el-select v-model="form.status" style="width: 100%" disabled="true">
            <el-option v-for="item in editableStatusOptions" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item> -->
        <el-form-item label="商品备注"><el-input v-model="form.remark" type="textarea" :rows="4" maxlength="500" show-word-limit placeholder="尺寸、设计细节、保养说明等普通备注" /></el-form-item>
      </el-form>

      <div v-if="!options.certTypes.length && !optionsLoading" class="attribute-form-hint"><el-icon><InfoFilled /></el-icon><span>暂无证书类型，可先在“数据字典”维护后回填。</span></div>

      <div class="source-panel">
        <span class="source-title">入库来源</span>
        <div><span>来源单号</span><b>{{ form.inboundNo || '-' }}</b></div>
        <div><span>入库时间</span><b>{{ dateText(form.inboundTime) }}</b></div>
        <div><span>供应商</span><b>{{ form.supplierName || '未指定' }}</b></div>
      </div>

      <template #footer>
        <div class="drawer-footer"><el-button plain @click="editorOpen = false">取消</el-button><el-button v-if="canEdit" v-hasPermi="['jingyue:goods:edit']" type="primary" :loading="saving" @click="submit">保存档案</el-button></div>
      </template>
    </el-drawer>
  </div>
</template>
<script setup name="JyGoodsArchive">
import { Search, Collection, PictureFilled, EditPen, Document, Lock, InfoFilled, CopyDocument } from '@element-plus/icons-vue'
import { useClipboard } from '@vueuse/core'
import { listGoods, getGoods, getGoodsOptions, saveGoodsItem } from '@/api/jingyue/goods'
import { upload } from '@/api/common'
import { serverRange } from '@/utils/serverTime'
const props = defineProps({
  embedded: { type: Boolean, default: false }
})
const emit = defineEmits(['go-inbound'])
const { proxy } = getCurrentInstance()
const { copy, isSupported } = useClipboard({ legacy: true })
const loading = ref(false)
const saving = ref(false)
const imageUploading = ref(false)
const rows = ref([])
const total = ref(0)
const query = reactive({ pageNum: 1, pageSize: 12, keyword: '', status: undefined, beginTime: undefined, endTime: undefined, period: undefined })
const dateRange = ref(recentWeekRange())
const editorOpen = ref(false)
const formRef = ref()
const form = reactive(emptyForm())
const options = reactive({ categories: [], materials: [], metals: [], certTypes: [] })
const optionsLoading = ref(false)

const statusOptions = [
  { value: 1, label: '在库' },
  { value: 2, label: '定金锁定' },
  { value: 3, label: '销售出库' },
  { value: 4, label: '赠送出库' },
  { value: 7, label: '入库中' },
  { value: 8, label: '返厂出库' },
  { value: 5, label: '盘亏' },
  { value: 6, label: '停用' }
]
const editableStatusOptions = statusOptions.filter((item) => [1, 5, 6].includes(item.value))
const unitOptions = ['条', '颗', '对', '件', '套']
const rules = {
  goodsName: [{ required: true, message: '请输入货品名称', trigger: 'blur' }],
  styleItemId: [{ required: true, message: '请选择二级款式分类', trigger: 'change' }]
}
const inStockCount = computed(() => rows.value.filter((item) => item.status === 1).length)
const stockCost = computed(() => rows.value.filter((item) => item.status === 1).reduce((sum, item) => sum + Number(item.costPrice || 0), 0))
const styleParents = computed(() => options.categories.filter((item) => item.level === 1))
const styleChildren = computed(() => options.categories.filter((item) => item.level === 2 && item.parentId === form.styleParentId))
const canEdit = computed(() => proxy.$auth.hasPermi('jingyue:goods:edit'))
const currentStatusTag = computed(() => {
  if (form.status === 1) return 'success'
  if (form.status === 2) return 'warning'
  if (form.status === 3) return 'info'
  return 'danger'
})

function emptyForm() {
  return {
    id: 0, goodsNo: '', goodsName: '', unit: '件', weight: null, costPrice: 0, salePrice: 0, pricingMode: 1, laborFee: 0, laborFeeMode: 1, packagingFee: 0, materialGroup: 3, imageFileId: null, imageUrl: '', status: 1, remark: '', inboundNo: '', inboundTime: '', supplierName: '',
    styleParentId: null, styleItemId: null, materialItemId: null, metalItemId: null, certTypeId: null,
    styleItemName: '', materialName: '', metalName: '', certTypeName: ''
  }
}
function recentWeekRange() { return serverRange(6) }
function syncDateRange() {
  query.beginTime = dateRange.value?.[0] || undefined
  query.endTime = dateRange.value?.[1] || undefined
}
async function copyGoodsNo(value) {
  const text = String(value || '').trim()
  if (!text) return proxy.$modal.msgWarning('没有可复制的商品编码')
  if (!isSupported.value) return proxy.$modal.msgWarning('当前浏览器不支持自动复制')
  await copy(text)
  proxy.$modal.msgSuccess('商品编码已复制')
}
function money(value) { return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }
function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }
function dateText(value) { return value ? String(value).replace('T', ' ').slice(0, 16) : '-' }
function statusLabel(value) { return statusOptions.find((item) => item.value === value)?.label || '未知状态' }
function statusClass(value) {
  if (value === 1) return 'in-stock'
  if (value === 6) return 'disabled'
  if (value === 7) return 'inbound'
  if (value === 8) return 'factory'
  return 'away'
}
function parentIdOfStyle(styleItemId) {
  return options.categories.find((item) => Number(item.id) === Number(styleItemId))?.parentId || null
}
function changeStyleParent() {
  form.styleItemId = null
  form.styleItemName = ''
}
function attributeTags(item) {
  return [item.styleItemName, item.materialName, item.metalName, item.certTypeName].filter(Boolean)
}
function pricingLabel(item) {
  return Number(item.pricingMode) === 2 ? '按克计价' : '按件计价'
}
function laborLabel(item) {
  const fee = Number(item.laborFee || 0)
  if (!fee) return '无工费'
  return Number(item.laborFeeMode) === 2 ? '工费 ' + number(fee) + ' 元/克' : '工费 ' + number(fee) + ' 元/件'
}
function loadOptions() {
  optionsLoading.value = true
  return getGoodsOptions().then((res) => {
    const data = res.data || {}
    options.categories = data.categories || []
    options.materials = data.materials || []
    options.metals = data.metals || []
    options.certTypes = data.certTypes || []
  }).catch((error) => {
    proxy.$modal.msgWarning(error?.message || '商品属性选项加载失败，请刷新重试')
  }).finally(() => { optionsLoading.value = false })
}

function loadList() {
  loading.value = true
  query.period = undefined
  syncDateRange()
  listGoods(query).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function search() { query.pageNum = 1; loadList() }
function handleGoInbound() { emit('go-inbound') }

async function openEditor(row) {
  try {
    const res = await getGoods(row.id)
    const data = res.data || {}
    Object.assign(form, {
      id: data.id,
      goodsNo: data.goodsNo || '',
      goodsName: data.goodsName || '',
      unit: data.unit || '件',
      weight: data.weight ?? null,
      costPrice: Number(data.costPrice || 0),
      salePrice: Number(data.salePrice || 0),
      pricingMode: Number(data.pricingMode || 1),
      laborFee: Number(data.laborFee || 0),
      packagingFee: Number(data.packagingFee || 0),
      materialGroup: Number(data.materialGroup || 3),
      laborFeeMode: Number(data.laborFeeMode || 1),
      imageFileId: data.imageFileId || null,
      imageUrl: data.imageUrl || '',
      status: data.status || 1,
      remark: data.remark || '',
      inboundNo: data.inboundNo || '',
      inboundTime: data.inboundTime || '',
      supplierName: data.supplierName || '',
      styleParentId: parentIdOfStyle(data.styleItemId),
      styleItemId: data.styleItemId || null,
      materialItemId: data.materialItemId || null,
      metalItemId: data.metalItemId || null,
      certTypeId: data.certTypeId || null,
      styleItemName: data.styleItemName || '',
      materialName: data.materialName || '',
      metalName: data.metalName || '',
      certTypeName: data.certTypeName || ''
    })
    editorOpen.value = true
    nextTick(() => formRef.value?.clearValidate?.())
  } catch (error) {}
}
function beforeImageUpload(file) {
  const allowed = ['image/jpeg', 'image/png', 'image/webp']
  if (!allowed.includes(file.type)) {
    proxy.$modal.msgWarning('仅支持 JPG、PNG、WebP 图片')
    return false
  }
  if (file.size > 10 * 1024 * 1024) {
    proxy.$modal.msgWarning('图片不能超过 10MB')
    return false
  }
  return true
}
function uploadImage(options) {
  const data = new FormData()
  data.append('file', options.file)
  data.append('fileNameType', '1')
  imageUploading.value = true
  upload(data).then((res) => {
    const file = res.data || {}
    if (!file.fileId || !file.url) throw new Error('上传结果不完整')
    form.imageFileId = String(file.fileId)
    form.imageUrl = file.url
    options.onSuccess(file)
  }).catch((error) => options.onError(error)).finally(() => { imageUploading.value = false })
}
function clearImage() {
  form.imageFileId = null
  form.imageUrl = ''
}
function beforeClose(done) {
  if (saving.value) return
  done()
}
function submit() {
  if (!canEdit.value) return proxy.$modal.msgWarning('没有商品档案编辑权限')
  formRef.value?.validate(async (valid) => {
    if (!valid) return
    saving.value = true
    try {
      await saveGoodsItem({
        id: form.id,
        goodsName: form.goodsName.trim(),
        unit: form.unit || '件',
        weight: form.weight ?? null,
        costPrice: Number(form.costPrice || 0),
        salePrice: Number(form.salePrice || 0),
        pricingMode: Number(form.pricingMode || 1),
        laborFee: Number(form.laborFee || 0),
        packagingFee: Number(form.packagingFee || 0),
        materialGroup: Number(form.materialGroup || 3),
        laborFeeMode: Number(form.laborFeeMode || 1),
        imageFileId: form.imageFileId || null,
        status: form.status,
        remark: form.remark.trim(),
        styleItemId: form.styleItemId || null,
        materialItemId: form.materialItemId || null,
        metalItemId: form.metalItemId || null,
        certTypeId: form.certTypeId || null
      })
      proxy.$modal.msgSuccess('商品档案已保存')
      editorOpen.value = false
      loadList()
    } finally {
      saving.value = false
    }
  })
}

defineExpose({ refresh: loadList })

onMounted(() => {
  loadOptions()
  loadList()
})
</script>
<style scoped>
.jy-goods { --rose-950: #3d2730; --rose-900: #51313a; --rose-700: #8c5361; --rose-600: #a86272; --rose-500: #bb7b8a; --rose-300: #e5c1c8; --rose-200: #efd7dc; --rose-100: #f8eaed; --gold: #bfa069; min-height: 100%; padding: 22px; color: var(--rose-950); background: radial-gradient(circle at 4% 0%, #fff3f5 0, transparent 26%), linear-gradient(180deg, #fffafb 0, #fdf8f8 100%); }
.jy-goods button { font-family: inherit; }
.field-hint { display: block; margin-top: 3px; color: #8b7a80; font-size: 11px; line-height: 1.35; }
.eyebrow { display: block; color: var(--rose-500); font-size:11.5px; font-weight: 700; letter-spacing: 2px; }
.archive-hero { position: relative; display: grid; grid-template-columns: minmax(0, 1fr) auto; align-items: center; gap: 20px; min-height: 112px; padding: 17px 22px; overflow: hidden; color: #4b3540; background: linear-gradient(112deg, #fffafb 0%, #fdf2f5 62%, #f7e4e9 100%); border: 1px solid #efdce2; border-radius: 17px; box-shadow: 0 8px 22px rgba(196,120,138,.07); }
.hero-content { position: relative; z-index: 2; max-width: 710px; }
.archive-hero .eyebrow { color: #b56f80; }
.archive-hero h1 { margin: 4px 0 3px; color: #4b3540; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 23px; font-weight: 500; letter-spacing: .055em; }
.archive-hero p { max-width: 680px; margin: 0; color: #92737d; font-size: 11px; line-height: 1.55; }
.hero-note { display: inline-flex; align-items: center; gap: 6px; margin-top: 8px; padding: 4px 9px; color: #9b6573; background: #fff7f9; border: 1px solid #efd8de; border-radius: 999px; font-size:11.5px; }
.hero-stats { position: relative; z-index: 2; display: grid; grid-template-columns: repeat(3, 96px); background: rgba(255,255,255,.66); border: 1px solid #ecd6dc; border-radius: 12px; }
.hero-stats > div { display: flex; flex-direction: column; gap: 5px; padding: 10px 12px; border-left: 1px solid #f0dde2; }
.hero-stats > div:first-child { border-left: 0; }
.hero-stats span { color: #a48690; font-size:11.5px; }
.hero-stats b { color: #704552; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.hero-stats b em { color: #a37c86; font-size:11.5px; font-style: normal; }
.hero-rings { position: absolute; right: -85px; bottom: -155px; width: 330px; height: 330px; border: 1px solid rgba(196,120,138,.11); border-radius: 50%; }
.hero-rings::before, .hero-rings::after { content: ''; position: absolute; inset: 48px; border: 1px solid rgba(196,120,138,.09); border-radius: 50%; }
.hero-rings::after { inset: 96px; }
.hero-rings i { position: absolute; width: 6px; height: 6px; background: #d9a6b2; border-radius: 50%; box-shadow: 0 0 16px rgba(217,166,178,.45); }
.hero-rings i:nth-child(1) { left: 50px; top: 88px; }
.hero-rings i:nth-child(2) { left: 132px; top: 37px; width: 4px; height: 4px; }
.hero-rings i:nth-child(3) { left: 179px; top: 125px; width: 5px; height: 5px; }
.archive-toolbar { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 16px; margin: 16px 0; padding: 13px 16px; background: rgba(255,255,255,.9); border: 1px solid #efdee1; border-radius: 14px; }
.search-area { display: flex; flex-wrap: wrap; gap: 8px; }
.search-area :deep(.el-input) { width: 240px; }
.search-area :deep(.el-date-editor) { width: 248px; }
.search-area :deep(.el-select) { width: 125px; }
.view-note { display: flex; align-items: center; gap: 7px; color: #9c848b; font-size:11.5px; white-space: nowrap; }
.live-dot { width: 5px; height: 5px; background: #80a78e; border-radius: 50%; }
.view-note :deep(.el-button) { font-size:11.5px; }
.goods-archive { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 15px; min-height: 420px; }
.archive-card { overflow: hidden; background: #fff; border: 1px solid #f0e0e3; border-radius: 17px; box-shadow: 0 8px 22px rgba(103,58,70,.04); cursor: pointer; transition: .22s ease; }
.archive-card:hover { transform: translateY(-4px); border-color: #dcb8c0; box-shadow: 0 15px 30px rgba(104,57,70,.1); }
.card-visual { position: relative; height: 192px; overflow: hidden; background: linear-gradient(145deg, #fbf0f2, #f5dfe3); }
.card-visual > .el-image { width: 100%; height: 100%; transition: transform .45s ease; }
.archive-card:hover .card-visual > .el-image { transform: scale(1.04); }
.visual-placeholder { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 9px; width: 100%; height: 100%; color: #bb93a0; background: radial-gradient(circle at 50% 40%, #fffafb 0, #f8e8eb 72%); }
.visual-placeholder .el-icon { font-size: 31px; }
.visual-placeholder span { color: #ba9ca4; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: 2px; }
.status-ribbon { position: absolute; right: 10px; top: 10px; display: flex; align-items: center; gap: 5px; padding: 5px 8px; color: #6b5158; background: rgba(255,251,251,.9); border-radius: 999px; font-size:11px; backdrop-filter: blur(6px); }
.status-ribbon i { width: 5px; height: 5px; background: #79a788; border-radius: 50%; }
.archive-card.away .status-ribbon i { background: #c7a566; }
.archive-card.disabled .status-ribbon i { background: #a99a9d; } .archive-card.inbound .status-ribbon i { background: #7b9ec9; } .archive-card.factory .status-ribbon i { background: #9b7fb8; }
.card-code { position: absolute; left: 10px; bottom: 10px; display: flex; align-items: center; gap: 2px; max-width: calc(100% - 20px); padding: 3px 5px 3px 8px; overflow: hidden; color: #fff; background: rgba(68,41,49,.72); border-radius: 6px; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11px; letter-spacing: .5px; white-space: nowrap; backdrop-filter: blur(6px); }
.code-text { min-width: 0; overflow: hidden; text-overflow: ellipsis; }
.copy-code { display: inline-grid; flex: none; place-items: center; width: 21px; height: 21px; padding: 0; color: inherit; background: transparent; border: 0; border-radius: 4px; cursor: pointer; opacity: .7; transition: .18s; }
.copy-code:hover { color: #fff; background: rgba(255,255,255,.14); opacity: 1; }
.card-body { padding: 15px 16px 0; }
.card-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; }
.item-label { color: #b1989e; font-size:11px; letter-spacing: .5px; }
.card-heading h3 { margin: 4px 0 0; overflow: hidden; color: #4d343b; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.edit-button { display: grid; place-items: center; flex: none; width: 28px; height: 28px; color: #a1878e; background: #fff9fa; border: 1px solid #f1e2e5; border-radius: 9px; cursor: pointer; transition: .18s; }
.edit-button:hover { color: #9c5a68; background: #fdf0f2; border-color: #e4c3ca; }
.card-attributes { display: flex; flex-wrap: wrap; gap: 4px; min-height: 18px; margin-top: 8px; }
.card-attributes :deep(.el-tag) { max-width: 100%; color: #9d6170; background: #fff8f9; border-color: #efd8dd; font-size:11px; }
.unit-line { display: flex; align-items: center; gap: 8px; margin-top: 9px; color: #a77e88; font-size: 11.5px; }
.unit-line i { width: 3px; height: 3px; background: #d8b6be; border-radius: 50%; }
.attribute-form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 13px; margin-bottom: 2px; padding: 13px 13px 0; background: linear-gradient(135deg, #fffafb, #fdf3f5); border: 1px solid #f2e3e6; border-radius: 12px; }
.attribute-form-grid :deep(.el-form-item) { margin-bottom: 13px; }
.attribute-form-grid :deep(.el-select) { width: 100%; }
.attribute-form-hint { display: flex; align-items: flex-start; gap: 7px; margin: -3px 0 14px; padding: 8px 10px; color: #9b7b59; background: #fffaf1; border: 1px solid #f1e4cd; border-radius: 9px; font-size:11.5px; line-height: 1.6; }
.attribute-form-hint .el-icon { flex: none; margin-top: 1px; color: #bf9a61; }
.card-remark { display: -webkit-box; min-height: 31px; margin: 9px 0 12px; overflow: hidden; color: #a18b91; font-size:11.5px; line-height: 1.7; -webkit-box-orient: vertical; -webkit-line-clamp: 2; }
.price-row { display: grid; grid-template-columns: 1fr 1fr; padding: 11px 0; border-top: 1px solid #f6ebed; }
.price-row > div { display: flex; flex-direction: column; gap: 4px; }
.price-row > div + div { padding-left: 12px; border-left: 1px solid #f5e9eb; }
.price-row span { color: #b0989e; font-size:11px; }
.price-row b { color: #74545d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; }
.price-row b.sale { color: #a25464; }
.card-body footer { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin: 0 -16px; padding: 9px 16px; color: #a78f95; background: #fdf9f9; border-top: 1px solid #f6ebed; font-size:11px; }
.source { display: flex; align-items: center; gap: 4px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.empty-archive { grid-column: 1 / -1; display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 440px; padding: 30px; color: #9e858c; background: rgba(255,255,255,.75); border: 1px dashed #e4c9cf; border-radius: 18px; text-align: center; }
.empty-display { display: grid; place-items: center; width: 74px; height: 74px; margin-bottom: 16px; color: #bd8492; background: linear-gradient(145deg, #fffafb, #f5e3e7); border: 1px solid #eed6db; border-radius: 50%; font-size: 29px; }
.empty-archive h3 { margin: 9px 0 6px; color: #63454e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }
.empty-archive p { margin: 0 0 18px; font-size:12px; }
.archive-footer { display: flex; align-items: center; justify-content: space-between; gap: 14px; margin-top: 17px; color: #a58d94; font-size:11.5px; }
.drawer-heading h2 { margin: 5px 0 3px; color: #4d333b; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 22px; font-weight: 500; }
.drawer-heading p { margin: 0; color: #a28b91; font-size:11.5px; }
.drawer-code-card { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 18px; padding: 13px 15px; background: linear-gradient(135deg, #fff8f9, #fdf0f2); border: 1px solid #efdde1; border-radius: 12px; }
.drawer-code-card > div { display: flex; flex-direction: column; gap: 4px; }
.drawer-code-card span { color: #a48d93; font-size:11px; }
.drawer-code-card strong { display: flex; align-items: center; gap: 3px; color: #754d58; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 16px; font-weight: 500; letter-spacing: .8px; }
.drawer-code-card .copy-code { color: #a48d93; opacity: 1; }
.drawer-code-card .copy-code:hover { color: #9b5364; background: #fff; }
.image-editor { display: grid; grid-template-columns: 126px minmax(0, 1fr); gap: 15px; align-items: center; margin-bottom: 19px; }
.image-uploader { width: 126px; }
.image-uploader :deep(.el-upload) { width: 126px; height: 126px; }
.image-frame { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 7px; width: 126px; height: 126px; overflow: hidden; color: #af8a93; background: linear-gradient(145deg, #fffafb, #f9ebee); border: 1px dashed #dfbcc4; border-radius: 14px; cursor: pointer; }
.image-frame.filled { border-style: solid; border-color: #ecd5d9; }
.image-frame .el-image { width: 100%; height: 100%; }
.image-frame .el-icon { font-size: 24px; }
.image-frame span { font-size:11.5px; }
.image-copy h3 { margin: 0 0 5px; color: #62464f; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 500; }
.image-copy p { margin: 0 0 6px; color: #a38b92; font-size:11.5px; line-height: 1.65; }
.image-copy :deep(.el-button) { font-size:11.5px; padding: 0; }
.goods-form :deep(.el-form-item__label) { padding-bottom: 5px; color: #916f78; font-size:12px; }
.price-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.price-fields :deep(.el-input-number) { width: 100%; }
.unit-fields { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.unit-fields :deep(.el-input-number), .unit-fields :deep(.el-select) { width: 100%; }
.goods-form :deep(.el-input__wrapper), .goods-form :deep(.el-textarea__inner) { border-radius: 9px; }
.source-panel { margin-top: 5px; padding: 14px 15px; background: #fcf8f7; border: 1px solid #f1e6e6; border-radius: 12px; }
.source-title { display: block; margin-bottom: 10px; color: #806268; font-size:11.5px; letter-spacing: .6px; }
.source-panel > div { display: flex; justify-content: space-between; gap: 12px; padding: 5px 0; border-top: 1px dashed #f0e3e5; font-size:11.5px; }
.source-panel > div:first-of-type { border-top: 0; }
.source-panel span { color: #aa9299; }
.source-panel b { color: #6c525a; font-weight: 500; }
.drawer-footer { display: flex; justify-content: flex-end; gap: 9px; }
:deep(.el-button) { border-radius: 9px; }
:deep(.el-drawer__header) { margin-bottom: 8px; padding-bottom: 16px; border-bottom: 1px solid #f3e7e9; }
:deep(.el-drawer__footer) { border-top: 1px solid #f3e7e9; }
@media (max-width: 1420px) { .goods-archive { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
@media (max-width: 1180px) { .archive-hero { grid-template-columns: 1fr; } .hero-stats { justify-self: start; } .goods-archive { grid-template-columns: repeat(2, minmax(0, 1fr)); } .search-area :deep(.el-input) { width: 210px; } .search-area :deep(.el-date-editor) { width: 235px; } }
@media (max-width: 820px) { .attribute-form-grid { grid-template-columns: 1fr; } .jy-goods:not(.is-embedded) { padding: 12px; } .archive-hero { padding: 23px; } .hero-stats { grid-template-columns: repeat(3, 1fr); width: 100%; } .hero-stats > div { padding: 12px 8px; } .archive-toolbar, .search-area { align-items: stretch; flex-direction: column; } .search-area :deep(.el-input), .search-area :deep(.el-date-editor), .search-area :deep(.el-select) { width: 100%; } .view-note { justify-content: flex-end; } .goods-archive { grid-template-columns: 1fr; } .archive-footer { align-items: flex-start; flex-direction: column; } .image-editor { grid-template-columns: 105px minmax(0, 1fr); } .image-uploader, .image-uploader :deep(.el-upload), .image-frame { width: 105px; } .image-frame { height: 112px; } }
.jy-goods.is-embedded { min-height: auto; padding: 0; background: transparent; }
.jy-goods.is-embedded .archive-toolbar { margin-top: 0; }
.jy-goods.is-embedded .goods-archive { min-height: 360px; }
</style>
