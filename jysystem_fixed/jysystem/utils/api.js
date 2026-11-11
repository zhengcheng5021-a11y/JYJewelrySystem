const { request, uploadFile } = require('./request')

function login(data) { return request({ url: '/login', method: 'POST', data }) }
function getInfo() { return request({ url: '/getInfo', method: 'GET' }) }
function logout() { return request({ url: '/LogOut', method: 'POST' }) }
function upload(filePath) { return uploadFile(filePath, { classifyType: 'jingyue' }) }
function inboundOptions() { return request({ url: '/jingyue/inbound/options', method: 'GET' }) }
function inboundDashboard() { return request({ url: '/jingyue/inbound/dashboard', method: 'GET' }) }
function inboundList(params) { return request({ url: '/jingyue/inbound/list', method: 'GET', data: params }) }
function inboundDetail(id) { return request({ url: '/jingyue/inbound/' + id, method: 'GET' }) }
function saveInbound(data) { return request({ url: '/jingyue/inbound', method: 'POST', data }) }
function confirmInbound(id) { return request({ url: '/jingyue/inbound/' + id + '/confirm', method: 'POST' }) }
function deleteInbound(id) { return request({ url: '/jingyue/inbound/' + id, method: 'DELETE' }) }
function memberList(params) { return request({ url: '/jingyue/member/list', method: 'GET', data: params }) }
function memberDetail(id) { return request({ url: '/jingyue/member/' + id, method: 'GET' }) }
function memberOptions() { return request({ url: '/jingyue/member/options', method: 'GET' }) }
function saveMember(data) { return request({ url: '/jingyue/member', method: 'POST', data }) }
function stockList(params) { return request({ url: '/jingyue/stock/list', method: 'GET', data: params }) }
function stockSummary() { return request({ url: '/jingyue/stock/summary', method: 'GET' }) }
function stockLedger(params) { return request({ url: '/jingyue/stock/ledger', method: 'GET', data: params }) }
function saleDashboard() { return request({ url: '/jingyue/sale/dashboard', method: 'GET' }) }
function saleList(params) { return request({ url: '/jingyue/sale/list', method: 'GET', data: params }) }
function saleGoods(keyword) { return request({ url: '/jingyue/sale/goods', method: 'GET', data: { keyword } }) }
function saleCustomer(keyword) { return request({ url: '/jingyue/sale/customer', method: 'GET', data: { keyword } }) }
function saleOptions() { return request({ url: '/jingyue/sale/options', method: 'GET' }) }
function previewSale(data) { return request({ url: '/jingyue/sale/preview', method: 'POST', data }) }
function saleDetail(id) { return request({ url: '/jingyue/sale/' + id, method: 'GET' }) }
function confirmSale(id) { return request({ url: '/jingyue/sale/' + id + '/confirm', method: 'POST' }) }
function saveSale(data) { return request({ url: '/jingyue/sale', method: 'POST', data }) }
function workItems(params) { return request({ url: '/jingyue/workitem/list', method: 'GET', data: params }) }
function workStats() { return request({ url: '/jingyue/workitem/stats', method: 'GET' }) }
function workOptions() { return request({ url: '/jingyue/workitem/options', method: 'GET' }) }
function saveWorkItem(data) { return request({ url: '/jingyue/workitem', method: 'POST', data }) }
function completeWorkItem(id, completed) { return request({ url: '/jingyue/workitem/' + id + '/complete?completed=' + (completed !== false), method: 'POST' }) }


function scriptList(params) { return request({ url: '/jingyue/script/list', method: 'GET', data: params }) }
function saveScript(data) { return request({ url: '/jingyue/script', method: 'POST', data }) }
function deleteScript(id) { return request({ url: '/jingyue/script/' + encodeURIComponent(String(id)), method: 'DELETE' }) }
function customList(params) { return request({ url: '/jingyue/custom/list', method: 'GET', data: params }) }
function customArrive(id, data) { return request({ url: '/jingyue/custom/' + id + '/arrive', method: 'POST', data: data || {} }) }
function customDetail(id) { return request({ url: '/jingyue/custom/' + id, method: 'GET' }) }
function saveCustom(data) { return request({ url: '/jingyue/custom', method: 'POST', data }) }
function refundCustom(data) { return request({ url: '/jingyue/custom/refund', method: 'POST', data }) }
function deleteCustom(id) { return request({ url: '/jingyue/custom/' + id, method: 'DELETE' }) }
function lookupOptions(typeCode) { return request({ url: '/jingyue/lookup/options', method: 'GET', data: { typeCode } }) }
function followList(params) { return request({ url: '/jingyue/memberfollow/list', method: 'GET', data: params }) }
function saveFollow(data) { return request({ url: '/jingyue/memberfollow', method: 'POST', data }) }
function dailyList(params) { return request({ url: '/jingyue/dailytask/list', method: 'GET', data: params }) }
function dailyComplete(id, completed) { return request({ url: '/jingyue/dailytask/' + id + '/complete?completed=' + (completed !== false), method: 'POST' }) }
function salesGoals(params) { return request({ url: '/jingyue/report/goals', method: 'GET', data: params }) }

module.exports = { login, getInfo, logout, upload, inboundOptions, inboundList, inboundDetail, saveInbound, confirmInbound, deleteInbound, memberList, memberDetail, memberOptions, saveMember, stockList, stockSummary, stockLedger, saleDashboard, saleList, saleGoods, saleCustomer, saleOptions, previewSale, saveSale, saleDetail, confirmSale, inboundDashboard, workItems, workStats, workOptions, saveWorkItem, completeWorkItem, scriptList, saveScript, deleteScript, customList, customDetail, customArrive, saveCustom, refundCustom, deleteCustom, lookupOptions, followList, saveFollow, dailyList, dailyComplete, salesGoals }
