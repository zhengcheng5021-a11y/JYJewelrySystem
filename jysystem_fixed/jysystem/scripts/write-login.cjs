const fs = require('fs')
const svg = fs.readFileSync('scripts/logo-svg.min.txt', 'utf8')
const loginWxml = `<view class="page login-page" style="{{theme.screenBg}}">
  <view wx:if="{{logo}}" style="position:absolute;left:-999rpx;width:0;height:0;overflow:hidden;">${svg}</view>
  <canvas type="2d" id="zenCanvas" class="zen-canvas" bindtouchstart="onTouch"></canvas>
  <view class="mist">
    <view class="mountain"></view>
    <view class="orb" style="background:{{theme.orbGradient}};"></view>
  </view>
  <view class="screen">
    <view class="theme-row">
      <button class="theme-pill" bindtap="toggleMenu">
        <view class="dot" style="background:{{theme.dot}};"></view>
        <text>{{theme.label}}</text>
        <text class="caret">{{menuOpen ? '▲' : '▼'}}</text>
      </button>
    </view>
    <view class="brand" bindtap="pulseSparkles">
      <view class="logo-shell" style="background:{{theme.logoGlow}};">
        <rich-text class="logo-rich" nodes="{{logoNode}}"></rich-text>
      </view>
      <text class="brand-name" style="color:{{theme.textColor}};">JY手作珠宝</text>
      <view class="brand-slogan" style="color:{{theme.subTextColor}};">
        <text class="mini-line"></text><text>东方美学 · 为你而饰</text><text class="mini-line"></text>
      </view>
      <view class="knot" style="color:{{theme.subTextColor}};">
        <text class="mini-line wide"></text><text class="infinity">∞</text><text class="mini-line wide"></text>
      </view>
    </view>
    <view class="form-card" style="background:{{theme.cardBg}};border-color:{{theme.cardBorder}};">
      <view class="glow" style="background:{{theme.cardGlow}};"></view>
      <view class="card-head" style="border-color:{{theme.headBorder}};">
        <view class="head-left"><view class="seal" style="background:{{theme.seal}};"></view><text class="form-title" style="color:{{theme.textColor}};">会员账号登录</text></view>
        <text class="badge" style="color:{{theme.subTextColor}};">官方认证通道</text>
      </view>
      <text class="label" style="color:{{theme.subTextColor}};">账号 / 手机号</text>
      <view class="input-box" style="background:{{theme.inputBg}};border-color:{{theme.inputBorder}};">
        <text class="icon" style="color:{{theme.inputPlaceholder}};">@</text>
        <input class="input" value="{{username}}" style="color:{{theme.inputColor}};" placeholder="请输入用户名或手机号" placeholder-class="placeholder" placeholder-style="color:{{theme.inputPlaceholder}}" bindinput="onUser" />
        <text wx:if="{{username}}" class="clear" bindtap="clearUser">×</text>
      </view>
      <text class="label" style="color:{{theme.subTextColor}};">登录密码</text>
      <view class="input-box" style="background:{{theme.inputBg}};border-color:{{theme.inputBorder}};">
        <text class="icon" style="color:{{theme.inputPlaceholder}};">*</text>
        <input class="input" password="{{!passwordVisible}}" value="{{password}}" style="color:{{theme.inputColor}};" placeholder="请输入登录密码" placeholder-class="placeholder" placeholder-style="color:{{theme.inputPlaceholder}}" bindinput="onPass" />
        <text class="eye" style="color:{{theme.subTextColor}};" bindtap="togglePassword">{{passwordVisible ? '显示' : '隐藏'}}</text>
      </view>
      <view class="aux" style="color:{{theme.subTextColor}};">
        <view class="check" bindtap="toggleRemember"><view class="checkbox {{remember ? 'on' : ''}}" style="{{remember ? 'background:' + theme.buttonColor : ''}};"><text wx:if="{{remember}}" class="tick">✓</text></view><text>记住账号</text></view>
        <text bindtap="forgot">忘记密码？</text>
      </view>
      <button class="login-btn shimmer" style="background:{{theme.buttonGradient}};color:{{theme.buttonTextColor}};" loading="{{loading}}" bindtap="submitLogin">立 即 登 录</button>
      <view class="secondary" style="color:{{theme.subTextColor}};">
        <text bindtap="register">新用户注册</text><text class="divider">|</text><text bindtap="guest">暂不登录，先逛逛</text>
      </view>
      <view class="agreement" style="color:{{theme.subTextColor}};">
        <view class="checkbox {{agree ? 'on' : ''}}" style="{{agree ? 'background:' + theme.buttonColor : ''}};" bindtap="toggleAgree"><text wx:if="{{agree}}" class="tick">✓</text></view>
        <text>已阅读并同意 <text style="color:{{theme.linkColor}};" data-type="user" bindtap="openModal">《用户服务协议》</text> 与 <text style="color:{{theme.linkColor}};" data-type="privacy" bindtap="openModal">《隐私保护政策》</text></text>
      </view>
    </view>
    <view class="home-indicator" style="background:{{theme.indicator}};"></view>
  </view>
  <view wx:if="{{menuOpen}}" class="mask" bindtap="closeMenu">
    <view class="theme-menu" catchtap="noop">
      <view class="menu-kicker">东方画境 · 意境切换</view>
      <view class="menu-item" wx:for="{{themes}}" wx:key="id" data-id="{{item.id}}" bindtap="chooseTheme">
        <view class="menu-left"><view class="menu-dot" style="background:{{item.dot}};"></view><text>{{item.name}}</text></view>
        <text wx:if="{{theme.id===item.id}}" style="color:{{item.accent}};">已选</text>
      </view>
    </view>
  </view>
  <view wx:if="{{modal}}" class="modal-mask" bindtap="closeModal">
    <view class="modal-card">
      <view class="modal-head"><text class="modal-title">{{modalTitle}}</text><text class="modal-close" bindtap="closeModal">×</text></view>
      <scroll-view scroll-y class="modal-body"><text>{{modalBody}}</text></scroll-view>
      <button class="modal-ok" bindtap="closeModal">已阅读并同意</button>
    </view>
  </view>
</view>
`
fs.writeFileSync('pages/login/login.wxml', loginWxml)
console.log('login wxml')
