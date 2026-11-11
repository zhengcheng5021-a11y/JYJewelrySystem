import request from '@/utils/request'

export function listMetalPrices(query) {
  return request({ url: 'jingyue/pricing/metal/list', method: 'get', params: query })
}
export function saveMetalPrice(data) {
  return request({ url: 'jingyue/pricing/metal', method: 'post', data })
}
export function deleteMetalPrice(id) {
  return request({ url: 'jingyue/pricing/metal/' + id, method: 'delete' })
}
export function listDiscountRules(query) {
  return request({ url: 'jingyue/pricing/discount/list', method: 'get', params: query })
}
export function saveDiscountRule(data) {
  return request({ url: 'jingyue/pricing/discount', method: 'post', data })
}
export function deleteDiscountRule(id) {
  return request({ url: 'jingyue/pricing/discount/' + id, method: 'delete' })
}
export function getPricingOptions() {
  return request({ url: 'jingyue/pricing/options', method: 'get' })
}
