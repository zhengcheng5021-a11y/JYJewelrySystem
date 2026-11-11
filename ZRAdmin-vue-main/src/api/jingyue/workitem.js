import request from '@/utils/request'

export function listWorkItems(query) {
  return request({ url: 'jingyue/workitem/list', method: 'get', params: query })
}
export function getWorkItemStats() {
  return request({ url: 'jingyue/workitem/stats', method: 'get' })
}
export function getWorkItemOptions() {
  return request({ url: 'jingyue/workitem/options', method: 'get' })
}
export function getQuickActions() {
  return request({ url: 'jingyue/workitem/quick-actions', method: 'get' })
}
export function saveQuickAction(data) {
  return request({ url: 'jingyue/workitem/quick-actions', method: 'post', data })
}
export function deleteQuickAction(id) {
  return request({ url: 'jingyue/workitem/quick-actions/' + id, method: 'delete' })
}
export function getWorkItem(id) {
  return request({ url: 'jingyue/workitem/' + id, method: 'get' })
}
export function getWorkItemEvidence(id) {
  return request({ url: 'jingyue/workitem/' + id + '/evidence', method: 'get' })
}
export function saveWorkItem(data) {
  return request({ url: 'jingyue/workitem', method: 'post', data })
}
export function deleteWorkItem(id) {
  return request({ url: 'jingyue/workitem/' + id, method: 'delete' })
}
export function completeWorkItem(id, completed = true) {
  return request({ url: 'jingyue/workitem/' + id + '/complete', method: 'post', params: { completed } })
}
export function updateWorkItemProgress(id, data) {
  return request({ url: 'jingyue/workitem/' + id + '/progress', method: 'post', data })
}
export function recalculateWorkItem(id) {
  return request({ url: 'jingyue/workitem/' + id + '/recalculate', method: 'post' })
}
export function listWorkTemplates(query) {
  return request({ url: 'jingyue/worktemplate/list', method: 'get', params: query })
}
export function getWorkTemplate(id) {
  return request({ url: 'jingyue/worktemplate/' + id, method: 'get' })
}
export function saveWorkTemplate(data) {
  return request({ url: 'jingyue/worktemplate', method: 'post', data })
}
export function deleteWorkTemplate(id) {
  return request({ url: 'jingyue/worktemplate/' + id, method: 'delete' })
}
export function applyWorkTemplate(id, data) {
  return request({ url: 'jingyue/worktemplate/' + id + '/apply', method: 'post', data })
}
export function listMemberFollows(query) {
  return request({ url: 'jingyue/memberfollow/list', method: 'get', params: query })
}
export function saveMemberFollow(data) {
  return request({ url: 'jingyue/memberfollow', method: 'post', data })
}
export function listDailyTasks(query) {
  return request({ url: 'jingyue/dailytask/list', method: 'get', params: query })
}
export function listDailyTaskTemplates() {
  return request({ url: 'jingyue/dailytask/templates', method: 'get' })
}
export function saveDailyTaskTemplate(data) {
  return request({ url: 'jingyue/dailytask/template', method: 'post', data })
}
export function deleteDailyTaskTemplate(id) {
  return request({ url: 'jingyue/dailytask/template/' + id, method: 'delete' })
}
export function completeDailyTask(id, completed = true) {
  return request({ url: 'jingyue/dailytask/' + id + '/complete', method: 'post', params: { completed } })
}
export function updateDailyTask(id, data) {
  return request({ url: 'jingyue/dailytask/' + id, method: 'post', data })
}
export function sortDailyTasks(ids) {
  return request({ url: 'jingyue/dailytask/sort', method: 'post', data: { ids } })
}
export function deleteDailyTask(id) {
  return request({ url: 'jingyue/dailytask/' + id, method: 'delete' })
}
