import request from '@/utils/request'

export function listActivity(query) {
  return request({ url: 'jingyue/activity/list', method: 'get', params: query })
}
export function getActivity(id) {
  return request({ url: 'jingyue/activity/' + id, method: 'get' })
}
export function getActivityOptions() {
  return request({ url: 'jingyue/activity/options', method: 'get' })
}
export function getActivityDashboard() {
  return request({ url: 'jingyue/activity/dashboard', method: 'get' })
}
export function saveActivity(data) {
  return request({ url: 'jingyue/activity', method: 'post', data })
}
export function setActivityStatus(data) {
  return request({ url: 'jingyue/activity/status', method: 'post', data })
}
export function deleteActivity(id) {
  return request({ url: 'jingyue/activity/' + id, method: 'delete' })
}
