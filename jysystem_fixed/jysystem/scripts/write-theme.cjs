const fs = require('fs')
const path = require('path')
const root = path.resolve(__dirname, '..')
function write(rel, content) {
  fs.writeFileSync(path.join(root, rel), content.replace(/\n/g, '\r\n'), 'utf8')
}
write('utils/theme.js', `const KEY = 'jingyue_theme'

const THEMES = {
  blush: {
    id: 'blush',
    label: '绯粉春霖',
    short: '画境 · 绯粉',
    navFront: '#000000',
    navBg: '#FDF5F2',
    tabColor: '#8d6d68',
    tabSelected: '#5A3835',
    tabBg: '#FDF5F2',
    tabBorder: 'white',
    style: '--ink:#3A2624;--muted:#8a645f;--paper:#FDF5F2;--surface:rgba(255,255,255,.62);--line:rgba(216,156,146,.35);--jade:#5A3835;--jade-soft:#f8e4df;--gold:#b58555;--red:#9a4d55;--blue:#7d5b63;background:linear-gradient(180deg,#FDF5F2 0%,#F8E8E4 52%,#F1DAD4 100%);color:#3A2624;'
  },
  dark: {
    id: 'dark',
    label: '玄黛金辉',
    short: '画境 · 玄黛',
    navFront: '#ffffff',
    navBg: '#1D1618',
    tabColor: '#b7a093',
    tabSelected: '#DFB07A',
    tabBg: '#1D1618',
    tabBorder: 'black',
    style: '--ink:#FBE9DC;--muted:#D8BEAA;--paper:#1D1618;--surface:rgba(35,27,30,.72);--line:rgba(223,176,122,.28);--jade:#DFB07A;--jade-soft:rgba(223,176,122,.16);--gold:#DFB07A;--red:#e7b2a4;--blue:#d8beaa;background:linear-gradient(180deg,#1D1618 0%,#2A1F22 52%,#181113 100%);color:#FBE9DC;'
  },
  celadon: {
    id: 'celadon',
    label: '烟雨天青',
    short: '画境 · 天青',
    navFront: '#000000',
    navBg: '#EFF6F3',
    tabColor: '#6d857c',
    tabSelected: '#243831',
    tabBg: '#EFF6F3',
    tabBorder: 'white',
    style: '--ink:#21352E;--muted:#5d756c;--paper:#EFF6F3;--surface:rgba(255,255,255,.66);--line:rgba(144,180,168,.4);--jade:#243831;--jade-soft:#e4f0eb;--gold:#b08a45;--red:#8d5a55;--blue:#3e564e;background:linear-gradient(180deg,#EFF6F3 0%,#E3EEE9 52%,#D4E4DC 100%);color:#21352E;'
  }
}

function current() {
  const id = wx.getStorageSync(KEY) || 'blush'
  return THEMES[id] || THEMES.blush
}

function apply(page) {
  const theme = current()
  if (page && page.setData) page.setData({ theme })
  wx.setNavigationBarColor({ frontColor: theme.navFront, backgroundColor: theme.navBg, animation: { duration: 280, timingFunc: 'easeIn' } })
  wx.setTabBarStyle({ color: theme.tabColor, selectedColor: theme.tabSelected, backgroundColor: theme.tabBg, borderStyle: theme.tabBorder })
  return theme
}

function use(id, page) {
  if (!THEMES[id]) return current()
  wx.setStorageSync(KEY, id)
  return apply(page)
}

function list() { return [THEMES.blush, THEMES.dark, THEMES.celadon] }

module.exports = { THEMES, current, apply, use, list }
`)
write('components/theme-switch/theme-switch.js', `const theme = require('../../utils/theme')
Component({
  data: { theme: theme.current(), open: false, options: theme.list() },
  lifetimes: { attached() { this.refresh() } },
  pageLifetimes: { show() { this.refresh() } },
  methods: {
    refresh() { this.setData({ theme: theme.current(), options: theme.list() }) },
    toggle() { this.setData({ open: !this.data.open }) },
    choose(e) {
      const next = theme.use(e.currentTarget.dataset.id)
      this.setData({ theme: next, open: false })
      this.triggerEvent('change', next)
    }
  }
})
`)
write('components/theme-switch/theme-switch.json', '{ "component": true }\n')
write('components/theme-switch/theme-switch.wxml', `<view class="theme-switch">
  <view class="theme-pill" bindtap="toggle">
    <text class="dot"></text>
    <text>{{theme.short}}</text>
    <text class="caret">{{open ? '▴' : '▾'}}</text>
  </view>
  <view wx:if="{{open}}" class="theme-menu">
    <view class="menu-kicker">东方画境 · 意境切换</view>
    <view class="menu-item {{theme.id===item.id?'on':''}}" wx:for="{{options}}" wx:key="id" data-id="{{item.id}}" bindtap="choose">
      <text>{{item.label}}</text>
      <text wx:if="{{theme.id===item.id}}">已选</text>
    </view>
  </view>
</view>
`)
write('components/theme-switch/theme-switch.wxss', `.theme-switch { position: relative; z-index: 20; }
.theme-pill { display: inline-flex; align-items: center; gap: 10rpx; min-height: 58rpx; padding: 0 18rpx; border: 1rpx solid var(--line); border-radius: 999rpx; background: var(--surface); color: var(--ink); font-size: 22rpx; }
.dot { width: 14rpx; height: 14rpx; border-radius: 50%; background: var(--jade); }
.caret { color: var(--muted); font-size: 18rpx; }
.theme-menu { position: absolute; left: 0; top: 70rpx; width: 320rpx; padding: 12rpx; border: 1rpx solid var(--line); border-radius: 22rpx; background: var(--surface); box-shadow: 0 16rpx 40rpx rgba(60,40,39,.12); }
.menu-kicker { padding: 8rpx 12rpx 12rpx; border-bottom: 1rpx solid var(--line); color: var(--muted); font-size: 20rpx; letter-spacing: 2rpx; }
.menu-item { display: flex; align-items: center; justify-content: space-between; min-height: 72rpx; padding: 0 12rpx; border-radius: 16rpx; color: var(--ink); font-size: 24rpx; }
.menu-item.on { background: var(--jade-soft); font-weight: 700; }
`)
console.log('theme ok')
