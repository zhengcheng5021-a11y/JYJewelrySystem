import request from '@/utils/request'

export function listStocktakes(query) {
  return request({ url: 'jingyue/stocktake/list', method: 'get', params: query })
}

export function getStocktake(id) {
  return request({ url: 'jingyue/stocktake/' + id, method: 'get' })
}

export function getStocktakeDashboard() {
  return request({ url: 'jingyue/stocktake/dashboard', method: 'get' })
}

export function getStocktakeOptions(takeType, keyword) {
  return request({ url: 'jingyue/stocktake/options', method: 'get', params: { takeType, keyword } })
}

export function initializeStocktake(data) {
  return request({ url: 'jingyue/stocktake/initialize', method: 'post', data })
}

export function saveStocktake(data) {
  return request({ url: 'jingyue/stocktake', method: 'post', data })
}

export function deleteStocktake(id) {
  return request({ url: 'jingyue/stocktake/' + id, method: 'delete' })
}

export function confirmStocktake(id) {
  return request({ url: 'jingyue/stocktake/' + id + '/confirm', method: 'post' })
}

export function getStocktakeReport(query) {
  return request({ url: 'jingyue/stocktake/report', method: 'get', params: query })
}
