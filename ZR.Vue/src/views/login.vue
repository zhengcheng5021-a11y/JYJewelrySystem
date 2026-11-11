<template>
  <div class="login-page">
    <section class="hero" aria-hidden="true">
      <img class="hero-img" :src="hero" alt="" />
      <div class="hero-shade"></div>
      <div class="sheen"></div>
      <span class="petal p1"></span>
      <span class="petal p2"></span>
      <span class="petal p3"></span>
      <i class="glint g1"></i>
      <i class="glint g2"></i>
    </section>

    <section class="panel">
      <el-form ref="loginForm" :model="loginForm" :rules="loginRules" class="login-form">
        <img class="mark" :src="mark" alt="JY手作珠宝" />
        <h1>登录</h1>
        <p class="hint">进入 JY 手作珠宝工作台</p>
        <el-form-item prop="username">
          <el-input v-model="loginForm.username" type="text" auto-complete="off" placeholder="账号">
            <svg-icon slot="prefix" icon-class="user" class="el-input__icon input-icon" />
          </el-input>
        </el-form-item>
        <el-form-item prop="password">
          <el-input v-model="loginForm.password" type="password" auto-complete="off" placeholder="密码" @keyup.enter.native="handleLogin">
            <svg-icon slot="prefix" icon-class="password" class="el-input__icon input-icon" />
          </el-input>
        </el-form-item>
        <el-form-item prop="code" v-if="showCaptcha != 'off'">
          <el-input
            v-model="loginForm.code"
            auto-complete="off"
            placeholder="验证码"
            class="code-input"
            @keyup.enter.native="handleLogin"
            ref="codeTxt"
          >
            <svg-icon slot="prefix" icon-class="validCode" class="el-input__icon input-icon" />
          </el-input>
          <div class="login-code">
            <img :src="codeUrl" @click="getCode" class="login-code-img" />
          </div>
        </el-form-item>
        <div class="form-row">
          <el-checkbox v-model="loginForm.rememberMe">记住密码</el-checkbox>
          <router-link class="link-type" to="/register">注册账号</router-link>
        </div>
        <el-button :loading="loading" type="primary" class="submit-btn" @click.native.prevent="handleLogin">
          <span v-if="!loading">登录</span>
          <span v-else>登录中...</span>
        </el-button>
      </el-form>
      <div class="footer">{{ copyRight }}</div>
    </section>
  </div>
</template>

<script>
import { getCodeImg } from '@/api/system/login'
import Cookies from 'js-cookie'
import defaultSettings from '@/settings'
import { encrypt, decrypt } from '@/utils/jsencrypt'
import hero from '@/assets/image/login-hero.png'
import mark from '@/assets/logo/logo-cutout.png'

export default {
  name: 'Login',
  data() {
    return {
      hero,
      mark,
      codeUrl: '',
      cookiePassword: '',
      loginForm: {
        username: '',
        password: '',
        rememberMe: false,
        code: '',
        uuid: '',
      },
      title: defaultSettings.title,
      loginRules: {
        username: [{ required: true, trigger: 'blur', message: '用户名不能为空' }],
        password: [{ required: true, trigger: 'blur', message: '密码不能为空' }],
        code: [{ required: true, trigger: 'change', message: '验证码不能为空' }],
      },
      showCaptcha: '',
      loading: false,
      redirect: undefined,
    }
  },
  computed: {
    copyRight: function () {
      return defaultSettings.copyRight
    },
  },
  watch: {
    $route: {
      handler: function (route) {
        this.redirect = route.query && route.query.redirect
      },
      immediate: true,
    },
  },
  created() {
    this.getCode()
    this.getCookie()
  },
  methods: {
    getCode() {
      getCodeImg().then((res) => {
        this.codeUrl = res.data.img
        this.loginForm.uuid = res.data.uuid
        this.showCaptcha = res.data.captchaOff
        this.$forceUpdate()
      })
    },
    getCookie() {
      const username = Cookies.get('username')
      const password = Cookies.get('password')
      const rememberMe = Cookies.get('rememberMe')

      this.loginForm = {
        username: username === undefined ? this.loginForm.username : username,
        password: password === undefined ? this.loginForm.password : decrypt(password),
        rememberMe: rememberMe === undefined ? false : Boolean(rememberMe),
      }
    },
    handleLogin() {
      this.$refs.loginForm.validate((valid) => {
        if (valid) {
          this.loading = true
          if (this.loginForm.rememberMe) {
            Cookies.set('username', this.loginForm.username, { expires: 30 })
            Cookies.set('password', encrypt(this.loginForm.password), {
              expires: 30,
            })
            Cookies.set('rememberMe', this.loginForm.rememberMe, {
              expires: 30,
            })
          } else {
            Cookies.remove('username')
            Cookies.remove('password')
            Cookies.remove('rememberMe')
          }
          this.$store
            .dispatch('Login', this.loginForm)
            .then(() => {
              this.msgSuccess('登录成功')
              this.$router.push({ path: this.redirect || '/' })
            })
            .catch(() => {
              this.loading = false
              this.getCode()
            })
        }
      })
    },
  },
}
</script>

<style lang="scss">
.login-page {
  min-height: 100%;
  display: flex;
  background: #fff;
}

.hero {
  position: relative;
  flex: 1;
  min-height: 100vh;
  overflow: hidden;
  background: linear-gradient(145deg, #f9e9ec 0%, #f4dce3 100%);
  border-right: 1px solid rgba(196, 120, 138, 0.12);
}
.hero-img {
  position: absolute;
  z-index: 1;
  width: 78%;
  height: 78%;
  left: 11%;
  top: 11%;
  object-fit: contain;
  object-position: center;
  mix-blend-mode: multiply;
  filter: saturate(0.92) contrast(1.02);
  transform-origin: 50% 42%;
  animation: ken 26s ease-in-out infinite alternate;
}
.hero-shade {
  position: absolute;
  inset: 0;
  z-index: 2;
  background: radial-gradient(circle at 50% 46%, rgba(255, 255, 255, 0.15), transparent 50%), linear-gradient(90deg, rgba(255, 244, 246, 0) 70%, rgba(255, 255, 255, 0.35) 100%);
  pointer-events: none;
}
.sheen {
  z-index: 3;
  position: absolute;
  top: -20%;
  left: -40%;
  width: 38%;
  height: 140%;
  background: linear-gradient(100deg, transparent 0%, rgba(255, 255, 255, 0.08) 42%, rgba(255, 255, 255, 0.45) 50%, rgba(255, 255, 255, 0.08) 58%, transparent 100%);
  transform: rotate(8deg);
  animation: sweep 9s ease-in-out infinite;
  pointer-events: none;
}
.petal {
  z-index: 4;
  position: absolute;
  width: 14px;
  height: 18px;
  border-radius: 70% 10% 70% 10%;
  background: linear-gradient(160deg, rgba(255, 214, 224, 0.95), rgba(212, 140, 156, 0.55));
  opacity: 0.75;
  pointer-events: none;
  animation: fall 14s linear infinite;
}
.p1 { left: 18%; animation-duration: 16s; }
.p2 { left: 62%; animation-duration: 19s; animation-delay: -6s; width: 10px; height: 14px; }
.p3 { left: 40%; animation-duration: 22s; animation-delay: -11s; }
.glint {
  z-index: 4;
  position: absolute;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: radial-gradient(circle, #fff 0%, rgba(255, 255, 255, 0) 70%);
  pointer-events: none;
  animation: twinkle 4.8s ease-in-out infinite;
}
.g1 { left: 46%; top: 58%; }
.g2 { left: 58%; top: 34%; animation-delay: 1.8s; }

.panel {
  width: auto;
  flex: 1;
  max-width: none;
  min-height: 100vh;
  background: #fff;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 48px 56px 28px;
  position: relative;
}
.login-form {
  width: 100%;
}
.mark {
  width: 168px;
  height: auto;
  display: block;
  margin: 0 auto 18px;
}
h1 {
  margin: 0;
  text-align: center;
  font-size: 28px;
  font-weight: 600;
  letter-spacing: 6px;
  color: #3d2c32;
}
.hint {
  margin: 8px 0 28px;
  text-align: center;
  color: #a08a92;
  font-size: 13px;
  letter-spacing: 1px;
}
.login-form .el-input {
  height: 44px;
}
.login-form .el-input__inner {
  height: 44px;
  border-radius: 10px;
  border-color: #f0e2e6;
  background: #fff;
}
.input-icon {
  width: 14px;
  margin-left: 2px;
}
.code-input {
  width: 62%;
}
.login-code {
  width: 34%;
  height: 44px;
  float: right;
}
.login-code-img {
  width: 100%;
  height: 44px;
  border-radius: 10px;
  cursor: pointer;
  object-fit: cover;
}
.form-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 2px 0 22px;
}
.submit-btn {
  width: 100%;
  height: 46px;
  border: none;
  border-radius: 12px;
  letter-spacing: 4px;
  font-size: 15px;
  background: linear-gradient(135deg, #d9a0ae, #c4788a);
}
.footer {
  margin-top: 36px;
  text-align: center;
  color: #c4b0b6;
  font-size: 12px;
}

@keyframes ken {
  from { transform: scale(1); }
  to { transform: scale(1.06) translate3d(-1.2%, -1%, 0); }
}
@keyframes sweep {
  0%, 62% { left: -45%; opacity: 0; }
  70% { opacity: 1; }
  100% { left: 120%; opacity: 0; }
}
@keyframes fall {
  0% { top: -8%; transform: rotate(0deg); opacity: 0; }
  12% { opacity: 0.8; }
  100% { top: 108%; transform: rotate(220deg); opacity: 0; }
}
@keyframes twinkle {
  0%, 100% { opacity: 0.15; transform: scale(0.7); }
  50% { opacity: 1; transform: scale(1.5); }
}

@media (max-width: 900px) {
  .login-page { flex-direction: column; }
  .hero { flex: none; min-height: 42vh; }
  .panel { width: 100%; min-height: auto; padding: 32px 24px 24px; }
  .hero-img { width: 70%; height: 82%; left: 15%; top: 4%; }
}
@media (prefers-reduced-motion: reduce) {
  .hero-img, .sheen, .petal, .glint { animation: none; }
}
</style>
