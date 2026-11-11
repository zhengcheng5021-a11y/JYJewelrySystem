import request from '@/utils/request'

export function listMaterial(query) {
  return request({ url: 'jingyue/material/list', method: 'get', params: query })
}

export function getMaterial(id) {
  return request({ url: 'jingyue/material/' + id, method: 'get' })
}

export function getMaterialSummary() {
  return request({ url: 'jingyue/material/summary', method: 'get' })
}

export function getMaterialOptions() {
  return request({ url: 'jingyue/material/options', method: 'get' })
}

export function getAvailableMaterials() {
  return request({ url: 'jingyue/material/available', method: 'get' })
}

export function saveMaterial(data) {
  return request({ url: 'jingyue/material', method: 'post', data })
}

export function deleteMaterial(id) {
  return request({ url: 'jingyue/material/' + id, method: 'delete' })
}

export function listMaterialDocs(query) {
  return request({ url: 'jingyue/material/doc/list', method: 'get', params: query })
}

export function getMaterialDoc(id) {
  return request({ url: 'jingyue/material/doc/' + id, method: 'get' })
}

export function getMaterialDocDashboard() {
  return request({ url: 'jingyue/material/doc/dashboard', method: 'get' })
}

export function saveMaterialDoc(data) {
  return request({ url: 'jingyue/material/doc', method: 'post', data })
}

export function deleteMaterialDoc(id) {
  return request({ url: 'jingyue/material/doc/' + id, method: 'delete' })
}

export function confirmMaterialDoc(id) {
  return request({ url: 'jingyue/material/doc/' + id + '/confirm', method: 'post' })
}

export function listMaterialLedger(query) {
  return request({ url: 'jingyue/material/ledger', method: 'get', params: query })
}
