import request from '@/utils/request'

export function listBillRules() {
  return request({ url: 'jingyue/bill/rules', method: 'get' })
}
export function saveBillRule(data) {
  return request({ url: 'jingyue/bill/rule', method: 'post', data })
}
export function deleteBillRule(id) {
  return request({ url: 'jingyue/bill/rule/' + id, method: 'delete' })
}
export function previewBillNo(billType) {
  return request({ url: 'jingyue/bill/preview', method: 'get', params: { billType } })
}
export function issueBillNo(data) {
  return request({ url: 'jingyue/bill/issue', method: 'post', data })
}
export function listBillIssues(query) {
  return request({ url: 'jingyue/bill/issues', method: 'get', params: query })
}
