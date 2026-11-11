import request from '@/utils/request'

export function listLookupTypes() {
  return request({ url: 'jingyue/lookup/types', method: 'get' })
}
export function saveLookupType(data) {
  return request({ url: 'jingyue/lookup/type', method: 'post', data })
}
export function deleteLookupType(id) {
  return request({ url: 'jingyue/lookup/type/' + id, method: 'delete' })
}
export function listLookupItems(typeId) {
  return request({ url: 'jingyue/lookup/items', method: 'get', params: { typeId } })
}
export function lookupOptions(typeCode) {
  return request({ url: 'jingyue/lookup/options', method: 'get', params: { typeCode } })
}
export function saveLookupItem(data) {
  return request({ url: 'jingyue/lookup/item', method: 'post', data })
}
export function deleteLookupItem(id) {
  return request({ url: 'jingyue/lookup/item/' + id, method: 'delete' })
}
