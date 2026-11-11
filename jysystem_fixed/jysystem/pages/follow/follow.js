const themeUtil = require('../../utils/theme')
const session = require('../../utils/session')
const api = require('../../utils/api')
const list = require('../../utils/list')
const format = require('../../utils/format')
const ui = require('../../utils/ui')
const bridge = require('../../utils/bridge')
const navigation = require('../../utils/navigation')

const PAGE_SIZE = 20

function decorateFollow(item) {
  return Object.assign({}, item, {
    timeText: format.dateText(item.followTime || item.createTime),
    nextText: item.nextFollowTime ? format.dateText(item.nextFollowTime) : '未定'
  })
}

Page({
  data: {
    theme: {},
    keyword: '',
    rows: [],
    pageNum: 1,
    totalCount: 0,
    hasMore: false,
    loadingMore: false,
    loading: false,
    error: '',
    memberId: 0,
    memberName: '',
    content: '',
    nextTime: '',
    saving: false,
    drawer: false,
    drawerKeyword: '',
    drawerMembers: []
  },
  onShow() {
    themeUtil.apply(this)
    if (!session.requireLogin()) return
    const member = bridge.takeFollowMember()
    if (member && member.memberId) {
      this.setData({ memberId: member.memberId, memberName: member.memberName || '', keyword: member.memberName || member.phone || '' })
    }
    this.load()
  },
  async onPullDownRefresh() {
    await this.load()
    wx.stopPullDownRefresh()
  },
  onReachBottom() {
    if (this.data.hasMore && !this.data.loading && !this.data.loadingMore) {
      this.loadMore()
    }
  },
  scrollTop() { wx.pageScrollTo({ scrollTop: 0, duration: 260 }) },
  noop() {},
  onKeyword(e) { this.setData({ keyword: e.detail.value }) },
  onContent(e) { this.setData({ content: e.detail.value }) },
  onNextTime(e) { this.setData({ nextTime: e.detail.value }) },
  onDrawerKeyword(e) { this.setData({ drawerKeyword: e.detail.value }) },
  clearMember() {
    this.setData({ memberId: 0, memberName: '', keyword: '' })
    this.load()
  },
  async openMemberDrawer() {
    this.setData({ drawer: true, drawerKeyword: '', drawerMembers: [] })
    await this.searchDrawerMembers()
  },
  closeDrawer() {
    this.setData({ drawer: false, drawerKeyword: '' })
  },
  async searchDrawerMembers() {
    try {
      const data = await api.memberList({ pageNum: 1, pageSize: 40, keyword: this.data.drawerKeyword, status: 0 })
      this.setData({ drawerMembers: list.rows(data) })
    } catch (error) {
      ui.toast(error.message || '客户加载失败')
    }
  },
  selectMember(e) {
    const id = String(e.currentTarget.dataset.id)
    const row = this.data.drawerMembers.find((item) => String(item.id) === id)
    if (row) {
      this.setData({ memberId: row.id, memberName: row.memberName || '', drawer: false })
      this.load()
    }
  },
  async load() {
    this.setData({ loading: true, error: '', pageNum: 1 })
    try {
      const data = await api.followList({ pageNum: 1, pageSize: PAGE_SIZE, keyword: this.data.keyword, memberId: this.data.memberId || undefined })
      const rows = list.rows(data).map(decorateFollow)
      const total = list.total(data) || rows.length
      this.setData({
        rows,
        pageNum: 1,
        totalCount: total,
        hasMore: rows.length < total && rows.length >= PAGE_SIZE
      })
    } catch (error) { this.setData({ error: error.message || '加载失败' }) }
    finally { this.setData({ loading: false }) }
  },
  async loadMore() {
    if (this.data.loadingMore || !this.data.hasMore) return
    const nextPage = this.data.pageNum + 1
    this.setData({ loadingMore: true })
    try {
      const data = await api.followList({ pageNum: nextPage, pageSize: PAGE_SIZE, keyword: this.data.keyword, memberId: this.data.memberId || undefined })
      const nextRows = list.rows(data).map(decorateFollow)
      const merged = this.data.rows.concat(nextRows)
      const total = list.total(data) || merged.length
      this.setData({
        rows: merged,
        pageNum: nextPage,
        totalCount: total,
        hasMore: nextRows.length >= PAGE_SIZE && merged.length < total
      })
    } catch (error) { ui.toast(error.message || '加载更多失败') }
    finally { this.setData({ loadingMore: false }) }
  },
  async saveFollow() {
    if (!this.data.memberId) return ui.toast('请先选择要跟进的客户')
    if (!this.data.content.trim()) return ui.toast('请填写跟进内容')
    this.setData({ saving: true })
    try {
      await api.saveFollow({ memberId: this.data.memberId, followType: 1, content: this.data.content.trim(), nextFollowTime: this.data.nextTime ? this.data.nextTime + ' 00:00:00' : null })
      this.setData({ content: '', nextTime: '' })
      this.load()
      ui.toast('跟进记录已保存', 'success')
    } catch (error) { ui.toast(error.message || '保存失败') }
    finally { this.setData({ saving: false }) }
  },
  toTask(e) {
    const row = this.data.rows[e.currentTarget.dataset.index]
    if (!row) return
    bridge.putFollowTask({ title: '回访客户【' + (row.memberName || '未记名') + '】：' + String(row.content || '').slice(0, 20), remark: row.content || '' })
    navigation.tab('/pages/tasks/tasks')
  },
  onTheme() { themeUtil.apply(this) }
})
