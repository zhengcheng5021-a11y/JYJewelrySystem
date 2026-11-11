import settings from '@/settings'
// auth.js

const TokenKey = 'ZR-Token'

function getStorage() {
  return settings.tokenSaveType === 'sessionStorage' ? sessionStorage : localStorage
}

export function getToken() {
  return getStorage().getItem(TokenKey)
}

export function setToken(token) {
  return getStorage().setItem(TokenKey, token)
}

export function removeToken() {
  return getStorage().removeItem(TokenKey)
}
