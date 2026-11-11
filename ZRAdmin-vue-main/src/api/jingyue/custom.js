import request from '@/utils/request'

export function listCustom(query) {
  return request({ url: 'jingyue/custom/list', method: 'get', params: query })
}
export function getCustom(id) {
  return request({ url: 'jingyue/custom/' + id, method: 'get' })
}
export function listAvailableCustom(query) {
  return request({ url: 'jingyue/custom/available', method: 'get', params: query })
}
export function getCustomDashboard() {
  return request({ url: 'jingyue/custom/dashboard', method: 'get' })
}
export function saveCustom(data) {
  return request({ url: 'jingyue/custom', method: 'post', data })
}
export function arriveCustom(id, data) {
  return request({ url: 'jingyue/custom/' + id + '/arrive', method: 'post', data })
}
export function pickupCustom(id, data) {
  return request({ url: 'jingyue/custom/' + id + '/pickup', method: 'post', data })
}
export function deleteCustom(id) {
  return request({ url: 'jingyue/custom/' + id, method: 'delete' })
}
export function refundCustom(data) {
  return request({ url: 'jingyue/custom/refund', method: 'post', data })
}
