function rows(result) {
  if (Array.isArray(result)) return result
  if (!result || typeof result !== 'object') return []
  const candidates = [result.result, result.items, result.rows, result.list, result.records]
  for (let i = 0; i < candidates.length; i += 1) {
    if (Array.isArray(candidates[i])) return candidates[i]
  }
  return []
}

function total(result) {
  if (!result || typeof result !== 'object') return 0
  return Number(result.totalNum || result.total || result.totalCount || rows(result).length || 0)
}

function hasMore(page, pageSize, result) {
  const count = rows(result).length
  return count > 0 && (count >= Number(pageSize || 0) || total(result) > Number(page || 1) * Number(pageSize || 0))
}

function appendUnique(current, incoming) {
  const seen = new Set((current || []).map((item) => String(item.id)))
  return (current || []).concat((incoming || []).filter((item) => !seen.has(String(item.id))))
}

module.exports = { rows, total, hasMore, appendUnique }
