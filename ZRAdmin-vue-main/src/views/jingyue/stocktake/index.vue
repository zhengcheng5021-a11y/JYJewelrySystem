<template>
  <div class="stocktake-page">
    <header class="page-hero">
      <div class="hero-copy">
        <span class="eyebrow">STOCKTAKE CONTROL</span>
        <h1>盘点管理</h1>
        <p>正品按件、物料按克核对账实。确认前可反复修改，确认后自动生成库存流水，库存变化会阻止过期盘点覆盖账面。</p>
      </div>
      <div class="hero-actions">
        <el-button plain :loading="loading" @click="refreshAll"><el-icon><Refresh /></el-icon>刷新</el-button>
<el-button v-hasPermi="['jingyue:stocktake:add']" type="primary" :loading="initializing" @click="openCreate(takeType)"><el-icon><Plus /></el-icon>新建{{ takeTypeText(takeType) }}</el-button>
      </div>
      <i class="hero-glow"></i>
    </header>

    <section class="metric-board" v-loading="dashboardLoading">
      <article class="metric-feature">
        <span>待确认盘点</span>
        <strong>{{ dashboard.draftCount || 0 }}<em> 单</em></strong>
        <p>确认后才会改动库存，草稿不会影响账面</p>
        <div class="metric-foot"><span>今日已确认 {{ dashboard.confirmedToday || 0 }} 单</span><b>事务确认</b></div>
      </article>
      <article>
        <span class="metric-label">今日差异明细</span>
        <strong>{{ dashboard.diffLineCount || 0 }}<em> 条</em></strong>
        <small>已确认盘点中的盘盈盘亏行</small>
        <i class="metric-icon"><el-icon><DocumentChecked /></el-icon></i>
      </article>
      <article>
        <span class="metric-label">今日盘点件数</span>
        <strong>{{ dashboard.goodsCount || 0 }}<em> 件</em></strong>
        <small>正品盘点明细合计</small>
        <i class="metric-icon gold"><el-icon><Goods /></el-icon></i>
      </article>
      <article>
        <span class="metric-label">今日盘点物料</span>
        <strong>{{ dashboard.materialCount || 0 }}<em> 项</em></strong>
        <small>物料盘点明细合计</small>
        <i class="metric-icon"><el-icon><Coin /></el-icon></i>
      </article>
    </section>

    <el-tabs v-model="activeTab" class="main-tabs" @tab-change="onTabChange">
      <el-tab-pane label="正品盘点" name="goods">
        <div class="tab-body">
          <section class="list-panel">
            <div class="panel-head">
              <div>
                <span class="section-kicker">GOODS COUNT</span>
                <h2>正品盘点单</h2>
                <p>单件货品实盘只能为 0 或 1；锁定库存不能被盘点静默释放</p>
              </div>
              <div class="filters">
                <el-input v-model="query.keyword" clearable placeholder="单号或备注" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
                <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="onDateChange" />
                <el-select v-model="query.status" clearable placeholder="全部状态" @change="search"><el-option label="待确认" :value="0" /><el-option label="已确认" :value="1" /></el-select>
                <el-button @click="search">查询</el-button>
              </div>
            </div>
            <div v-loading="loading" class="doc-list">
              <article v-for="row in rows" :key="row.id" class="doc-row" :class="{ drafting: row.status === 0 }">
                <button type="button" class="doc-main" @click="openEdit(row)">
                  <span class="doc-sign"><el-icon><Box /></el-icon></span>
                  <span class="doc-copy"><b>{{ row.stocktakeNo }}</b><small>{{ row.takeTypeText }} · {{ row.remark || '未填写备注' }}</small></span>
                  <span class="doc-stat"><b>{{ number(row.lineCount) }}<em> 条</em></b><small>盘点明细</small></span>
                  <span class="doc-stat"><b :class="amountTone(row.diffAmount)">{{ signedMoney(row.diffAmount) }}</b><small>净差异金额</small></span>
                  <span class="doc-date"><b>{{ dateText(row.takeTime) }}</b><small>{{ row.createBy || '操作人' }}</small></span>
                  <el-tag :type="row.status === 0 ? 'warning' : 'success'" effect="plain" round>{{ row.statusText }}</el-tag>
                </button>
                <div class="doc-actions">
                  <el-button link type="primary" @click="openEdit(row)">{{ row.status === 0 ? '编辑' : '查看' }}</el-button>
                  <el-button v-if="row.status === 0" v-hasPermi="['jingyue:stocktake:confirm']" link type="success" @click="confirmRow(row)">确认</el-button>
                  <el-button v-if="row.status === 0 || row.status === 1" v-hasPermi="['jingyue:stocktake:remove']" link type="danger" @click="removeRow(row)">删除</el-button>
                </div>
              </article>
              <div v-if="!loading && !rows.length" class="empty-panel">
                <el-icon><Document /></el-icon>
                <h3>还没有正品盘点单</h3>
                <p>先建草稿，逐件核对后再确认；确认时会再次校验库存。</p>
                <el-button v-hasPermi="['jingyue:stocktake:add']" type="primary" :loading="initializing" @click="openCreate(1)">新建正品盘点</el-button>
              </div>
            </div>
            <div class="list-foot"><span>共 {{ total }} 张盘点单</span><pagination v-show="total > 0" v-model:page="query.pageNum" v-model:limit="query.pageSize" :total="total" @pagination="loadList" /></div>
          </section>
        </div>
      </el-tab-pane>

      <el-tab-pane label="物料盘点" name="material">
        <div class="tab-body">
          <section class="list-panel">
            <div class="panel-head">
              <div><span class="section-kicker">MATERIAL COUNT</span><h2>物料盘点单</h2><p>物料按克盘点，实盘重量不能低于锁定重量</p></div>
              <div class="filters">
                <el-input v-model="query.keyword" clearable placeholder="单号或备注" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
                <el-date-picker v-model="dateRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="onDateChange" />
                <el-select v-model="query.status" clearable placeholder="全部状态" @change="search"><el-option label="待确认" :value="0" /><el-option label="已确认" :value="1" /></el-select>
                <el-button @click="search">查询</el-button>
              </div>
            </div>
            <div v-loading="loading" class="doc-list">
              <article v-for="row in rows" :key="row.id" class="doc-row" :class="{ drafting: row.status === 0 }">
                <button type="button" class="doc-main" @click="openEdit(row)">
                  <span class="doc-sign material"><el-icon><Coin /></el-icon></span>
                  <span class="doc-copy"><b>{{ row.stocktakeNo }}</b><small>{{ row.takeTypeText }} · {{ row.remark || '未填写备注' }}</small></span>
                  <span class="doc-stat"><b>{{ number(row.lineCount) }}<em> 条</em></b><small>盘点明细</small></span>
                  <span class="doc-stat"><b :class="amountTone(row.diffAmount)">{{ signedMoney(row.diffAmount) }}</b><small>净差异金额</small></span>
                  <span class="doc-date"><b>{{ dateText(row.takeTime) }}</b><small>{{ row.createBy || '操作人' }}</small></span>
                  <el-tag :type="row.status === 0 ? 'warning' : 'success'" effect="plain" round>{{ row.statusText }}</el-tag>
                </button>
                <div class="doc-actions">
                  <el-button link type="primary" @click="openEdit(row)">{{ row.status === 0 ? '编辑' : '查看' }}</el-button>
                  <el-button v-if="row.status === 0" v-hasPermi="['jingyue:stocktake:confirm']" link type="success" @click="confirmRow(row)">确认</el-button>
                  <el-button v-if="row.status === 0 || row.status === 1" v-hasPermi="['jingyue:stocktake:remove']" link type="danger" @click="removeRow(row)">删除</el-button>
                </div>
              </article>
              <div v-if="!loading && !rows.length" class="empty-panel">
                <el-icon><Document /></el-icon>
                <h3>还没有物料盘点单</h3>
                <p>物料按克盘点，确认后写重量流水并保留平均成本。</p>
                <el-button v-hasPermi="['jingyue:stocktake:add']" type="primary" :loading="initializing" @click="openCreate(2)">新建物料盘点</el-button>
              </div>
            </div>
            <div class="list-foot"><span>共 {{ total }} 张盘点单</span><pagination v-show="total > 0" v-model:page="query.pageNum" v-model:limit="query.pageSize" :total="total" @pagination="loadList" /></div>
          </section>
        </div>
      </el-tab-pane>

      <el-tab-pane label="综合盘点报表" name="report">
        <div class="tab-body">
          <section class="report-panel">
            <div class="panel-head">
              <div><span class="section-kicker">COUNT REPORT</span><h2>综合盘点报表</h2><p>只汇总已确认盘点单，正品数量与物料重量分开统计</p></div>
              <div class="filters">
                <el-date-picker v-model="reportRange" type="daterange" value-format="YYYY-MM-DD" range-separator="至" start-placeholder="开始日期" end-placeholder="结束日期" unlink-panels clearable @change="loadReport" />
                <el-select v-model="reportQuery.takeType" clearable placeholder="全部类型" @change="loadReport"><el-option label="正品盘点" :value="1" /><el-option label="物料盘点" :value="2" /></el-select>
                <el-input v-model="reportQuery.keyword" clearable placeholder="单号或备注" :prefix-icon="Search" @keyup.enter="loadReport" @clear="loadReport" />
                <el-button :loading="reportLoading" @click="loadReport">查询</el-button>
              </div>
            </div>
            <div class="report-metrics" v-loading="reportLoading">
              <div class="report-primary"><span>净差异金额</span><b :class="amountTone(report.netDiffAmount)">{{ signedMoney(report.netDiffAmount) }}</b><small>盘盈金额减盘亏金额</small></div>
              <div><span>盘盈金额</span><b class="gain">{{ money(report.gainAmount) }}</b></div>
              <div><span>盘亏金额</span><b class="loss">{{ money(report.lossAmount) }}</b></div>
              <div><span>正品盘盈 / 盘亏</span><b>{{ number(report.goodsGainQty) }} / {{ number(report.goodsLossQty) }} <em>件</em></b></div>
              <div><span>物料盘盈 / 盘亏</span><b>{{ weight(report.materialGainWeight) }} / {{ weight(report.materialLossWeight) }} <em>g</em></b></div>
              <div><span>盘点单 / 明细</span><b>{{ report.documentCount || 0 }} / {{ report.lineCount || 0 }} <em>条</em></b></div>
            </div>
            <div class="report-list-block report-category-block">
              <div class="sub-head"><h3>按大类汇总</h3><span>已确认盘点 · 应盘与实盘</span></div>
              <el-table :data="report.categories" size="small" class="report-table" empty-text="暂无已确认分类汇总" max-height="360">
                <el-table-column label="类型" width="82"><template #default="{ row }">{{ row.takeType === 1 ? NORMAL_LABEL : MATERIAL_LABEL }}</template></el-table-column>
                <el-table-column label="大类" min-width="180" prop="parentCategoryName" />
                <el-table-column label="应盘" width="118" align="right"><template #default="{ row }">{{ number(row.bookQty) }} <em>{{ row.takeType === 1 ? PIECE_LABEL : GRAM_LABEL }}</em></template></el-table-column>
                <el-table-column label="实盘" width="118" align="right"><template #default="{ row }"><b class="tone-actual">{{ number(row.actualQty) }}</b> <em>{{ row.takeType === 1 ? PIECE_LABEL : GRAM_LABEL }}</em></template></el-table-column>
                <el-table-column label="差异" width="106" align="right"><template #default="{ row }"><b :class="amountTone(row.diffQty)">{{ signed(row.diffQty) }}</b></template></el-table-column>
                <el-table-column label="差异金额" width="122" align="right"><template #default="{ row }"><b :class="amountTone(row.diffAmount)">{{ signedMoney(row.diffAmount) }}</b></template></el-table-column>
                <el-table-column label="明细" width="82" align="right"><template #default="{ row }">{{ row.itemCount }} <em>条</em></template></el-table-column>
              </el-table>
            </div>
            <div class="report-lists">
              <div class="report-list-block">
                <div class="sub-head"><h3>差异明细</h3><span>{{ report.diffLineCount || 0 }} 条差异</span></div>
                <el-table :data="report.differences" size="small" class="report-table" empty-text="暂无差异明细" max-height="360">
                  <el-table-column label="盘点单" min-width="130"><template #default="{ row }">{{ documentNo(row.stocktakeId) }}</template></el-table-column>
                  <el-table-column label="类型" width="82"><template #default="{ row }">{{ row.itemType === 1 ? NORMAL_LABEL : MATERIAL_LABEL }}</template></el-table-column>
                  <el-table-column label="货品 / 物料" min-width="180"><template #default="{ row }"><b>{{ row.itemName || row.itemNo }}</b><small class="cell-sub">{{ row.itemNo }}</small></template></el-table-column>
                  <el-table-column label="账面" width="108" align="right"><template #default="{ row }">{{ number(row.bookQty) }} <em>{{ row.itemType === 1 ? (row.unit || PIECE_LABEL) : GRAM_LABEL }}</em></template></el-table-column>
                  <el-table-column label="实盘" width="108" align="right"><template #default="{ row }">{{ number(row.actualQty) }} <em>{{ row.itemType === 1 ? (row.unit || PIECE_LABEL) : GRAM_LABEL }}</em></template></el-table-column>
                  <el-table-column label="差异" width="122" align="right"><template #default="{ row }"><b :class="amountTone(row.diffQty)">{{ signed(row.diffQty) }}</b></template></el-table-column>
                  <el-table-column label="差异金额" width="126" align="right"><template #default="{ row }"><b :class="amountTone(row.diffAmount)">{{ signedMoney(row.diffAmount) }}</b></template></el-table-column>
                </el-table>
              </div>
              <div class="report-list-block">
                <div class="sub-head"><h3>已确认盘点单</h3><span>{{ report.documentCount || 0 }} 张</span></div>
                <el-table :data="report.documents" size="small" class="report-table" empty-text="暂无已确认盘点单" max-height="360">
                  <el-table-column label="单号" min-width="150"><template #default="{ row }"><button type="button" class="link-button" @click="openEdit(row)">{{ row.stocktakeNo }}</button></template></el-table-column>
                  <el-table-column label="类型" width="100" prop="takeTypeText" />
                  <el-table-column label="盘点时间" width="114"><template #default="{ row }">{{ dateText(row.takeTime) }}</template></el-table-column>
                  <el-table-column label="差异" width="122" align="right"><template #default="{ row }"><b :class="amountTone(row.diffAmount)">{{ signedMoney(row.diffAmount) }}</b></template></el-table-column>
                </el-table>
              </div>
            </div>
          </section>
        </div>
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="editorOpen" fullscreen :show-close="false" :close-on-click-modal="false" :close-on-press-escape="false" :destroy-on-close="false" class="stocktake-editor-dialog" @closed="onEditorClosed">
      <div class="editor-shell">
        <div class="drawer-heading">
          <span>{{ readOnly ? 'STOCKTAKE RECORD' : 'STOCKTAKE DRAFT' }}</span>
          <b>{{ form.id ? form.stocktakeNo : '新建' + takeTypeText(editorTakeType) }}</b>
          <small v-if="draftDirty" class="draft-state">本机暂存待保存</small>
          <div class="drawer-heading-actions">
            <el-button v-if="form.id" v-hasPermi="['jingyue:stocktake:remove']" size="small" type="danger" plain :loading="deleting" @click="removeEditor">删除盘点单</el-button>
            <button type="button" aria-label="关闭" @click="requestCloseEditor"><el-icon><Close /></el-icon></button>
          </div>
        </div>
        <div v-if="editorLoading" class="editor-loading"><el-icon class="is-loading"><Loading /></el-icon>正在读取盘点单</div>
        <div v-else class="editor-scroll">
          <div class="editor-body">
            <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="doc-form" :disabled="readOnly">
            <div class="form-grid">
              <el-form-item label="盘点时间" prop="takeTime"><el-date-picker v-model="form.takeTime" type="datetime" value-format="YYYY-MM-DDTHH:mm:ss" style="width:100%" /></el-form-item>
              <el-form-item label="备注"><el-input v-model="form.remark" maxlength="500" show-word-limit placeholder="选填，例如：月末抽盘、柜台交接" /></el-form-item>
            </div>
            <div class="form-section">
              <div class="form-caption"><span>SNAPSHOT</span><b>{{ editorTakeType === 1 ? '按款式大类快速盘点 · 明细在弹窗核对' : '全部启用物料 · 按分类核对克重' }}</b></div>
              <div class="snapshot-note">一级分类默认折叠；可直接修改一级实盘总数，系统按未锁定商品自动分配，再进入明细弹窗逐件修正。实盘与应盘不一致时必须填写异常备注。</div>
              <div class="count-board">
                <div><span>应盘</span><b>{{ number(totalBookQty) }}<em>{{ editorTakeType === 1 ? ' 件' : ' g' }}</em></b></div>
                <div><span>未盘</span><b class="tone-warn">{{ number(pendingBookQty) }}<em>{{ editorTakeType === 1 ? ' 件' : ' g' }}</em></b></div>
                <div><span>实盘</span><b class="tone-actual">{{ number(totalActualQty) }}<em>{{ editorTakeType === 1 ? ' 件' : ' g' }}</em></b></div>
                <div><span>差异</span><b :class="amountTone(totalDiffQty)">{{ signed(totalDiffQty) }}<em>{{ editorTakeType === 1 ? ' 件' : ' g' }}</em></b></div>
                <div class="count-progress"><span>分类完成 {{ completedCategoryCount }} / {{ form.categories.length }} · {{ categoryProgress }}%</span><el-progress :percentage="categoryProgress" :stroke-width="7" :show-text="false" :color="categoryProgress === 100 ? '#7d9c82' : '#b06e7a'" /></div>
              </div>
              <div class="material-scroll">
                <div v-if="!form.lines.length" class="state-box">当前没有可盘点的明细。</div>
                <section v-for="material in materialGroups" :key="material.key" class="material-block" :class="{ done: material.completedCount === material.categories.length }">
                <header class="material-head" @click="toggleMaterial(material.key)">
                  <span class="material-chevron"><el-icon><ArrowRight v-if="!isMaterialOpen(material.key)" /><ArrowDown v-else /></el-icon></span>
                  <div><b>{{ material.name }}</b><small>{{ material.parentCount }} 个款式大类 · {{ material.categories.length }} 个分类 · {{ material.itemCount }} 条明细</small></div>
                  <span class="material-stat"><em>应盘</em><b>{{ number(material.bookQty) }}</b></span>
                  <span class="material-stat"><em>实盘</em><b>{{ number(material.actualQty) }}</b></span>
                  <span class="material-stat"><em>差异</em><b :class="amountTone(material.diffQty)">{{ signed(material.diffQty) }}</b></span>
                  <el-tag :type="material.completedCount === material.categories.length ? 'success' : 'warning'" effect="plain" round>{{ material.completedCount === material.categories.length ? '全部完成' : '进行中' }}</el-tag>
                </header>
                <div v-show="isMaterialOpen(material.key)" class="material-body">
                  <div v-if="!material.parents.length" class="state-box">该材质下没有可盘点分类。</div>
                  <section v-for="parent in material.parents" :key="parent.key" class="parent-block" :class="{ done: parent.completedCount === parent.categories.length }">
                    <header class="parent-head" @click="toggleParent(parent.key)">
                      <span class="parent-chevron"><el-icon><ArrowRight v-if="!isParentOpen(parent.key)" /><ArrowDown v-else /></el-icon></span>
                      <div class="parent-title"><b>{{ parent.name }}</b><small>{{ parent.categories.length }} 个分类 · {{ parent.itemCount }} 条明细 · {{ parent.completedCount }} 已完成</small></div>
                      <div class="parent-numbers">
                        <span><em>应盘</em><b>{{ number(parent.bookQty) }}</b></span>
                        <span class="parent-actual" @click.stop>
                          <em>实盘</em>
                          <el-input-number v-if="!readOnly && editorTakeType === 1" :model-value="parent.actualQty" :min="0" :max="parent.capacity" :precision="0" :step="1" controls-position="right" size="small" @change="(value) => applyParentActual(parent, value)" />
                          <b v-else>{{ number(parent.actualQty) }}</b>
                        </span>
                        <span><em>差异</em><b :class="amountTone(parent.diffQty)">{{ signed(parent.diffQty) }}</b></span>
                      </div>
                      <el-tag :type="parent.completedCount === parent.categories.length ? 'success' : 'warning'" effect="plain" round>{{ parent.completedCount === parent.categories.length ? '已完成' : '待完成' }}</el-tag>
                    </header>
                    <div v-if="parent.diffQty !== 0" class="anomaly-row">
                      <span>异常备注 <i v-if="parent.completedCount > 0">*</i></span>
                      <el-input v-if="!readOnly" v-model="parentRemarks[parent.key]" maxlength="200" placeholder="账实不一致，请说明原因（确认时必填）" @click.stop @blur="syncParentRemark(parent)" />
                      <p v-else>{{ parent.anomalyRemark || '未填写异常备注' }}</p>
                    </div>
                    <div v-if="isParentOpen(parent.key)" class="subcategory-list">
                      <div v-for="category in parent.categories" :key="category.categoryKey" class="subcategory-row">
                        <div class="subcategory-title"><b>{{ category.categoryName }}</b><small>{{ category.itemCount }} 条 · 应盘 {{ number(category.bookQty) }}</small></div>
                        <div class="subcategory-stat"><span>实盘</span><b>{{ number(category.actualQty) }}</b></div>
                        <div class="subcategory-stat"><span>差异</span><b :class="amountTone(category.diffQty)">{{ signed(category.diffQty) }}</b></div>
                        <div class="subcategory-actions">
                          <el-button size="small" plain @click.stop="openLineDialog(parent, category)">查看明细</el-button>
                          <el-button v-if="!readOnly" size="small" :type="category.isCompleted ? 'info' : 'success'" plain :loading="categorySaving === category.categoryKey" @click.stop="toggleCategory(category)">{{ category.isCompleted ? '取消完成' : '标记完成' }}</el-button>
                        </div>
                        <div v-if="!category.isCompleted" class="category-reason" @click.stop>
                          <el-input v-model="category.incompleteReason" maxlength="200" placeholder="本分类未完成原因（确认时必填）" :disabled="readOnly" @blur="saveCategoryReason(category)" />
                        </div>
                      </div>
                    </div>
                  </section>
                </div>
                </section>
              </div>
            </div>
            </el-form>
          </div>
        </div>
        <div class="editor-actions">
          <span v-if="draftDirty" class="draft-hint"><el-icon><Document /></el-icon>修改会在关闭或离开页面时自动暂存到本机</span>
          <el-button @click="requestCloseEditor">{{ readOnly ? '关闭' : '取消' }}</el-button>
          <template v-if="!readOnly">
            <el-button :loading="saving" @click="save(false)">保存草稿</el-button>
            <el-button type="primary" :loading="saving" @click="save(true)">保存并确认</el-button>
          </template>
        </div>
      </div>
    </el-dialog>

    <el-dialog v-model="lineDialogOpen" :title="lineDialogTitle" width="min(880px, 92vw)" destroy-on-close class="stocktake-line-dialog">
      <div v-if="lineDialogParent" class="line-dialog-anomaly">
        <span>大类异常备注 <i v-if="lineDialogParent.diffQty !== 0">*</i></span>
        <el-input v-if="!readOnly" v-model="parentRemarks[lineDialogParent.key]" maxlength="200" placeholder="大类账实不一致时必填" @change="syncParentRemark(lineDialogParent)" />
        <p v-else>{{ lineDialogParent.anomalyRemark || '未填写异常备注' }}</p>
      </div>
      <div class="line-dialog-summary">
        <div><span>应盘</span><b>{{ number(lineDialogCategory?.bookQty || 0) }}</b></div>
        <div><span>实盘</span><b>{{ number(lineDialogCategory?.actualQty || 0) }}</b></div>
        <div><span>差异</span><b :class="amountTone(lineDialogCategory?.diffQty)">{{ signed(lineDialogCategory?.diffQty || 0) }}</b></div>
      </div>
      <div class="line-dialog-head"><span>商品 / 物料</span><span>{{ editorTakeType === 1 ? '应盘' : '应盘克重' }}</span><span>{{ editorTakeType === 1 ? '实盘' : '实盘克重' }}</span><span>差异</span><span>备注</span></div>
      <div v-if="lineDialogLines.length" class="line-dialog-body">
        <div v-for="line in lineDialogLines" :key="line.key" class="line-dialog-row">
          <div class="line-identity"><b>{{ line.itemName || line.itemNo }}</b><small>{{ line.itemNo }}<template v-if="line.barcode"> · {{ line.barcode }}</template><template v-if="line.metalName"> · {{ line.metalName }}</template></small></div>
          <div class="readonly-value">{{ number(line.bookQty) }} <em>{{ line.unit || (editorTakeType === 1 ? '件' : 'g') }}</em></div>
          <el-input-number v-model="line.actualQty" :disabled="readOnly" :min="0" :max="line.itemMode === 1 && editorTakeType === 1 ? 1 : 1000000" :precision="editorTakeType === 1 ? 0 : 4" :step="editorTakeType === 1 ? 1 : 0.01" controls-position="right" size="small" @change="recalculate(line)" />
          <div class="diff-value" :class="amountTone(line.diffQty)">{{ signed(line.diffQty) }}</div>
          <el-input v-model="line.remark" :disabled="readOnly" maxlength="200" placeholder="本行备注" size="small" />
        </div>
      </div>
      <div v-else class="state-box">该分类下没有商品明细。</div>
      <template #footer><el-button type="primary" @click="lineDialogOpen = false">完成核对</el-button></template>
    </el-dialog>
  </div>
</template>

<script setup name="JyStocktake">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue'
import { getCurrentInstance } from 'vue'
import { onBeforeRouteLeave } from 'vue-router'
import { serverDateTimeText } from '@/utils/serverTime'
import useUserStore from '@/store/modules/user'
import {
  listStocktakes, getStocktake, getStocktakeDashboard,
  initializeStocktake, saveStocktake, deleteStocktake, confirmStocktake, getStocktakeReport
} from '@/api/jingyue/stocktake'

const { proxy } = getCurrentInstance()
const NORMAL_LABEL = '正品'
const MATERIAL_LABEL = '物料'
const PIECE_LABEL = '件'
const GRAM_LABEL = 'g'
const activeTab = ref('goods')
const loading = ref(false)
const dashboardLoading = ref(false)
const editorLoading = ref(false)
const initializing = ref(false)
const categorySaving = ref('')
const saving = ref(false)
const deleting = ref(false)
const reportLoading = ref(false)
const readOnly = ref(false)
const draftDirty = ref(false)
const draftReady = ref(false)
const draftRestoring = ref(false)
const rows = ref([])
const total = ref(0)
function recentWeekRange() {
  const format = (date) => {
    const pad = (value) => String(value).padStart(2, '0')
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
  }
  const end = new Date()
  const begin = new Date(end)
  begin.setDate(end.getDate() - 6)
  return [format(begin), format(end)]
}
const dateRange = ref(recentWeekRange())
const editorOpen = ref(false)
const formRef = ref()
const takeType = ref(1)
const editorTakeType = ref(1)
const reportRange = ref(null)
const lineDialogOpen = ref(false)
const lineDialogTitle = ref('')
const lineDialogCategory = ref(null)
const lineDialogParent = ref(null)
const lineDialogLines = ref([])
const parentOpen = reactive({})
const materialOpen = reactive({})
const parentRemarks = reactive({})
const userStore = useUserStore()
const dashboard = reactive({ draftCount: 0, confirmedToday: 0, diffLineCount: 0, netDiffAmount: 0, goodsCount: 0, materialCount: 0, pending: [] })
const report = reactive(emptyReport())
const query = reactive({ pageNum: 1, pageSize: 12, keyword: '', status: undefined, takeType: 1, beginTime: recentWeekRange()[0], endTime: recentWeekRange()[1] })
const reportQuery = reactive({ takeType: undefined, keyword: '', beginTime: undefined, endTime: undefined })
const form = reactive(emptyForm())
const rules = { takeTime: [{ required: true, message: '请选择盘点时间', trigger: 'change' }] }
let lineKey = 0
let draftTimer = 0
let closingEditor = false

function emptyReport() {
  return { documentCount: 0, lineCount: 0, diffLineCount: 0, goodsGainQty: 0, goodsLossQty: 0, materialGainWeight: 0, materialLossWeight: 0, gainAmount: 0, lossAmount: 0, netDiffAmount: 0, goodsDocumentCount: 0, materialDocumentCount: 0, categories: [], documents: [], differences: [] }
}
function emptyForm() {
  return { id: 0, stocktakeNo: '', storeId: null, takeType: 1, status: 0, takeTime: serverDateTimeText().replace(' ', 'T'), remark: '', lines: [], categories: [] }
}
function lineMaterialGroup(lines, categoryKey) {
  const line = (lines || []).find((item) => (item.categoryKey || 'uncategorized') === categoryKey)
  const group = Number(line?.materialGroup || 0)
  if (group >= 1 && group <= 3) return group
  const matched = String(categoryKey || '').match(/^m(\d+)(?::|$)/)
  const parsed = Number(matched?.[1] || 0)
  return parsed >= 1 && parsed <= 3 ? parsed : 3
}
function draftStorageKey() {
  return `jy:stocktake:draft:${userStore.tenantId || '0'}:${form.id || form.stocktakeNo || 'new'}`
}
function draftSnapshot() {
  return {
    takeTime: form.takeTime,
    remark: form.remark,
    categories: form.categories.map((category) => ({
      categoryKey: category.categoryKey,
      isCompleted: !!category.isCompleted,
      incompleteReason: category.incompleteReason || '',
      anomalyRemark: category.anomalyRemark || ''
    })),
    lines: form.lines.map((line) => ({ id: line.id, actualQty: Number(line.actualQty || 0), remark: line.remark || '' })),
    updatedAt: new Date().toISOString()
  }
}
function saveLocalDraft(markDirty = true) {
  if (readOnly.value || !form.id || closingEditor) return false
  try {
    localStorage.setItem(draftStorageKey(), JSON.stringify(draftSnapshot()))
    if (markDirty) draftDirty.value = true
    return true
  } catch (error) {
    console.error('stocktake local draft save failed', error)
    return false
  }
}
function restoreLocalDraft() {
  let draft
  try {
    draft = JSON.parse(localStorage.getItem(draftStorageKey()) || 'null')
  } catch (error) {
    localStorage.removeItem(draftStorageKey())
    return false
  }
  if (!draft || draft.updatedAt === undefined) return false
  draftRestoring.value = true
  try {
    if (draft.takeTime) form.takeTime = draft.takeTime
    if (draft.remark !== undefined) form.remark = draft.remark || ''
    const lineMap = new Map((draft.lines || []).map((line) => [line.id, line]))
    form.lines.forEach((line) => {
      const cached = lineMap.get(line.id)
      if (!cached) return
      line.actualQty = Number(cached.actualQty || 0)
      line.remark = cached.remark || ''
      const diff = Number((line.actualQty - Number(line.bookQty || 0)).toFixed(4))
      line.diffQty = diff
      line.diffAmount = Number((diff * Number(line.unitCost || 0)).toFixed(2))
    })
    const categoryMap = new Map((draft.categories || []).map((category) => [category.categoryKey, category]))
    form.categories.forEach((category) => {
      const cached = categoryMap.get(category.categoryKey)
      if (!cached) return
      category.isCompleted = !!cached.isCompleted
      category.incompleteReason = cached.incompleteReason || ''
      category.anomalyRemark = cached.anomalyRemark || ''
      const key = category.parentCategoryKey || 'parent-' + category.categoryKey
      parentRemarks[key] = category.anomalyRemark
    })
    rebuildTotals()
    draftDirty.value = true
    return true
  } finally {
    draftRestoring.value = false
  }
}
function clearLocalDraft() {
  try { localStorage.removeItem(draftStorageKey()) } catch (error) { console.error('stocktake local draft clear failed', error) }
  draftDirty.value = false
}
function requestCloseEditor() {
  if (closingEditor) return
  if (!readOnly.value && draftDirty.value && !saveLocalDraft(false)) {
    proxy.$modal.msgWarning('本机暂存失败，请先保存草稿后再关闭')
    return
  }
  if (!readOnly.value && draftDirty.value) {
    proxy.$modal.confirm('当前修改已暂存到本机，下次打开此盘点单会自动恢复。现在退出编辑吗？', '退出盘点编辑').then(() => {
      closingEditor = true
      editorOpen.value = false
    }).catch(() => {})
    return
  }
  closingEditor = true
  editorOpen.value = false
}
function onEditorClosed() {
  lineDialogOpen.value = false
  draftReady.value = false
  closingEditor = false
}
function onBeforeUnload() {
  if (editorOpen.value && draftDirty.value) saveLocalDraft(false)
}
watch(() => draftSnapshot(), () => {
  if (!draftReady.value || readOnly.value || draftRestoring.value || closingEditor) return
  draftDirty.value = true
  window.clearTimeout(draftTimer)
  draftTimer = window.setTimeout(() => saveLocalDraft(false), 700)
}, { deep: true })
onBeforeUnmount(() => {
  window.clearTimeout(draftTimer)
  window.removeEventListener('beforeunload', onBeforeUnload)
})
window.addEventListener('beforeunload', onBeforeUnload)
onBeforeRouteLeave(() => {
  if (editorOpen.value && draftDirty.value) saveLocalDraft(false)
  return true
})
const takeTypeText = (type) => Number(type) === 2 ? '物料盘点' : '正品盘点'
const money = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const number = (value) => Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 4 })
const weight = (value) => Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 4 })
const signed = (value) => { const n = Number(value || 0); return n > 0 ? '+' + number(n) : number(n) }
const isSingleGoods = (line) => editorTakeType.value === 1 && Number(line.itemMode || 0) === 1
const signedMoney = (value) => { const n = Number(value || 0); return (n > 0 ? '+¥' : n < 0 ? '-¥' : '¥') + money(Math.abs(n)) }
const amountTone = (value) => { const n = Number(value || 0); return n > 0 ? 'tone-gain' : n < 0 ? 'tone-loss' : 'tone-neutral' }
const dateText = (value) => value ? String(value).slice(0, 10) : '—'
const diffCount = computed(() => form.lines.filter((line) => Math.abs(Number(line.diffQty || 0)) > 0.00005).length)
const diffAmount = computed(() => form.lines.reduce((sum, line) => sum + Number(line.diffAmount || 0), 0))
const completedCategoryCount = computed(() => form.categories.filter((category) => category.isCompleted).length)
const categoryProgress = computed(() => form.categories.length ? Math.round((completedCategoryCount.value / form.categories.length) * 100) : 0)
const totalBookQty = computed(() => form.lines.reduce((sum, line) => sum + Number(line.bookQty || 0), 0))
const totalActualQty = computed(() => form.lines.reduce((sum, line) => sum + Number(line.actualQty || 0), 0))
const totalDiffQty = computed(() => form.lines.reduce((sum, line) => sum + Number(line.diffQty || 0), 0))
const completedKeys = computed(() => new Set(form.categories.filter((category) => category.isCompleted).map((category) => category.categoryKey)))
const pendingBookQty = computed(() => form.lines.filter((line) => !completedKeys.value.has(line.categoryKey)).reduce((sum, line) => sum + Number(line.bookQty || 0), 0))
const parentGroups = computed(() => {
  const groups = new Map()
  form.categories.forEach((category) => {
    const key = category.parentCategoryKey || 'parent-' + category.categoryKey
    if (!groups.has(key)) groups.set(key, { key, name: category.parentCategoryName || category.categoryName, categories: [] })
    groups.get(key).categories.push(category)
  })
  return Array.from(groups.values()).map((group) => {
    const keys = new Set(group.categories.map((category) => category.categoryKey))
    const lines = form.lines.filter((line) => keys.has(line.categoryKey))
    const itemCount = group.categories.reduce((sum, category) => sum + Number(category.itemCount || 0), 0)
    const completedCount = group.categories.filter((category) => category.isCompleted).length
    const bookQty = group.categories.reduce((sum, category) => sum + Number(category.bookQty || 0), 0)
    const actualQty = group.categories.reduce((sum, category) => sum + Number(category.actualQty || 0), 0)
    const lockedQty = lines.reduce((sum, line) => sum + Number(line.lockedQty || 0), 0)
    return {
      ...group, itemCount, completedCount, bookQty, actualQty,
      diffQty: Number((actualQty - bookQty).toFixed(4)),
      capacity: editorTakeType.value === 1 ? lines.length : 1000000,
      lockedQty,
      anomalyRemark: group.categories.map((category) => category.anomalyRemark).find((value) => String(value || '').trim()) || ''
    }
  })
})

function materialGroupOf(category) {
  const value = Number(category?.materialGroup || 0)
  if (value >= 1 && value <= 3) return value
  const matched = String(category?.categoryKey || '').match(/^m(\d+)(?::|$)/)
  const parsed = Number(matched?.[1] || 0)
  return parsed >= 1 && parsed <= 3 ? parsed : 3
}
function materialName(group) {
  return group === 1 ? '足金类' : group === 2 ? '足银类' : '其他类'
}
const materialGroups = computed(() => {
  const groups = new Map()
  parentGroups.value.forEach((parent) => {
    const group = materialGroupOf(parent.categories[0])
    if (!groups.has(group)) groups.set(group, { key: 'm' + group, group, name: materialName(group), parents: [], categories: [] })
    const bucket = groups.get(group)
    bucket.parents.push(parent)
    bucket.categories.push(...parent.categories)
  })
  return Array.from(groups.values()).sort((a, b) => a.group - b.group).map((bucket) => {
    const bookQty = bucket.categories.reduce((sum, category) => sum + Number(category.bookQty || 0), 0)
    const actualQty = bucket.categories.reduce((sum, category) => sum + Number(category.actualQty || 0), 0)
    return {
      ...bucket,
      parentCount: bucket.parents.length,
      itemCount: bucket.categories.reduce((sum, category) => sum + Number(category.itemCount || 0), 0),
      completedCount: bucket.categories.filter((category) => category.isCompleted).length,
      bookQty,
      actualQty,
      diffQty: Number((actualQty - bookQty).toFixed(4))
    }
  })
})

function categoryLines(category) { return form.lines.filter((line) => line.categoryKey === category.categoryKey) }
function isParentOpen(key) { return parentOpen[key] !== false }
function toggleParent(key) { parentOpen[key] = !isParentOpen(key) }
function isMaterialOpen(key) { return materialOpen[key] !== false }
function toggleMaterial(key) { materialOpen[key] = !isMaterialOpen(key) }
function openLineDialog(parent, category) {
  lineDialogCategory.value = category
  lineDialogParent.value = parent
  lineDialogLines.value = categoryLines(category)
  lineDialogTitle.value = parent.name + ' / ' + category.categoryName
  lineDialogOpen.value = true
}
function syncParentRemark(parent) {
  const value = Number(parent.diffQty || 0) === 0 ? '' : String(parentRemarks[parent.key] || '').trim()
  parent.categories.forEach((category) => { category.anomalyRemark = value })
}
function refreshCategoryTotals(category) {
  const group = categoryLines(category)
  category.actualQty = group.reduce((sum, line) => sum + Number(line.actualQty || 0), 0)
  category.diffQty = group.reduce((sum, line) => sum + Number(line.diffQty || 0), 0)
}
// 汇总值始终从明细重算，避免输入后顶部摘要或分类数字滞后。
function rebuildTotals() {
  form.categories.forEach((category) => {
    const group = categoryLines(category)
    category.bookQty = group.reduce((sum, line) => sum + Number(line.bookQty || 0), 0)
    category.actualQty = group.reduce((sum, line) => sum + Number(line.actualQty || 0), 0)
    category.diffQty = group.reduce((sum, line) => sum + Number(line.diffQty || 0), 0)
  })
}
function applyParentActual(parent, value) {
  if (readOnly.value || editorTakeType.value !== 1) return
  const target = Math.max(0, Math.min(parent.capacity, Math.round(Number(value || 0))))
  const lines = form.lines.filter((line) => parent.categories.some((category) => category.categoryKey === line.categoryKey))
  const locked = lines.filter((line) => Number(line.lockedQty || 0) > 0).length
  if (target < locked) {
    proxy.$modal.msgWarning(`「${parent.name}」有 ${locked} 件锁定商品，实盘不能少于 ${locked} 件`)
    return
  }
  const lockedLines = lines.filter((line) => Number(line.lockedQty || 0) > 0)
  const freeLines = lines.filter((line) => Number(line.lockedQty || 0) <= 0)
  const currentFree = freeLines.reduce((sum, line) => sum + Number(line.actualQty || 0), 0)
  const delta = target - locked - currentFree
  if (delta > 0) {
    freeLines.filter((line) => Number(line.actualQty || 0) <= 0).slice(0, delta)
      .forEach((line) => { line.actualQty = 1; recalculate(line) })
  } else if (delta < 0) {
    freeLines.filter((line) => Number(line.actualQty || 0) > 0).slice(0, Math.abs(delta))
      .forEach((line) => { line.actualQty = 0; recalculate(line) })
  }
  const after = locked + freeLines.reduce((sum, line) => sum + Number(line.actualQty || 0), 0)
  if (after !== target) proxy.$modal.msgWarning(`「${parent.name}」最多只能录入 ${after} 件`)
  parent.categories.forEach(refreshCategoryTotals)
  syncParentRemark(parent)
}
async function toggleCategory(category) {
  if (!form.id) return
  const nextCompleted = !category.isCompleted
  const previousReason = category.incompleteReason
  categorySaving.value = category.categoryKey
  try {
    category.isCompleted = nextCompleted
    if (nextCompleted) category.incompleteReason = ''
    await saveStocktake(payload())
    proxy.$modal.msgSuccess(nextCompleted ? '分类进度已保存' : '已取消分类完成')
  } catch (error) {
    category.isCompleted = !nextCompleted
    category.incompleteReason = previousReason
    throw error
  } finally { categorySaving.value = '' }
}
async function saveCategoryReason(category) {
  if (readOnly.value || !form.id || category.isCompleted) return
  try { await saveStocktake(payload()) }
  catch (error) { proxy.$modal.msgError('分类未完成原因保存失败，请稍后重试') }
}
function recalculate(line) {
  const book = Number(line.bookQty || 0)
  const actual = Number(line.actualQty || 0)
  line.diffQty = Number((actual - book).toFixed(4))
  line.diffAmount = Number((line.diffQty * Number(line.unitCost || 0)).toFixed(2))
  const category = form.categories.find((item) => item.categoryKey === line.categoryKey)
  if (category) refreshCategoryTotals(category)
}
function loadList() {
  loading.value = true
  query.takeType = takeType.value
  listStocktakes({ ...query, status: query.status === '' ? undefined : query.status }).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function loadDashboard() {
  dashboardLoading.value = true
  getStocktakeDashboard().then((res) => Object.assign(dashboard, res.data || {})).finally(() => { dashboardLoading.value = false })
}
function refreshAll() { loadList(); loadDashboard() }
function search() { query.pageNum = 1; loadList() }
function onDateChange(value) { query.beginTime = value?.[0] || undefined; query.endTime = value?.[1] || undefined; search() }
function onTabChange(name) {
  if (name === 'report') { loadReport(); return }
  takeType.value = name === 'material' ? 2 : 1
  query.pageNum = 1
  loadList()
}
async function openCreate(type) {
  editorTakeType.value = type
  readOnly.value = false
  editorOpen.value = true
  editorLoading.value = true
  initializing.value = true
  try {
    const now = emptyForm()
    const res = await initializeStocktake({ takeType: type, takeTime: now.takeTime, remark: '' })
    await openEdit({ id: res.data, takeType: type, status: 0 })
  } finally { initializing.value = false }
}
async function openEdit(row) {
  editorLoading.value = true
  readOnly.value = row.status !== 0
  editorTakeType.value = Number(row.takeType || 1)
  Object.assign(form, emptyForm())
  editorOpen.value = true
  try {
    const res = await getStocktake(row.id)
    const data = res.data || {}
    editorTakeType.value = Number(data.takeType || 1)
    readOnly.value = Number(data.status) !== 0
    Object.assign(form, {
      id: data.id, stocktakeNo: data.stocktakeNo, storeId: data.storeId,
      takeType: data.takeType, status: Number(data.status || 0), takeTime: String(data.takeTime || '').slice(0, 19), remark: data.remark || '',
      lines: (data.lines || []).map((line) => ({
        key: 'l' + Date.now() + (lineKey++), id: line.id,
        goodsItemId: line.goodsItemId || 0, materialId: line.materialId || 0,
        itemNo: line.itemNo, itemName: line.itemName, barcode: line.barcode, metalName: line.metalName,
        materialGroup: line.materialGroup, materialGroupName: line.materialGroupName,
        unit: line.unit || (editorTakeType.value === 1 ? '件' : 'g'), itemMode: line.itemMode,
        categoryKey: line.categoryKey || 'uncategorized', categoryName: line.categoryName || '未分类',
        bookQty: Number(line.bookQty || 0), actualQty: Number(line.actualQty || 0),
        diffQty: Number(line.diffQty || 0), diffAmount: Number(line.diffAmount || 0),
        lockedQty: Number(line.lockedQty || 0), unitCost: Number(line.unitCost || 0), remark: line.remark || ''
      })),
        categories: (data.categories || []).map((category) => ({
        categoryKey: category.categoryKey, categoryName: category.categoryName,
        parentCategoryKey: category.parentCategoryKey, parentCategoryName: category.parentCategoryName,
          materialGroup: category.materialGroup || lineMaterialGroup(data.lines, category.categoryKey),
          materialGroupName: category.materialGroupName || materialName(category.materialGroup || lineMaterialGroup(data.lines, category.categoryKey)),
        itemCount: Number(category.itemCount || 0), isCompleted: !!category.isCompleted,
        incompleteReason: category.incompleteReason || '', anomalyRemark: category.anomalyRemark || ''
      }))
    })
    if (!form.categories.length && form.lines.length) {
      const map = new Map()
      form.lines.forEach((line) => {
        if (!map.has(line.categoryKey)) map.set(line.categoryKey, {
          categoryKey: line.categoryKey, categoryName: line.categoryName,
          parentCategoryKey: 'parent-' + line.categoryKey, parentCategoryName: line.categoryName,
          materialGroup: line.materialGroup, materialGroupName: line.materialGroupName,
          itemCount: 1, isCompleted: true, incompleteReason: '', anomalyRemark: ''
        })
      })
      form.categories = Array.from(map.values())
    }
    rebuildTotals()
    draftReady.value = false
    form.categories.forEach((category) => {
      const key = category.parentCategoryKey || 'parent-' + category.categoryKey
      parentOpen[key] = parentOpen[key] ?? false
      parentRemarks[key] = category.anomalyRemark || parentRemarks[key] || ''
    })
    form.categories.forEach((category) => { materialOpen['m' + materialGroupOf(category)] = materialOpen['m' + materialGroupOf(category)] ?? false })
    if (!readOnly.value && restoreLocalDraft()) proxy.$modal.msgSuccess('已恢复本机未保存草稿')
    await nextTick()
    draftReady.value = true
  } finally { editorLoading.value = false }
}
function payload() {
  form.categories.forEach((category) => {
    const key = category.parentCategoryKey || 'parent-' + category.categoryKey
    category.anomalyRemark = String(parentRemarks[key] || '').trim()
  })
  return {
    id: form.id, storeId: form.storeId, takeType: editorTakeType.value, takeTime: form.takeTime,
    remark: form.remark,
    categories: form.categories.map((category) => ({
      categoryKey: category.categoryKey, categoryName: category.categoryName,
      parentCategoryKey: category.parentCategoryKey, parentCategoryName: category.parentCategoryName,
      isCompleted: category.isCompleted, incompleteReason: category.incompleteReason,
      anomalyRemark: category.anomalyRemark
    })),
    lines: form.lines.map((line) => ({
      id: line.id, goodsItemId: editorTakeType.value === 1 ? line.goodsItemId : 0,
      materialId: editorTakeType.value === 2 ? line.materialId : 0,
      actualQty: Number(line.actualQty || 0), remark: line.remark,
      categoryKey: line.categoryKey, categoryName: line.categoryName
    }))
  }
}
function save(confirmAfter) {
  formRef.value?.validate().then(async () => {
    if (!form.lines.length) return proxy.$modal.msgWarning('盘点单没有明细')
    if (form.lines.some((line) => line.actualQty === undefined || line.actualQty === null || line.actualQty === '')) return proxy.$modal.msgWarning('请填写所有实盘数量')
    form.categories.forEach((category) => {
      const key = category.parentCategoryKey || 'parent-' + category.categoryKey
      category.anomalyRemark = String(parentRemarks[key] || '').trim()
    })
    if (confirmAfter && form.categories.some((category) => !category.isCompleted && !String(category.incompleteReason || '').trim()))
      return proxy.$modal.msgWarning('未完成分类必须填写原因')
    const diffParent = parentGroups.value.find((parent) => parent.completedCount > 0 && Math.abs(Number(parent.diffQty || 0)) > 0.00005 && !String(parentRemarks[parent.key] || '').trim())
    if (diffParent)
      return proxy.$modal.msgWarning(`「${diffParent.name}」账实不一致，请填写异常备注`)
    saving.value = true
    try {
      if (confirmAfter) {
        try { await proxy.$modal.confirm('确认后将按实盘数更新库存并生成流水，单据不能再修改。库存若已变化，本次确认会整单失败。', '确认盘点') }
        catch (cancel) { saving.value = false; return }
      }
      const res = await saveStocktake(payload())
      const id = res.data
      if (confirmAfter) await confirmStocktake(id)
      clearLocalDraft()
      proxy.$modal.msgSuccess(confirmAfter ? '盘点已确认，库存和流水已更新' : '盘点草稿已保存')
      closingEditor = true
      editorOpen.value = false
      lineDialogOpen.value = false
      refreshAll()
      if (activeTab.value === 'report') loadReport()
    } finally { saving.value = false }
  }).catch(() => {})
}
function confirmRow(row) {
  proxy.$modal.confirm('确认「' + row.stocktakeNo + '」？确认时会校验账面库存并写入盘盈盘亏流水。', '确认盘点').then(() => {
    saving.value = true
    return confirmStocktake(row.id)
  }).then(() => { proxy.$modal.msgSuccess('盘点已确认'); refreshAll() }).catch(() => {}).finally(() => { saving.value = false })
}
function removeRow(row) {
  const confirmed = row.status === 1
  const message = confirmed
    ? `「${row.stocktakeNo}」已经确认完成。删除只会隐藏盘点单及其快照，不会回滚或删除已生成的库存流水，是否继续？`
    : `删除草稿「${row.stocktakeNo}」？删除后本次盘点快照作废，不会改动库存。`
  proxy.$modal.confirm(message, confirmed ? '删除已确认盘点单' : '删除盘点单').then(() => {
    deleting.value = true
    return deleteStocktake(row.id)
  }).then(() => { proxy.$modal.msgSuccess('盘点单已删除'); refreshAll() }).catch(() => {}).finally(() => { deleting.value = false })
}
function removeEditor() {
  if (!form.id) return
  const confirmed = form.status === 1
  proxy.$modal.confirm(confirmed
    ? `「${form.stocktakeNo}」已经确认完成。删除只隐藏盘点快照，不会回滚库存流水，是否继续？`
    : `删除草稿「${form.stocktakeNo}」？删除后本次盘点快照作废，不会改动库存。`, confirmed ? '删除已确认盘点单' : '删除盘点单').then(() => {
    deleting.value = true
    return deleteStocktake(form.id)
  }).then(() => {
    clearLocalDraft()
    proxy.$modal.msgSuccess('盘点单已删除')
    closingEditor = true
    editorOpen.value = false
    lineDialogOpen.value = false
    refreshAll()
  }).catch(() => {}).finally(() => { deleting.value = false })
}
function loadReport() {
  reportLoading.value = true
  reportQuery.beginTime = reportRange.value?.[0] || undefined
  reportQuery.endTime = reportRange.value?.[1] || undefined
  getStocktakeReport(reportQuery).then((res) => Object.assign(report, emptyReport(), res.data || {})).finally(() => { reportLoading.value = false })
}
function documentNo(id) { return report.documents.find((it) => it.id === id)?.stocktakeNo || '—' }

loadDashboard()
loadList()
</script>

<style scoped>
.stocktake-page { display: flex; flex-direction: column; gap: 16px; min-height: calc(100vh - 132px); color: #4d373e; }
.page-hero { position: relative; display: flex; align-items: center; justify-content: space-between; gap: 24px; overflow: hidden; padding: 14px 22px; border: 1px solid #f1e1e3; border-radius: 14px; background: linear-gradient(120deg,#fffaf7 0%,#fdf1ee 48%,#f7e8e7 100%); box-shadow: 0 9px 28px rgba(111,74,81,.06); }
.page-hero > div:first-child { position: relative; z-index: 1; }
.eyebrow, .section-kicker { display: block; color: #b06e7a; font-size: 11px; font-weight: 700; letter-spacing: 2.1px; }
.page-hero h1 { margin: 4px 0 2px; color: #4f363e; font-family: "Songti SC","STSong",Georgia,serif; font-size: 24px; font-weight: 600; }
.page-hero p { max-width: 780px; margin: 0; color: #9b8188; font-size: 12px; line-height: 1.45; }
.hero-actions { position: relative; z-index: 1; display: flex; gap: 8px; flex: none; }
.hero-glow { position: absolute; right: -50px; bottom: -90px; width: 260px; height: 260px; border-radius: 50%; background: radial-gradient(circle,rgba(231,177,163,.34),transparent 68%); pointer-events: none; }
.metric-board { display: grid; grid-template-columns: 1.15fr 1fr 1fr 1fr; gap: 12px; }
.metric-board article { position: relative; min-height: 88px; padding: 12px 15px; overflow: hidden; border: 1px solid #f2e4e5; border-radius: 12px; background: #fff; box-shadow: 0 6px 20px rgba(110,74,80,.045); }
.metric-feature { background: linear-gradient(140deg,#fdf7f4,#f9ece9); }
.metric-feature > span, .metric-label { display: block; color: #a78b91; font-size: 12px; }
.metric-feature strong, .metric-board article > strong { display: block; margin-top: 7px; color: #553b43; font-family: "Songti SC","STSong",Georgia,serif; font-size: 23px; font-weight: 600; }
.metric-feature strong em, .metric-board article > strong em { color: #ac8a8f; font-family: inherit; font-size: 12px; font-style: normal; font-weight: 400; }
.metric-feature p, .metric-board article > small { display: block; margin: 2px 0 0; color: #b09ba0; font-size: 10px; }
.metric-foot { display: flex; justify-content: space-between; margin-top: 6px; padding-top: 5px; border-top: 1px dashed #eed8d8; color: #a4878d; font-size: 10px; }
.metric-foot b { color: #a86672; font-weight: 600; }
.metric-icon { position: absolute; right: 17px; top: 17px; display: grid; place-items: center; width: 30px; height: 30px; border-radius: 50%; background: #fdf2f1; color: #bc7e84; font-style: normal; }
.metric-icon.gold { color: #b29360; background: #fcf7ed; }
.main-tabs { padding: 2px 2px 0; }
.tab-body { padding-top: 6px; }
.list-panel, .report-panel { display: flex; flex-direction: column; padding: 17px 19px 11px; border: 1px solid #f2e4e5; border-radius: 16px; background: #fff; box-shadow: 0 7px 22px rgba(106,68,77,.045); }
.panel-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; padding: 0 2px 13px; }
.panel-head h2 { margin: 3px 0 2px; color: #573e45; font-size: 19px; font-weight: 600; }
.panel-head p { margin: 0; color: #b09ba0; font-size: 11px; }
.filters { display: flex; align-items: center; gap: 7px; }
.filters .el-input { width: 160px; }.filters .el-select { width: 116px; }.filters .el-date-editor { width: 230px; }
.doc-list { flex: 1; min-height: 380px; }
.doc-row { display: flex; align-items: stretch; gap: 5px; margin-bottom: 7px; border: 1px solid #f3e8e9; border-radius: 10px; background: #fff; transition: border-color .2s, box-shadow .2s; }
.doc-row:hover { border-color: #eccfd2; box-shadow: 0 5px 16px rgba(124,79,85,.055); }.doc-row.drafting { background: #fffdf9; border-color: #f3e7d9; }
.doc-main { display: grid; flex: 1; grid-template-columns: 34px minmax(180px,1.5fr) minmax(100px,.8fr) minmax(120px,.9fr) 100px 70px; align-items: center; gap: 13px; min-width: 0; padding: 12px 10px 12px 13px; border: 0; background: transparent; color: inherit; text-align: left; cursor: pointer; }
.doc-sign { display: grid; place-items: center; width: 29px; height: 29px; border-radius: 8px; background: #fdf1ef; color: #b8777e; }.doc-sign.material { color: #a28a62; background: #fbf5e8; }
.doc-copy { min-width: 0; }.doc-copy b { display: block; overflow: hidden; color: #60474e; font-size: 12.5px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.doc-copy small { display: block; overflow: hidden; margin-top: 4px; color: #b09aa0; font-size: 10.5px; text-overflow: ellipsis; white-space: nowrap; }
.doc-stat b, .doc-date b { display: block; color: #624950; font-size: 12.5px; font-weight: 500; font-variant-numeric: tabular-nums; }.doc-stat b em { color: #b59b9f; font-size: 10px; font-style: normal; }.doc-stat small, .doc-date small { display: block; margin-top: 3px; color: #b19ba0; font-size: 10px; }
.doc-actions { display: flex; flex: none; align-items: center; padding: 0 9px 0 3px; border-left: 1px solid #f5ebec; }.doc-actions .el-button { margin-left: 7px; }
.list-foot { display: flex; align-items: center; justify-content: space-between; padding-top: 10px; color: #b19ba0; font-size: 11px; }
.empty-panel { display: flex; flex-direction: column; align-items: center; justify-content: center; min-height: 280px; color: #ac959a; }.empty-panel .el-icon { font-size: 26px; color: #c79599; }.empty-panel h3 { margin: 11px 0 4px; color: #694f56; font-size: 16px; font-weight: 500; }.empty-panel p { margin: 0 0 15px; font-size: 12px; }
.report-metrics { display: grid; grid-template-columns: 1.2fr 1fr 1fr 1.2fr 1.2fr 1fr; gap: 1px; overflow: hidden; border: 1px solid #f2e6e7; border-radius: 10px; background: #f2e6e7; }
.report-metrics > div { min-height: 82px; padding: 13px 14px; background: #fffaf8; }.report-metrics span { display: block; color: #ac959a; font-size: 10.5px; }.report-metrics b { display: block; margin-top: 8px; color: #624950; font-size: 18px; font-weight: 500; font-variant-numeric: tabular-nums; }.report-metrics b em { color: #b29ba0; font-size: 10px; font-style: normal; }.report-primary { background: #fdf7f4 !important; }
.report-lists { display: grid; grid-template-columns: 1.35fr 1fr; gap: 16px; margin-top: 16px; }.sub-head { display: flex; align-items: baseline; justify-content: space-between; padding: 0 2px 8px; }.sub-head h3 { margin: 0; color: #624950; font-size: 14px; font-weight: 600; }.sub-head span { color: #b09a9f; font-size: 10.5px; }
.report-category-block { margin-top: 16px; padding: 12px 13px 8px; border: 1px solid #f2e6e7; border-radius: 10px; background: #fffaf8; }.report-category-block .sub-head { padding-bottom: 10px; }
.report-table .cell-sub { display: block; margin-top: 2px; color: #b09aa0; font-size: 10px; }.report-table em { color: #b29ba0; font-size: 10px; font-style: normal; }
.link-button { padding: 0; border: 0; background: transparent; color: #a5636d; font-size: 12px; cursor: pointer; }.link-button:hover { text-decoration: underline; }
.tone-gain { color: #a7774c !important; }.tone-loss { color: #b45f68 !important; }.tone-neutral { color: #938086 !important; }
.stocktake-editor-dialog :deep(.el-dialog__header) { display: none; }
.stocktake-editor-dialog :deep(.el-dialog__body) { height: 100vh; padding: 0; background: #fbf8f7; }
.editor-shell { display: flex; flex-direction: column; height: 100vh; color: #523b42; }
.drawer-heading { display: flex; flex: none; align-items: center; gap: 10px; min-height: 58px; padding: 0 22px; background: #fffdfc; border-bottom: 1px solid #eee0e2; box-shadow: 0 3px 12px rgba(100,65,72,.04); }
.drawer-heading > span { color: #bfa080; font-size: 11px; letter-spacing: 1.9px; }
.drawer-heading > b { color: #5b4048; font-size: 16px; font-weight: 500; }
.drawer-heading > button { display: grid; place-items: center; width: 27px; height: 27px; margin-left: auto; padding: 0; color: #9c838a; background: #fff; border: 1px solid #f0e1e3; border-radius: 50%; cursor: pointer; }
.draft-state { margin-left: 4px; padding: 3px 8px; color: #a56d4b; font-size: 10px; background: #fff7e8; border: 1px solid #f0dfbd; border-radius: 99px; }
.editor-scroll { flex: 1; min-height: 0; overflow-y: auto; }
.editor-loading { display: flex; flex: 1; align-items: center; justify-content: center; gap: 8px; min-height: 260px; color: #a8898f; font-size: 13px; }
.editor-body { display: flex; flex-direction: column; max-width: 1280px; height: 100%; min-height: 0; margin: 0 auto; padding: 12px 22px 14px; }
.doc-form { display: flex; flex: 1; flex-direction: column; min-height: 0; margin-top: 0; }
.form-grid { display: grid; flex: none; grid-template-columns: 1fr 2fr; gap: 0 16px; }
.doc-summary { display: grid; grid-template-columns: repeat(3,1fr); gap: 1px; margin-bottom: 16px; overflow: hidden; border: 1px solid #f2e6e7; border-radius: 8px; background: #f2e6e7; }.doc-summary div { padding: 10px 13px; background: #fffaf8; }.doc-summary span { display: block; color: #ac959a; font-size: 10.5px; }.doc-summary b { display: block; margin-top: 4px; color: #624950; font-size: 15px; font-weight: 500; }.doc-summary b em { color: #b29ba0; font-size: 10px; font-style: normal; }
.form-section { display: flex; flex: 1; flex-direction: column; min-height: 0; margin-bottom: 0; padding-bottom: 0; border-bottom: 0; }.form-caption { display: flex; flex: none; align-items: center; gap: 8px; margin-bottom: 8px; color: #654a51; font-size: 13px; }.form-caption span { color: #bfa080; font-size: 10px; letter-spacing: 1px; }.form-caption b { font-weight: 500; }
.snapshot-note { flex: none; margin-bottom: 8px; padding: 6px 10px; color: #9a7f85; font-size: 11px; line-height: 1.55; border: 1px solid #f0e2dd; border-radius: 7px; background: #fffaf6; }
.material-scroll { flex: 1; min-height: 0; overflow-y: auto; overscroll-behavior: contain; padding-right: 3px; }
.material-block { margin-bottom: 8px; overflow: hidden; border: 1px solid #eadfda; border-radius: 10px; background: #fffdfb; }
.material-block.done { border-color: #dbe7d9; }
.material-head { display: grid; grid-template-columns: 20px minmax(220px,1fr) repeat(3,minmax(85px,.45fr)) auto; gap: 12px; align-items: center; min-height: 50px; padding: 8px 14px; cursor: pointer; background: linear-gradient(90deg,#fdf7f2,#fffdfb); user-select: none; }
.material-block.done .material-head { background: linear-gradient(90deg,#f4faf2,#fffdfb); }
.material-chevron { display: grid; place-items: center; color: #a98976; font-size: 14px; }
.material-head > div { min-width: 0; }
.material-head > div b { display: block; color: #593f46; font-size: 14px; font-weight: 650; }
.material-head > div small { display: block; margin-top: 2px; color: #aa9399; font-size: 10.5px; }
.material-stat { display: flex; flex-direction: column; min-width: 0; }
.material-stat em { color: #b09a9f; font-size: 9.5px; font-style: normal; }
.material-stat b { color: #5f464d; font-size: 13px; font-weight: 600; font-variant-numeric: tabular-nums; }
.material-body { padding: 7px 9px 9px 24px; border-top: 1px solid #f3e8e3; background: #fffcfa; }
.drawer-heading-actions { display: flex; align-items: center; gap: 9px; margin-left: auto; }.drawer-heading-actions > button { display: grid; place-items: center; width: 27px; height: 27px; flex: none; padding: 0; color: #9c838a; background: #fff; border: 1px solid #f0e1e3; border-radius: 50%; cursor: pointer; }
.count-board { display: grid; flex: none; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 1px; margin-bottom: 9px; overflow: hidden; border: 1px solid #f0e3e4; border-radius: 10px; background: #f0e3e4; }.count-board > div { min-width: 0; padding: 8px 12px; background: #fffdfc; }.count-board span { display: block; overflow: hidden; color: #aa9398; font-size: 10.5px; text-overflow: ellipsis; white-space: nowrap; }.count-board b { display: block; margin-top: 3px; overflow: hidden; color: #5e454c; font-size: 18px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; font-variant-numeric: tabular-nums; }.count-board b em { margin-left: 3px; color: #b29ba0; font-size: 10px; font-style: normal; font-weight: 400; }.count-board .count-progress { grid-column: 1 / -1; display: flex; align-items: center; gap: 14px; padding: 7px 12px; background: #fffaf8; }.count-board .count-progress span { flex: none; font-size: 10.5px; }.count-board .count-progress .el-progress { flex: 1; }.tone-warn { color: #b3835b !important; }.tone-actual { color: #567f89 !important; }
.parent-block { margin-bottom: 9px; overflow: hidden; border: 1px solid #efe2df; border-radius: 9px; background: #fffdfb; }.parent-block.done { border-color: #dce8dc; }.parent-head { display: grid; grid-template-columns: 18px minmax(130px,1fr) minmax(280px,1.4fr) auto; gap: 10px; align-items: center; min-height: 53px; padding: 8px 12px; cursor: pointer; background: #fffaf6; user-select: none; }.parent-block.done .parent-head { background: #f8fbf6; }.parent-chevron { display: grid; place-items: center; color: #ac8a90; font-size: 13px; }.parent-title { min-width: 0; }.parent-title b { display: block; overflow: hidden; color: #563e45; font-size: 13px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.parent-title small { display: block; overflow: hidden; margin-top: 2px; color: #ad969b; font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }.parent-numbers { display: grid; grid-template-columns: repeat(3, minmax(65px,1fr)); gap: 8px; align-items: center; min-width: 0; }.parent-numbers > span { display: flex; flex-direction: column; gap: 1px; min-width: 0; }.parent-numbers em { overflow: hidden; color: #b09a9f; font-size: 9.5px; font-style: normal; text-overflow: ellipsis; white-space: nowrap; }.parent-numbers b { overflow: hidden; color: #5f464d; font-size: 13px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; font-variant-numeric: tabular-nums; }.parent-actual .el-input-number { width: 98px; }.parent-actual .el-input-number .el-input__inner { padding: 0 6px; font-size: 13px; font-weight: 500; }.anomaly-row { display: flex; align-items: center; gap: 10px; padding: 7px 13px 7px 40px; border-top: 1px solid #f5e9e3; background: #fffcf6; }.anomaly-row > span { flex: none; color: #a98976; font-size: 10.5px; }.anomaly-row > span i { color: #bd5f62; font-style: normal; }.anomaly-row .el-input { max-width: 600px; }.anomaly-row p { margin: 0; color: #8b7379; font-size: 11px; }
.subcategory-list { border-top: 1px solid #f3e8e3; }.subcategory-row { display: grid; grid-template-columns: minmax(140px,1.4fr) minmax(64px,.6fr) minmax(64px,.6fr) auto; gap: 9px; align-items: center; padding: 8px 12px 8px 40px; border-bottom: 1px solid #f7efeb; }.subcategory-row:last-child { border-bottom: 0; }.subcategory-title { min-width: 0; }.subcategory-title b { display: block; overflow: hidden; color: #624a50; font-size: 11.5px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }.subcategory-title small { display: block; margin-top: 2px; color: #b09a9f; font-size: 9.5px; }.subcategory-stat { display: flex; flex-direction: column; gap: 1px; }.subcategory-stat span { color: #b09a9f; font-size: 9.5px; }.subcategory-stat b { color: #5f464d; font-size: 12px; font-weight: 500; font-variant-numeric: tabular-nums; }.subcategory-actions { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; justify-content: flex-end; }.subcategory-actions .el-button { margin-left: 0; }.subcategory-row > .category-reason { grid-column: 1 / -1; padding: 0; border: 0; background: transparent; }.subcategory-row > .category-reason .el-input { max-width: 100%; }
.line-dialog-summary { display: grid; grid-template-columns: repeat(3,1fr); gap: 1px; margin-bottom: 12px; overflow: hidden; border: 1px solid #f1e3e5; border-radius: 8px; background: #f1e3e5; }.line-dialog-summary > div { padding: 8px 12px; background: #fffaf8; }.line-dialog-summary span { display: block; color: #ad969b; font-size: 10px; }.line-dialog-summary b { display: block; margin-top: 2px; color: #5d454c; font-size: 15px; font-weight: 500; }.line-dialog-anomaly { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; padding: 8px 10px; border: 1px solid #f1e2d9; border-radius: 7px; background: #fffcf6; }.line-dialog-anomaly > span { flex: none; color: #a98976; font-size: 11px; }.line-dialog-anomaly > span i { color: #bd5f62; font-style: normal; }.line-dialog-anomaly p { margin: 0; color: #8b7379; font-size: 11px; }.line-dialog-head, .line-dialog-row { display: grid; grid-template-columns: minmax(150px,1.4fr) minmax(64px,.55fr) minmax(100px,.8fr) minmax(60px,.5fr) minmax(120px,1fr); gap: 9px; align-items: center; padding: 7px 4px; }.line-dialog-head { color: #ad969b; font-size: 10px; border-bottom: 1px solid #f2e6e2; }.line-dialog-body { max-height: min(58vh, 520px); overflow-y: auto; }.line-dialog-row { border-bottom: 1px solid #f8f0ed; }.line-dialog-row:last-child { border-bottom: 0; }
.category-block { margin-bottom: 14px; overflow: hidden; border: 1px solid #f0e4df; border-radius: 10px; background: #fffdfc; }.category-block.done { border-color: #dce8dc; background: #fbfdf9; }
.category-head { display: grid; grid-template-columns: minmax(200px,1fr) minmax(120px,220px) auto auto; gap: 12px; align-items: center; padding: 11px 13px; background: #fff9f5; border-bottom: 1px solid #f3e9e1; }.category-block.done .category-head { background: #f5faf2; border-bottom-color: #e1ecdf; }.category-title { display: flex; align-items: center; gap: 9px; min-width: 0; }.category-mark { display: grid; place-items: center; width: 28px; height: 28px; flex: none; border-radius: 7px; color: #b27879; background: #f8eae5; }.category-block.done .category-mark { color: #719275; background: #e8f2e5; }.category-title b { display: block; overflow: hidden; color: #5d434a; font-size: 13px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }.category-title small { display: block; margin-top: 2px; color: #ae979b; font-size: 10.5px; }.category-progress { min-width: 100px; }.category-reason { padding: 9px 13px; border-bottom: 1px solid #f5ece7; background: #fffcf8; }.category-reason .el-input { max-width: 640px; }
.category-table-head, .snapshot-line { display: grid; grid-template-columns: minmax(170px,1.25fr) minmax(85px,.65fr) minmax(120px,.8fr) minmax(85px,.65fr) minmax(150px,1.1fr); gap: 12px; align-items: center; padding: 8px 13px; }.category-table-head { color: #ab9398; font-size: 10.5px; background: #fffdfc; border-bottom: 1px solid #f6eeea; }.snapshot-line { border-bottom: 1px solid #f8f1ed; }.snapshot-line:last-child { border-bottom: 0; }.line-identity { min-width: 0; }.line-identity b { display: block; overflow: hidden; color: #5e454c; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }.line-identity small { display: block; overflow: hidden; margin-top: 2px; color: #b09aa0; font-size: 10px; text-overflow: ellipsis; white-space: nowrap; }.readonly-value, .diff-value { display: flex; min-height: 30px; align-items: center; color: #624950; font-size: 12.5px; font-variant-numeric: tabular-nums; }.readonly-value em, .diff-value em { margin-left: 4px; color: #b29ba0; font-size: 10px; font-style: normal; }.state-box { display: flex; align-items: center; justify-content: center; min-height: 78px; color: #b09aa0; font-size: 12px; border: 1px dashed #eedfe1; border-radius: 8px; }
.editor-actions { display: flex; flex: none; align-items: center; justify-content: flex-end; gap: 8px; min-height: 58px; padding: 0 22px; background: #fffdfc; border-top: 1px solid #f0e2e4; }
.draft-hint { display: inline-flex; align-items: center; gap: 5px; margin-right: auto; color: #a78e93; font-size: 11px; }
.draft-hint .el-icon { color: #b08b6d; }
@media (max-width: 1280px) { .metric-board { grid-template-columns: 1fr 1fr; }.report-metrics { grid-template-columns: 1fr 1fr 1fr; }.report-lists { grid-template-columns: 1fr; }.doc-main { grid-template-columns: 30px minmax(160px,1.5fr) .8fr .9fr 74px; }.doc-date { display: none; }.panel-head { align-items: flex-start; flex-direction: column; }.filters { flex-wrap: wrap; } }
@media (max-width: 1100px) { .parent-head { grid-template-columns: 18px minmax(110px,1fr) minmax(250px,1.5fr) auto; }.line-dialog-head, .line-dialog-row { grid-template-columns: minmax(130px,1.3fr) 62px minmax(92px,.7fr) 55px minmax(100px,.9fr); } }
@media (max-width: 760px) { .page-hero { align-items: flex-start; flex-direction: column; }.hero-actions { align-self: flex-start; }.metric-board,.report-metrics { grid-template-columns: 1fr; }.form-grid { grid-template-columns: 1fr; }.doc-main { grid-template-columns: 28px 1fr auto; }.doc-stat,.doc-main > .el-tag { display: none; }.filters .el-input,.filters .el-select,.filters .el-date-editor { width: 100%; }.doc-row { flex-direction: column; }.doc-actions { justify-content: flex-end; padding: 0 10px 8px; border-left: 0; }.count-board { grid-template-columns: 1fr 1fr; }.parent-head { grid-template-columns: 18px 1fr auto; }.parent-numbers { grid-column: 2 / -1; grid-row: 2; }.parent-actual .el-input-number { width: 86px; }.anomaly-row { align-items: flex-start; flex-direction: column; gap: 5px; padding-left: 13px; }.anomaly-row .el-input { width: 100%; }.subcategory-row { grid-template-columns: 1fr 64px 64px; padding-left: 13px; }.subcategory-actions { grid-column: 1 / -1; justify-content: flex-start; }.line-dialog-head { display: none; }.line-dialog-row { grid-template-columns: 1fr 1fr; gap: 7px; padding: 10px 2px; }.line-dialog-row .line-identity { grid-column: 1 / -1; }.line-dialog-row > .el-input { grid-column: 1 / -1; }.line-dialog-anomaly { align-items: flex-start; flex-direction: column; gap: 5px; }.line-dialog-anomaly .el-input { width: 100%; } }
</style>
