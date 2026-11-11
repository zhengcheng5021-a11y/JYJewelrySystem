import request from '@/utils/request'

export function listScript(query) {
  return request({ url: 'jingyue/script/list', method: 'get', params: query })
}
export function saveScript(data) {
  return request({ url: 'jingyue/script', method: 'post', data })
}
export function deleteScript(id) {
  return request({ url: 'jingyue/script/' + id, method: 'delete' })
}
