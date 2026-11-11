import request from '@/utils/request'

export function listGoods(query) {
  return request({ url: 'jingyue/goods/list', method: 'get', params: query })
}
export function getGoods(id) {
  return request({ url: 'jingyue/goods/' + id, method: 'get' })
}
export function getGoodsOptions() {
  return request({ url: 'jingyue/goods/options', method: 'get' })
}
export function saveGoodsItem(data) {
  return request({ url: 'jingyue/goods/item', method: 'post', data })
}