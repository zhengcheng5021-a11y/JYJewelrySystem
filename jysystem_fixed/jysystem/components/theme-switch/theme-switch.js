const theme = require('../../utils/theme')
Component({
  data: { theme: theme.current(), open: false, options: theme.list() },
  lifetimes: {
    attached() {
      this.refresh()
      this.unsubscribe = theme.subscribe((next) => this.setData({ theme: next }))
    },
    detached() {
      if (this.unsubscribe) this.unsubscribe()
    }
  },
  pageLifetimes: { show() { this.refresh() } },
  methods: {
    refresh() { this.setData({ theme: theme.current(), options: theme.list(), open: false }) },
    toggle() { this.setData({ open: !this.data.open }) },
    close() { this.setData({ open: false }) },
    choose(e) {
      const next = theme.use(e.currentTarget.dataset.id)
      this.setData({ theme: next, open: false })
      this.triggerEvent('change', next)
    }
  }
})
