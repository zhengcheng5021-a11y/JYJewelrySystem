import request from '@/utils/request'
import { downFile } from '@/utils/request'
/**
 * 文件存储分页查询
 * @param {查询条件} data
 */
export function listSysfile(query) {
  return request({
    url: 'tool/file/list',
    method: 'get',
    params: query
  })
}

/**
 * 新增文件存储
 * @param data
 */
export function addSysfile(data) {
  return request({
    url: 'tool/file',
    method: 'post',
    data: data
  })
}

/**
 * 修改文件存储
 * @param data
 */
export function updateSysfile(data) {
  return request({
    url: 'tool/file',
    method: 'PUT',
    data: data
  })
}

/**
 * 获取文件存储详情
 * @param {Id}
 */
export function getSysfile(id) {
  return request({
    url: 'tool/file/' + id,
    method: 'get'
  })
}

/**
 * 删除文件存储
 * @param {主键} pid
 */
export function delSysfile(pid) {
  return request({
    url: 'tool/file/' + pid,
    method: 'delete'
  })
}

// 导出文件存储
export async function exportSysfile(query) {
  // return request({
  //   url: 'tool/file/export',
  //   method: 'get',
  //   params: query
  // })
  await downFile('/tool/file/export', { ...query })
}

/**
 * 文件存储分组查询
 * @param {查询条件} data
 */
export function listSysfileByGroup(query) {
  return request({
    url: 'tool/file/listByGroup',
    method: 'get',
    params: query
  })
}

/**
 * 移动文件分组
 * @param data
 */
export function moveFileGroup(data) {
  return request({
    url: 'tool/file/move',
    method: 'POST',
    data: data
  })
}
