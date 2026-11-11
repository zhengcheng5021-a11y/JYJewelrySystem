import defaultSettings from '@/settings'

const { theme, sideTheme, showSettings, topNav, tagsView, fixedHeader, sidebarLogo, dynamicTitle } = defaultSettings

const storageSetting = JSON.parse(localStorage.getItem('layout-setting')) || ''
const legacyThemes = ['#409EFF', '#409eff', '#1890ff', '#1890FF']
const resolvedTheme = !storageSetting.theme || legacyThemes.indexOf(storageSetting.theme) > -1 ? theme : storageSetting.theme
const state = {
  title: '',
  theme: resolvedTheme, //主题颜色
  sideTheme: storageSetting.sideTheme || sideTheme, //侧边主题样式
  topNav: storageSetting.topNav === undefined ? topNav : storageSetting.topNav,
  showSettings: showSettings,
  tagsView: storageSetting.tagsView === undefined ? tagsView : storageSetting.tagsView,
  fixedHeader: storageSetting.fixedHeader === undefined ? fixedHeader : storageSetting.fixedHeader,
  sidebarLogo: storageSetting.sidebarLogo === undefined ? sidebarLogo : storageSetting.sidebarLogo,
  dynamicTitle: storageSetting.dynamicTitle === undefined ? dynamicTitle : storageSetting.dynamicTitle
}

const mutations = {
  CHANGE_SETTING: (state, { key, value }) => {
    if (state.hasOwnProperty(key)) {
      state[key] = value
    }
  }
}

const actions = {
  //修改布局设置
  changeSetting({ commit }, data) {
    commit('CHANGE_SETTING', data)
  },
  // 设置网页标题
  setTitle({ commit }, title) {
    state.title = title;
    document.title = state.dynamicTitle ? `${title} - ${process.env.VUE_APP_TITLE}` : process.env.VUE_APP_TITLE;
  }
}

export default {
  namespaced: true,
  state,
  mutations,
  actions
}