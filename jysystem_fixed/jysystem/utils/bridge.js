const storage = require('./storage')

const KEYS = {
  saleGoods: 'jy_quick_sale_goods',
  saleMember: 'jy_quick_sale_member',
  saleCustom: 'jy_quick_sale_custom',
  followMember: 'jy_quick_follow_member',
  followTask: 'jy_quick_follow_task',
  saleDraft: 'jy_sale_draft',
  inboundDraft: 'jy_inbound_draft',
  uiPreferences: 'jy_ui_preferences'
}

function putGoods(item) { storage.set(KEYS.saleGoods, item || null) }
function takeGoods() { return storage.take(KEYS.saleGoods, null) }
function putSaleMember(item) { storage.set(KEYS.saleMember, item || null) }
function takeSaleMember() { return storage.take(KEYS.saleMember, null) }
function putCustom(item) { storage.set(KEYS.saleCustom, item || null) }
function takeCustom() { return storage.take(KEYS.saleCustom, null) }
function putFollowMember(item) { storage.set(KEYS.followMember, item || null) }
function takeFollowMember() { return storage.take(KEYS.followMember, null) }
function putFollowTask(item) { storage.set(KEYS.followTask, item || null) }
function takeFollowTask() { return storage.take(KEYS.followTask, null) }
function saveSaleDraft(value) { storage.set(KEYS.saleDraft, value || null) }
function getSaleDraft() { return storage.get(KEYS.saleDraft, null) }
function clearSaleDraft() { storage.remove(KEYS.saleDraft) }
function saveInboundDraft(value) { storage.set(KEYS.inboundDraft, value || null) }
function getInboundDraft() { return storage.get(KEYS.inboundDraft, null) }
function clearInboundDraft() { storage.remove(KEYS.inboundDraft) }
function preferences() { return storage.get(KEYS.uiPreferences, {}) || {} }
function preference(key, fallback) {
  const value = preferences()[key]
  return value === undefined ? fallback : value
}
function setPreference(key, value) {
  const next = Object.assign({}, preferences())
  next[key] = value
  storage.set(KEYS.uiPreferences, next)
  return next
}

module.exports = {
  KEYS,
  putGoods,
  takeGoods,
  putSaleMember,
  takeSaleMember,
  putCustom,
  takeCustom,
  putFollowMember,
  takeFollowMember,
  putFollowTask,
  takeFollowTask,
  saveSaleDraft,
  getSaleDraft,
  clearSaleDraft,
  saveInboundDraft,
  getInboundDraft,
  clearInboundDraft,
  preferences,
  preference,
  setPreference
}
