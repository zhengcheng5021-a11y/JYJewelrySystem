<template>
  <div class="material-ledger">
    <header class="page-hero">
      <div><span class="eyebrow">WEIGHT MOVEMENTS</span><h1>物料流水</h1><p>每一笔物料入库、出库都会留下不可篡改的重量与成本轨迹，可按物料连续追溯。</p></div>
      <div class="hero-actions"><el-button plain :loading="loading" @click="loadList"><el-icon><Refresh /></el-icon>刷新</el-button></div>
      <i class="hero-glow"></i>
    </header>
    <section class="list-panel">
      <div class="panel-head"><div><span class="section-kicker">MATERIAL LEDGER</span><h2>重量流水</h2><p>变动重量为正数表示入库，负数表示出库</p></div><div class="filters"><el-input v-model="query.keyword" clearable placeholder="物料编码或单号" :prefix-icon="Search" @keyup.enter="search" @clear="search" /><el-select v-model="query.materialId" clearable filterable placeholder="全部物料" @change="search"><el-option v-for="item in materials" :key="item.id" :label="item.materialCode + ' · ' + item.materialName" :value="item.id" /></el-select><el-select v-model="query.changeType" clearable placeholder="全部方向" @change="search"><el-option label="物料入库" :value="1" /><el-option label="物料出库" :value="2" /></el-select><el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="onDateChange" /></div></div>
      <el-table v-loading="loading" :data="rows" height="100%" row-key="id">
        <el-table-column label="时间 / 单号" min-width="185"><template #default="{ row }"><div class="stack"><b>{{ dateTime(row.bizTime) }}</b><small>{{ row.bizNo }}</small></div></template></el-table-column>
        <el-table-column label="物料" min-width="165"><template #default="{ row }"><div class="stack"><b>{{ row.materialName || row.materialCode }}</b><small>{{ row.materialCode }}</small></div></template></el-table-column>
        <el-table-column label="方向" width="105"><template #default="{ row }"><el-tag :type="row.changeType === 1 ? 'success' : 'warning'" effect="plain">{{ row.changeTypeText }}</el-tag></template></el-table-column>
        <el-table-column label="变动重量" width="135" align="right"><template #default="{ row }"><b :class="row.changeWeight > 0 ? 'plus' : 'minus'">{{ signed(row.changeWeight) }} g</b></template></el-table-column>
        <el-table-column label="变动前" width="115" align="right"><template #default="{ row }">{{ weight(row.beforeWeight) }} g</template></el-table-column>
        <el-table-column label="变动后" width="115" align="right"><template #default="{ row }">{{ weight(row.afterWeight) }} g</template></el-table-column>
        <el-table-column label="成本 / 克" width="125" align="right"><template #default="{ row }">¥{{ money(row.unitCost) }}</template></el-table-column>
        <el-table-column label="金额" width="125" align="right"><template #default="{ row }">¥{{ money(row.amount) }}</template></el-table-column>
        <el-table-column label="业务类型" width="105"><template #default="{ row }">{{ row.bizTypeText }}</template></el-table-column>
        <el-table-column label="操作人" width="100"><template #default="{ row }">{{ row.createBy || '—' }}</template></el-table-column>
        <template #empty><div class="empty-panel"><el-icon><Tickets /></el-icon><h3>还没有物料流水</h3><p>物料出入库确认后，重量变动会在这里留下记录。</p></div></template>
      </el-table>
      <div class="list-foot"><span>共 {{ total }} 条流水</span><pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" /></div>
    </section>
  </div>
</template>

<script setup name="JyMaterialLedger">
import { useRoute } from 'vue-router'
import { Search, Refresh, Tickets } from '@element-plus/icons-vue'
import { listMaterialLedger, getAvailableMaterials } from '@/api/jingyue/material'
const route = useRoute()
const loading = ref(false)
const rows = ref([])
const total = ref(0)
const materials = ref([])
const dateRange = ref(null)
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', materialId: route.query.materialId ? Number(route.query.materialId) : undefined, changeType: undefined, beginTime: undefined, endTime: undefined })
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const weight = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
const signed = (value) => (Number(value) > 0 ? '+' : '') + weight(value)
const dateTime = (value) => value ? String(value).replace('T', ' ').slice(0, 16) : '—'
function loadList() { loading.value = true; listMaterialLedger(query).then((res) => { rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 }).finally(() => { loading.value = false }) }
function search() { query.pageNum = 1; loadList() }
function onDateChange(value) { query.beginTime = value?.[0] || undefined; query.endTime = value?.[1] || undefined; search() }
getAvailableMaterials().then((res) => { materials.value = res.data || [] })
loadList()
</script>

<style scoped>
.material-ledger { display:flex;flex-direction:column;gap:16px;min-height:calc(100vh - 132px);color:#4d373e; }.page-hero { position:relative;display:flex;align-items:flex-end;justify-content:space-between;gap:24px;overflow:hidden;padding:24px 28px;border:1px solid #f1e1e3;border-radius:18px;background:linear-gradient(120deg,#fffaf7 0%,#fdf1ee 48%,#f7e8e7 100%);box-shadow:0 9px 28px rgba(111,74,81,.06); }.page-hero > div:first-child { position:relative;z-index:1; }.eyebrow,.section-kicker { display:block;color:#b06e7a;font-size:11px;font-weight:700;letter-spacing:2.1px; }.page-hero h1 { margin:7px 0 5px;color:#4f363e;font-family:"Songti SC","STSong",Georgia,serif;font-size:31px;font-weight:600; }.page-hero p { max-width:680px;margin:0;color:#9b8188;font-size:13px;line-height:1.65; }.hero-actions { position:relative;z-index:1;display:flex;gap:8px;flex:none; }.hero-glow { position:absolute;right:-50px;bottom:-90px;width:260px;height:260px;border-radius:50%;background:radial-gradient(circle,rgba(231,177,163,.34),transparent 68%);pointer-events:none; }
.list-panel { display:flex;flex:1;min-height:430px;flex-direction:column;padding:17px 19px 11px;border:1px solid #f2e4e5;border-radius:16px;background:#fff;box-shadow:0 7px 22px rgba(106,68,77,.045); }.panel-head { display:flex;align-items:flex-end;justify-content:space-between;gap:18px;padding:0 2px 13px; }.panel-head h2 { margin:3px 0 2px;color:#573e45;font-size:19px;font-weight:600; }.panel-head p { margin:0;color:#b09ba0;font-size:11px; }.filters { display:flex;align-items:center;gap:7px; }.filters .el-input { width:170px; }.filters .el-select { width:130px; }.filters .el-date-editor { width:225px; }.filters .el-button { margin-left:0; }
.stack { line-height:1.4; }.stack b { display:block;color:#62484f;font-size:12.5px;font-weight:500; }.stack small { display:block;margin-top:2px;color:#b09aa0;font-size:10.5px; }.plus { color:#a96b72;font-size:13px;font-weight:600;font-variant-numeric:tabular-nums; }.minus { color:#8f7b66;font-size:13px;font-weight:600;font-variant-numeric:tabular-nums; }.list-foot { display:flex;align-items:center;justify-content:space-between;padding-top:10px;color:#b19ba0;font-size:11px; }.empty-panel { display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:280px;color:#ac959a; }.empty-panel .el-icon { font-size:26px;color:#c79599; }.empty-panel h3 { margin:11px 0 4px;color:#694f56;font-size:16px;font-weight:500; }.empty-panel p { margin:0;font-size:12px; }
@media (max-width:1100px) { .panel-head { align-items:flex-start;flex-direction:column; }.filters { flex-wrap:wrap; } }
@media (max-width:720px) { .page-hero { align-items:flex-start;flex-direction:column; }.hero-actions { align-self:flex-start; }.filters .el-input,.filters .el-select,.filters .el-date-editor { width:100%; } }
</style>
