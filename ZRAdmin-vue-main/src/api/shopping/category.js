import request from '@/utils/request'

/**
 * 文章目录分页查询
 * @param {查询条件} data
 */
export function listCategory(query) {
  return request({
    url: 'shopping/category/list',
    method: 'get',
    params: query
  })
}

/**
 * 文章目录tree查询
 * @param {查询条件} data
 */
export function treelistCategory(query) {
  return request({
    url: 'shopping/category/treelist',
    method: 'get',
    params: query
  })
}

/**
 * 新增文章目录
 * @param data
 */
export function addCategory(data) {
  return request({
    url: 'shopping/category',
    method: 'post',
    data: data
  })
}

/**
 * 修改文章目录
 * @param data
 */
export function updateCategory(data) {
  return request({
    url: 'shopping/category',
    method: 'PUT',
    data: data
  })
}

/**
 * 获取文章目录详情
 * @param {Id}
 */
export function getCategory(id) {
  return request({
    url: 'shopping/category/' + id,
    method: 'get'
  })
}

/**
 * 删除文章目录
 * @param {主键} pid
 */
export function delCategory(pid) {
  return request({
    url: 'shopping/category/' + pid,
    method: 'delete'
  })
}

// 导出文章目录
export function exportCategory(query) {
  return request({
    url: 'shopping/category/export',
    method: 'get',
    params: query
  })
}

export function changeSort(data) {
  return request({
    url: 'shopping/category/ChangeSort',
    method: 'get',
    params: data
  })
}
