const fs = require('fs')
const pages = ['pages/inbound/inbound.wxml','pages/inbound/edit.wxml','pages/members/members.wxml','pages/stock/stock.wxml','pages/sale/sale.wxml','pages/tasks/tasks.wxml','pages/profile/profile.wxml']
for (const p of pages) {
  let s = fs.readFileSync(p, 'utf8')
  s = s.replace(/<view class="page scene" style="\{\{theme\.style\}\}"><theme-switch bind:change="onTheme"><\/theme-switch><view class="page[^"]*">/, '<view class="page scene" style="{{theme.style}}">\n  <theme-switch bind:change="onTheme"></theme-switch>')
  fs.writeFileSync(p, s)
}
const home = `<view class="page scene with-tab" style="{{theme.style}}">
  <view class="brand-row">
    <theme-switch bind:change="onTheme"></theme-switch>
    <text class="brand-en">JY ATELIER</text>
  </view>
  <view class="brand-block">
    <view class="mark">JY</view>
    <view class="brand-name">JY手作珠宝</view>
    <view class="brand-slogan"><text class="line"></text><text>东方美学 · 为你而饰</text><text class="line"></text></view>
    <view class="hello">{{userName}} · {{today}}</view>
  </view>
  <view class="glass">
    <view class="glass-head"><text class="seal"></text><text>今日柜面</text><text class="ghost" bindtap="load">刷新</text></view>
    <view class="grid-2">
      <view class="stat" bindtap="go" data-url="/pages/sale/sale"><text>今日成交</text><b>{{saleAmount}}</b><small>{{saleOrders}} 单</small></view>
      <view class="stat" bindtap="tab" data-tab="/pages/inbound/inbound"><text>待确认入库</text><b>{{draftCount}}</b><small>成本 {{inboundCost}}</small></view>
      <view class="stat" bindtap="go" data-url="/pages/stock/stock"><text>在库件数</text><b>{{stockQty}}</b><small>货值 {{stockSale}}</small></view>
      <view class="stat" bindtap="tab" data-tab="/pages/tasks/tasks"><text>今日待办</text><b>{{todoCount}}</b><small>逾期 {{overdueCount}}</small></view>
    </view>
  </view>
  <view wx:if="{{error}}" class="glass warn">{{error}}</view>
  <view class="glass">
    <view class="glass-head"><text class="seal"></text><text>现场操作</text></view>
    <view class="op" bindtap="go" data-url="/pages/inbound/edit"><view><b>拍照入库</b><small>拍货品、填名称和价格，先存草稿</small></view><text>›</text></view>
    <view class="op" bindtap="go" data-url="/pages/sale/sale"><view><b>现场开单</b><small>搜货品、认客户、预览后收款</small></view><text>›</text></view>
    <view class="op" bindtap="go" data-url="/pages/stock/stock"><view><b>查库存</b><small>按编码、名称或条码查找</small></view><text>›</text></view>
    <view class="op" bindtap="tab" data-tab="/pages/members/members"><view><b>记客户</b><small>搜索档案，补一条新客户</small></view><text>›</text></view>
  </view>
</view>
`
fs.writeFileSync('pages/home/home.wxml', home)
const login = `<view class="page scene login-page" style="{{theme.style}}">
  <view class="brand-row"><theme-switch bind:change="onTheme"></theme-switch><text class="brand-en">官方认证通道</text></view>
  <view class="brand-block">
    <view class="mark float">JY</view>
    <view class="brand-name">JY手作珠宝</view>
    <view class="brand-slogan"><text class="line"></text><text>东方美学 · 为你而饰</text><text class="line"></text></view>
  </view>
  <view class="glass login-card">
    <view class="glass-head"><text class="seal"></text><text>会员账号登录</text></view>
    <text class="label">服务地址</text>
    <input class="field" value="{{apiBase}}" placeholder="http://localhost:8888" placeholder-class="ph" data-field="apiBase" bindinput="onInput" />
    <text class="label">租户编号</text>
    <input class="field" value="{{tenantId}}" placeholder="0" placeholder-class="ph" data-field="tenantId" bindinput="onInput" />
    <text class="label">账号 / 手机号</text>
    <input class="field" value="{{username}}" placeholder="请输入用户名或手机号" placeholder-class="ph" data-field="username" bindinput="onInput" />
    <text class="label">登录密码</text>
    <view class="field-row">
      <input class="field bare" password="{{!showPassword}}" value="{{password}}" placeholder="请输入登录密码" placeholder-class="ph" data-field="password" bindinput="onInput" />
      <text class="eye" bindtap="togglePassword">{{showPassword ? '隐藏' : '显示'}}</text>
    </view>
    <view class="aux"><text>记住账号</text><text>模拟器请关闭域名校验</text></view>
    <button class="enter" loading="{{loading}}" disabled="{{loading}}" bindtap="submit">立 即 登 录</button>
    <view wx:if="{{error}}" class="warn-text">{{error}}</view>
  </view>
</view>
`
fs.writeFileSync('pages/login/login.wxml', login)
console.log('views')
