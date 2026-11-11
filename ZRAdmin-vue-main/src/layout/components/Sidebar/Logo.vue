<template>
  <div class="logo-container" :class="{ collapse: collapse }">
    <transition name="sidebarLogoFade">
      <router-link v-if="collapse" key="collapse" class="sidebar-logo-link" to="/">
        <img :src="logo" class="sidebar-logo" />
      </router-link>
      <router-link v-else key="expand" class="sidebar-logo-link" to="/">
        <img v-if="logo" :src="logo" class="sidebar-logo" />
        <h1 class="sidebar-title">{{ title }}</h1>
      </router-link>
    </transition>
  </div>
</template>

<script setup>
import logo from '@/assets/logo/logo-jy-transparent.png'

defineProps({
  collapse: {
    type: Boolean,
    required: true
  }
})

const title = ref(import.meta.env.VITE_APP_TITLE)
</script>

<style lang="scss" scoped>
.sidebarLogoFade-enter-active {
  transition: opacity 1.5s;
}

.sidebarLogoFade-enter,
.sidebarLogoFade-leave-to {
  opacity: 0;
}

.logo-container {
  position: relative;
  min-width: var(--el-aside-width);
  height: 64px;
  line-height: 64px;
  text-align: left;
  border-bottom: 1px solid #f2e9eb;

  & .sidebar-logo-link {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    height: 100%;
    width: 100%;
    overflow: hidden;
    padding: 0 16px;
    box-sizing: border-box;

    & .sidebar-logo {
      width: auto;
      height: 42px;
      max-width: 72px;
      object-fit: contain;
      flex-shrink: 0;
      vertical-align: middle;
      margin-right: 8px;
    }

    & .sidebar-title {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      margin: 0;
      color: var(--base-logo-title-color);
      font-weight: 650;
      line-height: 1.2;
      font-size: 15px;
      letter-spacing: 1px;
      font-family:
        Avenir,
        Helvetica Neue,
        Arial,
        Helvetica,
        sans-serif;
    }
  }

  &.collapse {
    .sidebar-logo-link {
      justify-content: center;
      padding: 0;
    }
    .sidebar-logo {
      margin-right: 0px;
    }
  }
}
</style>
