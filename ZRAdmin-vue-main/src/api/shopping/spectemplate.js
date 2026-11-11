import request from '@/utils/request'

/**
 * 规格模板分页查询
 * @param {查询条件} data
 */
export function listMmsSpecTemplate(query) {
  return request({
    url: 'shopping/SpecTemplate/list',
    method: 'get',
    params: query
  })
}
/**
 *
 * @param {查询条件} data
 */
export function listSpecTemplate(query) {
  return request({
    url: 'shopping/SpecTemplate/tplList',
    method: 'get',
    params: query
  })
}

/**
 * 新增规格模板
 * @param data
 */
export function addMmsSpecTemplate(data) {
  return request({
    url: 'shopping/SpecTemplate',
    method: 'post',
    data: data
  })
}
/**
 * 修改规格模板
 * @param data
 */
export function updateMmsSpecTemplate(data) {
  return request({
    url: 'shopping/SpecTemplate',
    method: 'PUT',
    data: data
  })
}
/**
 * 获取规格模板详情
 * @param {Id}
 */
export function getMmsSpecTemplate(id) {
  return request({
    url: 'shopping/SpecTemplate/' + id,
    method: 'get'
  })
}

/**
 * 删除规格模板
 * @param {主键} pid
 */
export function delMmsSpecTemplate(pid) {
  return request({
    url: 'shopping/SpecTemplate/delete/' + pid,
    method: 'POST'
  })
}
