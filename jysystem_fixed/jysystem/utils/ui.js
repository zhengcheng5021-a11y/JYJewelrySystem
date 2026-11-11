function toast(title, icon) {
  wx.showToast({ title: String(title || '').slice(0, 28), icon: icon || 'none', duration: 2200 })
}

function alert(title, content) {
  return new Promise((resolve) => {
    wx.showModal({
      title: title || '提示',
      content: content || '',
      showCancel: false,
      confirmText: '知道了',
      success: resolve,
      fail: resolve
    })
  })
}

function confirm(title, content, confirmText) {
  return new Promise((resolve, reject) => {
    wx.showModal({
      title: title || '请确认',
      content: content || '',
      confirmText: confirmText || '确认',
      cancelText: '取消',
      success(res) { if (res.confirm) resolve(true); else reject(new Error('cancelled')) },
      fail: reject
    })
  })
}

function pick(label, items, display) {
  return new Promise((resolve, reject) => {
    const list = Array.isArray(items) ? items : []
    if (!list.length) {
      toast('暂无可选项')
      reject(new Error('empty'))
      return
    }
    wx.showActionSheet({
      alertText: label || '请选择',
      itemList: list.map((item) => String((display ? display(item) : item) || '未命名').slice(0, 42)),
      success(res) { resolve(list[res.tapIndex]) },
      fail: reject
    })
  })
}

function debounce(fn, delay) {
  let timer = null
  return function debounced() {
    const args = Array.from(arguments)
    clearTimeout(timer)
    timer = setTimeout(() => fn.apply(this, args), delay || 300)
  }
}

function loading(title) { wx.showLoading({ title: title || '加载中', mask: true }) }
function hideLoading() { wx.hideLoading() }

module.exports = { toast, alert, confirm, pick, debounce, loading, hideLoading }
