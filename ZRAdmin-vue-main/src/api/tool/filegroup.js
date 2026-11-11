import request from '@/utils/request'

/**
 * 文件分组分页查询
 * @param {查询条件} data
 */
export function listFileGroup(query) {
  return request({
    url: 'tool/FileGroup/list',
    method: 'get',
    params: query
  })
}

/**
 * 文件分组tree查询
 * @param {查询条件} data
 */
export function treelistFileGroup(query) {
  return request({
    url: 'tool/FileGroup/treelist',
    method: 'get',
    params: query
  })
}
/**
 * 新增文件分组
 * @param data
 */
export function addFileGroup(data) {
  return request({
    url: 'tool/FileGroup',
    method: 'post',
    data: data
  })
}
/**
 * 修改文件分组
 * @param data
 */
export function updateFileGroup(data) {
  return request({
    url: 'tool/FileGroup',
    method: 'PUT',
    data: data
  })
}
/**
 * 获取文件分组详情
 * @param {Id}
 */
export function getFileGroup(id) {
  return request({
    url: 'tool/FileGroup/' + id,
    method: 'get'
  })
}

/**
 * 删除文件分组
 * @param {主键} pid
 */
export function delFileGroup(pid) {
  return request({
    url: 'tool/FileGroup/delete/' + pid,
    method: 'POST'
  })
}
export function changeSort(data) {
  return request({
    url: 'tool/FileGroup/ChangeSort',
    method: 'get',
    params: data
  })
}
