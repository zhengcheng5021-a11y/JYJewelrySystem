import request from '@/utils/request'

export function listReferralRewards(query) {
  return request({ url: 'jingyue/referral/list', method: 'get', params: query })
}
export function getReferralSummary(query) {
  return request({ url: 'jingyue/referral/summary', method: 'get', params: query })
}
export function payoutReferralRewards(data) {
  return request({ url: 'jingyue/referral/payout', method: 'post', data })
}
export function cancelReferralPayout(id) {
  return request({ url: 'jingyue/referral/' + id + '/cancel-payout', method: 'post' })
}
export function getReferralGraph(memberId) {
  return request({ url: 'jingyue/referral/graph/' + memberId, method: 'get' })
}
export function findReferrer(keyword) {
  return request({ url: 'jingyue/referral/referrer', method: 'get', params: { keyword } })
}

export function listReferralRates() {
  return request({ url: 'jingyue/referral/rates', method: 'get' })
}
export function saveReferralRates(data) {
  return request({ url: 'jingyue/referral/rates', method: 'post', data })
}

export function adjustReferralAmount(id, data) {
  return request({ url: 'jingyue/referral/' + id + '/amount', method: 'post', data })
}
