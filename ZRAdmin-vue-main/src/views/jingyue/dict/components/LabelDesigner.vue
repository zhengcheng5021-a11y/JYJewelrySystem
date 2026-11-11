<template>
  <div class="label-designer">
    <header class="designer-bar">
      <div class="designer-title">
        <span>LABEL STUDIO</span>
        <b>{{ model.templateName || '未命名标签' }}</b>
        <small>{{ model.widthMm }} x {{ model.heightMm }} mm · 203 DPI · 8 dots/mm</small>
      </div>
      <div class="designer-actions">
        <button type="button" class="tool-btn" @click="testConnection">连接测试</button>
        <button type="button" class="tool-btn" :disabled="previewBusy" @click="refreshPreview(true)">打印预览</button>
        <button type="button" class="tool-btn primary" :disabled="saving" @click="saveTemplate">{{ saving ? '保存中' : '保存模板' }}</button>
      </div>
    </header>

    <div class="designer-workspace">
      <aside class="settings-panel">
        <section class="panel-block">
          <div class="block-title">基础信息</div>
          <label class="ctl"><span>模板名称</span><input v-model.trim="model.templateName" maxlength="100" placeholder="例如：手串吊牌 50x30" /></label>
          <div class="ctl-row">
            <label class="ctl"><span>状态</span><select v-model.number="model.status"><option :value="0">启用</option><option :value="1">停用</option></select></label>
            <label class="ctl"><span>打印份数</span><input v-model.number="model.copies" type="number" min="1" max="50" step="1" /></label>
          </div>
        </section>
        <section class="panel-block">
          <div class="block-title">标签与纸张 <small>毫米</small></div>
          <div class="ctl-row">
            <label class="ctl"><span>标签宽度</span><input v-model.number="model.widthMm" type="number" min="20" max="120" step="1" @change="fitElements" /></label>
            <label class="ctl"><span>标签高度</span><input v-model.number="model.heightMm" type="number" min="15" max="180" step="1" @change="fitElements" /></label>
          </div>
          <div class="ctl-row">
            <label class="ctl"><span>纸张间距</span><input v-model.number="model.gapMm" type="number" min="0" max="10" step="1" /></label>
            <label class="ctl"><span>左边距</span><input v-model.number="model.offsetMm" type="number" min="0" max="20" step="1" /></label>
          </div>
        </section>
        <section class="panel-block">
          <div class="block-title">打印机 <small>{{ agentReady ? '本机已连接' : '未连接' }}</small></div>
          <label class="ctl"><span>通讯端口</span>
            <select v-model="model.port">
              <option value="USB,">USB,（芯烨 USB）</option>
              <option value="COM1,9600">COM1,9600</option>
              <option value="COM2,9600">COM2,9600</option>
              <option value="COM3,9600">COM3,9600</option>
            </select>
          </label>
          <div class="ctl-row">
            <label class="ctl"><span>打印速度</span><input v-model.number="model.speed" type="number" min="1" max="10" step="1" /></label>
            <label class="ctl"><span>打印浓度</span><input v-model.number="model.density" type="number" min="0" max="15" step="1" /></label>
          </div>
        </section>
        <section class="panel-block">
          <div class="block-title">模板备注</div>
          <textarea v-model="model.remark" maxlength="500" rows="3" placeholder="选填"></textarea>
        </section>
      </aside>

      <main class="canvas-panel">
        <div class="canvas-toolbar">
          <span class="canvas-size">{{ model.widthMm }} x {{ model.heightMm }} mm</span>
          <label class="snap"><input v-model="snap" type="checkbox" /> 网格吸附 1 mm</label>
          <span class="toolbar-spacer"></span>
          <span class="canvas-hint">{{ selected ? '拖动元素调整位置，右下角缩放' : '点击元素编辑属性' }}</span>
          <div class="zoom-tools">
            <button type="button" title="缩小" @click="changeZoom(-0.1)">−</button>
            <span>{{ Math.round(zoom * 100) }}%</span>
            <button type="button" title="放大" @click="changeZoom(0.1)">+</button>
            <button type="button" class="fit" title="适应窗口" @click="fitCanvas">适</button>
          </div>
        </div>
        <div ref="viewportRef" class="canvas-viewport">
          <div class="canvas-wrap">
            <div ref="stageRef" class="canvas-stage" :style="stageStyle" @pointerdown.self="selectedId = ''">
              <div class="canvas-grid minor" :style="gridMinorStyle"></div>
              <div class="canvas-grid major" :style="gridMajorStyle"></div>
              <div v-for="element in elements" :key="element.id" class="design-element" :class="{ selected: element.id === selectedId, barcode: element.type === 'barcode', qrcode: element.type === 'qrcode' }" :style="elementStyle(element)" @pointerdown.stop="startDrag($event, element)">
                <span v-if="element.type === 'text'" class="text-content">{{ textPreview(element) }}</span>
                <template v-else-if="element.type === 'barcode'"><span class="barcode-bars"></span><small>{{ sampleData[element.fieldKey] || '条码内容' }}</small></template>
                <template v-else><span class="qr-pattern"></span><small>QR</small></template>
                <span v-if="element.id === selectedId" class="element-tag">{{ fieldLabel(element.fieldKey) }}</span>
                <span v-if="element.id === selectedId" class="resize-handle" @pointerdown.stop.prevent="startResize($event, element)"></span>
              </div>
              <div v-if="!elements.length" class="canvas-empty">从下方字段池添加内容</div>
            </div>
          </div>
        </div>
        <div class="field-pool">
          <div class="pool-head"><b>参数字段池</b><span>点击添加，画布内拖拽定位</span></div>
          <div class="pool-chips">
            <button v-for="field in availableFields" :key="field.key" type="button" class="field-chip" @click="addElement(field.key, 'text')">{{ field.label }}</button>
            <button type="button" class="field-chip code-chip" @click="addElement('barcode', 'barcode')">条码（图）</button>
            <button type="button" class="field-chip code-chip" @click="addElement('barcode', 'qrcode')">二维码</button>
          </div>
        </div>
      </main>

      <aside class="inspector-panel">
        <section class="panel-block inspector-block">
          <div class="block-title">元素属性 <small>{{ elements.length }} 个元素</small></div>
          <template v-if="selected">
            <label class="ctl"><span>元素类型</span><select v-model="selected.type"><option value="text">文字</option><option value="barcode">条码</option><option value="qrcode">二维码</option></select></label>
            <label class="ctl"><span>绑定字段</span><select v-model="selected.fieldKey"><option v-for="field in availableFields" :key="field.key" :value="field.key">{{ field.label }}</option></select></label>
            <label class="ctl"><span>前缀 / 文案（可用 {value}）</span><input v-model="selected.label" maxlength="80" /></label>
            <label v-if="selected.fieldKey === 'customText1' || selected.fieldKey === 'customText2'" class="ctl"><span>本元素固定文本</span><input v-model="selected.customText" maxlength="80" placeholder="例如：静悦珠宝 / 足银999" /></label>
            <div class="ctl-row">
              <label class="ctl"><span>X 坐标 mm</span><input v-model.number="selected.x" type="number" step="0.5" /></label>
              <label class="ctl"><span>Y 坐标 mm</span><input v-model.number="selected.y" type="number" step="0.5" /></label>
            </div>
            <div class="ctl-row">
              <label class="ctl"><span>宽度 mm</span><input v-model.number="selected.width" type="number" min="2" step="0.5" /></label>
              <label class="ctl"><span>高度 mm</span><input v-model.number="selected.height" type="number" min="2" step="0.5" /></label>
            </div>
            <div class="ctl-row">
              <label class="ctl"><span>字号 px</span><input v-model.number="selected.fontSize" type="number" min="5" max="72" step="1" /></label>
              <label class="ctl"><span>旋转</span><select v-model.number="selected.rotation"><option :value="0">0°</option><option :value="90">90°</option><option :value="180">180°</option><option :value="270">270°</option></select></label>
            </div>
            <label class="bold-check"><input v-model="selected.bold" type="checkbox" /> 文字加粗</label>
            <button type="button" class="danger-btn" @click="removeSelected">删除此元素</button>
            <p class="shortcut-note">方向键微调 0.5 mm，Shift + 方向键 1 mm，Delete 删除。</p>
          </template>
          <div v-else class="inspector-empty">点击画布元素后，在这里精确调整坐标和样式。</div>
        </section>
        <section class="panel-block preview-block">
          <div class="block-title">出纸预览 <small>{{ agentReady ? '代理在线' : '代理离线' }}</small></div>
          <div class="preview-frame">
            <img v-if="previewImage" :src="previewImage" alt="203 DPI 标签预览" />
            <div v-else class="preview-empty">{{ previewBusy ? '正在生成位图…' : '本机打印代理启动后可生成真实位图预览' }}</div>
          </div>
          <div class="preview-caption"><span>与打印位图同源</span><span>{{ previewMessage }}</span></div>
        </section>
        <section class="panel-block log-block">
          <div class="block-title">运行日志</div>
          <div class="log-box"><p v-for="(line, index) in logs" :key="index">{{ line }}</p></div>
        </section>
      </aside>
    </div>
    <footer class="designer-footer">
      <span>{{ errorMessage || '模板以结构化坐标 JSON 保存，入库与库存打印共用同一套排版。' }}</span>
      <div>
        <button type="button" class="tool-btn" @click="$emit('cancel')">取消</button>
        <button type="button" class="tool-btn primary" :disabled="saving" @click="saveTemplate">{{ saving ? '保存中' : '保存模板' }}</button>
      </div>
    </footer>
  </div>
</template>

<script setup name="LabelDesigner">
import { agentHealth, agentPreview, agentStatus } from '@/api/jingyue/printAgent'

const props = defineProps({
  template: { type: Object, default: () => ({}) },
  availableFields: { type: Array, default: () => [] },
  saving: { type: Boolean, default: false }
})
const emit = defineEmits(['save', 'cancel'])

const PIXELS_PER_MM = 8
const sampleData = {
  goodsNo: 'JY2026100001',
  barcode: '6901234567890',
  goodsName: '静悦·银饰手串',
  styleCategory: '手串',
  material: '足银',
  metal: 'S999',
  weight: '18.6 g',
  salePrice: '¥1,280.00',
  costPrice: '¥680.00',
  certType: '国检证书',
  certNo: 'JY-CERT-0001',
  supplier: '静悦合作工坊',
  inboundDate: '2026-10-27',
  storeNote: '手工编绳，附保养卡'
}

const model = reactive(normalizeTemplate(props.template))
const elements = ref(model.fields || [])
const selectedId = ref('')
const snap = ref(true)
const zoom = ref(1)
const stageRef = ref(null)
const viewportRef = ref(null)
const agentReady = ref(false)
const previewBusy = ref(false)
const previewImage = ref('')
const previewMessage = ref('尚未生成')
const logs = ref(['等待检查本机打印服务。'])
const errorMessage = ref('')
let previewTimer = null
let dragState = null
let elementSeed = 1

const selected = computed(() => elements.value.find((item) => item.id === selectedId.value) || null)
const stageStyle = computed(() => ({
  width: model.widthMm * PIXELS_PER_MM * zoom.value + 'px',
  height: model.heightMm * PIXELS_PER_MM * zoom.value + 'px'
}))
const gridMinorStyle = computed(() => ({ backgroundSize: PIXELS_PER_MM * zoom.value + 'px ' + PIXELS_PER_MM * zoom.value + 'px' }))
const gridMajorStyle = computed(() => ({ backgroundSize: PIXELS_PER_MM * 10 * zoom.value + 'px ' + PIXELS_PER_MM * 10 * zoom.value + 'px' }))

function normalizeTemplate(source) {
  const base = source || {}
  return {
    id: Number(base.id || 0),
    templateName: base.templateName || '',
    widthMm: Number(base.widthMm || 50),
    heightMm: Number(base.heightMm || 30),
    gapMm: Number(base.gapMm ?? 2),
    offsetMm: Number(base.offsetMm ?? 4),
    copies: Number(base.copies || 1),
    port: base.port || 'USB,',
    speed: Number(base.speed || 4),
    density: Number(base.density ?? 8),
    status: Number(base.status || 0),
    remark: base.remark || '',
    showBarcode: Number(base.showBarcode || 0),
    showQrCode: Number(base.showQrCode || 0),
    fields: (base.fields || []).map((item, index) => ({
      id: 'el-' + index + '-' + Date.now(),
      key: item.fieldKey || item.key,
      fieldKey: item.fieldKey || item.key,
      type: item.type || 'text',
      label: item.label || '',
      customText: item.customText || '',
      x: Number(item.x || 0),
      y: Number(item.y || 0),
      width: Number(item.width || 30),
      height: Number(item.height || 6),
      fontSize: Number(item.fontSize || 16),
      bold: Boolean(item.bold),
      rotation: Number(item.rotation || 0),
      enabled: 1,
      sortNo: index + 1
    }))
  }
}

function defaultElements() {
  const width = Number(model.widthMm || 50)
  const height = Number(model.heightMm || 30)
  return [
    { id: 'el-default-name', key: 'goodsName', fieldKey: 'goodsName', type: 'text', label: '', x: 3, y: 2, width: Math.max(10, width - 6), height: 6, fontSize: 20, bold: true, rotation: 0, enabled: 1, sortNo: 1 },
    { id: 'el-default-code', key: 'goodsNo', fieldKey: 'goodsNo', type: 'text', label: 'NO. ', x: 3, y: 9, width: Math.max(10, width - 6), height: 4, fontSize: 12, bold: false, rotation: 0, enabled: 1, sortNo: 2 },
    { id: 'el-default-price', key: 'salePrice', fieldKey: 'salePrice', type: 'text', label: '', x: 3, y: 14, width: Math.max(10, width - 6), height: 6, fontSize: 19, bold: true, rotation: 0, enabled: 1, sortNo: 3 },
    { id: 'el-default-barcode', key: 'barcode', fieldKey: 'barcode', type: 'barcode', label: '', x: 3, y: Math.max(20, height - 10), width: Math.max(10, width - 6), height: Math.max(7, height - Math.max(20, height - 10) - 2), fontSize: 12, bold: false, rotation: 0, enabled: 1, sortNo: 4 }
  ].filter((item) => item.y + item.height <= height + 2)
}

function elementStyle(element) {
  return {
    left: element.x * PIXELS_PER_MM * zoom.value + 'px',
    top: element.y * PIXELS_PER_MM * zoom.value + 'px',
    width: Math.max(12, element.width * PIXELS_PER_MM * zoom.value) + 'px',
    height: Math.max(12, element.height * PIXELS_PER_MM * zoom.value) + 'px',
    fontSize: Math.max(6, Number(element.fontSize || 12) * zoom.value) + 'px',
    fontWeight: element.bold ? 700 : 400,
    transform: element.rotation ? 'rotate(' + element.rotation + 'deg)' : 'none'
  }
}

function fieldLabel(key) {
  return props.availableFields.find((item) => item.key === key)?.label || key || '文本'
}

function textPreview(element) {
  const value = element.customText || sampleData[element.fieldKey] || fieldLabel(element.fieldKey)
  const label = element.label || ''
  if (label.includes('{value}')) return label.replace('{value}', value)
  return label + value
}

function nextPosition() {
  const index = elements.value.length
  return { x: 2 + (index % 3) * 2, y: Math.min(Math.max(1, Number(model.heightMm || 30) - 10), 2 + index * 6) }
}

function addElement(fieldKey, type) {
  const pos = nextPosition()
  const isCode = type !== 'text'
  const element = {
    id: 'el-' + Date.now() + '-' + elementSeed++,
    key: fieldKey,
    fieldKey,
    type,
    label: type === 'text' ? defaultPrefix(fieldKey) : '',
    customText: fieldKey === 'customText1' || fieldKey === 'customText2' ? '静悦珠宝' : '',
    x: pos.x,
    y: pos.y,
    width: Math.max(8, Number(model.widthMm || 50) - pos.x - 3),
    height: isCode ? Math.min(12, Math.max(7, Number(model.heightMm || 30) - pos.y - 2)) : 6,
    fontSize: type === 'text' ? 16 : 12,
    bold: type === 'text' && fieldKey === 'goodsName',
    rotation: 0,
    enabled: 1,
    sortNo: elements.value.length + 1
  }
  elements.value.push(element)
  selectedId.value = element.id
  schedulePreview()
}

function defaultPrefix(fieldKey) {
  if (fieldKey === 'goodsName' || fieldKey === 'salePrice') return ''
  return fieldLabel(fieldKey) + '：'
}

function startDrag(event, element) {
  if (event.button !== 0) return
  selectedId.value = element.id
  dragState = { mode: 'move', startX: event.clientX, startY: event.clientY, originX: Number(element.x || 0), originY: Number(element.y || 0), element }
}

function startResize(event, element) {
  selectedId.value = element.id
  dragState = { mode: 'resize', startX: event.clientX, startY: event.clientY, originW: Number(element.width || 10), originH: Number(element.height || 4), element }
}

function onPointerMove(event) {
  if (!dragState) return
  const dx = (event.clientX - dragState.startX) / (PIXELS_PER_MM * zoom.value)
  const dy = (event.clientY - dragState.startY) / (PIXELS_PER_MM * zoom.value)
  if (dragState.mode === 'move') {
    dragState.element.x = roundMm(dragState.originX + dx)
    dragState.element.y = roundMm(dragState.originY + dy)
  } else {
    dragState.element.width = Math.max(2, roundMm(dragState.originW + dx))
    dragState.element.height = Math.max(2, roundMm(dragState.originH + dy))
  }
  clampElement(dragState.element)
}

function onPointerUp() {
  if (!dragState) return
  dragState = null
  schedulePreview()
}

function roundMm(value) {
  return snap.value ? Math.round(value) : Math.round(value * 2) / 2
}

function clampElement(element) {
  const width = Number(model.widthMm || 50)
  const height = Number(model.heightMm || 30)
  element.x = Math.min(width - 1, Math.max(-5, Number(element.x || 0)))
  element.y = Math.min(height - 1, Math.max(-5, Number(element.y || 0)))
  element.width = Math.min(width - Math.max(0, element.x), Math.max(2, Number(element.width || 2)))
  element.height = Math.min(height - Math.max(0, element.y), Math.max(2, Number(element.height || 2)))
}

function removeSelected() {
  if (!selectedId.value) return
  elements.value = elements.value.filter((item) => item.id !== selectedId.value)
  selectedId.value = ''
  schedulePreview()
}

function changeZoom(delta) {
  zoom.value = Math.min(2.5, Math.max(0.35, Math.round((zoom.value + delta) * 10) / 10))
}

function fitCanvas() {
  const viewport = viewportRef.value
  if (!viewport) return
  const availableW = Math.max(240, viewport.clientWidth - 56)
  const availableH = Math.max(220, viewport.clientHeight - 56)
  zoom.value = Math.min(availableW / (Number(model.widthMm || 50) * PIXELS_PER_MM), availableH / (Number(model.heightMm || 30) * PIXELS_PER_MM), 2.5)
}

function fitElements() {
  if (model.widthMm < 20) model.widthMm = 20
  if (model.heightMm < 15) model.heightMm = 15
  elements.value.forEach(clampElement)
  nextTick(fitCanvas)
  schedulePreview()
}

function onKeydown(event) {
  if (!selected.value) return
  if (['INPUT', 'SELECT', 'TEXTAREA', 'BUTTON'].includes(event.target?.tagName)) return
  const step = event.shiftKey ? 1 : 0.5
  if (event.key === 'Delete' || event.key === 'Backspace') {
    event.preventDefault()
    removeSelected()
    return
  }
  const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] }
  if (moves[event.key]) {
    event.preventDefault()
    selected.value.x += moves[event.key][0]
    selected.value.y += moves[event.key][1]
    clampElement(selected.value)
    schedulePreview()
  }
}

function schedulePreview() {
  window.clearTimeout(previewTimer)
  if (!agentReady.value) return
  previewTimer = window.setTimeout(() => refreshPreview(), 550)
}

async function checkAgent() {
  try {
    await agentHealth()
    agentReady.value = true
    logs.value.unshift('本机打印代理在线：127.0.0.1:18180')
  } catch (error) {
    agentReady.value = false
    logs.value.unshift(String(error.message || error))
  }
}

async function refreshPreview(manual = false) {
  if (previewBusy.value) return
  if (!agentReady.value) {
    await checkAgent()
    if (!agentReady.value) {
      if (manual) previewMessage.value = '本机代理未启动'
      return
    }
  }
  previewBusy.value = true
  try {
    const result = await agentPreview(buildAgentRequest())
    previewImage.value = result.previewImage || ''
    previewMessage.value = '预览已更新'
    if (manual) logs.value.unshift('已按当前坐标生成 203 DPI 预览。')
  } catch (error) {
    previewMessage.value = '预览失败'
    logs.value.unshift(String(error.message || error))
    if (manual) errorMessage.value = String(error.message || error)
  } finally {
    previewBusy.value = false
  }
}

async function testConnection() {
  try {
    const result = await agentStatus(model.port)
    agentReady.value = true
    logs.value.unshift(result.message || '打印机连接正常。')
    errorMessage.value = ''
  } catch (error) {
    agentReady.value = false
    errorMessage.value = String(error.message || error)
    logs.value.unshift(errorMessage.value)
  }
}

function buildAgentRequest() {
  return {
    widthMm: Math.round(Number(model.widthMm || 50)),
    heightMm: Math.round(Number(model.heightMm || 30)),
    copies: Math.max(1, Number(model.copies || 1)),
    port: model.port || 'USB,',
    speed: Math.max(1, Number(model.speed || 4)),
    density: Math.max(0, Number(model.density ?? 8)),
    gapMm: Math.max(0, Number(model.gapMm ?? 2)),
    offsetMm: Math.max(0, Number(model.offsetMm ?? 4)),
    elements: elements.value.map((item) => ({
      id: item.id,
      type: item.type,
      fieldKey: item.fieldKey,
      label: item.label || '',
      x: Number(item.x || 0),
      y: Number(item.y || 0),
      width: Number(item.width || 10),
      height: Number(item.height || 4),
      fontSize: Number(item.fontSize || 16),
      bold: Boolean(item.bold),
      rotation: Number(item.rotation || 0)
    })),
    data: { ...sampleData, customText1: elements.value.find((item) => item.fieldKey === 'customText1')?.customText || '', customText2: elements.value.find((item) => item.fieldKey === 'customText2')?.customText || '' }
  }
}

function saveTemplate() {
  errorMessage.value = ''
  if (!model.templateName) {
    errorMessage.value = '请填写模板名称。'
    return
  }
  if (!elements.value.length) {
    errorMessage.value = '模板至少放置一个打印元素。'
    return
  }
  emit('save', {
    id: Number(model.id || 0),
    templateName: model.templateName,
    widthMm: Number(model.widthMm),
    heightMm: Number(model.heightMm),
    gapMm: Number(model.gapMm ?? 2),
    offsetMm: Number(model.offsetMm ?? 4),
    copies: Number(model.copies || 1),
    port: model.port || 'USB,',
    speed: Number(model.speed || 4),
    density: Number(model.density ?? 8),
    showBarcode: elements.value.some((item) => item.type === 'barcode') ? 1 : 0,
    showQrCode: elements.value.some((item) => item.type === 'qrcode') ? 1 : 0,
    status: Number(model.status || 0),
    remark: model.remark || '',
    fields: elements.value.map((item, index) => ({
      key: item.fieldKey,
      fieldKey: item.fieldKey,
      label: item.label || '',
      customText: item.customText || '',
      type: item.type,
      x: Number(item.x || 0),
      y: Number(item.y || 0),
      width: Number(item.width || 10),
      height: Number(item.height || 4),
      fontSize: Number(item.fontSize || 16),
      bold: Boolean(item.bold),
      rotation: Number(item.rotation || 0),
      enabled: 1,
      sortNo: index + 1
    }))
  })
}

watch(() => [model.widthMm, model.heightMm], () => nextTick(fitCanvas))
watch(elements, schedulePreview, { deep: true })
onMounted(async () => {
  if (!elements.value.length) elements.value = defaultElements()
  window.addEventListener('pointermove', onPointerMove)
  window.addEventListener('pointerup', onPointerUp)
  window.addEventListener('keydown', onKeydown)
  await checkAgent()
  await nextTick()
  fitCanvas()
  if (agentReady.value) refreshPreview()
})
onUnmounted(() => {
  window.clearTimeout(previewTimer)
  window.removeEventListener('pointermove', onPointerMove)
  window.removeEventListener('pointerup', onPointerUp)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<style scoped>
.label-designer { display: flex; flex-direction: column; height: min(78vh, 850px); min-height: 620px; overflow: hidden; color: #3f3035; background: #f5f0f1; font-family: "Microsoft YaHei", "微软雅黑", sans-serif; }
.designer-bar { flex: 0 0 58px; display: flex; align-items: center; gap: 20px; padding: 0 16px; color: #fff7f8; background: linear-gradient(100deg, #5b3540, #744551); }
.designer-title { display: flex; align-items: baseline; gap: 12px; min-width: 0; }
.designer-title span { flex: none; color: #e9c9d0; font-size: 10px; letter-spacing: 1.5px; }
.designer-title small { color: #d8c3c8; font-size: 11px; }
.designer-actions { display: flex; gap: 8px; margin-left: auto; }
.tool-btn { height: 32px; padding: 0 13px; color: #754752; background: #fffafb; border: 1px solid #ead6da; border-radius: 5px; font-size: 12px; cursor: pointer; }
.tool-btn:hover { border-color: #ca8c9b; }
.tool-btn.primary { color: #fff; background: #b75f72; border-color: #b75f72; font-weight: 600; }
.tool-btn:disabled { cursor: wait; opacity: .55; }
.designer-workspace { flex: 1; min-height: 0; display: grid; grid-template-columns: clamp(188px, 23%, 226px) minmax(0, 1fr) clamp(218px, 27%, 278px); }
.settings-panel, .inspector-panel { min-height: 0; overflow-y: auto; background: #fffcfc; }
.settings-panel { border-right: 1px solid #eadde0; }
.inspector-panel { border-left: 1px solid #eadde0; }
.panel-block { padding: 13px 14px; border-bottom: 1px solid #f0e5e7; }
.block-title { display: flex; justify-content: space-between; gap: 8px; margin-bottom: 10px; color: #574148; font-size: 12px; font-weight: 600; }
.block-title small { color: #a78e95; font-size: 10px; font-weight: 400; }
.ctl { display: block; margin-bottom: 9px; }
.ctl > span { display: block; margin-bottom: 4px; color: #917981; font-size: 11px; }
.ctl input, .ctl select, textarea { width: 100%; height: 30px; padding: 0 8px; color: #45343a; background: #fff; border: 1px solid #e5d3d7; border-radius: 4px; outline: none; font-size: 12px; }
.ctl input:focus, .ctl select:focus, textarea:focus { border-color: #c77d8d; box-shadow: 0 0 0 2px rgba(199,125,141,.12); }
textarea { height: auto; padding: 7px 8px; resize: vertical; }
.ctl-row { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; }
.canvas-panel { min-width: 0; min-height: 0; display: flex; flex-direction: column; background: #e9e2e0; }
.canvas-toolbar { flex: 0 0 43px; display: flex; align-items: center; gap: 12px; padding: 0 13px; color: #756167; background: #faf7f6; border-bottom: 1px solid #dfd3d1; font-size: 11px; }
.canvas-size { color: #5e454c; font-weight: 600; }
.snap { display: flex; align-items: center; gap: 5px; cursor: pointer; }
.toolbar-spacer { flex: 1; }
.canvas-hint { min-width: 0; overflow: hidden; color: #a1878e; text-overflow: ellipsis; white-space: nowrap; }
.zoom-tools { display: flex; align-items: center; gap: 2px; }
.zoom-tools button { width: 25px; height: 24px; padding: 0; color: #705861; background: #fff; border: 1px solid #e1d1d4; border-radius: 3px; cursor: pointer; }
.zoom-tools span { width: 42px; text-align: center; }
.zoom-tools .fit { width: 28px; margin-left: 4px; }
.canvas-viewport { flex: 1; min-height: 250px; overflow: auto; padding: 28px; }
.canvas-wrap { display: grid; place-items: center; min-width: 100%; min-height: 100%; }
.canvas-stage { position: relative; flex: none; overflow: hidden; background: #fff; border: 1px solid #cbb8b5; box-shadow: 0 10px 28px rgba(79,49,56,.16); }
.canvas-grid { position: absolute; inset: 0; pointer-events: none; }
.canvas-grid.minor { background-image: linear-gradient(to right, rgba(200,178,179,.28) 1px, transparent 1px), linear-gradient(to bottom, rgba(200,178,179,.28) 1px, transparent 1px); }
.canvas-grid.major { background-image: linear-gradient(to right, rgba(150,112,119,.3) 1px, transparent 1px), linear-gradient(to bottom, rgba(150,112,119,.3) 1px, transparent 1px); }
.design-element { position: absolute; box-sizing: border-box; display: flex; align-items: center; overflow: hidden; cursor: move; border: 1px dashed transparent; user-select: none; transform-origin: center; }
.design-element:hover { border-color: rgba(185,95,114,.48); }
.design-element.selected { border-color: #b85d70; background: rgba(244,209,217,.12); box-shadow: 0 0 0 1px #b85d70; }
.text-content { width: 100%; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; line-height: 1.15; pointer-events: none; }
.barcode { flex-direction: column; justify-content: center; gap: 2px; }
.barcode-bars { width: 100%; height: 62%; background: repeating-linear-gradient(90deg, #171717 0 1px, transparent 1px 3px, #171717 3px 5px, transparent 5px 6px, #171717 6px 8px, transparent 8px 11px); }
.barcode small { width: 100%; overflow: hidden; color: #333; font-size: .55em; text-align: center; white-space: nowrap; }
.qrcode { flex-direction: column; justify-content: center; gap: 2px; }
.qr-pattern { width: 68%; height: 68%; background: repeating-conic-gradient(#151515 0 25%, #fff 0 50%) 0 0 / 25% 25%; border: 2px solid #151515; }
.qrcode small { color: #333; font-size: .55em; }
.element-tag { position: absolute; top: -17px; left: -1px; padding: 1px 5px; color: #fff; background: #9e5263; border-radius: 2px 2px 0 0; font-size: 9px; white-space: nowrap; }
.resize-handle { position: absolute; right: -4px; bottom: -4px; width: 8px; height: 8px; background: #fff; border: 1px solid #a95468; cursor: nwse-resize; }
.canvas-empty { position: absolute; inset: 0; display: grid; place-items: center; color: #b7a3a6; font-size: 12px; pointer-events: none; }
.field-pool { flex: 0 0 auto; padding: 10px 13px 12px; background: #faf7f6; border-top: 1px solid #dcd0ce; }
.pool-head { display: flex; align-items: baseline; gap: 10px; margin-bottom: 7px; }
.pool-head b { color: #5c454b; font-size: 12px; }
.pool-head span { color: #a38b91; font-size: 10px; }
.pool-chips { display: flex; flex-wrap: wrap; gap: 5px; max-height: 71px; overflow-y: auto; }
.field-chip { height: 25px; padding: 0 9px; color: #6d535b; background: #fff; border: 1px solid #e2d1d5; border-radius: 13px; font-size: 11px; cursor: pointer; }
.field-chip:hover { color: #8d3f53; border-color: #c88292; background: #fff8f9; }
.field-chip.code-chip { color: #76502d; border-color: #dfc9a9; background: #fffaf1; }
.inspector-empty { padding: 16px 4px; color: #ad959b; font-size: 11px; line-height: 1.7; }
.bold-check { display: flex; align-items: center; gap: 6px; margin: 1px 0 9px; color: #796067; font-size: 11px; }
.danger-btn { width: 100%; height: 29px; color: #a34f5c; background: #fff; border: 1px solid #e6c5ca; border-radius: 4px; cursor: pointer; }
.shortcut-note { margin: 8px 0 0; color: #b09a9f; font-size: 10px; line-height: 1.6; }
.preview-frame { display: grid; place-items: center; min-height: 132px; padding: 8px; background: #f1eae8; border: 1px solid #e4d8d6; }
.preview-frame img { max-width: 100%; max-height: 220px; background: #fff; box-shadow: 0 2px 8px rgba(51,35,39,.12); }
.preview-empty { color: #ad989d; font-size: 10px; text-align: center; line-height: 1.7; }
.preview-caption { display: flex; justify-content: space-between; gap: 8px; margin-top: 5px; color: #b09a9f; font-size: 9px; }
.log-box { max-height: 91px; overflow-y: auto; padding: 7px 8px; color: #8b747b; background: #f7f1f1; border: 1px solid #eee2e3; font-size: 10px; line-height: 1.65; }
.log-box p { margin: 0; }
.designer-footer { flex: 0 0 49px; display: flex; align-items: center; gap: 12px; padding: 0 16px; background: #fffcfc; border-top: 1px solid #e7dadc; }
.designer-footer > span { flex: 1; overflow: hidden; color: #a18990; font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }
.designer-footer > div { display: flex; gap: 8px; }
@container (max-width: 820px) {
  .label-designer { min-height: 720px; }
  .designer-workspace { display: flex; flex-direction: column; overflow-y: auto; }
  .settings-panel { flex: none; display: block; overflow: visible; border-right: 0; border-bottom: 1px solid #eadde0; }
  .settings-panel .panel-block { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 10px; align-items: end; }
  .settings-panel .panel-block > .block-title, .settings-panel .panel-block > .ctl:first-of-type, .settings-panel .panel-block > textarea { grid-column: 1 / -1; }
  .canvas-panel { flex: none; height: 560px; }
  .inspector-panel { flex: none; overflow: visible; border-top: 1px solid #eadde0; border-left: 0; }
  .designer-title span { display: none; }
  .designer-title b { min-width: 0; overflow: hidden; font-size: 14px; text-overflow: ellipsis; white-space: nowrap; }
  .designer-title small, .canvas-hint { display: none; }
}
@media (max-width: 820px) {
  .label-designer { min-height: 720px; }
  .designer-workspace { display: flex; flex-direction: column; overflow-y: auto; }
  .settings-panel { flex: none; display: block; overflow: visible; border-right: 0; border-bottom: 1px solid #eadde0; }
  .settings-panel .panel-block { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); column-gap: 10px; align-items: end; }
  .settings-panel .panel-block > .block-title, .settings-panel .panel-block > .ctl:first-of-type, .settings-panel .panel-block > textarea { grid-column: 1 / -1; }
  .canvas-panel { flex: none; height: 560px; }
  .inspector-panel { flex: none; overflow: visible; border-top: 1px solid #eadde0; border-left: 0; }
  .designer-title span { display: none; }
  .designer-title b { min-width: 0; overflow: hidden; font-size: 14px; text-overflow: ellipsis; white-space: nowrap; }
  .designer-title small, .canvas-hint { display: none; }
}
</style>
