import request from '@/utils/request'

export function listSale(query) {
  return request({ url: 'jingyue/sale/list', method: 'get', params: query })
}
export function getSale(id) {
  return request({ url: 'jingyue/sale/' + id, method: 'get' })
}
export function getSaleOptions() {
  return request({ url: 'jingyue/sale/options', method: 'get' })
}
export function getSaleDashboard() {
  return request({ url: 'jingyue/sale/dashboard', method: 'get' })
}
export function previewSale(data) {
  return request({ url: 'jingyue/sale/preview', method: 'post', data })
}
export function saveSale(data) {
  return request({ url: 'jingyue/sale', method: 'post', data })
}
export function confirmSale(id) {
  return request({ url: 'jingyue/sale/' + id + '/confirm', method: 'post' })
}
export function deleteSale(id) {
  return request({ url: 'jingyue/sale/' + id, method: 'delete' })
}

export function matchSaleCustomer(keyword) {
  return request({ url: 'jingyue/sale/customer', method: 'get', params: { keyword } })
}
export function findSaleGoods(keyword, extra) {
  return request({ url: 'jingyue/sale/goods', method: 'get', params: { keyword, ...(extra || {}) } })
}
export function listReturnOrders(query) {
  return request({ url: 'jingyue/sale/return/orders', method: 'get', params: query })
}
export function getReturnSaleDetail(saleId) {
  return request({ url: 'jingyue/sale/return/sale/' + saleId, method: 'get' })
}
export function saveSaleReturn(data) {
  return request({ url: 'jingyue/sale/return', method: 'post', data })
}
export function listSaleReturns(query) {
  return request({ url: 'jingyue/sale/return/list', method: 'get', params: query })
}
export function getSaleReturn(id) {
  return request({ url: 'jingyue/sale/return/' + id, method: 'get' })
}
