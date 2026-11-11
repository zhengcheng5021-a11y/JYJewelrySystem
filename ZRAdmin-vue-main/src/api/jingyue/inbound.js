import request from '@/utils/request'

export function listInbound(query) {
  return request({ url: 'jingyue/inbound/list', method: 'get', params: query })
}
export function getInbound(id) {
  return request({ url: 'jingyue/inbound/' + id, method: 'get' })
}
export function getInboundDashboard() {
  return request({ url: 'jingyue/inbound/dashboard', method: 'get' })
}
export function getInboundOptions() {
  return request({ url: 'jingyue/inbound/options', method: 'get' })
}
export function saveInbound(data) {
  return request({ url: 'jingyue/inbound', method: 'post', data })
}
export function deleteInbound(id) {
  return request({ url: 'jingyue/inbound/' + id, method: 'delete' })
}
export function confirmInbound(id) {
  return request({ url: 'jingyue/inbound/' + id + '/confirm', method: 'post' })
}