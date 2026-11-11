function parseDate(value) {
  if (value instanceof Date) return value
  if (typeof value === 'string') {
    // iOS cannot parse "yyyy-MM-dd HH:mm:ss" consistently; normalize it first.
    const normalized = value.trim().replace(/^(\d{4}-\d{2}-\d{2})[ T](\d{2}:\d{2}(?::\d{2})?)/, '$1T$2')
    return new Date(normalized)
  }
  return new Date(value)
}

function money(value) {
  const n = Number(value || 0)
  return n.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function dateText(value) {
  if (!value) return '未设置'
  const d = parseDate(value)
  if (Number.isNaN(d.getTime())) return String(value).slice(0, 10)
  const pad = (n) => String(n).padStart(2, '0')
  return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
}

function dateTimeText(value) {
  if (!value) return '未设置'
  const d = parseDate(value)
  if (Number.isNaN(d.getTime())) return String(value)
  const pad = (n) => String(n).padStart(2, '0')
  return dateText(value) + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes())
}

function todayText() { return dateText(new Date()) }

function statusText(status) {
  const map = { 0: '草稿', 1: '已确认', 2: '锁定', 3: '已售出', 4: '赠送', 5: '盘亏', 6: '停用', 7: '入库中', 8: '返厂', 9: '作废' }
  return map[Number(status)] || '未知'
}

module.exports = { money, dateText, dateTimeText, todayText, statusText }
