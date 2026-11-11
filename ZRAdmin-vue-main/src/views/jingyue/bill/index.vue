<template>
  <div class="jy-bill">
    <header>
      <div>
        <p>静悦基础</p>
        <h2>单据编号</h2>
      </div>
      <el-button v-hasPermi="['jingyue:bill:add']" type="primary" @click="openRule()">新增规则</el-button>
    </header>
    <div class="rule-grid" v-loading="loading">
      <article v-for="rule in rules" :key="rule.id">
        <div class="rule-top">
          <strong>{{ rule.billName }}</strong>
          <el-tag :type="rule.status === 0 ? 'success' : 'info'">{{ rule.status === 0 ? '启用' : '停用' }}</el-tag>
        </div>
        <em>{{ rule.billType }} · {{ rule.prefix }} · {{ patternText(rule.datePattern) }} · {{ cycleText(rule.resetCycle) }}</em>
        <b>{{ rule.nextNo || '已停用' }}</b>
        <small>下一号只是预览，点取号才会占用。已用过的号码会自动跳过。</small>
        <div class="actions">
          <el-button v-hasPermi="['jingyue:bill:edit']" link type="primary" @click="openRule(rule)">调整</el-button>
          <el-button v-if="rule.status === 0" v-hasPermi="['jingyue:bill:issue']" link type="primary" @click="take(rule)">取一个号</el-button>
          <el-button v-if="rule.isPreset !== 1" v-hasPermi="['jingyue:bill:remove']" link type="danger" @click="removeRule(rule)">删除</el-button>
        </div>
      </article>
    </div>
    <section>
      <div class="toolbar">
        <h3>取号记录</h3>
        <el-input v-model="query.billNo" clearable placeholder="单号" @keyup.enter="loadIssues" />
        <el-button @click="loadIssues">查询</el-button>
      </div>
      <el-table :data="issues" v-loading="issueLoading">
        <el-table-column prop="billNo" label="单号" min-width="180" />
        <el-table-column prop="billName" label="单据" width="140" />
        <el-table-column prop="cycleKey" label="周期" width="120" />
        <el-table-column prop="seqNo" label="流水" width="90" />
        <el-table-column prop="createBy" label="取号人" width="120" />
        <el-table-column prop="createTime" label="时间" min-width="170" />
      </el-table>
      <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadIssues" />
    </section>

    <el-dialog v-model="open" :title="form.id ? '调整编号规则' : '新增编号规则'" width="520px" append-to-body>
      <el-form :model="form" label-width="96px">
        <el-form-item label="类型编码"><el-input v-model="form.billType" :disabled="form.isPreset === 1" maxlength="32" /></el-form-item>
        <el-form-item label="名称"><el-input v-model="form.billName" maxlength="50" /></el-form-item>
        <el-form-item label="前缀"><el-input v-model="form.prefix" maxlength="8" /></el-form-item>
        <el-form-item label="日期">
          <el-select v-model="form.datePattern">
            <el-option v-for="item in patterns" :key="item.value" :label="item.label" :value="item.value" />
          </el-select>
        </el-form-item>
        <el-form-item label="流水位数"><el-input-number v-model="form.seqLength" :min="2" :max="8" /></el-form-item>
        <el-form-item label="重置">
          <el-select v-model="form.resetCycle">
            <el-option label="不重置" value="none" />
            <el-option label="按年" value="year" />
            <el-option label="按月" value="month" />
            <el-option label="按日" value="day" />
          </el-select>
        </el-form-item>
        <el-form-item label="状态">
          <el-radio-group v-model="form.status"><el-radio :value="0">启用</el-radio><el-radio :value="1">停用</el-radio></el-radio-group>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" maxlength="500" /></el-form-item>
        <p class="sample">示例 {{ sample }}。重置不能比日期更细，整号不超过 32 位。</p>
      </el-form>
      <template #footer>
        <el-button @click="open = false">取消</el-button>
        <el-button type="primary" @click="submit">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup name="JyBill">
import { listBillRules, saveBillRule, deleteBillRule, issueBillNo, listBillIssues } from '@/api/jingyue/bill'
import { serverNow } from '@/utils/serverTime'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const issueLoading = ref(false)
const open = ref(false)
const rules = ref([])
const issues = ref([])
const total = ref(0)
const query = reactive({ pageNum: 1, pageSize: 10, billNo: '' })
const patterns = [
  { value: '', label: '不带日期' },
  { value: 'yyyy', label: '四位年' },
  { value: 'yyyyMM', label: '年月' },
  { value: 'yyyyMMdd', label: '年月日' },
  { value: 'yyMMdd', label: '两位年加月日' }
]
const form = reactive({ id: 0, billType: '', billName: '', prefix: '', datePattern: 'yyyyMMdd', seqLength: 4, resetCycle: 'day', status: 0, remark: '', isPreset: 0 })

const sample = computed(() => {
  const now = serverNow()
  const pad = (n) => String(n).padStart(2, '0')
  const map = {
    '': '',
    yyyy: String(now.getFullYear()),
    yyyyMM: String(now.getFullYear()) + pad(now.getMonth() + 1),
    yyyyMMdd: String(now.getFullYear()) + pad(now.getMonth() + 1) + pad(now.getDate()),
    yyMMdd: String(now.getFullYear()).slice(2) + pad(now.getMonth() + 1) + pad(now.getDate())
  }
  const date = map[form.datePattern] || ''
  const seq = '1'.padStart(Number(form.seqLength) || 4, '0')
  return (form.prefix || 'XX').toUpperCase() + date + seq
})

function patternText(value) {
  return patterns.find((it) => it.value === value)?.label || '不带日期'
}
function cycleText(value) {
  return { none: '不重置', year: '按年', month: '按月', day: '按日' }[value] || value
}
function loadRules() {
  loading.value = true
  listBillRules().then((res) => { rules.value = res.data || [] }).finally(() => { loading.value = false })
}
function loadIssues() {
  issueLoading.value = true
  listBillIssues(query).then((res) => {
    issues.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { issueLoading.value = false })
}
function openRule(row) {
  Object.assign(form, row || { id: 0, billType: '', billName: '', prefix: '', datePattern: 'yyyyMMdd', seqLength: 4, resetCycle: 'day', status: 0, remark: '', isPreset: 0 })
  open.value = true
}
function submit() {
  saveBillRule(form).then(() => { open.value = false; loadRules() })
}
function removeRule(row) {
  proxy.$modal.confirm('确认删除「' + row.billName + '」？取过号的规则不能删。').then(() => deleteBillRule(row.id)).then(loadRules).catch(() => {})
}
function take(row) {
  proxy.$modal.confirm('确认为「' + row.billName + '」取一个正式号码？号码会记入记录，不能退回。').then(() => issueBillNo({ billType: row.billType })).then((res) => {
    proxy.$modal.msgSuccess('已取号 ' + (res.data?.billNo || ''))
    loadRules()
    loadIssues()
  }).catch(() => {})
}
onMounted(() => { loadRules(); loadIssues() })
</script>

<style scoped lang="scss">
.jy-bill { color: #3d2c32; }
header, .toolbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
header { padding-bottom: 14px; border-bottom: 1px solid #f0e1e5; }
header p { margin: 0 0 4px; color: #c4788a; letter-spacing: 2px; font-size: 12px; }
header h2, .toolbar h3 { margin: 0; }
header h2 { font-size: 24px; font-weight: 650; }
.rule-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin: 16px 0; }
article { display: flex; flex-direction: column; gap: 6px; min-height: 168px; padding: 14px 14px 10px; background: linear-gradient(180deg, #fff 0%, #fff8f8 100%); border: 1px solid #f3e4e8; border-radius: 14px; }
.rule-top { display: flex; align-items: center; justify-content: space-between; }
article em { color: #a07b88; font-style: normal; font-size: 12px; }
article b { font-size: 20px; letter-spacing: 0.5px; }
article small { color: #8d7380; line-height: 1.5; }
.actions { display: flex; gap: 8px; margin-top: auto; }
section { padding: 14px; background: #fff; border: 1px solid #f3e4e8; border-radius: 14px; }
.toolbar { margin-bottom: 12px; }
.toolbar .el-input { width: 220px; margin-left: auto; }
.sample { margin: 0; color: #8d7380; font-size: 12px; line-height: 1.6; }
@media (max-width: 1200px) { .rule-grid { grid-template-columns: 1fr 1fr; } }
@media (max-width: 720px) { .rule-grid { grid-template-columns: 1fr; } }
</style>
