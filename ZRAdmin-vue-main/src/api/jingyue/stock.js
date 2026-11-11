import request from '@/utils/request'

export function listStock(query) {
  return request({ url: 'jingyue/stock/list', method: 'get', params: query })
}
export function getStockSummary() {
  return request({ url: 'jingyue/stock/summary', method: 'get' })
}
export function getStockLedger(query) {
  return request({ url: 'jingyue/stock/ledger', method: 'get', params: query })
}
