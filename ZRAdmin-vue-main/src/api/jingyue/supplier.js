import request from '@/utils/request'

export function listSupplier(query) {
  return request({ url: 'jingyue/supplier/list', method: 'get', params: query })
}
export function saveSupplier(data) {
  return request({ url: 'jingyue/supplier', method: 'post', data })
}
export function deleteSupplier(id) {
  return request({ url: 'jingyue/supplier/' + id, method: 'delete' })
}
