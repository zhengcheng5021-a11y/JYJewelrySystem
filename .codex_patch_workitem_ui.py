from pathlib import Path

root = Path(r'D:\work\其他文件\静悦平台\ZrAdminNetCore-master\ZRAdmin-vue-main\src\views')

# 1. 子目标创建时不应继承父待办标题
p = root / 'system' / 'dailyschedule' / 'index.vue'
s = p.read_text(encoding='utf-8')
old = "  editorInitialTitle.value = title || parent?.title || ''"
new = "  editorInitialTitle.value = title || ''"
if old not in s:
    raise SystemExit('index.vue target not found')
s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='')

# 2. 详情页四象限、无日期、金额/件数展示
p = root / 'system' / 'dailyschedule' / 'WorkItemDetail.vue'
s = p.read_text(encoding='utf-8')
repls = [
(
'''            <el-tag :type="item.priority === 3 ? 'danger' : item.priority === 1 ? 'info' : 'warning'" effect="light" size="small">{{ item.priorityName }}优先级</el-tag>''',
'''            <el-tag :type="priorityTagType(item.priority)" effect="light" size="small">{{ item.priorityName }}</el-tag>'''
),
(
'''            <span>{{ item.status === 1 ? '完成于 ' + formatTime(item.finishTime) : '截止 ' + (formatTime(item.dueTime) || formatDate(item.endDate)) }}</span>''',
'''            <span v-if="item.status === 1">完成于 {{ formatTime(item.finishTime) }}</span>
            <span v-else-if="Number(item.hasDateRange) === 1">截止 {{ formatTime(item.dueTime) || formatDate(item.endDate) }}</span>
            <span v-else>未设置时间区间</span>'''
),
(
'''                <small>{{ child.itemKindName }}<i>·</i>{{ metricSummary(child) }}<i>·</i>{{ child.ownerName || '未分配' }}</small>''',
'''                <small>{{ childMeta(child) }}</small>'''
),
(
'''function periodText(row) { return `${formatDate(row.startDate)} 至 ${formatDate(row.endDate)}` }''',
'''function priorityTagType(value) {
  return ({ 4: 'danger', 3: 'warning', 2: 'primary', 1: 'info' })[Number(value)] || 'info'
}
function periodText(row) {
  if (Number(row?.hasDateRange) !== 1) return '未设置时间区间'
  return `${formatDate(row.startDate)} 至 ${formatDate(row.endDate)}`
}'''
),
(
'''function metricSummary(row) {
  if (row.progressMode === 2) {
    if (row.metricCode === 'customer_follow_count') return `${number(row.currentValue)} 位客户 · ${number(row.currentAuxValue)} 次跟进`
    if (row.metricCode === 'customer_deal_count') return `${number(row.currentValue)} 位成交客户`
    if (row.metricCode === 'sales_net_amount' || row.metricCode === 'category_sales_amount') return `¥${number(row.currentValue)} / ¥${number(row.targetValue)}`
    return `${number(row.currentValue)} / ${number(row.targetValue)}`
  }
  return `${number(row.currentValue)} / ${number(row.targetValue)}`
}''',
'''function metricSummary(row) {
  if (row.progressMode === 2) {
    if (row.metricCode === 'customer_follow_count') return `${number(row.currentValue)} 位客户 · ${number(row.currentAuxValue)} 次跟进`
    if (row.metricCode === 'customer_deal_count') return `${number(row.currentValue)} 位成交客户`
    if (row.metricCode === 'sales_net_amount' || row.metricCode === 'category_sales_amount') return `¥${number(row.currentValue)} / ¥${number(row.targetValue)}`
    return `${number(row.currentValue)} / ${number(row.targetValue)}`
  }
  if (Number(row.isPerformanceRelated) === 1) {
    return Number(row.targetMode) === 2
      ? `${number(row.currentValue)} / ${number(row.targetValue)} 件`
      : `¥${number(row.currentValue)} / ¥${number(row.targetValue)}`
  }
  return Number(row.targetValue) > 0
    ? `${number(row.currentValue)} / ${number(row.targetValue)}`
    : '手工进度'
}
function childMeta(row) {
  return [
    row.priorityName,
    row.categoryName,
    metricSummary(row),
    Number(row.hasDateRange) === 1 && row.targetFinishDate ? `计划 ${formatDate(row.targetFinishDate)}` : '',
    row.ownerName || '未分配'
  ].filter(Boolean).join(' · ')
}'''
)
]
for old, new in repls:
    if old not in s:
        raise SystemExit('WorkItemDetail target not found:\n' + old[:160])
    s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='')

# 3. 模板四象限、目标口径和 apply 请求字段
p = root / 'system' / 'dailyschedule' / 'WorkTemplateDialog.vue'
s = p.read_text(encoding='utf-8')
repls = [
(
'''                <div class="row-meta"><span>{{ metricName(item.metricCode) || '手工维护' }}</span><span v-if="item.targetValue > 0">目标 {{ number(item.targetValue) }}</span><span>{{ periodName(item.periodType) }}</span></div>''',
'''                <div class="row-meta"><span>{{ metricName(item.metricCode) || '手工维护' }}</span><span v-if="item.categoryName">{{ item.categoryName }}</span><span v-if="item.targetValue > 0">目标 {{ number(item.targetValue) }} {{ Number(item.targetMode) === 2 ? '件' : '元' }}</span><span>{{ periodName(item.periodType) }}</span></div>'''
),
(
'''              <el-tag :type="item.priority === 3 ? 'danger' : item.priority === 1 ? 'info' : 'warning'" size="small" effect="light">{{ priorityName(item.priority) }}</el-tag>''',
'''              <el-tag :type="priorityTagType(item.priority)" size="small" effect="light">{{ priorityName(item.priority) }}</el-tag>'''
),
(
'''                  <el-option label="高优先级" :value="3" /><el-option label="中优先级" :value="2" /><el-option label="低优先级" :value="1" />''',
'''                  <el-option label="重要且紧急" :value="4" /><el-option label="重要不紧急" :value="3" /><el-option label="不重要但紧急" :value="2" /><el-option label="不重要不紧急" :value="1" />'''
),
(
'''function priorityName(value) { return ({ 3: '高', 2: '中', 1: '低' })[value] || '中' }''',
'''function priorityName(value) {
  return ({ 4: '重要且紧急', 3: '重要不紧急', 2: '不重要但紧急', 1: '不重要不紧急' })[Number(value)] || '未设置'
}
function priorityTagType(value) {
  return ({ 4: 'danger', 3: 'warning', 2: 'primary', 1: 'info' })[Number(value)] || 'info'
}'''
),
(
'''      ownerUserId: applyForm.ownerUserId || null,
      ownerName: applyForm.ownerName || null
    })''',
'''      ownerUserId: applyForm.ownerUserId || null,
      ownerName: applyForm.ownerName || null,
      hasDateRange: 1,
      isPerformanceRelated: selected.value.items?.some((it) => it.categoryId || it.metricCode) ? 1 : 0
    })'''
)
]
for old, new in repls:
    if old not in s:
        raise SystemExit('WorkTemplateDialog target not found:\n' + old[:160])
    s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='')

# 4. 销售页 resetForm 清理异步状态，避免新开单后按钮永久 loading
p = root / 'jingyue' / 'sale' / 'index.vue'
s = p.read_text(encoding='utf-8')
old = '''function resetForm() {
  saleEpoch += 1
  cancelPendingPreview()'''
new = '''function resetForm() {
  saleEpoch += 1
  // 旧请求在代次递增后会停止回写；这里同步清掉旧请求留下的提交状态，
  // 否则收款后自动新开单会让“确认收款/保存草稿”按钮一直 loading。
  saving.value = false
  confirming.value = false
  cancelPendingPreview()'''
if old not in s:
    raise SystemExit('sale resetForm target not found')
s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='')

print('patched 4 frontend files')
