const session = require('./utils/session')

App({
  globalData: { apiBase: '', user: null },
  onLaunch() {
    const info = session.getConfig()
    this.globalData.apiBase = info.apiBase
    this.globalData.user = session.getUser()
  }
})
