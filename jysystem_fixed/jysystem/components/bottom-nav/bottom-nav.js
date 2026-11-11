const themeUtil = require('../../utils/theme')
const navigation = require('../../utils/navigation')

const TABS = [
  { id: 'home', icon: '❖', label: '今日', path: '/pages/home/home' },
  { id: 'inbound', icon: '📷', label: '入库', path: '/pages/inbound/inbound' },
  { id: 'members', icon: '♛', label: '客户', path: '/pages/members/members' },
  { id: 'tasks', icon: '⚡', label: '待办', path: '/pages/tasks/tasks' },
  { id: 'profile', icon: '❀', label: '我的', path: '/pages/profile/profile' }
]

Component({
  data: { theme: {}, tabs: TABS },
  properties: {
    active: { type: String, value: '' }
  },
  lifetimes: {
    attached() {
      this.refresh()
      this.unsubscribe = themeUtil.subscribe((theme) => this.setData({ theme }))
    },
    detached() {
      if (this.unsubscribe) this.unsubscribe()
    }
  },
  pageLifetimes: { show() { this.refresh() } },
  methods: {
    refresh() { this.setData({ theme: themeUtil.current(), tabs: TABS }) },
    open(e) {
      const path = e.currentTarget.dataset.path
      const pages = getCurrentPages()
      const current = pages.length ? '/' + pages[pages.length - 1].route : ''
      if (!path || path === current) return
      navigation.tab(path)
    }
  }
})
