import request from '@/utils/request'
import { downFile } from '@/utils/request'

/**
 * 品牌表分页查询
 * @param {查询条件} data
 */
export function listMmsBrand(query) {
  return request({
    url: 'shopping/brand/list',
    method: 'get',
    params: query
  })
}

/**
 * 新增品牌表
 * @param data
 */
export function addMmsBrand(data) {
  return request({
    url: 'shopping/brand',
    method: 'post',
    data: data
  })
}
/**
 * 修改品牌表
 * @param data
 */
export function updateMmsBrand(data) {
  return request({
    url: 'shopping/brand',
    method: 'PUT',
    data: data
  })
}
/**
 * 获取品牌表详情
 * @param {Id}
 */
export function getMmsBrand(id) {
  return request({
    url: 'shopping/brand/' + id,
    method: 'get'
  })
}

/**
 * 删除品牌表
 * @param {主键} pid
 */
export function delMmsBrand(pid) {
  return request({
    url: 'shopping/brand/delete/' + pid,
    method: 'POST'
  })
}
// 导出品牌表
export async function exportMmsBrand(query) {
  await downFile('shopping/brand/export', { ...query })
}
