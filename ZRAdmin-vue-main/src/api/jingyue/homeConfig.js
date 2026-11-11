import request from '@/utils/request'

export function getBirthdayReminders() {
  return request({ url: 'jingyue/home-config/birthdays', method: 'get' })
}

export function getStockAlerts() {
  return request({ url: 'jingyue/home-config/stock-alerts', method: 'get' })
}

export function getStockAlertSettings() {
  return request({ url: 'jingyue/home-config/stock-alert-settings', method: 'get' })
}

export function saveStockAlertSetting(data) {
  return request({ url: 'jingyue/home-config/stock-alert-settings', method: 'post', data })
}

export function getPrintOptions() {
  return request({ url: 'jingyue/home-config/print-options', method: 'get' })
}

export function savePrintTemplate(data) {
  return request({ url: 'jingyue/home-config/print-template', method: 'post', data })
}

export function deletePrintTemplate(id) {
  return request({ url: 'jingyue/home-config/print-template/' + id, method: 'delete' })
}

export function savePrintBinding(data) {
  return request({ url: 'jingyue/home-config/print-binding', method: 'post', data })
}

export function renderPrintLabel(data) {
  return request({ url: 'jingyue/home-config/print-render', method: 'post', data })
}
