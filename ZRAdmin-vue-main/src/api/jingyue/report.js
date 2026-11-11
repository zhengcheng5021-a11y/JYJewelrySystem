import request from '@/utils/request'

export function getSalesMetrics(query) {
  return request({ url: 'jingyue/report/sales-metrics', method: 'get', params: query })
}
export function getStoreTraffic(query) {
  return request({ url: 'jingyue/report/store-traffic', method: 'get', params: query })
}
export function saveStoreTraffic(data) {
  return request({ url: 'jingyue/report/store-traffic', method: 'post', data })
}
export function getStylePerformance(query) {
  return request({ url: 'jingyue/report/style-performance', method: 'get', params: query })
}
export function getSalesLedger(query) {
  return request({ url: 'jingyue/report/sales-ledger', method: 'get', params: query })
}
export function getInventoryReport(query) {
  return request({ url: 'jingyue/report/inventory', method: 'get', params: query })
}
export function getStockAgeReport(query) {
  return request({ url: 'jingyue/report/stock-age', method: 'get', params: query })
}
export function getStockAgeDetails(query) {
  return request({ url: 'jingyue/report/stock-age/details', method: 'get', params: query })
}
export function getSalesGoals(query) {
  return request({ url: 'jingyue/report/goals', method: 'get', params: query })
}
export function saveGoalYear(data) {
  return request({ url: 'jingyue/report/goals/year', method: 'post', data })
}
export function saveGoalMonth(data) {
  return request({ url: 'jingyue/report/goals/month', method: 'post', data })
}
export function saveGoalCategory(data) {
  return request({ url: 'jingyue/report/goals/category', method: 'post', data })
}
export function getOpsBreakdown(query) {
  return request({ url: 'jingyue/report/ops-breakdown', method: 'get', params: query })
}
export function saveGoalWeights(data) {
  return request({ url: 'jingyue/report/goals/weights', method: 'post', data })
}
export function saveGoalPlans(data) {
  return request({ url: 'jingyue/report/goals/plans', method: 'post', data })
}
export function listCombos(query) {
  return request({ url: 'jingyue/report/combos', method: 'get', params: query })
}
export function getComboReport(query) {
  return request({ url: 'jingyue/report/combos/report', method: 'get', params: query })
}
export function saveCombo(data) {
  return request({ url: 'jingyue/report/combos', method: 'post', data })
}
export function setComboStatus(id, status) {
  return request({ url: `jingyue/report/combos/${id}/status`, method: 'put', params: { status } })
}
export function deleteCombo(id) {
  return request({ url: `jingyue/report/combos/${id}`, method: 'delete' })
}
