<template>
  <div class="jy-sheet">
    <header class="jy-sheet__head">
      <div>
        <p>静悦基础</p>
        <h2>{{ currentLabel }}</h2>
      </div>
      <span>{{ currentHint }}</span>
    </header>
    <div class="jy-sheet__body">
      <aside>
        <button
          v-for="item in nav"
          :key="item.key"
          type="button"
          :class="{ on: activeKey === item.key }"
          @click="switchNav(item.key)">
          <strong>{{ item.title }}</strong>
          <em>{{ item.meta }}</em>
        </button>
        <button type="button" :class="{ on: activeKey === 'stock-alert' }" @click="switchNav('stock-alert')">
          <strong>库存预警设置</strong>
          <em>按一级款式设置</em>
        </button>
        <button type="button" :class="{ on: activeKey === 'print-template' }" @click="switchNav('print-template')">
          <strong>标签打印模板</strong>
          <em>模板设计与分类绑定</em>
        </button>
        <el-button v-hasPermi="['jingyue:lookup:add']" class="add-type" plain @click="openType()">新增字典</el-button>
      </aside>
      <section>
        <template v-if="activeKey === 'stock-alert'">
          <div class="toolbar alert-toolbar">
            <div class="alert-copy"><b>库存预警线</b><span>统计一级款式下所有在库商品；低于最低库存量标红，接近预警线标橙。</span></div>
            <el-button :loading="alertLoading" plain @click="loadAlertSettings">刷新库存</el-button>
          </div>
          <div v-loading="alertLoading" class="config-list alert-list">
            <article v-for="row in alertRows" :key="row.styleItemId" class="config-row alert-row" :class="'alert-' + row.alertLevel">
              <div class="config-title"><b>{{ row.styleItemName }}</b><small>一级款式分类</small></div>
              <div class="stock-now"><span>当前在库</span><b>{{ number(row.currentQty) }}</b><em>件</em></div>
              <label class="field-cell"><span>最低库存</span><el-input-number v-model="row.minQty" :min="0" :max="999999" :precision="0" controls-position="right" /></label>
              <label class="field-cell remark-cell"><span>备注</span><el-input v-model="row.remark" maxlength="500" placeholder="选填" /></label>
              <span class="alert-state" :class="'alert-' + row.alertLevel">{{ row.status === 0 ? (row.alertLevel === 2 ? '低于预警线' : row.alertLevel === 1 ? '临近预警线' : '库存正常') : '已停用' }}</span>
              <div class="row-actions">
                <el-switch v-model="row.status" :active-value="0" :inactive-value="1" inline-prompt active-text="启用" inactive-text="停用" />
                <el-button v-hasPermi="['jingyue:stock:alert','jingyue:dict:edit']" type="primary" plain size="small" @click="saveAlert(row)">保存</el-button>
              </div>
            </article>
            <div v-if="!alertRows.length && !alertLoading" class="config-empty">还没有一级款式分类，请先在款式分类中维护大类。</div>
          </div>
        </template>

        <template v-else-if="activeKey === 'print-template'">
          <div v-if="printDesignerOpen" class="designer-shell">
            <label-designer :template="printTemplateForm" :available-fields="printAvailableFields" :saving="printSaving" @save="submitPrintTemplate" @cancel="printDesignerOpen = false" />
          </div>
          <div v-else v-loading="printLoading" class="print-config">
            <div class="toolbar print-toolbar">
              <div class="alert-copy"><b>标签打印模板</b><span>画布按毫米定位；模板保存为结构化坐标 JSON，按一级款式分类绑定后用于入库和库存标签。</span></div>
              <el-button v-hasPermi="['jingyue:print:template','jingyue:dict:edit']" type="primary" @click="openPrintTemplate()">新增模板</el-button>
            </div>
            <section class="print-section">
              <div class="print-section-head"><b>模板库</b><small>{{ printTemplates.length }} 个模板</small></div>
              <div class="template-grid">
                <article v-for="template in printTemplates" :key="template.id" class="template-card" :class="{ off: template.status === 1 }">
                  <div class="template-preview" :style="{ aspectRatio: template.widthMm + ' / ' + template.heightMm }">
                    <span v-for="field in template.fields.slice(0, 12)" :key="field.key || field.fieldKey" class="thumb-field" :class="field.type" :style="thumbStyle(field, template)">{{ field.type === 'text' ? (field.label || fieldLabelOf(field.fieldKey || field.key)) : field.type === 'barcode' ? '||||' : 'QR' }}</span>
                  </div>
                  <div class="template-copy"><b>{{ template.templateName }}</b><small>{{ template.widthMm }} × {{ template.heightMm }} mm · {{ template.fields.length }} 个字段</small></div>
                  <div class="template-actions"><el-button link type="primary" @click="openPrintTemplate(template)">编辑</el-button><el-button v-hasPermi="['jingyue:print:template','jingyue:dict:edit']" link type="danger" @click="removePrintTemplate(template)">删除</el-button></div>
                </article>
                <div v-if="!printTemplates.length" class="config-empty">还没有打印模板，先新增一个模板。</div>
              </div>
            </section>
            <section class="print-section">
              <div class="print-section-head"><b>按一级款式绑定</b><small>每个分类只保留一个默认模板</small></div>
              <div class="binding-list">
                <article v-for="style in styleParents" :key="style.id" class="binding-row">
                  <div class="config-title"><b>{{ style.itemName }}</b><small>一级款式分类</small></div>
                  <el-select v-model="bindingForm[style.id]" clearable filterable placeholder="选择打印模板" @change="saveBinding(style)">
                    <el-option v-for="template in enabledPrintTemplates" :key="template.id" :label="template.templateName" :value="template.id" />
                  </el-select>
                  <span class="binding-state">{{ bindingName(style.id) || '未绑定' }}</span>
                </article>
                <div v-if="!styleParents.length" class="config-empty">还没有一级款式分类。</div>
              </div>
            </section>
          </div>
        </template>

        <template v-else-if="activeKey === 'supplier'">
          <div class="toolbar">
            <el-input v-model="supplierQuery.keyword" clearable placeholder="编码、名称、联系人或电话" @keyup.enter="loadSupplier" />
            <el-select v-model="supplierQuery.status" clearable placeholder="状态" @change="loadSupplier">
              <el-option label="启用" :value="0" />
              <el-option label="停用" :value="1" />
            </el-select>
            <el-button type="primary" @click="loadSupplier">查询</el-button>
            <el-button v-hasPermi="['jingyue:supplier:add']" @click="openSupplier()">新增供应商</el-button>
          </div>
          <el-table v-loading="loading" :data="supplierRows" height="100%">
            <el-table-column prop="supplierCode" label="编码" width="140" />
            <el-table-column prop="supplierName" label="名称" min-width="160" />
            <el-table-column prop="contactName" label="联系人" width="120" />
            <el-table-column prop="contactPhone" label="电话" width="150" />
            <el-table-column prop="address" label="地址" min-width="180" show-overflow-tooltip />
            <el-table-column label="状态" width="90">
              <template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right">
              <template #default="{ row }">
                <el-button v-hasPermi="['jingyue:supplier:edit']" link type="primary" @click="openSupplier(row)">编辑</el-button>
                <el-button v-hasPermi="['jingyue:supplier:remove']" link type="danger" @click="removeSupplier(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <pagination v-show="supplierTotal > 0" :total="supplierTotal" v-model:page="supplierQuery.pageNum" v-model:limit="supplierQuery.pageSize" @pagination="loadSupplier" />
        </template>

        <template v-else-if="activeKey === 'script'">
          <div class="toolbar">
            <el-input v-model="scriptQuery.keyword" clearable placeholder="标题或内容" @keyup.enter="loadScript" />
            <el-select v-model="scriptQuery.styleItemId" clearable filterable placeholder="款式" @change="loadScript">
              <el-option v-for="item in styleOptions" :key="item.id" :label="optionLabel(item)" :value="item.id" />
            </el-select>
            <el-button type="primary" @click="loadScript">查询</el-button>
            <el-button v-hasPermi="['jingyue:script:add']" @click="openScript()">新增话术</el-button>
          </div>
          <el-table v-loading="loading" :data="scriptRows" height="100%">
            <el-table-column prop="title" label="标题" min-width="160" />
            <el-table-column label="星级" width="150">
              <template #default="{ row }"><el-rate :model-value="row.sortNo || 3" disabled size="small" /></template>
            </el-table-column>
            <el-table-column prop="styleName" label="款式" min-width="140" />
            <el-table-column prop="materialName" label="材质" width="120" />
            <el-table-column prop="metalName" label="成色" width="120" />
            <el-table-column prop="content" label="内容" min-width="220" show-overflow-tooltip />
            <el-table-column label="状态" width="90">
              <template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template>
            </el-table-column>
            <el-table-column label="操作" width="140" fixed="right">
              <template #default="{ row }">
                <el-button v-hasPermi="['jingyue:script:edit']" link type="primary" @click="openScript(row)">编辑</el-button>
                <el-button v-hasPermi="['jingyue:script:remove']" link type="danger" @click="removeScript(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <pagination v-show="scriptTotal > 0" :total="scriptTotal" v-model:page="scriptQuery.pageNum" v-model:limit="scriptQuery.pageSize" @pagination="loadScript" />
        </template>

        <template v-else>
          <div class="toolbar">
            <div class="type-note">
              <b>{{ currentType?.typeName }}</b>
              <span>{{ currentType?.levelLimit === 2 ? '大类和子级，两层，不能再往下挂' : '只有一层' }}</span>
            </div>
            <el-button v-hasPermi="['jingyue:lookup:edit']" @click="openType(currentType)">编辑字典</el-button>
            <el-button v-if="currentType?.isPreset !== 1" v-hasPermi="['jingyue:lookup:remove']" @click="removeType">删除字典</el-button>
            <el-button v-hasPermi="['jingyue:lookup:add']" type="primary" @click="openItem()">{{ currentType?.levelLimit === 2 ? '新增大类' : '新增数据' }}</el-button>
          </div>
          <el-table
            v-loading="loading"
            :data="itemRows"
            row-key="id"
            height="100%"
            :tree-props="{ children: 'children' }"
            default-expand-all>
            <el-table-column prop="itemName" label="名称" min-width="180" />
            <el-table-column prop="itemCode" label="编码" width="160" />
            <el-table-column label="层级" width="90">
              <template #default="{ row }">{{ row.parentId ? '子级' : currentType?.levelLimit === 2 ? '大类' : '一层' }}</template>
            </el-table-column>
            <el-table-column prop="sortNo" label="排序" width="80" />
            <el-table-column label="状态" width="90">
              <template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template>
            </el-table-column>
            <el-table-column prop="remark" label="备注" min-width="160" show-overflow-tooltip />
            <el-table-column label="操作" width="220" fixed="right">
              <template #default="{ row }">
                <el-button v-if="currentType?.levelLimit === 2 && !row.parentId" v-hasPermi="['jingyue:lookup:add']" link type="primary" @click="openItem(null, row.id)">加子级</el-button>
                <el-button v-hasPermi="['jingyue:lookup:edit']" link type="primary" @click="openItem(row)">编辑</el-button>
                <el-button v-if="row.isPreset !== 1" v-hasPermi="['jingyue:lookup:remove']" link type="danger" @click="removeItem(row)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
        </template>
      </section>
    </div>

    <el-dialog v-model="typeOpen" :title="typeForm.id ? '编辑字典' : '新增字典'" width="480px" append-to-body>
      <el-form :model="typeForm" label-width="88px">
        <el-form-item label="编码"><el-input v-model="typeForm.typeCode" :disabled="typeForm.isPreset === 1" maxlength="32" /></el-form-item>
        <el-form-item label="名称"><el-input v-model="typeForm.typeName" maxlength="50" /></el-form-item>
        <el-form-item label="层级">
          <el-radio-group v-model="typeForm.levelLimit" :disabled="typeForm.isPreset === 1">
            <el-radio :value="1">一层</el-radio>
            <el-radio :value="2">大类 + 子级</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="排序"><el-input-number v-model="typeForm.sortNo" :min="0" :max="9999" /></el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="typeForm.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="typeForm.remark" type="textarea" maxlength="500" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="typeOpen = false">取消</el-button>
        <el-button type="primary" @click="submitType">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="itemOpen" :title="itemForm.id ? '编辑数据' : '新增数据'" width="480px" append-to-body>
      <el-form :model="itemForm" label-width="88px">
        <el-form-item v-if="currentType?.levelLimit === 2" label="上级">
          <el-select v-model="itemForm.parentId" clearable placeholder="不选就是大类" :disabled="itemLockedParent">
            <el-option v-for="item in rootItems" :key="item.id" :label="item.itemName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="编码"><el-input v-model="itemForm.itemCode" :disabled="itemForm.isPreset === 1" maxlength="32" /></el-form-item>
        <el-form-item label="名称"><el-input v-model="itemForm.itemName" maxlength="100" /></el-form-item>
        <el-form-item label="排序"><el-input-number v-model="itemForm.sortNo" :min="0" :max="9999" /></el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="itemForm.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="itemForm.remark" type="textarea" maxlength="500" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="itemOpen = false">取消</el-button>
        <el-button type="primary" @click="submitItem">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="supplierOpen" :title="supplierForm.id ? '编辑供应商' : '新增供应商'" width="520px" append-to-body>
      <el-form :model="supplierForm" label-width="88px">
        <el-form-item label="编码"><el-input v-model="supplierForm.supplierCode" maxlength="32" /></el-form-item>
        <el-form-item label="名称"><el-input v-model="supplierForm.supplierName" maxlength="100" /></el-form-item>
        <el-form-item label="联系人"><el-input v-model="supplierForm.contactName" maxlength="50" /></el-form-item>
        <el-form-item label="电话"><el-input v-model="supplierForm.contactPhone" maxlength="30" /></el-form-item>
        <el-form-item label="地址"><el-input v-model="supplierForm.address" maxlength="200" /></el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="supplierForm.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="supplierForm.remark" type="textarea" maxlength="500" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="supplierOpen = false">取消</el-button>
        <el-button type="primary" @click="submitSupplier">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="scriptOpen" :title="scriptForm.id ? '编辑话术' : '新增话术'" width="640px" append-to-body>
      <el-form :model="scriptForm" label-width="88px">
        <el-form-item label="标题"><el-input v-model="scriptForm.title" maxlength="100" /></el-form-item>
        <el-form-item label="款式">
          <el-select v-model="scriptForm.styleItemId" clearable filterable placeholder="不限">
            <el-option v-for="item in styleOptions" :key="item.id" :label="optionLabel(item)" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="材质">
          <el-select v-model="scriptForm.materialItemId" clearable filterable placeholder="不限">
            <el-option v-for="item in materialOptions" :key="item.id" :label="item.itemName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="成色">
          <el-select v-model="scriptForm.metalItemId" clearable filterable placeholder="不限">
            <el-option v-for="item in metalOptions" :key="item.id" :label="item.itemName" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="内容"><el-input v-model="scriptForm.content" type="textarea" :rows="6" maxlength="4000" show-word-limit /></el-form-item>
        <el-form-item label="星级"><el-rate v-model="scriptForm.sortNo" :max="5" show-score score-template="{value} 星" /></el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="scriptForm.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="scriptForm.remark" type="textarea" maxlength="500" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="scriptOpen = false">取消</el-button>
        <el-button type="primary" @click="submitScript">保存</el-button>
      </template>
    </el-dialog>

  </div>
</template>

<script setup name="JyDict">
import { listLookupTypes, saveLookupType, deleteLookupType, listLookupItems, lookupOptions, saveLookupItem, deleteLookupItem } from '@/api/jingyue/lookup'
import { listSupplier, saveSupplier, deleteSupplier } from '@/api/jingyue/supplier'
import { listScript, saveScript, deleteScript } from '@/api/jingyue/script'
import { getStockAlertSettings, saveStockAlertSetting, getPrintOptions, savePrintTemplate, deletePrintTemplate, savePrintBinding } from '@/api/jingyue/homeConfig'
import LabelDesigner from './components/LabelDesigner.vue'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const types = ref([])
const activeKey = ref('supplier')
const itemRows = ref([])
const styleOptions = ref([])
const materialOptions = ref([])
const metalOptions = ref([])
const supplierRows = ref([])
const supplierTotal = ref(0)
const scriptRows = ref([])
const scriptTotal = ref(0)
const typeOpen = ref(false)
const itemOpen = ref(false)
const supplierOpen = ref(false)
const scriptOpen = ref(false)
const printDesignerOpen = ref(false)
const alertLoading = ref(false)
const printLoading = ref(false)
const printSaving = ref(false)
const alertRows = ref([])
const printTemplates = ref([])
const printBindings = ref([])
const printAvailableFields = ref([])
const bindingForm = reactive({})
const itemLockedParent = ref(false)
const supplierQuery = reactive({ pageNum: 1, pageSize: 10, keyword: '', status: undefined })
const scriptQuery = reactive({ pageNum: 1, pageSize: 10, keyword: '', styleItemId: undefined })
const typeForm = reactive({ id: 0, typeCode: '', typeName: '', levelLimit: 1, status: 0, sortNo: 0, remark: '', isPreset: 0 })
const itemForm = reactive({ id: 0, typeId: 0, parentId: undefined, itemCode: '', itemName: '', status: 0, sortNo: 0, remark: '', isPreset: 0 })
const supplierForm = reactive({ id: 0, supplierCode: '', supplierName: '', contactName: '', contactPhone: '', address: '', status: 0, remark: '' })
const scriptForm = reactive({ id: 0, title: '', styleItemId: undefined, materialItemId: undefined, metalItemId: undefined, content: '', status: 0, sortNo: 3, remark: '' })
const printTemplateForm = reactive({
  id: 0, templateName: '', widthMm: 50, heightMm: 30, gapMm: 2, offsetMm: 4, copies: 1,
  port: 'USB,', speed: 4, density: 8, showBarcode: 0, showQrCode: 0, status: 0, remark: '', fields: []
})

const nav = computed(() => {
  const dicts = types.value.map((it) => ({
    key: 'type-' + it.id,
    title: it.typeName,
    meta: (it.levelLimit === 2 ? '两层' : '一层') + ' · ' + it.itemCount
  }))
  return [{ key: 'supplier', title: '供应商档案', meta: '进货来源' }, ...dicts, { key: 'script', title: '销售话术', meta: '款式 / 材质 / 成色' }]
})
const currentType = computed(() => types.value.find((it) => 'type-' + it.id === activeKey.value))
const styleParents = computed(() => styleOptions.value.filter((it) => !it.parentId))
const enabledPrintTemplates = computed(() => printTemplates.value.filter((it) => it.status === 0))
watch(styleParents, (styles) => {
  styles.forEach((style) => {
    if (bindingForm[style.id] === undefined) bindingForm[style.id] = printBindings.value.find((it) => it.styleItemId === style.id)?.templateId
  })
})
const currentLabel = computed(() => nav.value.find((it) => it.key === activeKey.value)?.title || '基础资料')
const currentHint = computed(() => {
  if (activeKey.value === 'stock-alert') return '按一级款式分类维护最低在库量，首页会同步显示低于和临近预警线的分类。'
  if (activeKey.value === 'print-template') return '设计入库商品标签，按一级款式分类绑定，库存商品详情可选择模板打印。'
  if (activeKey.value === 'supplier') return '供应商不进系统字典，单独建档。'
  if (activeKey.value === 'script') return '话术可以不限款式，也可以挂到大类或子级。'
  if (currentType.value?.typeCode === 'style') return '上面是大类，下面是款式。初始分类可以改，也可以删。'
  if (currentType.value?.typeCode === 'metal') return '金属成色只做一层，不预填未确认的成色名称。'
  if (currentType.value?.typeCode === 'coupon_type') return '折扣券、代金券、礼品券是预置数据，只能停用。'
  return '预置字典不能删除。停用后新业务不要再引用。'
})
const rootItems = computed(() => itemRows.value.filter((it) => !it.parentId))

function optionLabel(item) {
  const parent = styleOptions.value.find((it) => it.id === item.parentId)
  return parent ? parent.itemName + ' / ' + item.itemName : item.itemName
}

function switchNav(key) {
  activeKey.value = key
  refresh()
}

function number(value) { return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 }) }

function loadTypes() {
  return listLookupTypes().then((res) => {
    types.value = res.data || []
    if (activeKey.value.startsWith('type-') && !currentType.value && types.value.length) {
      activeKey.value = 'type-' + types.value[0].id
    }
  })
}

function loadItems() {
  if (!currentType.value) return
  loading.value = true
  listLookupItems(currentType.value.id).then((res) => {
    itemRows.value = res.data || []
  }).finally(() => { loading.value = false })
}

function loadOptions() {
  lookupOptions('style').then((res) => { styleOptions.value = res.data || [] })
  lookupOptions('material').then((res) => { materialOptions.value = (res.data || []).filter((it) => !it.parentId) })
  lookupOptions('metal').then((res) => { metalOptions.value = (res.data || []).filter((it) => !it.parentId) })
}

function loadSupplier() {
  loading.value = true
  listSupplier(supplierQuery).then((res) => {
    supplierRows.value = res.data?.result || []
    supplierTotal.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}

function loadScript() {
  loading.value = true
  listScript(scriptQuery).then((res) => {
    scriptRows.value = res.data?.result || []
    scriptTotal.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}

function loadAlertSettings() {
  alertLoading.value = true
  getStockAlertSettings().then((res) => { alertRows.value = res.data || [] }).finally(() => { alertLoading.value = false })
}

function saveAlert(row) {
  saveStockAlertSetting({ styleItemId: row.styleItemId, minQty: Number(row.minQty || 0), status: row.status, remark: row.remark || '' })
    .then((res) => { Object.assign(row, res.data || {}); proxy.$modal.msgSuccess('库存预警设置已保存') })
}

function loadPrintOptions() {
  printLoading.value = true
  getPrintOptions().then((res) => {
    const data = res.data || {}
    printTemplates.value = data.templates || []
    printBindings.value = data.bindings || []
    printAvailableFields.value = data.availableFields || []
    Object.keys(bindingForm).forEach((key) => delete bindingForm[key])
    styleParents.value.forEach((style) => {
      bindingForm[style.id] = printBindings.value.find((it) => it.styleItemId === style.id)?.templateId
    })
  }).finally(() => { printLoading.value = false })
}

function bindingName(styleItemId) {
  return printBindings.value.find((it) => it.styleItemId === styleItemId)?.templateName
}

function openPrintTemplate(template) {
  Object.assign(printTemplateForm, template
    ? JSON.parse(JSON.stringify(template))
    : {
        id: 0, templateName: '', widthMm: 50, heightMm: 30, gapMm: 2, offsetMm: 4, copies: 1,
        port: 'USB,', speed: 4, density: 8, showBarcode: 0, showQrCode: 0, status: 0, remark: '', fields: []
      })
  printDesignerOpen.value = true
}

function submitPrintTemplate(payload) {
  printSaving.value = true
  savePrintTemplate(payload).then(() => {
    proxy.$modal.msgSuccess('打印模板已保存')
    printDesignerOpen.value = false
    loadPrintOptions()
  }).finally(() => { printSaving.value = false })
}

function fieldLabelOf(key) {
  return printAvailableFields.value.find((item) => item.key === key)?.label || '文字'
}

function thumbStyle(field, template) {
  const w = Number(template.widthMm || 50)
  const h = Number(template.heightMm || 30)
  return {
    left: (Number(field.x || 0) / w) * 100 + '%',
    top: (Number(field.y || 0) / h) * 100 + '%',
    width: (Number(field.width || 10) / w) * 100 + '%',
    height: (Number(field.height || 4) / h) * 100 + '%',
    fontSize: Math.max(6, Number(field.fontSize || 12) * 0.36) + 'px',
    fontWeight: field.bold ? 700 : 400
  }
}

function removePrintTemplate(template) {
  proxy.$modal.confirm('确认删除模板「' + template.templateName + '」？已绑定款式时需先解除绑定。')
    .then(() => deletePrintTemplate(template.id)).then(() => { proxy.$modal.msgSuccess('模板已删除'); loadPrintOptions() }).catch(() => {})
}

function saveBinding(style) {
  const templateId = bindingForm[style.id]
  if (!templateId) return
  savePrintBinding({ styleItemId: style.id, templateId }).then(() => { proxy.$modal.msgSuccess(style.itemName + ' 已绑定模板'); loadPrintOptions() })
}

function refresh() {
  if (activeKey.value === 'stock-alert') loadAlertSettings()
  else if (activeKey.value === 'print-template') loadPrintOptions()
  else if (activeKey.value === 'supplier') loadSupplier()
  else if (activeKey.value === 'script') loadScript()
  else loadItems()
}

function openType(row) {
  Object.assign(typeForm, row ? { ...row } : { id: 0, typeCode: '', typeName: '', levelLimit: 1, status: 0, sortNo: types.value.length + 1, remark: '', isPreset: 0 })
  typeOpen.value = true
}
function submitType() {
  saveLookupType(typeForm).then(() => {
    typeOpen.value = false
    loadTypes()
  })
}
function removeType() {
  proxy.$modal.confirm('删除后不能恢复，字典下不能还有数据。').then(() => deleteLookupType(currentType.value.id)).then(() => {
    activeKey.value = 'supplier'
    loadTypes()
  }).catch(() => {})
}

function openItem(row, parentId) {
  itemLockedParent.value = !!row?.children?.length
  Object.assign(itemForm, row
    ? { ...row, parentId: row.parentId || undefined }
    : { id: 0, typeId: currentType.value.id, parentId: parentId || undefined, itemCode: '', itemName: '', status: 0, sortNo: 0, remark: '', isPreset: 0 })
  itemForm.typeId = currentType.value.id
  itemOpen.value = true
}
function submitItem() {
  saveLookupItem({ ...itemForm, parentId: itemForm.parentId || null }).then(() => {
    itemOpen.value = false
    loadTypes()
    loadItems()
    loadOptions()
  })
}
function removeItem(row) {
  proxy.$modal.confirm('确认删除「' + row.itemName + '」？有子级或被话术使用时不能删。').then(() => deleteLookupItem(row.id)).then(() => {
    loadTypes()
    loadItems()
    loadOptions()
  }).catch(() => {})
}

function openSupplier(row) {
  Object.assign(supplierForm, row || { id: 0, supplierCode: '', supplierName: '', contactName: '', contactPhone: '', address: '', status: 0, remark: '' })
  supplierOpen.value = true
}
function submitSupplier() {
  saveSupplier(supplierForm).then(() => { supplierOpen.value = false; loadSupplier() })
}
function removeSupplier(row) {
  proxy.$modal.confirm('确认删除供应商「' + row.supplierName + '」？').then(() => deleteSupplier(row.id)).then(loadSupplier).catch(() => {})
}

function openScript(row) {
  Object.assign(scriptForm, row ? { ...row, sortNo: row.sortNo || 3 } : { id: 0, title: '', styleItemId: undefined, materialItemId: undefined, metalItemId: undefined, content: '', status: 0, sortNo: 3, remark: '' })
  scriptOpen.value = true
}
function submitScript() {
  saveScript({
    ...scriptForm,
    sortNo: Math.min(5, Math.max(1, Number(scriptForm.sortNo || 3))),
    styleItemId: scriptForm.styleItemId || null,
    materialItemId: scriptForm.materialItemId || null,
    metalItemId: scriptForm.metalItemId || null
  }).then(() => { scriptOpen.value = false; loadScript() })
}
function removeScript(row) {
  proxy.$modal.confirm('确认删除话术「' + row.title + '」？').then(() => deleteScript(row.id)).then(loadScript).catch(() => {})
}

onMounted(() => {
  loadTypes().then(() => {
    const style = types.value.find((it) => it.typeCode === 'style')
    const requestedTab = String(proxy.$route?.query?.tab || '')
    if (requestedTab === 'stock-alert' || requestedTab === 'print-template') activeKey.value = requestedTab
    else if (style) activeKey.value = 'type-' + style.id
    refresh()
  })
  loadOptions()
})
</script>

<style scoped lang="scss">
.jy-sheet { display: flex; flex-direction: column; min-height: calc(100vh - 132px); color: #3d2c32; }
.jy-sheet__head { display: flex; align-items: flex-end; justify-content: space-between; gap: 16px; padding: 4px 4px 14px; border-bottom: 1px solid #f0e1e5; }
.jy-sheet__head p { margin: 0 0 4px; color: #c4788a; letter-spacing: 2px; font-size: 12px; }
.jy-sheet__head h2 { margin: 0; font-size: 24px; font-weight: 650; }
.jy-sheet__head span { max-width: 360px; color: #8d7380; font-size: 13px; line-height: 1.6; text-align: right; }
.jy-sheet__body { display: grid; grid-template-columns: 220px 1fr; gap: 16px; min-height: 0; flex: 1; padding-top: 14px; }
aside { display: flex; flex-direction: column; gap: 6px; padding: 8px; background: #fff7f8; border: 1px solid #f3e4e8; border-radius: 14px; }
aside button { display: flex; flex-direction: column; align-items: flex-start; gap: 2px; width: 100%; padding: 10px 12px; text-align: left; color: inherit; background: transparent; border: 0; border-radius: 10px; cursor: pointer; }
aside button strong { font-size: 14px; font-weight: 600; }
aside button em { color: #a07b88; font-style: normal; font-size: 12px; }
aside button.on { background: #fff; box-shadow: inset 3px 0 0 #c4788a; }
.add-type { margin-top: 8px; }
section { display: flex; flex-direction: column; min-width: 0; min-height: 520px; padding: 12px; background: #fff; border: 1px solid #f3e4e8; border-radius: 14px; }
.toolbar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 12px; }
.toolbar .el-input, .toolbar .el-select { width: 220px; }
.alert-toolbar, .print-toolbar { justify-content: space-between; }
.alert-copy { display: flex; flex-direction: column; gap: 3px; }
.alert-copy b { color: #513a41; font-size: 15px; }
.alert-copy span { color: #9b7e87; font-size: 12px; }
.config-list { display: flex; flex-direction: column; gap: 7px; }
.config-row { display: grid; grid-template-columns: minmax(112px, .8fr) 92px 156px minmax(130px, 1.3fr) 82px 146px; gap: 10px; align-items: center; padding: 11px 12px; background: #fffafb; border: 1px solid #f4e7ea; border-radius: 9px; }
.config-row.alert-2 { background: #fff7f4; border-color: #f0d6cc; }
.config-row.alert-1 { background: #fffbf2; border-color: #f1e3c6; }
.config-title { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.config-title b { overflow: hidden; color: #513a41; font-size: 13px; text-overflow: ellipsis; white-space: nowrap; }
.config-title small { color: #a58a91; font-size: 11px; }
.stock-now { display: flex; align-items: baseline; gap: 3px; min-width: 0; padding: 5px 9px; background: #fff; border: 1px solid #f1e2e6; border-radius: 7px; }
.stock-now span { color: #b19aa0; font-size:11.5px; white-space: nowrap; }
.stock-now b { color: #6e4d57; font-size: 16px; font-variant-numeric: tabular-nums; }
.stock-now em { color: #aa939a; font-style: normal; font-size:11.5px; }
.field-cell { display: flex; flex-direction: column; gap: 3px; min-width: 0; }
.field-cell > span { color: #a58a91; font-size:11.5px; line-height: 1; }
.field-cell :deep(.el-input-number), .field-cell :deep(.el-input) { width: 100%; }
.row-actions { display: flex; align-items: center; justify-content: flex-end; gap: 8px; }
.alert-state { color: #8c747b; font-size: 11px; white-space: nowrap; }
.alert-state.alert-2 { color: #b55e55; font-weight: 600; }
.alert-state.alert-1 { color: #b38748; font-weight: 600; }
.config-empty { padding: 28px 12px; color: #a58a91; font-size: 12px; text-align: center; }
.print-config { display: flex; flex-direction: column; gap: 18px; }
.print-section { padding: 13px; background: #fdfafa; border: 1px solid #f3e4e8; border-radius: 10px; }
.print-section-head { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 11px; }
.print-section-head b { color: #513a41; font-size: 14px; }
.print-section-head small { color: #aa8e96; font-size: 11px; }
.template-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(155px, 1fr)); gap: 10px; }
.template-card { display: flex; flex-direction: column; min-width: 0; padding: 10px; background: #fff; border: 1px solid #f0dfe3; border-radius: 8px; }
.template-card.off { opacity: .55; }
.template-preview { position: relative; width: 100%; max-height: 128px; overflow: hidden; color: #67565a; background: #fffdfa; border: 1px dashed #cbb9a2; }
.thumb-field { position: absolute; box-sizing: border-box; overflow: hidden; line-height: 1.1; white-space: nowrap; text-overflow: ellipsis; }
.thumb-field.barcode { color: #3c2d2f; font-family: 'Courier New', monospace; letter-spacing: -.5px; text-align: center; }
.thumb-field.qrcode { display: grid; place-items: center; color: #fff; background: #322a2b; font-size: 7px; }
.designer-shell { margin: 0 -2px; container-type: inline-size; }
.preview-line { display: flex; align-items: center; gap: 4px; font-size:11px; }
.preview-line span { flex: none; color: #8e7a7e; }
.preview-line i { flex: 1; height: 1px; background: #e6d8db; }
.template-copy { display: flex; flex-direction: column; gap: 3px; margin: 8px 0 3px; }
.template-copy b { overflow: hidden; color: #513a41; font-size: 12px; text-overflow: ellipsis; white-space: nowrap; }
.template-copy small { color: #a58a91; font-size:12px; }
.template-actions { display: flex; justify-content: flex-end; }
.binding-list { display: flex; flex-direction: column; gap: 6px; }
.binding-row { display: grid; grid-template-columns: minmax(120px, 1fr) 220px 130px; gap: 12px; align-items: center; padding: 8px 10px; background: #fff; border-bottom: 1px solid #f6ebed; }
.binding-row:last-child { border-bottom: 0; }
.binding-state { color: #a3848c; font-size: 11px; }
.form-two { display: grid; grid-template-columns: 1fr 1fr; gap: 0 16px; }
.code-options { display: flex; gap: 18px; margin: -3px 0 16px; }
.field-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 6px 12px; padding: 10px 12px; background: #fdfafa; border: 1px solid #f3e4e8; border-radius: 8px; }
.template-form :deep(.el-form-item__label) { color: #806970; font-size: 12px; }
.type-note { display: flex; flex-direction: column; margin-right: auto; }
.type-note b { font-size: 15px; }
.type-note span { color: #8d7380; font-size: 12px; }
@media (max-width: 900px) {
  .jy-sheet__body { grid-template-columns: 1fr; }
  .jy-sheet__head { align-items: flex-start; flex-direction: column; }
  .jy-sheet__head span { text-align: left; }
  .config-row { grid-template-columns: 1fr 1fr; }
  .config-row .config-title { grid-column: 1 / -1; }
  .alert-row .remark-cell, .alert-row .row-actions { grid-column: 1 / -1; }
  .row-actions { justify-content: flex-start; }
  .binding-row { grid-template-columns: 1fr; gap: 5px; }
  .form-two, .field-grid { grid-template-columns: 1fr 1fr; }
}
</style>
