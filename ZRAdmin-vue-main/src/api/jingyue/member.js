import request, { downFile } from '@/utils/request'

export function listMember(query) {
  return request({ url: 'jingyue/member/list', method: 'get', params: query })
}

export async function exportMember(query) {
  await downFile('jingyue/member/export', { ...query })
}
export function getMember(id) {
  return request({ url: 'jingyue/member/' + id, method: 'get' })
}
export function getMemberOptions() {
  return request({ url: 'jingyue/member/options', method: 'get' })
}
export function getMemberDashboard() {
  return request({ url: 'jingyue/member/dashboard', method: 'get' })
}
export function saveMember(data) {
  return request({ url: 'jingyue/member', method: 'post', data })
}
export function deleteMember(id) {
  return request({ url: 'jingyue/member/' + id, method: 'delete' })
}
export function listMemberLevels() {
  return request({ url: 'jingyue/member/levels', method: 'get' })
}
export function saveMemberLevel(data) {
  return request({ url: 'jingyue/member/level', method: 'post', data })
}
export function adjustMemberPoints(data) {
  return request({ url: 'jingyue/member/points', method: 'post', data })
}
export function listMemberPoints(query) {
  return request({ url: 'jingyue/member/points', method: 'get', params: query })
}

export function getMemberHistory(id) {
  return request({ url: 'jingyue/member/' + id + '/history', method: 'get' })
}
export function listMemberReferrers(id) {
  return request({ url: 'jingyue/member/' + id + '/referrers', method: 'get' })
}
export function addMemberReferrer(id, data) {
  return request({ url: 'jingyue/member/' + id + '/referrer', method: 'post', data })
}
export function setPrimaryMemberReferrer(id, linkId) {
  return request({ url: 'jingyue/member/' + id + '/referrer/' + linkId + '/primary', method: 'post' })
}
export function removeMemberReferrer(id, linkId) {
  return request({ url: 'jingyue/member/' + id + '/referrer/' + linkId, method: 'delete' })
}
export function getPointRule() {
  return request({ url: 'jingyue/member/point-rule', method: 'get' })
}
export function savePointRule(data) {
  return request({ url: 'jingyue/member/point-rule', method: 'post', data })
}
export function clearAllPoints() {
  return request({ url: 'jingyue/member/points/clear-all', method: 'post' })
}
export function listMemberReviews(query) {
  return request({ url: 'jingyue/memberreview/list', method: 'get', params: query })
}
export function saveMemberReview(data) {
  return request({ url: 'jingyue/memberreview', method: 'post', data })
}
export function deleteMemberReview(id) {
  return request({ url: 'jingyue/memberreview/' + id, method: 'delete' })
}
