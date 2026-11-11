import request from '@/utils/request'

export function listFactoryReturn(query) {
  return request({ url: 'jingyue/factory/list', method: 'get', params: query })
}
export function getFactoryReturn(id) {
  return request({ url: 'jingyue/factory/' + id, method: 'get' })
}
export function findFactoryGoods(keyword) {
  return request({ url: 'jingyue/factory/goods', method: 'get', params: { keyword } })
}
export function saveFactoryReturn(data) {
  return request({ url: 'jingyue/factory', method: 'post', data })
}
export function confirmFactoryReturn(id) {
  return request({ url: 'jingyue/factory/' + id + '/confirm', method: 'post' })
}
export function deleteFactoryReturn(id) {
  return request({ url: 'jingyue/factory/' + id, method: 'delete' })
}