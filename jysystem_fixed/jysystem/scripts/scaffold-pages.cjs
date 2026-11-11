const fs = require('fs')
const path = require('path')
const root = path.resolve(__dirname, '..')
function write(rel, content) {
  const full = path.join(root, rel)
  fs.mkdirSync(path.dirname(full), { recursive: true })
  fs.writeFileSync(full, content.replace(/\n/g, '\r\n'), 'utf8')
}
const pageJson = (title) => JSON.stringify({
  navigationBarTitleText: title,
  usingComponents: {
    'page-head': '/components/page-head/page-head',
    'metric-tile': '/components/metric-tile/metric-tile',
    'state-view': '/components/state-view/state-view',
    'field-row': '/components/field-row/field-row'
  }
}, null, 2) + '\n'
const wxss = `page { background: #f4f0e8; }
.stack { display: flex; flex-direction: column; gap: 18rpx; }
.grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 16rpx; }
.card { overflow: hidden; }
.pad { padding: 8rpx 24rpx 20rpx; }
.list-item { display: flex; align-items: center; gap: 16rpx; padding: 22rpx 24rpx; border-bottom: 1rpx solid var(--line); }
.list-item:last-child { border-bottom: 0; }
.item-title { font-size: 30rpx; font-weight: 700; line-height: 1.35; }
.item-sub { margin-top: 6rpx; color: var(--muted); font-size: 23rpx; line-height: 1.45; }
.search { display: flex; gap: 12rpx; margin-top: 22rpx; }
.search .input { flex: 1; min-height: 76rpx; margin: 0; padding: 0 18rpx; border: 1rpx solid var(--line); border-radius: 14rpx; background: #fffdf8; }
.chips { display: flex; flex-wrap: wrap; gap: 12rpx; margin-top: 18rpx; }
.chip { padding: 10rpx 18rpx; border: 1rpx solid var(--line); border-radius: 999rpx; background: #fffdf8; color: var(--muted); font-size: 23rpx; }
.chip.active { border-color: var(--jade); background: var(--jade-soft); color: var(--jade); font-weight: 700; }
.actions { display: flex; gap: 14rpx; margin-top: 22rpx; }
.actions .btn { flex: 1; }
.photo { width: 168rpx; height: 168rpx; border: 1rpx dashed var(--line); border-radius: 16rpx; background: #faf7f1; }
.line-card { margin-top: 18rpx; padding: 20rpx; border: 1rpx solid var(--line); border-radius: 16rpx; background: #fffdf8; }
.bottom-bar { position: sticky; bottom: 0; display: flex; gap: 14rpx; margin: 28rpx -28rpx -52rpx; padding: 18rpx 28rpx calc(18rpx + env(safe-area-inset-bottom)); background: rgba(244, 240, 232, .96); }
.hero-card { padding: 28rpx; }
`
const pages = {
  'pages/login/login': '登录',
  'pages/home/home': '今日',
  'pages/inbound/inbound': '入库',
  'pages/inbound/edit': '录入入库',
  'pages/members/members': '客户',
  'pages/stock/stock': '库存',
  'pages/sale/sale': '开单',
  'pages/tasks/tasks': '待办',
  'pages/profile/profile': '我的'
}
Object.keys(pages).forEach((page) => {
  write(page + '.json', pageJson(pages[page]))
  write(page + '.wxss', wxss)
})
try { fs.unlinkSync(path.join(root, 'components/page-head/test.txt')) } catch (e) {}
console.log('scaffolded')
