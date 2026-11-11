<template>
  <div class="jy-inbound" :class="{ 'is-editor-mode': viewMode === 'editor' }">
    <transition name="view-fade" mode="out-in">
      <section v-if="viewMode === 'list'" key="list" class="list-view">
        <header class="page-hero">
          <div class="hero-copy">
            <span class="eyebrow">JEWELLERY OPERATIONS</span>
            <h1>{{ activeSection === 'inbound' ? '采购入库' : '商品档案' }}</h1>
            <p v-if="activeSection === 'inbound'">录入货品并确认入库，自动建立档案与库存记录。</p>
            <p v-else>维护商品属性、计价信息与状态。</p>
            <div class="section-switch" role="tablist" aria-label="采购入库与商品档案">
              <button type="button" role="tab" :aria-selected="activeSection === 'inbound'" :class="{ active: activeSection === 'inbound' }" @click="switchSection('inbound')"><el-icon><Box /></el-icon><span>入库单</span><small>录入与确认</small></button>
              <button v-if="canViewGoods" type="button" role="tab" :aria-selected="activeSection === 'goods'" :class="{ active: activeSection === 'goods' }" @click="switchSection('goods')"><el-icon><Collection /></el-icon><span>商品档案</span><small>价格与状态</small></button>
            </div>
          </div>
          <div v-if="activeSection === 'inbound'" class="hero-actions">
            <el-button :loading="loading" plain @click="refreshAll"><el-icon><Refresh /></el-icon>刷新</el-button>
            <el-button v-hasPermi="['jingyue:inbound:add']" class="primary-action" type="primary" @click="openCreate"><el-icon><Plus /></el-icon>新建入库单</el-button>
          </div>
          <div v-else class="hero-actions">
            <el-button plain @click="goodsRef?.refresh?.()"><el-icon><Refresh /></el-icon>刷新档案</el-button>
            <el-button v-hasPermi="['jingyue:inbound:add']" class="primary-action" type="primary" @click="switchSection('inbound')"><el-icon><Plus /></el-icon>去录入入库</el-button>
          </div>
          <div class="hero-ornament"><i></i><i></i><i></i></div>
        </header>

        <goods-archive v-if="activeSection === 'goods' && canViewGoods" ref="goodsRef" embedded @go-inbound="switchSection('inbound')" />

        <main v-else class="order-panel summary-list-panel">
            <div class="section-head">
              <div><span class="section-kicker">INBOUND ORDERS</span><h2>入库摘要单</h2><p>草稿优先排列，点击单据进入逐件录入工作台</p></div>
              <div class="filters">
                <el-input v-model="query.keyword" clearable placeholder="搜索单据编号或备注" :prefix-icon="Search" @keyup.enter="search" @clear="search" />
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
                <el-select v-model="query.status" clearable placeholder="全部状态" @change="search"><el-option label="待确认" :value="0" /><el-option label="已完成" :value="1" /></el-select>
                <el-button plain @click="search">查询</el-button>
              </div>
            </div>

            <div v-loading="loading" class="order-list">
              <template v-if="rows.length">
                <article v-for="row in rows" :key="row.id" class="order-row" :class="{ drafting: row.status === 0 }">
                  <button type="button" class="order-main" @click="openOrder(row)">
                    <span class="order-status-line"></span>
                    <span class="order-copy"><span class="order-no">{{ row.inboundNo }}</span><span class="order-sub"><span v-if="row.supplierName" class="supplier-chip">{{ row.supplierName }}</span><span class="order-remark">{{ row.remark || '未填写摘要备注' }}</span></span></span>
                    <span class="order-stat qty-stat"><b>{{ number(row.totalQty) }}</b><small>件货品</small></span>
                    <span class="order-stat cost-stat"><b>¥{{ money(row.totalCost) }}</b><small>入库成本</small></span>
                    <span class="order-date"><b>{{ dateText(row.inboundTime).slice(0, 10) }}</b><small>{{ row.periodText || periodOf(row.inboundTime) }}</small></span>
                    <el-tag :type="row.status === 0 ? 'warning' : 'success'" effect="plain" round>{{ row.status === 0 ? '待确认' : '已完成' }}</el-tag>
                  </button>
                  <div class="order-actions">
                    <el-button link type="primary" @click="openOrder(row)">{{ row.status === 0 ? '继续录入' : '查看明细' }}</el-button>
                    <el-button link type="primary" @click.stop="openPrintDialog(row)"><el-icon><Printer /></el-icon>打印标签</el-button>
                    <template v-if="row.status === 0">
                      <el-button v-hasPermi="['jingyue:inbound:confirm']" link type="success" @click="confirmRow(row)">完成入库</el-button>
                      <el-button v-hasPermi="['jingyue:inbound:remove']" link type="danger" @click="removeRow(row)">删除草稿</el-button>
                    </template>
                  </div>
                </article>
              </template>
              <el-empty v-else description="还没有入库摘要单"><el-button v-hasPermi="['jingyue:inbound:add']" type="primary" @click="openCreate">新建第一张</el-button></el-empty>
            </div>
            <pagination v-show="total > 0" :total="total" v-model:page="query.pageNum" v-model:limit="query.pageSize" @pagination="loadList" />
        </main>
      </section>

      <section v-else key="editor" class="editor-view">
        <header class="editor-topbar">
          <div class="editor-title">
            <button type="button" class="back-button" @click="leaveEditor"><el-icon><ArrowLeft /></el-icon></button>
            <div><span class="eyebrow">{{ readOnly ? 'INBOUND RECORD' : 'ENTRY WORKBENCH' }}</span><div class="title-line"><h1>{{ readOnly ? '查看入库单' : header.id ? '继续录入' : '新建入库单' }}</h1><el-tag :type="readOnly ? 'success' : 'warning'" effect="plain" round>{{ readOnly ? '已完成' : '草稿' }}</el-tag><span v-if="!readOnly" class="dirty-state" :class="{ active: dirty }"><i></i>{{ dirty ? '有未保存修改' : header.id ? '已保存' : '新单据' }}</span></div></div>
          </div>
          <div class="editor-actions">
            <template v-if="!readOnly">
              <el-tooltip content="保存当前草稿并返回摘要列表" placement="bottom"><el-button :loading="saving" plain @click="saveDraft">保存草稿</el-button></el-tooltip>
              <el-tooltip content="保存后留在本页，清空输入框并继续录入下一件" placement="bottom"><el-button :loading="saving" plain class="continue-action" @click="saveAndContinue">保存并继续录入</el-button></el-tooltip>
              <el-tooltip content="保存并锁定本单，货品进入商品档案和库存流水" placement="bottom"><el-button :loading="confirming" class="primary-action" type="primary" @click="completeInbound"><el-icon><CircleCheck /></el-icon>完成入库</el-button></el-tooltip>
            </template>
            <el-button v-else plain @click="leaveEditor">返回列表</el-button>
          </div>
        </header>


        <section class="summary-band">
          <div class="summary-fields">
            <label class="summary-field date-field"><span>入库日期</span><el-date-picker v-model="header.inboundTime" :disabled="readOnly || saving || hasIssuedCodes" type="date" value-format="YYYY-MM-DD" format="YYYY年MM月DD日" :clearable="false" /><small v-if="hasIssuedCodes" class="date-lock-hint"><el-icon><WarningFilled /></el-icon>已有货品编码，日期已锁定</small></label>
            <label class="summary-field supplier-field"><span>供应商</span><el-select v-model="header.supplierId" :disabled="readOnly || saving" clearable filterable placeholder="选择本次进货来源"><el-option v-for="item in options.suppliers" :key="item.id" :label="item.name" :value="item.id" /><template #empty><span class="supplier-empty">暂无启用供应商，请先到“静悦基础 → 供应商档案”维护</span></template></el-select></label>
            <label class="summary-field order-field"><span>单据编号</span><strong>{{ header.inboundNo || '保存后自动生成' }}</strong></label>
            <label class="summary-field period-field"><span>所属年月</span><strong>{{ periodLabel }}</strong></label>
            <label class="summary-field remark-field"><span>摘要备注</span><el-input v-model="header.remark" :disabled="readOnly || saving" maxlength="500" show-word-limit placeholder="例如：秋季银饰首批到货，用于快速定位明细" /></label>
          </div>
          <div class="summary-totals"><div><span>已录货品</span><b>{{ entries.length }}<em> 件</em></b></div><div><span>成本合计</span><b class="amount">¥{{ money(totalCost) }}</b></div><div><span>待保存编码</span><b :class="{ pending: unsavedCount > 0 }">{{ unsavedCount }}<em> 件</em></b></div></div>
        </section>

        <section class="wall-panel full-width">
          <div class="wall-head">
            <div class="wall-tools">
              <el-input v-model="cardKeyword" clearable placeholder="在本单中查找" :prefix-icon="Search" />
              <span class="wall-count">{{ filteredEntries.length }} / {{ entries.length }}</span>
              <el-button v-if="!readOnly" type="danger" plain class="batch-delete-action" :disabled="!selectedEntryKeys.length" @click="removeSelectedEntries"><el-icon><Delete /></el-icon>删除已选{{ selectedEntryKeys.length ? ' ' + selectedEntryKeys.length : '' }}</el-button>
              <el-button v-if="!readOnly" type="primary" class="add-line-action" @click="openLineDialog()"><el-icon><Plus /></el-icon>录入货品</el-button>
            </div>
          </div>
          <div v-if="filteredEntries.length" class="goods-list">
            <div class="goods-table-wrap">
              <div class="goods-list-head">
                <span class="select-col"><el-checkbox :model-value="allFilteredEntriesSelected" :indeterminate="entrySelectionIndeterminate" :disabled="readOnly" aria-label="全选本单货品" @change="toggleAllEntries" /></span>
                <span class="index-col">序号</span>
                <span>货品 / 编码</span>
                <span class="head-attrs">属性</span>
                <span class="center head-qty">内含数量</span>
                <span class="num head-cost">成本价</span>
                <span class="num head-sale">售价</span>
                <span class="center head-status">状态</span>
                <span class="center">操作</span>
              </div>
              <div class="goods-list-body">
                <div
                  v-for="(item, index) in filteredEntries"
                  :key="item._key"
                  class="goods-row"
                  :class="{ unsaved: !item.id, selected: editingKey === item._key, readonly: readOnly }"
                  @click="editLine(item)"
                >
                  <div class="select-col row-select" @click.stop>
                    <el-checkbox :model-value="isEntrySelected(item)" :disabled="readOnly" :aria-label="'选择货品 ' + item.goodsName" @change="setEntrySelected(item, $event)" />
                  </div>
                  <div class="index-col row-index">{{ index + 1 }}</div>
                  <div class="cell-goods">
                    <div class="row-thumb">
                      <el-image v-if="item.imageUrl" :src="item.imageUrl" fit="cover" :preview-src-list="[item.imageUrl]" preview-teleported @click.stop />
                      <el-icon v-else><PictureFilled /></el-icon>
                    </div>
                    <div class="row-goods-copy">
                      <b :title="item.goodsName">{{ item.goodsName }}</b>
                      <small :class="{ waiting: !item.id }">{{ item.goodsNo || '保存后生成编码' }}</small>
                      <small v-if="item.remark" class="row-inline-remark" :title="item.remark">{{ item.remark }}</small>
                    </div>
                  </div>
                  <div class="cell-attrs">
                    <div class="row-tags"><el-tag v-for="tag in entryAttributeTags(item)" :key="tag" size="small" effect="plain" round>{{ tag }}</el-tag><span v-if="!entryAttributeTags(item).length" class="cell-empty">未填写</span></div>
                  </div>
                  <div class="center cell-qty"><b>{{ number(item.qty) }}</b><small>{{ item.unit || '件' }}</small></div>
                  <div class="cell-price num">¥{{ money(item.costPrice) }}</div>
                  <div class="cell-price num sale">{{ Number(item.pricingMode) === 2 ? '按当前金价' : '¥' + money(item.salePrice) }}</div>
                  <div class="center cell-status"><el-tag :type="item.id ? 'success' : 'warning'" size="small" effect="plain" round>{{ item.id ? '已保存' : '未保存' }}</el-tag></div>
                  <div class="cell-actions" @click.stop>
                    <el-tooltip content="编辑这件货品" placement="top"><el-button link type="primary" :disabled="readOnly" @click="editLine(item)"><el-icon><EditPen /></el-icon></el-button></el-tooltip>
                    <el-tooltip content="复制这件货品" placement="top"><el-button link type="primary" :disabled="readOnly" @click="openCopyDialog(item)"><el-icon><CopyDocument /></el-icon></el-button></el-tooltip>
                    <el-tooltip content="移除这件货品" placement="top"><el-button link type="danger" :disabled="readOnly" @click="removeEntry(item)"><el-icon><Delete /></el-icon></el-button></el-tooltip>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-else class="wall-empty"><div class="empty-gem"><el-icon><Collection /></el-icon></div><h3>{{ entries.length ? '没有匹配的货品' : '还没有录入货品' }}</h3><p>{{ entries.length ? '换一个关键词试试' : '点击右上角“录入货品”，从全屏窗口开始添加。' }}</p></div>
        </section>
      </section>
    </transition>

    <el-dialog v-model="lineDialogOpen" fullscreen class="line-entry-dialog" :close-on-click-modal="false" :before-close="beforeCloseLineDialog" destroy-on-close @closed="handleLineDialogClosed">
      <template #header>
        <div class="line-dialog-header">
        <div class="line-dialog-title"><span class="step-badge">{{ editingKey ? 'EDIT' : 'NEW' }}</span><div><span class="eyebrow">PRODUCT ENTRY</span><h2>{{ editingKey ? '修改货品明细' : '录入货品明细' }}</h2><p>一条记录对应一件货品，可填写这件货品内含的对、套、颗数量。</p></div></div>
          <div class="line-dialog-status"><span class="dialog-code">{{ editingKey ? (entries.find((it) => it._key === editingKey)?.goodsNo || '保存后生成编码') : '保存后按年月日自动生成编码' }}</span><el-tag :type="readOnly ? 'success' : 'warning'" effect="plain" round>{{ readOnly ? '只读' : '本单尚未保存' }}</el-tag></div>
        </div>
      </template>
      <div class="line-dialog-body">
        <el-form ref="lineFormRef" :model="lineForm" :rules="lineRules" label-position="top" class="line-form dialog-line-form" :disabled="readOnly || saving">
          <section class="entry-section media-section">
            <div class="entry-section-head"><span>商品图片</span><small>建议使用正方形实拍图，突出材质与款式细节</small></div>
            <div class="dialog-media-row">
              <div class="photo-area">
                <el-upload class="photo-uploader" :show-file-list="false" :http-request="uploadLineImage" :before-upload="beforeImageUpload" accept="image/png,image/jpeg,image/webp">
                  <div class="photo-frame" :class="{ filled: lineForm.imageUrl }">
                    <el-image v-if="lineForm.imageUrl" :src="lineForm.imageUrl" fit="cover" :preview-src-list="[lineForm.imageUrl]" preview-teleported @click.stop />
                    <template v-else><el-icon><PictureFilled /></el-icon><span>{{ imageUploading ? '上传中…' : '上传货品照片' }}</span><small>JPG / PNG / WebP，不超过 10MB</small></template>
                  </div>
                </el-upload>
                <button v-if="lineForm.imageUrl && !readOnly" type="button" class="clear-photo" @click.stop="clearLineImage">移除照片</button>
              </div>
              <div class="dialog-basic-fields">
                <el-form-item label="货品名称" prop="goodsName" class="goods-name-field"><el-input ref="goodsNameInput" v-model="lineForm.goodsName" size="large" maxlength="100" placeholder="例如：和田玉平安扣项链" @keyup.enter="commitLine" /></el-form-item>
                <div class="entry-quantity-card">
                  <div class="entry-quantity-copy"><span>本件内含数量</span><small>同一件货品可包含多对、多套或多颗；数量与单位只描述这件货，不拆成多条库存。</small></div>
                  <div class="entry-quantity-side">
                    <div class="entry-quantity-controls">
                      <el-input-number v-model="lineForm.qty" class="entry-quantity-number" :min="0.01" :max="100000" :precision="2" :step="1" size="large" controls-position="right" />
                      <el-select v-model="lineForm.unit" size="large" class="entry-unit-select">
                        <el-option v-for="unit in unitOptions" :key="unit" :label="unit" :value="unit" />
                      </el-select>
                    </div>
                    <div class="entry-quantity-preview"><span>本件合计</span><strong>{{ number(lineForm.qty) }} {{ lineForm.unit || '件' }}</strong></div>
                  </div>
                </div>
                <div class="dialog-price-row dialog-price-large">
                  <el-form-item label="成本价（每单位）"><el-input-number v-model="lineForm.costPrice" size="large" :min="0" :precision="2" :step="10" controls-position="right" /></el-form-item>
                  <el-form-item :label="lineForm.pricingMode === 2 ? '本次入库金属价（元/克）' : '销售价（每件）'"><el-input-number v-model="lineForm.salePrice" size="large" :min="0" :precision="2" :step="10" controls-position="right" /><small v-if="lineForm.pricingMode === 2" class="price-source-hint">记录本批采购金价，不影响销售金价维护</small></el-form-item>
                  <el-form-item label="重量（克）"><el-input-number v-model="lineForm.weight" size="large" :min="0" :max="100000" :precision="2" :step="0.1" controls-position="right" placeholder="按克计价必填" /></el-form-item>
                </div>
                <div class="dialog-price-row dialog-price-large">
                  <el-form-item label="计价方式"><el-radio-group v-model="lineForm.pricingMode"><el-radio-button :value="1">按件</el-radio-button><el-radio-button :value="2">按克</el-radio-button></el-radio-group></el-form-item>
                  <el-form-item :label="lineForm.pricingMode === 2 ? '工费（元/克）' : '工费（元/件）'"><el-input-number v-model="lineForm.laborFee" size="large" :min="0" :precision="2" :step="5" controls-position="right" /></el-form-item>
                  <el-form-item label="包装费（每件）"><el-input-number v-model="lineForm.packagingFee" size="large" :min="0" :precision="2" :step="1" controls-position="right" /></el-form-item>
                </div>
                <div class="price-preview"><span>售价预览</span><strong>¥{{ money(lineSalePreview) }}</strong><small v-if="lineForm.pricingMode === 2">克重 ×（当前金价 + 每克工费）+ 每件包装费</small><small v-else>每件售价手动录入</small></div>
                <div class="entry-code-note"><span>货品编码</span><strong>{{ editingKey ? (entries.find((it) => it._key === editingKey)?.goodsNo || '保存后生成') : '保存时按年月日自动生成' }}</strong></div>
              </div>
            </div>
          </section>

          <section class="entry-section attribute-section">
            <div class="entry-section-head"><span>商品属性</span><small>选择款式大类后，再选择对应的二级分类</small></div>
            <div v-loading="optionsLoading" class="dialog-attribute-grid">
              <el-form-item label="材质大类"><el-select v-model="lineForm.materialGroup" placeholder="选择材质大类"><el-option :value="1" label="足金类" /><el-option :value="2" label="足银类" /><el-option :value="3" label="其他类" /></el-select></el-form-item>
              <el-form-item label="款式大类"><el-select v-model="lineForm.styleParentId" clearable filterable :loading="optionsLoading" :placeholder="styleParentPlaceholder" @visible-change="ensureStyleOptions" @change="changeStyleParent"><el-option v-for="item in styleParents" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="款式分类" prop="styleItemId"><el-select v-model="lineForm.styleItemId" clearable filterable :disabled="!lineForm.styleParentId" placeholder="请选择二级分类"><el-option v-for="item in styleChildren" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="材质"><el-select v-model="lineForm.materialItemId" clearable placeholder="请选择材质"><el-option v-for="item in options.materials" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="金属成色"><el-select v-model="lineForm.metalItemId" clearable placeholder="请选择金属成色"><el-option v-for="item in options.metals" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
              <el-form-item label="证书类型"><el-select v-model="lineForm.certTypeId" clearable placeholder="请选择证书类型"><el-option v-for="item in options.certTypes" :key="item.id" :label="item.itemName" :value="item.id" /></el-select></el-form-item>
            </div>
            <div v-if="!optionsLoading && !styleParents.length" class="attribute-hint attribute-error"><el-icon><WarningFilled /></el-icon><span>款式大类没有加载出来。请检查网络后下拉重试，或点击页面右上角刷新。</span></div>
            <div v-else-if="!options.certTypes.length" class="attribute-hint"><el-icon><WarningFilled /></el-icon><span>暂无证书类型，请先在“数据字典”维护证书类型后再选择。</span></div>
          </section>

          <section class="entry-section remark-section">
            <div class="entry-section-head"><span>货品备注</span><small>记录尺寸、工艺、设计细节或保养说明</small></div>
            <el-form-item label="备注"><el-input v-model="lineForm.remark" type="textarea" :rows="2" maxlength="500" show-word-limit placeholder="例如：足银 999，活口可调，适合日常佩戴" /></el-form-item>
          </section>
        </el-form>
      </div>
      <template #footer>
        <div class="line-dialog-footer"><span class="dialog-footnote"><el-icon><WarningFilled /></el-icon>当前修改只有点击“加入本单”后才会生效；整张草稿仍需保存。</span><div><el-button plain @click="cancelLineDialog">取消</el-button><el-button type="primary" class="commit-button" @click="commitLine"><el-icon><Check /></el-icon>{{ editingKey ? '更新这件货品' : '加入本单' }}</el-button></div></div>
      </template>
    </el-dialog>

    <el-dialog v-model="printDialogOpen" width="680px" class="print-dialog" :close-on-click-modal="false" destroy-on-close>
      <template #header>
        <div class="print-dialog-heading"><span class="step-badge">PRINT</span><div><h2>打印货品标签</h2><p>{{ printHeader.inboundNo || '入库单' }} · 只打印已生成编码的货品</p></div></div>
      </template>
      <div class="print-toolbar">
        <el-checkbox :model-value="printAllChecked" :indeterminate="printIndeterminate" @change="toggleAllPrintRows">全选</el-checkbox>
        <span>已选 {{ selectedPrintKeys.length }} / {{ printRows.length }} 件</span>
        <el-button link type="primary" :loading="printLoading" @click="loadPrintRows">重新读取</el-button>
      </div>
      <div class="print-template-row">
        <span>标签模板</span>
        <el-select v-model="printTemplateId" filterable placeholder="选择标签模板" :disabled="printing">
          <el-option v-for="template in availablePrintTemplates" :key="template.id" :label="template.templateName + ' · ' + template.widthMm + '×' + template.heightMm + 'mm'" :value="template.id" />
        </el-select>
        <small>{{ printTemplateHint }}</small>
      </div>
      <div v-loading="printLoading" class="print-list">
        <el-checkbox-group v-model="selectedPrintKeys">
          <label v-for="row in printRows" :key="row.id" class="print-row">
            <el-checkbox :value="String(row.id)" />
            <span class="print-row-copy"><b>{{ row.goodsName }}</b><small>{{ row.goodsNo }}</small></span>
            <span class="print-row-price">¥{{ money(row.salePrice) }}</span>
          </label>
        </el-checkbox-group>
        <div v-if="!printLoading && !printRows.length" class="print-empty">这张单据还没有已生成编码的货品，暂时无法打印标签。</div>
      </div>
      <div class="print-note"><el-icon><WarningFilled /></el-icon><span>请先在这台电脑启动“静悦标签打印”代理（127.0.0.1:18180），并确认芯烨打印机 USB 已连接。标签尺寸和打印内容以当前模板为准。</span></div>
      <template #footer>
        <div class="print-footer"><el-button plain @click="printDialogOpen = false">取消</el-button><el-button type="primary" :loading="printing" :disabled="!selectedPrintKeys.length || !selectedPrintTemplate || printLoading" @click="printSelected">打印已选标签</el-button></div>
      </template>
    </el-dialog>

    <el-dialog v-model="copyDialogOpen" width="420px" class="copy-dialog" :close-on-click-modal="false" destroy-on-close>
      <template #header>
        <div class="copy-dialog-heading"><span class="step-badge">COPY</span><div><h2>复制货品明细</h2><p>沿用当前名称、价格、照片和属性，生成新的货品编码</p></div></div>
      </template>
      <div v-if="copySource" class="copy-source">
        <span class="copy-source-name">{{ copySource.goodsName }}</span>
        <span class="copy-source-code">{{ copySource.goodsNo || '新货品，保存后生成编码' }}</span>
      </div>
      <el-form label-position="top" class="copy-form" @submit.prevent>
        <el-form-item label="复制件数（不含原商品）">
          <el-input-number v-model="copyForm.copies" :min="1" :max="copyMax" :precision="0" controls-position="right" />
        </el-form-item>
      </el-form>
      <div class="copy-note"><el-icon><WarningFilled /></el-icon><span>副本不提前占用编码，点击保存后按本单顺序连续生成。单张入库单最多 100 件，当前还可新增 {{ copyMax }} 件。</span></div>
      <template #footer>
        <div class="print-footer"><el-button plain @click="copyDialogOpen = false">取消</el-button><el-button type="primary" :disabled="copyMax <= 0" @click="confirmCopy">确认复制</el-button></div>
      </template>
    </el-dialog>
  </div>
</template>
<script setup name="JyInbound">
import { Search, Plus, CircleCheck, WarningFilled, Delete, Collection, ArrowLeft, PictureFilled, Box, PriceTag, Refresh, Check, Printer, CopyDocument, EditPen } from '@element-plus/icons-vue'
import GoodsArchive from '../goods/GoodsArchive.vue'
import { listInbound, getInbound, getInboundOptions, saveInbound, deleteInbound, confirmInbound } from '@/api/jingyue/inbound'
import { getPrintOptions, renderPrintLabel } from '@/api/jingyue/homeConfig'
import { agentPrint } from '@/api/jingyue/printAgent'
import { upload } from '@/api/common'
import { serverRange, serverTodayText } from '@/utils/serverTime'
import { ElMessageBox } from 'element-plus'
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'

const { proxy } = getCurrentInstance()
const route = useRoute()
const router = useRouter()
const viewMode = ref('list')
const lineDialogOpen = ref(false)
const activeSection = ref(route.query.view === 'goods' ? 'goods' : 'inbound')
const goodsRef = ref()
const canViewGoods = computed(() => proxy.$auth.hasPermi('jingyue:goods:list'))
const loading = ref(false)
const saving = ref(false)
const confirming = ref(false)
const imageUploading = ref(false)
const optionsLoading = ref(false)
const printDialogOpen = ref(false)
const printLoading = ref(false)
const printing = ref(false)
const copyDialogOpen = ref(false)
const copySource = ref(null)
const rows = ref([])
const total = ref(0)
const entries = ref([])
const printRows = ref([])
const selectedPrintKeys = ref([])
const printTemplateId = ref(null)
const printTemplates = ref([])
const printBindings = ref([])
const cardKeyword = ref('')
const selectedEntryKeys = ref([])
const lineFormRef = ref()
const goodsNameInput = ref()
const editingKey = ref('')
const dirty = ref(false)
const hydrating = ref(false)
let localKey = 0

const query = reactive({ pageNum: 1, pageSize: 10, keyword: '', status: undefined, beginTime: undefined, endTime: undefined })
const dateRange = ref(recentWeekRange())
const header = reactive(emptyHeader())
const lineForm = reactive(emptyLine())
const options = reactive({ suppliers: [], categories: [], materials: [], metals: [], certTypes: [] })
const printHeader = reactive({ id: 0, inboundNo: '' })
const copyForm = reactive({ copies: 1 })
const lineRules = {
  goodsName: [{ required: true, message: '请输入货品名称', trigger: 'blur' }],
  styleItemId: [{ required: true, message: '请选择二级款式分类', trigger: 'change' }]
}
const unitOptions = ['条', '颗', '对', '件', '套']

const readOnly = computed(() => header.status === 1)
const hasIssuedCodes = computed(() => entries.value.some((item) => !!item.goodsNo))
const periodLabel = computed(() => {
  const match = String(header.inboundTime || '').match(/^(\d{4})-(\d{2})/)
  return match ? `${match[1]}年${match[2]}月` : '-'
})
const totalCost = computed(() => entries.value.reduce((sum, item) => sum + Number(item.costPrice || 0) * Number(item.qty || 1), 0))
const unsavedCount = computed(() => entries.value.filter((item) => !item.id).length)
const styleParents = computed(() => options.categories
  .filter((item) => !item.parentId && Number(item.level || 1) === 1)
  .sort((a, b) => Number(a.sortNo || 0) - Number(b.sortNo || 0) || Number(a.id) - Number(b.id)))
const styleChildren = computed(() => options.categories.filter((item) => Number(item.parentId) === Number(lineForm.styleParentId)))
const styleParentPlaceholder = computed(() => optionsLoading.value ? '正在加载款式分类…' : styleParents.value.length ? '请选择大类' : '暂无款式大类，点击重试')
const lineSalePreview = computed(() => lineForm.pricingMode === 2
  ? Number(lineForm.weight || 0) * (Number(lineForm.salePrice || 0) + Number(lineForm.laborFee || 0)) + Number(lineForm.packagingFee || 0)
  : Number(lineForm.salePrice || 0))
const printAllChecked = computed(() => printRows.value.length > 0 && selectedPrintKeys.value.length === printRows.value.length)
const printIndeterminate = computed(() => selectedPrintKeys.value.length > 0 && selectedPrintKeys.value.length < printRows.value.length)
const availablePrintTemplates = computed(() => printTemplates.value.filter((it) => it.status === 0))
const selectedPrintTemplate = computed(() => availablePrintTemplates.value.find((it) => it.id === printTemplateId.value) || availablePrintTemplates.value[0] || null)
const printTemplateHint = computed(() => selectedPrintTemplate.value
  ? '已按一级款式匹配「' + selectedPrintTemplate.value.templateName + '」，多件不同分类时可临时切换模板。'
  : '请先在静悦基础 → 标签打印模板中新建并绑定一级款式模板。' )
const copyMax = computed(() => Math.max(0, 100 - entries.value.length))
const filteredEntries = computed(() => {
  const keyword = cardKeyword.value.trim().toLowerCase()
  if (!keyword) return entries.value
  return entries.value.filter((item) => [item.goodsName, item.goodsNo, item.remark, item.styleItemName, item.materialName, item.metalName, item.certTypeName].some((value) => String(value || '').toLowerCase().includes(keyword)))
})
const allFilteredEntriesSelected = computed(() => filteredEntries.value.length > 0 && filteredEntries.value.every((item) => selectedEntryKeys.value.includes(item._key)))
const entrySelectionIndeterminate = computed(() => filteredEntries.value.some((item) => selectedEntryKeys.value.includes(item._key)) && !allFilteredEntriesSelected.value)

function emptyHeader() {
  return { id: 0, inboundNo: '', inboundTime: todayText(), periodText: '', status: 0, supplierId: null, supplierName: '', remark: '' }
}
function emptyLine() {
  return {
    goodsName: '', costPrice: 0, salePrice: 0, packagingFee: 0, imageFileId: null, imageUrl: '', remark: '',
    qty: 1, unit: '件', weight: null, pricingMode: 1, laborFee: 0, laborFeeMode: 1, materialGroup: 3,
    styleParentId: null, styleItemId: null, materialItemId: null, metalItemId: null, certTypeId: null,
    styleItemName: '', materialName: '', metalName: '', certTypeName: ''
  }
}
function recentWeekRange() { return serverRange(6) }
function syncDateRange() {
  query.beginTime = dateRange.value?.[0] || undefined
  query.endTime = dateRange.value?.[1] || undefined
}
function todayText() {
  return serverTodayText()
}
function newKey() {
  localKey += 1
  return `local-${Date.now()}-${localKey}`
}
function money(value) {
  return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
function number(value) {
  return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
}
function dateText(value) {
  return value ? String(value).replace('T', ' ').slice(0, 16) : '-'
}
function periodOf(value) {
  const match = String(value || '').match(/^(\d{4})-(\d{2})/)
  return match ? `${match[1]}-${match[2]}` : '-'
}
function optionName(list, id) {
  return list.find((item) => Number(item.id) === Number(id))?.itemName || ''
}
function parentIdOfStyle(styleItemId) {
  return options.categories.find((item) => Number(item.id) === Number(styleItemId))?.parentId || null
}
function changeStyleParent() {
  const firstChild = styleChildren.value[0] || null
  lineForm.styleItemId = firstChild?.id || null
  lineForm.styleItemName = firstChild?.itemName || ''
  if (firstChild) nextTick(() => lineFormRef.value?.clearValidate?.('styleItemId'))
}
function entryAttributeTags(item) {
  const pricing = Number(item.pricingMode) === 2 ? '按克计价' : '按件计价'
  const weight = item.weight ? '银重 ' + number(item.weight) + ' g' : ''
  const laborFee = Number(item.laborFee || 0)
    ? (Number(item.laborFeeMode) === 2 ? '工费 ' + number(item.laborFee) + ' 元/克' : '工费 ' + number(item.laborFee) + ' 元/件')
    : ''
  return [pricing, weight, laborFee, item.styleItemName, item.materialName, item.metalName, item.certTypeName].filter(Boolean)
}
let optionsPromise = null
function loadOptions(force = false) {
  if (optionsPromise && !force) return optionsPromise
  optionsLoading.value = true
  optionsPromise = getInboundOptions().then((res) => {
    const data = res.data || {}
    options.suppliers = data.suppliers || []
    options.categories = Array.isArray(data.categories) ? data.categories : []
    options.materials = data.materials || []
    options.metals = data.metals || []
    options.certTypes = data.certTypes || []
    return options.categories.length > 0
  }).catch((error) => {
    proxy.$modal.msgWarning(error?.message || '商品属性选项加载失败，请检查网络后重试')
    return false
  }).finally(() => {
    optionsLoading.value = false
    optionsPromise = null
  })
  return optionsPromise
}
async function ensureStyleOptions(visible) {
  if (!visible || optionsLoading.value || styleParents.value.length || !lineDialogOpen.value) return
  // 首次请求偶发超时后，打开下拉时自动补一次，不要求用户刷新整页。
  await loadOptions(true)
  if (!styleParents.value.length && !optionsLoading.value) proxy.$modal.msgWarning('款式大类仍为空，请确认数据字典中有启用的一级款式分类')
}

watch([header, entries], () => {
  if (viewMode.value === 'editor' && !hydrating.value && !readOnly.value) dirty.value = true
}, { deep: true })
watch(cardKeyword, () => { selectedEntryKeys.value = [] })

function loadList() {
  loading.value = true
  syncDateRange()
  listInbound(query).then((res) => {
    rows.value = res.data?.result || []
    total.value = res.data?.totalNum || 0
  }).finally(() => { loading.value = false })
}
function refreshAll() {
  if (activeSection.value === 'goods') {
    goodsRef.value?.refresh?.()
    return
  }
  loadOptions()
  loadList()
}
function switchSection(section) {
  if (section === 'goods' && !canViewGoods.value) {
    proxy.$modal.msgWarning('当前账号没有商品档案查看权限')
    return
  }
  if (section === activeSection.value) return
  if (viewMode.value === 'editor' && !ensureCanLeave()) return
  if (section === 'goods' && viewMode.value === 'editor') {
    viewMode.value = 'list'
    refreshAll()
  }
  activeSection.value = section
  const query = { ...route.query }
  if (section === 'goods') query.view = 'goods'
  else delete query.view
  router.replace({ path: route.path, query })
}
function syncSectionFromRoute() {
  const requested = route.query.view === 'goods' ? 'goods' : 'inbound'
  const section = requested === 'goods' && !canViewGoods.value ? 'inbound' : requested
  if (section !== activeSection.value) activeSection.value = section
  if (requested !== section) router.replace({ path: route.path, query: {} })
}
watch(() => route.query.view, syncSectionFromRoute)
function search() {
  query.pageNum = 1
  loadList()
}
function openCreate() {
  if (!ensureCanLeave()) return
  hydrating.value = true
  viewMode.value = 'editor'
  Object.assign(header, emptyHeader())
  entries.value = []
  cardKeyword.value = ''
  selectedEntryKeys.value = []
  resetLine()
  nextTick(() => {
    hydrating.value = false
    dirty.value = false
    lineDialogOpen.value = true
    nextTick(() => goodsNameInput.value?.focus?.())
  })
}
async function openOrder(row) {
  if (!ensureCanLeave()) return
  hydrating.value = true
  try {
    const res = await getInbound(row.id)
    applyDetail(res.data)
    viewMode.value = 'editor'
    cardKeyword.value = ''
    resetLine()
    nextTick(() => { hydrating.value = false; dirty.value = false })
  } finally {
    hydrating.value = false
  }
}
function isEntrySelected(item) {
  return selectedEntryKeys.value.includes(item._key)
}
function setEntrySelected(item, checked) {
  const key = item._key
  if (checked) {
    if (!selectedEntryKeys.value.includes(key)) selectedEntryKeys.value.push(key)
  } else {
    selectedEntryKeys.value = selectedEntryKeys.value.filter((itemKey) => itemKey !== key)
  }
}
function toggleAllEntries(checked) {
  const visibleKeys = filteredEntries.value.map((item) => item._key)
  if (checked) selectedEntryKeys.value = Array.from(new Set([...selectedEntryKeys.value, ...visibleKeys]))
  else selectedEntryKeys.value = selectedEntryKeys.value.filter((key) => !visibleKeys.includes(key))
}
function applyDetail(data) {
  selectedEntryKeys.value = []
  Object.assign(header, {
    id: data.id,
    inboundNo: data.inboundNo,
    inboundTime: dateInput(data.inboundTime),
    periodText: data.periodText,
    status: data.status,
    supplierId: data.supplierId || null,
    supplierName: data.supplierName || '',
    remark: data.remark || ''
  })
  entries.value = (data.lines || []).map((item) => ({
    _key: `saved-${item.id}`,
    id: item.id,
    goodsNo: item.goodsNo,
    goodsName: item.goodsName || item.styleName || '',
    qty: Number(item.qty || 1),
    costPrice: Number(item.costPrice || 0),
    salePrice: Number(item.salePrice || 0),
    packagingFee: Number(item.packagingFee || 0),
    pricingMode: Number(item.pricingMode || 1),
    laborFee: Number(item.laborFee || 0),
    laborFeeMode: Number(item.laborFeeMode || 1),
    materialGroup: Number(item.materialGroup || 3),
    unit: item.unit || '件',
    weight: item.weight ?? null,
    imageFileId: item.imageFileId || null,
    imageUrl: item.imageUrl || '',
    remark: item.remark || '',
    styleParentId: parentIdOfStyle(item.styleItemId),
    styleItemId: item.styleItemId || null,
    materialItemId: item.materialItemId || null,
    metalItemId: item.metalItemId || null,
    certTypeId: item.certTypeId || null,
    styleItemName: item.styleItemName || '',
    materialName: item.materialName || '',
    metalName: item.metalName || '',
    certTypeName: item.certTypeName || ''
  }))
}
function dateInput(value) {
  return value ? String(value).replace('T', ' ').slice(0, 10) : todayText()
}
function toApiDateTime(value) {
  const date = String(value || '').slice(0, 10)
  return /^\d{4}-\d{2}-\d{2}$/.test(date) ? `${date}T00:00:00` : value
}
function resetLine() {
  editingKey.value = ''
  Object.assign(lineForm, emptyLine())
  nextTick(() => lineFormRef.value?.clearValidate?.())
}
function openLineDialog(item = null) {
  if (readOnly.value) return
  if (item) editLine(item)
  else {
    resetLine()
    lineDialogOpen.value = true
    nextTick(() => goodsNameInput.value?.focus?.())
  }
}
function handleLineDialogClosed() {
  resetLine()
}
function lineHasInput() {
  return Boolean(lineForm.goodsName?.trim() || lineForm.remark?.trim() || lineForm.imageUrl || lineForm.styleItemId
    || lineForm.materialItemId || lineForm.metalItemId || lineForm.certTypeId || Number(lineForm.costPrice) || Number(lineForm.salePrice))
}
async function cancelLineDialog() {
  if (!lineHasInput()) {
    lineDialogOpen.value = false
    return
  }
  try {
    await ElMessageBox.confirm('当前货品信息尚未加入本单，取消会丢失这次录入，是否继续？', '未保存货品', {
      type: 'warning', confirmButtonText: '放弃录入', cancelButtonText: '继续录入'
    })
    lineDialogOpen.value = false
  } catch (error) {}
}
async function beforeCloseLineDialog(done) {
  if (!lineHasInput()) return done()
  try {
    await ElMessageBox.confirm('当前货品信息尚未加入本单，关闭会丢失这次录入，是否继续？', '未保存货品', {
      type: 'warning', confirmButtonText: '放弃录入', cancelButtonText: '继续录入'
    })
    done()
  } catch (error) {}
}
function editLine(item) {
  if (readOnly.value) return
  lineDialogOpen.value = true
  editingKey.value = item._key
  Object.assign(lineForm, {
    goodsName: item.goodsName,
    qty: Number(item.qty || 1),
    costPrice: Number(item.costPrice || 0),
    salePrice: Number(item.salePrice || 0),
    packagingFee: Number(item.packagingFee || 0),
    pricingMode: Number(item.pricingMode || 1),
    laborFee: Number(item.laborFee || 0),
    laborFeeMode: Number(item.laborFeeMode || 1),
    materialGroup: Number(item.materialGroup || 3),
    unit: item.unit || '件',
    weight: item.weight ?? null,
    imageFileId: item.imageFileId || null,
    imageUrl: item.imageUrl || '',
    remark: item.remark || '',
    styleParentId: item.styleParentId || parentIdOfStyle(item.styleItemId),
    styleItemId: item.styleItemId || null,
    materialItemId: item.materialItemId || null,
    metalItemId: item.metalItemId || null,
    certTypeId: item.certTypeId || null,
    styleItemName: item.styleItemName || '',
    materialName: item.materialName || '',
    metalName: item.metalName || '',
    certTypeName: item.certTypeName || ''
  })
  nextTick(() => goodsNameInput.value?.focus?.())
}
function commitLine(silentWhenEmpty = false) {
  const hasContent = lineForm.goodsName.trim() || lineForm.remark.trim() || lineForm.imageUrl || lineForm.styleItemId || lineForm.materialItemId || lineForm.metalItemId || lineForm.certTypeId || Number(lineForm.costPrice) || Number(lineForm.salePrice)
  if (!hasContent && silentWhenEmpty) return Promise.resolve(true)
  return new Promise((resolve) => {
    lineFormRef.value?.validate((valid) => {
      if (!valid) return resolve(false)
      if (Number(lineForm.costPrice) < 0 || Number(lineForm.salePrice) < 0) {
        proxy.$modal.msgWarning('成本价和售价不能为负数')
        return resolve(false)
      }
      if (!Number.isFinite(Number(lineForm.qty)) || Number(lineForm.qty) <= 0) {
        proxy.$modal.msgWarning('本件内含数量必须大于 0')
        return resolve(false)
      }
      if (Number(lineForm.pricingMode) === 2 && (!Number(lineForm.weight) || Number(lineForm.weight) <= 0)) {
        proxy.$modal.msgWarning('按克计价必须填写重量')
        return resolve(false)
      }
      const value = {
        goodsName: lineForm.goodsName.trim(),
        qty: Number(Number(lineForm.qty).toFixed(2)),
        costPrice: Number(lineForm.costPrice || 0),
        salePrice: Number(lineForm.salePrice || 0),
        pricingMode: Number(lineForm.pricingMode || 1),
        laborFee: Number(lineForm.laborFee || 0),
        laborFeeMode: Number(lineForm.pricingMode === 2 ? 2 : 1),
        packagingFee: Number(lineForm.packagingFee || 0),
        materialGroup: Number(lineForm.materialGroup || 3),
        unit: lineForm.unit || '件',
        weight: lineForm.weight ?? null,
        imageFileId: lineForm.imageFileId || null,
        imageUrl: lineForm.imageUrl || '',
        remark: lineForm.remark.trim(),
        styleParentId: lineForm.styleParentId || null,
        styleItemId: lineForm.styleItemId || null,
        materialItemId: lineForm.materialItemId || null,
        metalItemId: lineForm.metalItemId || null,
        certTypeId: lineForm.certTypeId || null,
        styleItemName: optionName(options.categories, lineForm.styleItemId),
        materialName: optionName(options.materials, lineForm.materialItemId),
        metalName: optionName(options.metals, lineForm.metalItemId),
        certTypeName: optionName(options.certTypes, lineForm.certTypeId)
      }
      if (editingKey.value) {
        const index = entries.value.findIndex((item) => item._key === editingKey.value)
        if (index >= 0) entries.value[index] = { ...entries.value[index], ...value }
      } else {
        entries.value.push({ _key: newKey(), id: 0, goodsNo: '', ...value })
      }
      lineDialogOpen.value = false
      resolve(true)
    })
  })
}
async function removeEntry(item) {
  try {
    if (item.id) {
      await ElMessageBox.confirm('从本单移除这件货品？保存草稿后才会正式删除，货品编码不会被回收。', '移除货品', {
        type: 'warning', confirmButtonText: '移除', cancelButtonText: '取消'
      })
    }
    entries.value = entries.value.filter((it) => it._key !== item._key)
    selectedEntryKeys.value = selectedEntryKeys.value.filter((key) => key !== item._key)
    if (editingKey.value === item._key) resetLine()
  } catch (error) {}
}
async function removeSelectedEntries() {
  if (readOnly.value || !selectedEntryKeys.value.length) return
  const selectedSet = new Set(selectedEntryKeys.value)
  const selectedItems = entries.value.filter((item) => selectedSet.has(item._key))
  if (!selectedItems.length) return
  const hasSaved = selectedItems.some((item) => item.id)
  try {
    await ElMessageBox.confirm(
      hasSaved
        ? '删除已选 ' + selectedItems.length + ' 件货品？其中已保存的货品会在保存草稿后正式删除，货品编码不会被回收。'
        : '删除已选 ' + selectedItems.length + ' 件货品？',
      '批量删除货品',
      { type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消' }
    )
    entries.value = entries.value.filter((item) => !selectedSet.has(item._key))
    if (editingKey.value && selectedSet.has(editingKey.value)) resetLine()
    selectedEntryKeys.value = []
    proxy.$modal.msgSuccess('已删除 ' + selectedItems.length + ' 件货品')
  } catch (error) {}
}
function openCopyDialog(item) {
  if (readOnly.value) return
  if (copyMax.value <= 0) {
    proxy.$modal.msgWarning('单张入库单最多 100 件货品')
    return
  }
  copySource.value = item
  copyForm.copies = Math.min(1, copyMax.value)
  copyDialogOpen.value = true
}
function confirmCopy() {
  if (!copySource.value || copyMax.value <= 0) return
  const count = Math.max(1, Math.min(Number(copyForm.copies) || 1, copyMax.value))
  const source = copySource.value
  const copies = Array.from({ length: count }, () => ({
    _key: newKey(),
    id: 0,
    goodsNo: '',
    goodsName: source.goodsName,
    qty: Number(source.qty || 1),
    costPrice: Number(source.costPrice || 0),
    salePrice: Number(source.salePrice || 0),
    packagingFee: Number(source.packagingFee || 0),
    pricingMode: Number(source.pricingMode || 1),
    laborFee: Number(source.laborFee || 0),
    laborFeeMode: Number(source.laborFeeMode || 1),
    materialGroup: Number(source.materialGroup || 3),
    unit: source.unit || '件',
    weight: source.weight ?? null,
    imageFileId: source.imageFileId || null,
    imageUrl: source.imageUrl || '',
    remark: source.remark || '',
    styleParentId: source.styleParentId || parentIdOfStyle(source.styleItemId),
    styleItemId: source.styleItemId || null,
    materialItemId: source.materialItemId || null,
    metalItemId: source.metalItemId || null,
    certTypeId: source.certTypeId || null,
    styleItemName: source.styleItemName || '',
    materialName: source.materialName || '',
    metalName: source.metalName || '',
    certTypeName: source.certTypeName || ''
  }))
  entries.value.push(...copies)
  copyDialogOpen.value = false
  copySource.value = null
  proxy.$modal.msgSuccess(`已复制 ${count} 件，保存草稿后按顺序生成编码`)
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
function uploadLineImage(options) {
  const data = new FormData()
  data.append('file', options.file)
  data.append('fileNameType', '1')
  imageUploading.value = true
  upload(data).then((res) => {
    const file = res.data || {}
    if (!file.fileId || !file.url) throw new Error('上传结果不完整')
    lineForm.imageFileId = String(file.fileId)
    lineForm.imageUrl = file.url
    options.onSuccess(file)
  }).catch((error) => {
    options.onError(error)
  }).finally(() => { imageUploading.value = false })
}
function clearLineImage() {
  lineForm.imageFileId = null
  lineForm.imageUrl = ''
}

function buildPayload() {
  return {
    id: header.id,
    inboundTime: toApiDateTime(header.inboundTime),
    sourceType: 1,
    supplierId: header.supplierId || null,
    remark: header.remark.trim(),
    lines: entries.value.map((item) => ({
      id: item.id || 0,
      goodsName: item.goodsName,
      qty: Number(item.qty || 1),
      costPrice: Number(item.costPrice || 0),
      salePrice: Number(item.salePrice || 0),
      packagingFee: Number(item.packagingFee || 0),
      pricingMode: Number(item.pricingMode || 1),
      laborFee: Number(item.laborFee || 0),
      laborFeeMode: Number(item.laborFeeMode || 1),
      materialGroup: Number(item.materialGroup || 3),
      unit: item.unit || '件',
      weight: item.weight ?? null,
      imageFileId: item.imageFileId || null,
      remark: item.remark || '',
      styleItemId: item.styleItemId || null,
      materialItemId: item.materialItemId || null,
      metalItemId: item.metalItemId || null,
      certTypeId: item.certTypeId || null
    }))
  }
}
async function persist(notify = true) {
  if (!header.inboundTime) {
    proxy.$modal.msgWarning('请选择入库日期')
    return 0
  }
  const committed = await commitLine(true)
  if (!committed) return 0
  saving.value = true
  try {
    const res = await saveInbound(buildPayload())
    const id = Number(res.data || 0)
    if (!id) throw new Error('未返回入库单编号')
    const detail = await getInbound(id)
    applyDetail(detail.data)
    resetLine()
    await nextTick()
    dirty.value = false
    if (notify) proxy.$modal.msgSuccess('入库草稿已保存')
    return id
  } finally {
    saving.value = false
  }
}
async function saveDraft() {
  const id = await persist(true)
  if (!id) return
  dirty.value = false
  viewMode.value = 'list'
  refreshAll()
}
async function saveAndContinue() {
  const id = await persist(true)
  if (!id) return
  cardKeyword.value = ''
  lineDialogOpen.value = true
  nextTick(() => goodsNameInput.value?.focus?.())
}
async function completeInbound() {
  if (!entries.value.length) {
    proxy.$modal.msgWarning('请至少录入一件货品后再完成入库')
    return
  }
  confirming.value = true
  try {
    const id = await persist(false)
    if (!id) return
    await ElMessageBox.confirm(`确认完成这张入库单？${entries.value.length} 件货品将进入商品档案和库存流水，完成后再也不能修改。`, '完成入库', {
      type: 'warning', confirmButtonText: '确认完成', cancelButtonText: '再检查一下'
    })
    await confirmInbound(id)
    proxy.$modal.msgSuccess('入库已完成，商品档案已自动生成')
    dirty.value = false
    viewMode.value = 'list'
    activeSection.value = 'goods'
    router.replace({ path: route.path, query: { ...route.query, view: 'goods' } })
    refreshAll()
  } catch (error) {
  } finally {
    confirming.value = false
  }
}
async function confirmRow(row) {
  try {
    await ElMessageBox.confirm(`确认完成「${row.inboundNo}」？完成后货品会进入商品档案，单据不可修改。`, '完成入库', {
      type: 'warning', confirmButtonText: '确认完成', cancelButtonText: '取消'
    })
    await confirmInbound(row.id)
    proxy.$modal.msgSuccess('入库已完成')
    refreshAll()
  } catch (error) {}
}
async function openPrintDialog(row) {
  printHeader.id = row.id
  printHeader.inboundNo = row.inboundNo || ''
  printRows.value = []
  selectedPrintKeys.value = []
  printDialogOpen.value = true
  await loadPrintRows()
}
async function loadPrintRows() {
  if (!printHeader.id) return
  printLoading.value = true
  try {
    const [res, printRes, optionsRes] = await Promise.all([
      getInbound(printHeader.id),
      getPrintOptions(),
      options.categories.length ? Promise.resolve(null) : getInboundOptions()
    ])
    if (optionsRes) {
      const data = optionsRes.data || {}
      options.suppliers = data.suppliers || []
      options.categories = data.categories || []
      options.materials = data.materials || []
      options.metals = data.metals || []
      options.certTypes = data.certTypes || []
    }
    const printData = printRes.data || {}
    printTemplates.value = printData.templates || []
    printBindings.value = printData.bindings || []
    const lines = res.data?.lines || []
    printRows.value = lines.filter((item) => item.id && String(item.goodsNo || '').trim()).map((item) => ({
      id: item.id,
      goodsNo: String(item.goodsNo).trim(),
      goodsName: item.goodsName || item.styleName || '未命名货品',
      salePrice: Number(item.salePrice || 0),
      styleItemId: item.styleItemId || null,
      rootStyleItemId: parentIdOfStyle(item.styleItemId) || item.styleItemId || null
    }))
    const firstRootId = printRows.value.find((item) => item.rootStyleItemId)?.rootStyleItemId
    const boundTemplateId = printBindings.value.find((item) => Number(item.styleItemId) === Number(firstRootId))?.templateId
    printTemplateId.value = availablePrintTemplates.value.find((item) => Number(item.id) === Number(boundTemplateId))?.id
      || availablePrintTemplates.value[0]?.id
      || null
    selectedPrintKeys.value = printRows.value.map((item) => String(item.id))
  } catch (error) {
    printRows.value = []
    selectedPrintKeys.value = []
  } finally {
    printLoading.value = false
  }
}
function toggleAllPrintRows(checked) {
  selectedPrintKeys.value = checked ? printRows.value.map((item) => String(item.id)) : []
}
async function printSelected() {
  const rowsToPrint = printRows.value.filter((item) => selectedPrintKeys.value.includes(String(item.id)))
  if (!rowsToPrint.length) return proxy.$modal.msgWarning('请至少选择一件货品')
  if (!printTemplateId.value) return proxy.$modal.msgWarning('请先选择标签打印模板')
  printing.value = true
  let sent = 0
  try {
    for (const item of rowsToPrint) {
      const renderRes = await renderPrintLabel({ inboundLineId: Number(item.id), templateId: Number(printTemplateId.value) })
      const label = renderRes.data?.label
      if (!label) throw new Error('后端没有返回可打印的标签数据')
      await agentPrint({
        name: String(label.name || item.goodsName || '珠宝货品').slice(0, 30),
        sku: String(label.sku || item.goodsNo || '').slice(0, 32),
        price: String(label.price || Number(item.salePrice || 0).toFixed(2)).slice(0, 16),
        barcode: String(label.barcode || '').slice(0, 32),
        widthMm: Math.round(Number(label.widthMm || 50)),
        heightMm: Math.round(Number(label.heightMm || 30)),
        copies: Number(label.copies || 1),
        port: label.port || 'USB,',
        speed: Number(label.speed || 4),
        density: Number(label.density ?? 8),
        gapMm: Number(label.gapMm ?? 2),
        offsetMm: Number(label.offsetMm ?? 4),
        elements: label.elements || [],
        data: label.data || {},
        fields: label.fields || []
      })
      sent += 1
    }
    proxy.$modal.msgSuccess(`打印指令已发送 ${sent} 件，请检查标签机出纸情况`)
  } catch (error) {
    const message = String(error?.message || error || '')
    if (message === 'Failed to fetch' || error instanceof TypeError) {
      proxy.$modal.msgError('无法连接本机标签打印代理。请启动代理，检查 USB 打印机，并确认浏览器没有拦截跨域或混合内容。')
    } else {
      proxy.$modal.msgError(message || '标签打印失败')
    }
  } finally {
    printing.value = false
  }
}
async function removeRow(row) {
  try {
    await ElMessageBox.confirm(`删除草稿「${row.inboundNo}」？已生成的货品编码会保留追溯记录，但不会进入商品档案。`, '删除草稿', {
      type: 'warning', confirmButtonText: '删除', cancelButtonText: '取消'
    })
    await deleteInbound(row.id)
    proxy.$modal.msgSuccess('草稿已删除')
    refreshAll()
  } catch (error) {}
}
async function leaveEditor() {
  if (!readOnly.value && dirty.value) {
    try {
      await ElMessageBox.confirm('当前草稿还有未保存的修改，离开后将丢失这些内容。', '离开录入工作台', {
        type: 'warning', confirmButtonText: '放弃修改', cancelButtonText: '继续录入'
      })
    } catch (error) {
      return
    }
  }
  dirty.value = false
  viewMode.value = 'list'
  refreshAll()
}
function ensureCanLeave() {
  if (viewMode.value !== 'editor' || !dirty.value || readOnly.value) return true
  proxy.$modal.msgWarning('当前入库单有未保存内容，请先保存；红色提示消失后才能切换功能')
  return false
}
function handleBeforeUnload(event) {
  if (viewMode.value === 'editor' && dirty.value) {
    event.preventDefault()
    event.returnValue = ''
  }
}

onBeforeRouteLeave(async (to, from, next) => {
  if (viewMode.value !== 'editor' || !dirty.value) return next()
  try {
    await ElMessageBox.confirm('当前入库草稿尚未保存，确定离开吗？', '未保存提示', {
      type: 'warning', confirmButtonText: '离开', cancelButtonText: '留在本页'
    })
    dirty.value = false
    next()
  } catch (error) {
    next(false)
  }
})
onMounted(() => {
  refreshAll()
  window.addEventListener('beforeunload', handleBeforeUnload)
})
onBeforeUnmount(() => window.removeEventListener('beforeunload', handleBeforeUnload))
</script>
<style scoped>
.jy-inbound { --rose-950: #3d2730; --rose-900: #52323b; --rose-800: #6d414d; --rose-700: #8d5563; --rose-600: #a96273; --rose-500: #bd7b8a; --rose-300: #e4bfc7; --rose-200: #efd8dc; --rose-100: #f8eaed; --rose-50: #fdf7f7; --gold: #bfa069; width: 100%; max-width: 100%; min-width: 0; min-height: 100%; padding: 22px; overflow-x: hidden; color: var(--rose-950); background: radial-gradient(circle at 92% 0%, #fff4f5 0, transparent 32%), linear-gradient(180deg, #fffafb 0, #fdf8f8 100%); }
.jy-inbound button { font-family: inherit; }
.jy-inbound.is-editor-mode { display: flex; flex-direction: column; height: 100%; min-height: 0; padding: 16px 18px 10px; overflow: hidden; }
.jy-inbound.is-editor-mode .editor-view { flex: 1 1 auto; height: auto; min-height: 0; }
:global(.app-main:has(.jy-inbound.is-editor-mode)) { padding-bottom: 0; overflow-y: hidden; }
:global(.app-main:has(.jy-inbound.is-editor-mode) .route-switch-shell) { height: 100%; min-height: 0; padding-bottom: 0; overflow: hidden; }
.view-fade-enter-active, .view-fade-leave-active { transition: opacity .18s ease, transform .18s ease; }
.view-fade-enter-from { opacity: 0; transform: translateY(6px); }
.view-fade-leave-to { opacity: 0; transform: translateY(-4px); }
.eyebrow, .section-kicker { display: block; color: var(--rose-500); font-size:11.5px; font-weight: 700; letter-spacing: 2px; }
.page-hero { position: relative; display: flex; align-items: center; justify-content: space-between; gap: 20px; min-height: 104px; padding: 16px 22px; overflow: hidden; color: #4b3540; background: linear-gradient(112deg, #fffafb 0%, #fdf2f5 62%, #f7e4e9 100%); border: 1px solid #efdce2; border-radius: 17px; box-shadow: 0 8px 22px rgba(196,120,138,.07); }
.hero-copy { position: relative; z-index: 2; max-width: 710px; }
.page-hero .eyebrow { color: #b56f80; }
.page-hero h1 { margin: 4px 0 3px; color: #4b3540; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 23px; font-weight: 500; letter-spacing: .055em; }
.page-hero p { max-width: 660px; margin: 0; color: #92737d; font-size: 11px; line-height: 1.55; }
.section-switch { display: inline-flex; gap: 4px; margin-top: 8px; padding: 3px; background: #fff7f9; border: 1px solid #efd8de; border-radius: 11px; }
.section-switch button { display: grid; grid-template-columns: 17px auto; grid-template-rows: auto auto; column-gap: 6px; min-width: 116px; padding: 6px 10px; color: #9a7f88; background: transparent; border: 0; border-radius: 8px; text-align: left; cursor: pointer; transition: .2s ease; }
.section-switch button:hover { color: #8f5362; background: #fdf0f3; }
.section-switch button.active { color: #764451; background: #fff; box-shadow: 0 3px 10px rgba(196,120,138,.13); }
.section-switch .el-icon { grid-row: 1 / 3; align-self: center; font-size: 15px; }
.section-switch span { font-size:12px; font-weight: 650; letter-spacing: .3px; }
.section-switch small { font-size:11px; opacity: .7; }
.hero-chips { display: flex; gap: 7px; margin-top: 9px; }
.hero-chips span { display: inline-flex; align-items: center; gap: 5px; padding: 4px 9px; color: #956b77; background: #fff7f9; border: 1px solid #efd8de; border-radius: 999px; font-size:11.5px; }
.hero-actions { position: relative; z-index: 2; display: flex; gap: 9px; flex: none; }
.hero-actions :deep(.el-button) { border-color: #e6cbd2; color: #875563; background: #fffafb; }
.hero-actions :deep(.el-button:hover) { background: #fdf0f3; border-color: #d9adb8; color: #7b4757; }
.hero-actions :deep(.primary-action) { color: #fff; background: #c4788a; border-color: #c4788a; font-weight: 650; box-shadow: 0 5px 13px rgba(196,120,138,.22); }
.hero-ornament { position: absolute; right: 5%; top: -150px; width: 320px; height: 320px; border: 1px solid rgba(196,120,138,.1); border-radius: 50%; }
.hero-ornament::before, .hero-ornament::after { content: ''; position: absolute; inset: 45px; border: 1px solid rgba(196,120,138,.08); border-radius: 50%; }
.hero-ornament::after { inset: 95px; }
.hero-ornament i { position: absolute; width: 8px; height: 8px; background: #d9a6b2; border-radius: 50%; box-shadow: 0 0 18px rgba(217,166,178,.4); }
.hero-ornament i:nth-child(1) { left: 18px; top: 108px; }
.hero-ornament i:nth-child(2) { right: 55px; top: 42px; width: 5px; height: 5px; }
.hero-ornament i:nth-child(3) { right: 128px; bottom: 22px; width: 6px; height: 6px; }
.order-panel { margin-top: 12px; overflow: hidden; background: rgba(255,255,255,.94); border: 1px solid #efdee1; border-radius: 20px; box-shadow: 0 12px 30px rgba(103,58,70,.055); }
.summary-list-panel { padding: 26px 26px 12px; }
.section-head { display: flex; align-items: flex-end; justify-content: space-between; gap: 24px; padding-bottom: 18px; border-bottom: 1px solid #f4e7ea; }
.section-head h2 { margin: 5px 0 4px; color: #573b44; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 22px; font-weight: 500; }
.section-head p { margin: 0; color: #aa9299; font-size:12px; }
.filters { display: flex; align-items: center; justify-content: flex-end; flex: 1 1 380px; flex-wrap: wrap; gap: 9px; min-width: 0; }
.filters :deep(.el-input) { flex: 1 1 170px; width: auto; min-width: 140px; }
.filters :deep(.el-date-editor) { flex: 0 1 240px; width: auto; min-width: 210px; }
.filters :deep(.el-select) { flex: 0 1 120px; width: auto; min-width: 100px; }
.filters :deep(.el-input__wrapper), .filters :deep(.el-select__wrapper) { background: #fffafb; box-shadow: 0 0 0 1px #f0dee1 inset; }
.order-list { margin-top: 2px; }
.order-row { display: flex; align-items: center; gap: 12px; min-height: 84px; padding: 0 16px 0 0; border-bottom: 1px solid #f5eaec; transition: background .18s ease, transform .18s ease; }
.order-row:last-child { border-bottom: 0; }
.order-row:hover { background: #fffafb; }
.order-row.drafting { background: linear-gradient(90deg, rgba(253,242,244,.72), rgba(255,255,255,0) 52%); }
.order-main { display: grid; grid-template-columns: 3px minmax(210px, 1fr) 82px 112px 112px auto; align-items: center; flex: 1; gap: 18px; min-width: 0; min-height: 82px; padding: 12px 16px; color: inherit; background: transparent; border: 0; text-align: left; cursor: pointer; }
.order-status-line { width: 3px; height: 42px; background: #d8bdc4; border-radius: 999px; }
.drafting .order-status-line { background: linear-gradient(180deg, #b86d7e, #d9a8b3); box-shadow: 0 0 0 4px rgba(184,109,126,.08); }
.order-copy { display: flex; flex-direction: column; gap: 7px; min-width: 0; }
.order-no { overflow: hidden; color: #583b44; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 600; letter-spacing: .025em; text-overflow: ellipsis; white-space: nowrap; }
.order-sub { overflow: hidden; color: #ad959b; font-size:11.5px; text-overflow: ellipsis; white-space: nowrap; }
.supplier-chip { display: inline-block; margin-right: 8px; padding: 2px 7px; color: #9c5968; background: #fbeef1; border: 1px solid #f2dce1; border-radius: 999px; font-size: 11px; line-height: 1.45; vertical-align: 1px; }
.order-sub { display: flex; align-items: center; gap: 7px; min-width: 0; overflow: hidden; }
.order-sub .supplier-chip { flex: none; max-width: 145px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.order-remark { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.order-stat, .order-date { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.order-stat b, .order-date b { overflow: hidden; color: #74545d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.order-stat small, .order-date small { color: #b39da2; font-size:11px; white-space: nowrap; }
.order-actions { display: flex; align-items: center; gap: 2px; flex: none; }
.order-actions :deep(.el-button + .el-button) { margin-left: 2px; }
.continue-action { color: #9b5868 !important; border-color: #dfbcc4 !important; background: #fff9fa !important; }
.dirty-state { display: inline-flex; align-items: center; gap: 5px; color: #ab969b; font-size:11.5px; }
.dirty-state i { width: 5px; height: 5px; background: #c3b2b6; border-radius: 50%; }
.dirty-state.active { color: #a45c6d; }
.dirty-state.active i { background: #b45f73; box-shadow: 0 0 0 4px rgba(180,95,115,.1); }
.editor-view { display: flex; flex-direction: column; width: 100%; max-width: 100%; min-width: 0; height: calc(100vh - var(--base-header-height) - var(--base-tags-height) - var(--base-footer-height) - 84px); height: calc(100dvh - var(--base-header-height) - var(--base-tags-height) - var(--base-footer-height) - 84px); min-height: 0; overflow: hidden; }
.editor-topbar { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 12px 18px; margin-bottom: 14px; }
.editor-title { display: flex; align-items: center; gap: 12px; }
.back-button { display: grid; place-items: center; width: 38px; height: 38px; color: #7d5a64; background: #fff; border: 1px solid #edd8dc; border-radius: 12px; cursor: pointer; transition: .18s; }
.back-button:hover { color: #a75f70; border-color: #dbb7bf; background: #fff9fa; }
.title-line { display: flex; align-items: center; gap: 10px; }
.editor-title h1 { margin: 4px 0 0; color: var(--rose-900); font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 25px; font-weight: 500; }
.editor-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.editor-actions :deep(.primary-action) { color: #fff; background: linear-gradient(135deg, #b76d7e, #955262); border-color: transparent; box-shadow: 0 7px 16px rgba(151,82,98,.2); }
.summary-band { display: grid; flex: 0 0 auto; grid-template-columns: minmax(0, 1fr) minmax(230px, 310px); gap: 1px; width: 100%; max-width: 100%; min-width: 0; overflow: hidden; background: #efdce0; border: 1px solid #efdce0; border-radius: 17px; box-shadow: 0 8px 22px rgba(103,58,70,.04); }
.date-lock-hint { display: flex; align-items: center; gap: 4px; color: #a26a78; font-size:11px; line-height: 1.4; }
.summary-fields { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 10px 12px; min-width: 0; padding: 14px 17px; overflow: hidden; background: #fffdfd; }
.summary-field { display: flex; flex: 1 1 132px; flex-direction: column; gap: 6px; min-width: 0; }
.summary-field.date-field { flex: 0 1 152px; }
.summary-field.supplier-field { flex: 1 1 180px; }
.summary-field.order-field, .summary-field.period-field { flex: 0 1 118px; }
.summary-field.remark-field { flex: 2.5 1 220px; }
.summary-field > span { color: #a38c92; font-size:11.5px; letter-spacing: .5px; }
.summary-field strong { overflow: hidden; min-height: 32px; color: #5d424a; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 13px; font-weight: 500; line-height: 32px; text-overflow: ellipsis; white-space: nowrap; }
.summary-field :deep(.el-date-editor) { width: 100%; }
.summary-field :deep(.el-select) { width: 100%; }
.supplier-empty { display: block; padding: 10px 12px; color: #a68f95; font-size: 12px; line-height: 1.7; }
.summary-field :deep(.el-select__wrapper) { min-height: 32px; background: #fffafb; box-shadow: 0 0 0 1px #f0dee1 inset; }
.summary-field :deep(.el-input__wrapper) { background: #fffafb; box-shadow: 0 0 0 1px #f0dee1 inset; }
.summary-totals { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); align-items: center; background: linear-gradient(135deg, #fff8f9, #fdf1f2); }
.summary-totals > div { display: flex; flex-direction: column; gap: 5px; padding: 0 15px; border-left: 1px solid #f2e2e5; }
.summary-totals > div:first-child { border-left: 0; }
.summary-totals span { color: #a68f95; font-size:11.5px; }
.summary-totals b { color: #634951; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.summary-totals b em { color: #a58b92; font-size:11.5px; font-style: normal; }
.summary-totals b.amount { color: #a45c6c; font-size: 16px; }
.summary-totals b.pending { color: #b16b79; }
.wall-panel.full-width { display: flex; flex: 1 1 auto; flex-direction: column; width: 100%; max-width: 100%; min-height: 0; margin-top: 16px; padding: 0px 22px 22px; overflow: hidden; }
.add-line-action { color: #fff; background: linear-gradient(135deg, #bd7182, #9a5364); border-color: transparent; box-shadow: 0 6px 14px rgba(160, 83, 102, .16); }
.wall-panel { min-width: 0; padding: 22px; }
.wall-tools { display: flex; flex-wrap: wrap; align-items: center; gap: 9px; min-width: 0; }
.wall-tools :deep(.el-input) { flex: 1 1 160px; width: auto; min-width: 130px; max-width: 260px; }
.batch-delete-action { flex: none; color: #a55768; border-color: #e7c8cf; background: #fffafb; }
.batch-delete-action:not(.is-disabled):hover { color: #fff; background: #b86577; border-color: #b86577; }
.wall-tools :deep(.el-input__wrapper) { border-radius: 9px; }
.wall-count { color: #a88e95; font-size: 11.5px; white-space: nowrap; }
.goods-list { display: flex; flex: 1 1 auto; flex-direction: column; width: 100%; max-width: 100%; min-height: 0; margin-top: 14px; overflow: hidden; border: 1px solid #f1e2e5; border-radius: 12px; background: #fff; }
.goods-table-wrap { display: flex; flex: 1 1 auto; flex-direction: column; width: 100%; max-width: 100%; min-width: 0; min-height: 0; overflow: hidden; }
.goods-list-head, .goods-row { display: grid; flex: none; grid-template-columns: 34px 42px minmax(0, 1.8fr) minmax(72px, 1fr) 78px minmax(54px, .62fr) minmax(54px, .62fr) minmax(58px, .62fr) 76px; align-items: center; gap: clamp(4px, .5vw, 10px); width: 100%; max-width: 100%; min-width: 0; }
.goods-list-head { padding: 9px 12px; color: #a58c93; background: #fdf7f8; border-bottom: 1px solid #f2e4e6; font-size: 11.5px; font-weight: 600; letter-spacing: .3px; }
.select-col { display: flex; align-items: center; justify-content: center; min-width: 0; }
.index-col { color: #aa9298; font-size: 11.5px; text-align: center; white-space: nowrap; }
.row-index { color: #9c7f87; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; }
.goods-list-head .num, .goods-list-head .center { text-align: right; }
.goods-list-head .center:last-child { text-align: center; }
.wall-panel > .wall-head { flex: 0 0 auto; }
.goods-list-body { flex: 1 1 auto; min-height: 120px; overflow-y: auto; overflow-x: hidden; overscroll-behavior: contain; scrollbar-gutter: stable; }
.goods-row { padding: 9px 12px; border-bottom: 1px solid #f7ecee; cursor: pointer; transition: background .16s ease, box-shadow .16s ease; }
.goods-row:last-child { border-bottom: 0; }
.goods-row:hover { background: #fffafb; }
.goods-row.unsaved { background: linear-gradient(90deg, rgba(253,243,245,.7), rgba(255,255,255,0) 60%); }
.goods-row.selected { background: #fdf1f4; box-shadow: inset 3px 0 0 #b87384; }
.goods-row.readonly { cursor: default; }
.cell-goods { display: flex; align-items: center; gap: 9px; min-width: 0; }
.row-thumb { display: grid; place-items: center; flex: none; width: 44px; height: 44px; overflow: hidden; color: #c093a0; background: linear-gradient(145deg, #fbf0f2, #f7e4e7); border: 1px solid #f2e2e5; border-radius: 9px; }
.row-thumb :deep(.el-image) { width: 100%; height: 100%; }
.row-thumb .el-icon { font-size: 18px; }
.row-goods-copy { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.row-goods-copy b { overflow: hidden; color: #523942; font-size: 12.5px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.row-goods-copy small { overflow: hidden; color: #9c7e86; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 11px; letter-spacing: .3px; text-overflow: ellipsis; white-space: nowrap; }
.row-goods-copy small.waiting { color: #b4929b; font-style: italic; }
.row-goods-copy .row-inline-remark { color: #b09aa0; font-family: inherit; font-size: 10.5px; letter-spacing: 0; }
.cell-attrs { min-width: 0; }
.row-tags { display: flex; flex-wrap: wrap; gap: 4px; max-height: 42px; overflow: hidden; }
.row-tags :deep(.el-tag) { color: #9e6170; background: #fff8f9; border-color: #efd8dd; font-size: 10.5px; }
.cell-empty { color: #bda8ae; font-size: 11px; }
.cell-price { color: #6d4a54; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 12px; text-align: right; white-space: nowrap; }
.cell-price.sale { color: #a45c6c; font-weight: 600; }
.cell-qty { display: flex; align-items: baseline; justify-content: center; gap: 3px; white-space: nowrap; }
.cell-qty b { color: #8f5362; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 650; }
.cell-qty small { color: #aa8d95; font-size: 10.5px; }
.goods-row .center { display: flex; justify-content: flex-end; }
.cell-actions { display: flex; align-items: center; justify-content: center; gap: 2px; }
.cell-actions :deep(.el-button) { margin: 0; padding: 4px; font-size: 14px; }
.wall-empty { display: flex; flex: 1 1 auto; flex-direction: column; align-items: center; justify-content: center; min-height: 0; color: #a78e95; text-align: center; }
.empty-gem { display: grid; place-items: center; width: 62px; height: 62px; color: #bd8694; background: linear-gradient(145deg, #fff9fa, #f7e5e9); border: 1px solid #efd7dc; border-radius: 50%; font-size: 25px; }
.wall-empty h3 { margin: 13px 0 5px; color: #76545d; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 17px; font-weight: 500; }
.wall-empty p { max-width: 300px; margin: 0; font-size: 12px; line-height: 1.7; }

.line-dialog-header, .line-dialog-title, .line-dialog-status, .line-dialog-footer { display: flex; align-items: center; min-width: 0; }
.line-dialog-header { justify-content: space-between; gap: 18px; }
.line-dialog-title { gap: 12px; }
.line-dialog-title h2 { margin: 2px 0 3px; color: #53363f; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 22px; font-weight: 600; }
.line-dialog-title p { margin: 0; color: #a48b92; font-size: 12px; }
.line-dialog-status { flex: none; gap: 9px; }
.dialog-code { max-width: 310px; overflow: hidden; padding: 6px 10px; color: #8d5362; background: #fff6f8; border: 1px solid #efd8dd; border-radius: 8px; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 11.5px; text-overflow: ellipsis; white-space: nowrap; }
.line-dialog-body { background: linear-gradient(180deg, #fffafb 0, #fff 240px); }
.line-dialog-body, .line-dialog-footer { width: min(1440px, 100%); margin: 0 auto; }
.dialog-line-form { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); gap: 14px; margin: 0; align-items: stretch; }
.entry-section { min-width: 0; padding: 16px 18px; background: rgba(255,255,255,.96); border: 1px solid #efdee1; border-radius: 14px; box-shadow: 0 6px 18px rgba(103,58,70,.035); }
.media-section { grid-column: 1 / -1; }
.attribute-section, .remark-section { grid-column: auto; }
.entry-section-head { display: flex; align-items: baseline; justify-content: space-between; gap: 10px; margin-bottom: 12px; padding-bottom: 8px; border-bottom: 1px solid #f4e7ea; }
.entry-section-head span { color: #5d3c45; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 14px; font-weight: 600; }
.entry-section-head small { color: #b09aa0; font-size: 11px; line-height: 1.5; text-align: right; }
.dialog-media-row { display: grid; grid-template-columns: 176px minmax(0, 1fr); gap: 18px; align-items: start; }
.photo-area { display: flex; flex-direction: column; align-items: center; gap: 8px; }
.photo-uploader, .photo-uploader :deep(.el-upload), .photo-frame { width: 176px; }
.photo-uploader :deep(.el-upload), .photo-frame { height: 188px; }
.photo-frame { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 7px; overflow: hidden; color: #b08b93; background: linear-gradient(145deg, #fffafb, #fbf0f2); border: 1px dashed #dfbdc5; border-radius: 15px; cursor: pointer; transition: .18s; }
.photo-frame:hover { border-color: #c58b99; background: #fff5f7; }
.photo-frame.filled { border-style: solid; border-color: #ead3d7; }
.photo-frame .el-image { width: 100%; height: 100%; }
.photo-frame .el-icon { color: #bc7c8b; font-size: 29px; }
.photo-frame span { font-size: 12px; }
.photo-frame small { padding: 0 10px; color: #bda5aa; font-size: 10.5px; text-align: center; line-height: 1.5; }
.clear-photo { padding: 0; color: #b07b86; background: transparent; border: 0; font-size: 11.5px; cursor: pointer; }
.dialog-basic-fields { min-width: 0; }
.dialog-basic-fields :deep(.el-form-item) { margin-bottom: 12px; }
.dialog-price-row { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px 14px; min-width: 0; }
.dialog-price-row :deep(.el-form-item) { min-width: 0; margin-bottom: 4px; }
.dialog-price-row :deep(.el-input-number) { width: 100%; min-width: 0; }
.dialog-price-row :deep(.el-select) { width: 100%; }
.dialog-price-large { grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 10px 16px; align-items: start; }
.price-source-hint { display: block; margin-top: 3px; color: #8b7a80; font-size: 11px; line-height: 1.3; }
.dialog-price-large :deep(.el-form-item__label) { min-height: 20px; line-height: 20px; white-space: nowrap; }
.goods-name-field :deep(.el-input__inner) { font-size: 15px; font-weight: 600; }
.entry-quantity-card { display: grid; grid-template-columns: minmax(0, 1fr) minmax(250px, .95fr); align-items: center; gap: 14px; min-width: 0; margin-bottom: 12px; padding: 11px 14px; background: linear-gradient(135deg, #fff6f8, #fffafb); border: 1px solid #efd7dd; border-radius: 12px; }
.entry-quantity-copy { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.entry-quantity-copy span { color: #6b414d; font-size: 13px; font-weight: 650; }
.entry-quantity-copy small { overflow-wrap: anywhere; color: #a68a92; font-size: 11px; line-height: 1.6; white-space: normal; }
.entry-quantity-side { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.entry-quantity-controls { display: grid; grid-template-columns: minmax(0, 1fr) 88px; align-items: center; gap: 8px; width: 100%; min-width: 0; }
.entry-quantity-controls :deep(.el-input-number) { width: 100%; min-width: 0; }
.entry-quantity-controls :deep(.el-input-number .el-input__inner) { color: #8d4d5e; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 700; text-align: center; }
.entry-unit-select { width: 100%; min-width: 0; }
.entry-unit-select :deep(.el-select__selected-item) { color: #8d4d5e; font-size: 16px; font-weight: 650; }
.entry-quantity-preview { display: flex; align-items: baseline; justify-content: space-between; gap: 10px; min-width: 0; color: #9b5666; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 11px; }
.entry-quantity-preview span { flex: none; color: #a78a92; }
.entry-quantity-preview strong { min-width: 0; overflow-wrap: anywhere; font-size: 15px; font-weight: 700; text-align: right; }
.entry-code-note { display: flex; align-items: center; gap: 10px; padding: 7px 10px; color: #a18a90; background: #fff8f9; border: 1px solid #f0dee2; border-radius: 8px; font-size: 11px; }
.entry-code-note span { flex: none; }
.entry-code-note strong { min-width: 0; }
.entry-code-note strong { overflow: hidden; color: #815361; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 12px; font-weight: 500; text-overflow: ellipsis; white-space: nowrap; }
.dialog-attribute-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(148px, 1fr)); gap: 0 12px; min-height: 0; }
.dialog-attribute-grid :deep(.el-form-item) { margin-bottom: 10px; }
.remark-section :deep(.el-form-item) { margin-bottom: 0; }
.remark-section :deep(.el-form-item) { margin-bottom: 0; }
.remark-section :deep(.el-textarea__inner) { min-height: 58px !important; resize: vertical; line-height: 1.6; }
.line-dialog-footer { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
.line-dialog-footer > div { display: flex; align-items: center; gap: 9px; }
.dialog-footnote { display: inline-flex; align-items: center; gap: 6px; color: #a1868d; font-size: 11.5px; }
.dialog-footnote .el-icon { color: #c19a61; }
.line-form :deep(.el-form-item__label) { padding-bottom: 6px; color: #8f737b; font-size: 12px; }
.line-form :deep(.el-form-item) { min-width: 0; }
.line-form :deep(.el-input), .line-form :deep(.el-select), .line-form :deep(.el-textarea) { width: 100%; }
.line-form :deep(.el-input-number) { width: 100%; }
.line-form :deep(.el-input-number .el-input__inner) { text-align: left; }
.line-form :deep(.el-input__wrapper), .line-form :deep(.el-textarea__inner), .summary-band :deep(.el-input__wrapper) { border-radius: 9px; }
.attribute-hint { display: flex; align-items: flex-start; gap: 7px; margin-top: 2px; padding: 8px 10px; color: #9b7b59; background: #fffaf1; border: 1px solid #f1e4cd; border-radius: 9px; font-size: 11.5px; line-height: 1.6; }
.attribute-hint .el-icon { flex: none; margin-top: 1px; color: #be9b64; }
.attribute-hint.attribute-error { color: #9a4f5c; background: #fff4f5; border-color: #efd0d5; }
.attribute-hint.attribute-error .el-icon { color: #bd6b79; }
.commit-button { color: #fff; background: linear-gradient(135deg, #b97080, #965261); border-color: transparent; }

:global(.line-entry-dialog) { display: flex; flex-direction: column; height: 100vh; height: 100dvh; margin: 0; overflow: hidden; background: #fffafb; }
:global(.line-entry-dialog .el-dialog__header) { flex: none; margin: 0; padding: 10px 24px; background: rgba(255, 250, 251, .98); border-bottom: 1px solid #efdee2; box-shadow: 0 4px 16px rgba(102, 60, 72, .04); }
:global(.line-entry-dialog .el-dialog__body) { flex: 1 1 auto; min-height: 0; padding: 14px 24px 16px; overflow-y: auto; overscroll-behavior: contain; }
:global(.line-entry-dialog .el-dialog__footer) { flex: none; position: relative; z-index: 5; padding: 9px 24px; background: rgba(255, 255, 255, .98); border-top: 1px solid #efdee2; box-shadow: 0 -4px 18px rgba(102, 60, 72, .045); }
:global(.line-entry-dialog .el-dialog__headerbtn) { top: 15px; right: 20px; }

.print-dialog-heading { display: flex; align-items: center; gap: 11px; }
.print-dialog-heading h2 { margin: 0 0 4px; color: #573b44; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }
.print-dialog-heading p { margin: 0; color: #a88e95; font-size:11.5px; }
.print-toolbar { display: flex; align-items: center; gap: 14px; padding: 10px 12px; color: #9c8289; background: #fdf8f8; border: 1px solid #f1e2e5; border-radius: 10px; font-size:11.5px; }
.print-toolbar > span { flex: 1; }
.print-template-row { display: grid; grid-template-columns: 52px minmax(180px, 260px) minmax(0, 1fr); align-items: center; gap: 10px; margin-top: 10px; padding: 10px 12px; color: #8f747c; background: #fffafb; border: 1px solid #f1e2e5; border-radius: 10px; font-size:12px; }
.print-template-row :deep(.el-select) { width: 100%; }
.print-template-row small { color: #a58c93; font-size:11.5px; line-height: 1.55; }
.print-list { min-height: 150px; max-height: 360px; margin-top: 12px; overflow-y: auto; }
.print-list :deep(.el-checkbox-group) { display: grid; gap: 7px; }
.print-row { display: grid; grid-template-columns: 22px minmax(0, 1fr) auto; align-items: center; gap: 10px; padding: 11px 12px; background: #fff; border: 1px solid #f1e4e6; border-radius: 10px; cursor: pointer; transition: .18s ease; }
.print-row:hover { background: #fffafb; border-color: #dfbdc5; }
.print-row :deep(.el-checkbox) { margin: 0; }
.print-row-copy { display: flex; flex-direction: column; gap: 4px; min-width: 0; }
.print-row-copy b { overflow: hidden; color: #62434c; font-size: 11px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.print-row-copy small { color: #a58c93; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; letter-spacing: .5px; }
.print-row-price { color: #9f5968; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 12px; }
.print-empty { display: grid; place-items: center; min-height: 150px; color: #aa9299; background: #fffafb; border: 1px dashed #e6cbd1; border-radius: 11px; font-size:12px; text-align: center; }
.print-note { display: flex; align-items: flex-start; gap: 7px; margin-top: 12px; padding: 9px 10px; color: #9b7b59; background: #fffaf1; border: 1px solid #f1e4cd; border-radius: 9px; font-size:11.5px; line-height: 1.65; }
.print-note .el-icon { flex: none; margin-top: 1px; color: #bf9a61; }
.print-footer { display: flex; justify-content: flex-end; gap: 8px; }
.copy-dialog-heading { display: flex; align-items: center; gap: 11px; }
.copy-dialog-heading h2 { margin: 0 0 4px; color: #573b44; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size: 20px; font-weight: 500; }
.copy-dialog-heading p { margin: 0; color: #a88e95; font-size:11.5px; }
.copy-source { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border: 1px solid #f1e2e5; border-radius: 9px; background: #fffafb; }
.copy-source-name { overflow: hidden; color: #5c3e47; font-size: 12px; font-weight: 600; text-overflow: ellipsis; white-space: nowrap; }
.copy-source-code { margin-left: auto; flex: none; color: #a58c93; font-family: "Microsoft YaHei", "微软雅黑", "PingFang SC", sans-serif; font-size:11.5px; }
.copy-form { margin-top: 14px; }
.copy-form :deep(.el-form-item) { margin-bottom: 10px; }
.copy-note { display: flex; align-items: flex-start; gap: 7px; padding: 9px 10px; color: #9b7b59; background: #fffaf1; border: 1px solid #f1e4cd; border-radius: 9px; font-size:11.5px; line-height: 1.65; }
.copy-note .el-icon { flex: none; margin-top: 1px; color: #bf9a61; }
:deep(.el-button) { border-radius: 9px; }
:deep(.el-tag) { --el-tag-border-radius: 999px; }
:deep(.el-input-number .el-input__wrapper) { padding-left: 10px; }
@media (max-width: 1280px) {
  .goods-list-head, .goods-row { grid-template-columns: 32px 38px minmax(140px, 1.5fr) minmax(82px, 1fr) 68px 66px 66px 62px 74px; gap: 7px; }
  .summary-band { grid-template-columns: minmax(0, 1fr) minmax(210px, 260px); }
  .order-main { grid-template-columns: 3px minmax(170px, 1fr) 74px 100px 96px auto; gap: 12px; }
  .dialog-line-form { grid-template-columns: minmax(310px, .8fr) minmax(0, 1.3fr); }
}
@media (max-width: 1080px) {
  .summary-band { grid-template-columns: 1fr; }
  .summary-totals { min-height: 74px; }
  .order-main { grid-template-columns: 3px minmax(170px, 1fr) 74px 100px auto; }
  .order-date { display: none; }
  .media-section { grid-column: 1 / -1; }
  .dialog-attribute-grid { grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); }
}
@media (max-width: 900px) {
  .goods-list-head .head-attrs, .goods-row .cell-attrs { display: none; }
  .goods-list-head, .goods-row { grid-template-columns: 32px 38px minmax(0, 1fr) 70px 72px 72px 64px 78px; }
}
@media (max-width: 760px) {
  .jy-inbound { padding: 12px; }
  .editor-view { height: calc(100vh - var(--base-header-height) - var(--base-tags-height) - var(--base-footer-height) - 64px); height: calc(100dvh - var(--base-header-height) - var(--base-tags-height) - var(--base-footer-height) - 64px); min-height: 0; }
  .print-template-row { grid-template-columns: 1fr; }
  .print-row { grid-template-columns: 22px minmax(0, 1fr); }
  .print-row-price { grid-column: 2; }
  .section-switch { width: 100%; }
  .section-switch button { flex: 1; }
  .page-hero, .editor-topbar, .line-dialog-header, .line-dialog-footer { align-items: flex-start; flex-direction: column; }
  .page-hero { padding: 23px; }
  .hero-actions { width: 100%; }
  .hero-actions :deep(.el-button) { flex: 1; }
  .order-panel, .wall-panel { padding: 16px; }
  .section-head, .filters { align-items: stretch; flex-direction: column; }
  .filters :deep(.el-input), .filters :deep(.el-date-editor), .filters :deep(.el-select) { flex: 1 1 auto; width: 100%; min-width: 0; }
  .order-row { align-items: stretch; flex-direction: column; gap: 0; padding: 0 12px 11px; }
  .order-main { grid-template-columns: 3px minmax(140px, 1fr) 66px auto; gap: 12px; padding: 14px 0 11px; }
  .order-date, .cost-stat { display: none; }
  .order-actions { justify-content: flex-end; padding-left: 15px; }
  .summary-field, .summary-field.date-field, .summary-field.supplier-field, .summary-field.order-field, .summary-field.period-field, .summary-field.remark-field { flex: 1 1 100%; }
  .wall-head { align-items: flex-start; flex-direction: column; }
  .wall-tools { width: 100%; }
  .wall-tools :deep(.el-input) { flex: 1 1 160px; max-width: none; }
  .goods-list-head .head-status, .goods-row .cell-status { display: none; }
  .goods-list-head, .goods-row { grid-template-columns: 30px 36px minmax(0, 1fr) 66px 68px 68px 72px; }
  .dialog-line-form { grid-template-columns: 1fr; }
  .attribute-section, .remark-section { grid-column: 1 / -1; }
  .dialog-media-row { grid-template-columns: 140px minmax(0, 1fr); gap: 14px; }
  .photo-uploader, .photo-uploader :deep(.el-upload), .photo-frame { width: 140px; }
  .photo-uploader :deep(.el-upload), .photo-frame { height: 152px; }
  .photo-area { align-items: flex-start; }
  .dialog-price-row, .dialog-attribute-grid { grid-template-columns: 1fr; }
  .entry-quantity-card { padding: 14px 15px; }
  .entry-quantity-card { grid-template-columns: 1fr; }
  .entry-quantity-controls { grid-template-columns: minmax(0, 1fr) 88px; gap: 8px; }
  .entry-section { padding: 16px; }
  .entry-section-head { align-items: flex-start; flex-direction: column; }
  .entry-section-head small { text-align: left; }
  :global(.line-entry-dialog .el-dialog__header), :global(.line-entry-dialog .el-dialog__body), :global(.line-entry-dialog .el-dialog__footer) { padding-left: 16px; padding-right: 16px; }
}
@media (max-width: 480px) {
  .goods-list-head .head-cost, .goods-list-head .head-sale, .goods-row .cell-price { display: none; }
  .goods-list-head, .goods-row { grid-template-columns: 30px 36px minmax(0, 1fr) 66px 72px; }
  .line-dialog-status { align-items: flex-start; flex-direction: column; }
  .dialog-code { max-width: calc(100vw - 32px); }
  .line-dialog-footer > div { width: 100%; }
  .line-dialog-footer > div :deep(.el-button) { flex: 1; }
  .entry-quantity-controls { grid-template-columns: minmax(0, 1fr) 82px; }
  .entry-quantity-controls :deep(.el-input-number .el-input__inner) { font-size: 18px; }
  .entry-quantity-preview { align-items: flex-start; flex-direction: column; gap: 4px; }
  .entry-quantity-preview strong { text-align: left; }
  .dialog-price-large { grid-template-columns: 1fr; }
  .dialog-price-large :deep(.el-form-item__label) { white-space: normal; }
  .dialog-media-row { grid-template-columns: 1fr; }
  .photo-uploader, .photo-uploader :deep(.el-upload), .photo-frame { width: min(220px, 100%); }
  .photo-uploader :deep(.el-upload), .photo-frame { height: 220px; }
}
</style>




