<template>
  <div class="material-stock">
    <header class="page-hero">
      <div>
        <span class="eyebrow">WEIGHT LEDGER</span>
        <h1>物料库存</h1>
        <p>物料库存统一按克结存，与按件管理的正品库存分开核算。成本价为当前移动平均成本。</p>
      </div>
      <div class="hero-actions">
        <el-button plain :loading="loading" @click="refreshAll"><el-icon><Refresh /></el-icon>刷新</el-button>
        <el-button v-hasPermi="['jingyue:materialdoc:add']" type="primary" @click="$router.push('/material-center/material-inbound')"><el-icon><Bottom /></el-icon>物料入库</el-button>
      </div>
      <i class="hero-glow"></i>
    </header>
    <section class="metric-board" v-loading="summaryLoading">
      <article class="metric-feature"><span>物料种类</span><strong>{{ summary.materialCount || 0 }}<em> 种</em></strong><p>启用中的物料档案</p><div class="metric-foot"><span>零库存 {{ summary.lowStockCount || 0 }} 种</span><b>按克结存</b></div></article>
      <article><span class="metric-label">库存总重</span><strong>{{ weight(summary.totalStockWeight) }}<em> g</em></strong><small>所有启用物料合计</small><i class="metric-icon"><el-icon><Coin /></el-icon></i></article>
      <article><span class="metric-label">库存平均成本金额</span><strong class="money">¥{{ money(summary.totalStockCost) }}</strong><small>按移动平均成本估算</small><i class="metric-icon gold"><el-icon><Money /></el-icon></i></article>
      <article><span class="metric-label">库存告警</span><strong>{{ summary.lowStockCount || 0 }}<em> 种</em></strong><small>库存为 0 的物料数</small><i class="metric-icon"><el-icon><Warning /></el-icon></i></article>
    </section>
    <section class="list-panel">
      <div class="panel-head"><div><span class="section-kicker">MATERIAL BALANCE</span><h2>库存余额</h2><p>按物料查看当前可用重量和平均成本</p></div><div class="filters"><el-input v-model="query.keyword" clearable placeholder="编码或名称" :prefix-icon="Search" @keyup.enter="search" @clear="search" /><el-select v-model="query.categoryItemId" clearable filterable placeholder="全部分类" @change="search"><el-option v-for="item in categories" :key="item.id" :label="item.parentId ? '　└ ' + item.itemName : item.itemName" :value="item.id" /></el-select><el-button @click="search">查询</el-button></div></div>
      <el-table v-loading="loading" :data="rows" height="100%" row-key="id">
        <el-table-column label="物料" min-width="200"><template #default="{ row }"><div class="material-cell"><span class="material-mark">{{ row.materialName.slice(0,1) }}</span><div><b>{{ row.materialName }}</b><small>{{ row.materialCode }} · {{ row.categoryName || '未分类' }}</small></div></div></template></el-table-column>
        <el-table-column label="成色" width="105"><template #default="{ row }">{{ row.metalName || '—' }}</template></el-table-column>
        <el-table-column label="库存重量" width="155" align="right"><template #default="{ row }"><b class="weight-main">{{ weight(row.stockWeight) }}</b><small class="unit-tail"> g</small></template></el-table-column>
        <el-table-column label="锁定重量" width="125" align="right"><template #default="{ row }">{{ weight(row.lockedWeight) }} g</template></el-table-column>
        <el-table-column label="可用重量" width="135" align="right"><template #default="{ row }"><span :class="{ 'zero-stock': Number(row.availableWeight) <= 0 }">{{ weight(row.availableWeight) }} g</span></template></el-table-column>
        <el-table-column label="平均成本 / 克" width="145" align="right"><template #default="{ row }">¥{{ money(row.avgCostPerGram) }}</template></el-table-column>
        <el-table-column label="库存成本" width="145" align="right"><template #default="{ row }">¥{{ money(Number(row.stockWeight || 0) * Number(row.avgCostPerGram || 0)) }}</template></el-table-column>
        <el-table-column label="状态" width="86"><template #default="{ row }"><el-tag :type="row.status === 0 ? 'success' : 'info'" effect="plain">{{ row.status === 0 ? '启用' : '停用' }}</el-tag></template></el-table-column>
      <el-table-column label="操作" width="115" fixed="right"><template #default="{ row }"><el-button link type="primary" @click="$router.push({ path: '/material-center/material-ledger', query: { materialId: row.id } })">查看流水</el-button></template></el-table-column>
        <template #empty><div class="empty-panel"><el-icon><Files /></el-icon><h3>还没有物料库存</h3><p>物料入库单确认后，重量会自动进入这里。</p></div></template>
      </el-table>
      <div class="list-foot"><span>共 {{ total }} 项库存记录</span><pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" /></div>
    </section>
  </div>
</template>

<script setup name="JyMaterialStock">
import { Search, Refresh, Bottom, Coin, Money, Warning, Files } from '@element-plus/icons-vue'
import { listMaterial, getMaterialSummary, getMaterialOptions } from '@/api/jingyue/material'
const loading = ref(false)
const summaryLoading = ref(false)
const rows = ref([])
const total = ref(0)
const categories = ref([])
const summary = reactive({ materialCount: 0, totalStockWeight: 0, totalStockCost: 0, lowStockCount: 0 })
const query = reactive({ pageNum: 1, pageSize: 20, keyword: '', categoryItemId: undefined, status: 0 })
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const weight = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
function loadList() { loading.value = true; listMaterial(query).then((res) => { rows.value = res.data?.result || []; total.value = res.data?.totalNum || 0 }).finally(() => { loading.value = false }) }
function loadSummary() { summaryLoading.value = true; getMaterialSummary().then((res) => Object.assign(summary, res.data || {})).finally(() => { summaryLoading.value = false }) }
function loadOptions() { getMaterialOptions().then((res) => { categories.value = res.data?.categories || [] }) }
function search() { query.pageNum = 1; loadList() }
function refreshAll() { loadSummary(); loadList() }
loadOptions(); refreshAll()
</script>

<style scoped>
.material-stock { display: flex; flex-direction: column; gap: 16px; min-height: calc(100vh - 132px); color: #4d373e; }
.page-hero { position: relative; display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; overflow: hidden; padding: 24px 28px; border: 1px solid #f1e1e3; border-radius: 18px; background: linear-gradient(120deg,#fffaf7 0%,#fdf1ee 48%,#f7e8e7 100%); box-shadow: 0 9px 28px rgba(111,74,81,.06); }
.page-hero > div:first-child { position: relative; z-index: 1; }.eyebrow,.section-kicker { display:block;color:#b06e7a;font-size:11px;font-weight:700;letter-spacing:2.1px; }.page-hero h1 { margin:7px 0 5px;color:#4f363e;font-family:"Songti SC","STSong",Georgia,serif;font-size:31px;font-weight:600; }.page-hero p { max-width:680px;margin:0;color:#9b8188;font-size:13px;line-height:1.65; }.hero-actions { position:relative;z-index:1;display:flex;gap:8px;flex:none; }.hero-glow { position:absolute;right:-50px;bottom:-90px;width:260px;height:260px;border-radius:50%;background:radial-gradient(circle,rgba(231,177,163,.34),transparent 68%);pointer-events:none; }
.metric-board { display:grid;grid-template-columns:1.15fr 1fr 1fr 1fr;gap:12px; }.metric-board article { position:relative;min-height:126px;padding:18px 19px;overflow:hidden;border:1px solid #f2e4e5;border-radius:14px;background:#fff;box-shadow:0 6px 20px rgba(110,74,80,.045); }.metric-feature { background:linear-gradient(140deg,#fdf7f4,#f9ece9); }.metric-feature > span,.metric-label { display:block;color:#a78b91;font-size:12px; }.metric-feature strong,.metric-board article > strong { display:block;margin-top:13px;color:#553b43;font-family:"Songti SC","STSong",Georgia,serif;font-size:28px;font-weight:600; }.metric-feature strong em,.metric-board article > strong em { color:#ac8a8f;font-family:inherit;font-size:12px;font-style:normal;font-weight:400; }.metric-feature p,.metric-board article > small { display:block;margin:4px 0 0;color:#b09ba0;font-size:11px; }.metric-foot { display:flex;justify-content:space-between;margin-top:13px;padding-top:10px;border-top:1px dashed #eed8d8;color:#a4878d;font-size:11px; }.metric-foot b { color:#a86672;font-weight:600; }.metric-icon { position:absolute;right:17px;top:17px;display:grid;place-items:center;width:30px;height:30px;border-radius:50%;background:#fdf2f1;color:#bc7e84;font-style:normal; }.metric-icon.gold { color:#b29360;background:#fcf7ed; }
.list-panel { display:flex;flex:1;min-height:400px;flex-direction:column;padding:17px 19px 11px;border:1px solid #f2e4e5;border-radius:16px;background:#fff;box-shadow:0 7px 22px rgba(106,68,77,.045); }.panel-head { display:flex;align-items:flex-end;justify-content:space-between;gap:18px;padding:0 2px 13px; }.panel-head h2 { margin:3px 0 2px;color:#573e45;font-size:19px;font-weight:600; }.panel-head p { margin:0;color:#b09ba0;font-size:11px; }.filters { display:flex;align-items:center;gap:7px; }.filters .el-input { width:190px; }.filters .el-select { width:132px; }.filters .el-button { margin-left:0; }
.material-cell { display:flex;align-items:center;gap:10px; }.material-mark { display:grid;flex:none;place-items:center;width:31px;height:31px;border-radius:9px;background:#fdf0ee;color:#b5707b;font-family:"Songti SC","STSong",serif;font-size:16px; }.material-cell b { display:block;color:#62484f;font-size:12.5px;font-weight:500; }.material-cell small { display:block;margin-top:2px;color:#b09aa0;font-size:10.5px; }.weight-main { color:#5b4148;font-size:15px;font-weight:600;font-variant-numeric:tabular-nums; }.unit-tail { color:#b0989d;font-size:10px; }.zero-stock { color:#bd7d84; }.list-foot { display:flex;align-items:center;justify-content:space-between;padding-top:10px;color:#b19ba0;font-size:11px; }.empty-panel { display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:280px;color:#ac959a; }.empty-panel .el-icon { font-size:26px;color:#c79599; }.empty-panel h3 { margin:11px 0 4px;color:#694f56;font-size:16px;font-weight:500; }.empty-panel p { margin:0;font-size:12px; }
@media (max-width:1100px) { .metric-board { grid-template-columns:1fr 1fr; }.panel-head { align-items:flex-start;flex-direction:column; } }
@media (max-width:720px) { .page-hero { align-items:flex-start;flex-direction:column; }.hero-actions { align-self:flex-start; }.metric-board { grid-template-columns:1fr; }.filters { flex-wrap:wrap; }.filters .el-input,.filters .el-select { width:100%; } }
</style>
