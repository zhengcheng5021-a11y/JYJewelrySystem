import { getServerTime } from '@/api/jingyue/time'

const CACHE_MS = 5 * 60 * 1000
let clockOffsetMs = 0
let serverOffsetMs = null
let syncTask = null
let lastSyncAt = 0

function pad(value) {
  return String(value).padStart(2, '0')
}

function clientOffsetMs() {
  return -new Date().getTimezoneOffset() * 60000
}

function wallClockDate(instantMs) {
  // 将服务器真实时刻换算为“服务器本地墙上时间”，避免浏览器时区不同导致显示偏移。
  const adjusted = instantMs + clockOffsetMs + (serverOffsetMs == null ? 0 : serverOffsetMs - clientOffsetMs())
  return new Date(adjusted)
}

/** 同步服务器时钟。5 分钟内复用结果；失败时静默回退到浏览器时间。 */
export function syncServerTime(force = false) {
  if (!force && lastSyncAt && Date.now() - lastSyncAt < CACHE_MS) {
    return Promise.resolve(serverNow())
  }
  if (syncTask) return syncTask
  syncTask = getServerTime()
    .then((res) => {
      const data = res?.data || {}
      const timestamp = Number(data.timestamp)
      if (!Number.isFinite(timestamp)) throw new Error('服务器时间接口返回无效 timestamp')
      clockOffsetMs = timestamp - Date.now()
      serverOffsetMs = Number.isFinite(Number(data.utcOffsetMinutes)) ? Number(data.utcOffsetMinutes) * 60000 : null
      lastSyncAt = Date.now()
      return serverNow()
    })
    .catch((error) => {
      console.warn('[serverTime] 同步失败，暂用浏览器时钟', error)
      return serverNow()
    })
    .finally(() => {
      syncTask = null
    })
  return syncTask
}

/** 返回按服务器时区显示的当前时间。 */
export function serverNow() {
  return wallClockDate(Date.now())
}

export function formatServerDate(date = serverNow()) {
  const value = date instanceof Date ? date : new Date(date)
  if (Number.isNaN(value.getTime())) return ''
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`
}

export function serverTodayText() {
  return formatServerDate(serverNow())
}

export function serverDateTimeText(date = serverNow()) {
  const value = date instanceof Date ? date : new Date(date)
  if (Number.isNaN(value.getTime())) return ''
  return `${formatServerDate(value)} ${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

/** 以服务器今天为终点，返回最近 N 天（含今天）的日期区间。 */
export function serverRange(daysAgo = 6) {
  const end = serverNow()
  const start = new Date(end)
  start.setDate(start.getDate() - Math.max(0, Number(daysAgo) || 0))
  return [formatServerDate(start), formatServerDate(end)]
}

/** 按服务器墙上时间口径解析后端日期时间，避免浏览器时区换算。 */
export function parseServerDateTime(value) {
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : new Date(value.getTime())
  }
  if (value == null || value === '') return null

  const text = String(value).trim().replace('T', ' ')
  const match = /^(\d{4})-(\d{2})-(\d{2})(?:\s+(\d{2}):(\d{2})(?::(\d{2}))?)?/.exec(text)
  if (!match) return null

  const [, year, month, day, hour = '00', minute = '00', second = '00'] = match
  const date = new Date(Number(year), Number(month) - 1, Number(day), Number(hour), Number(minute), Number(second))
  return Number.isNaN(date.getTime()) ? null : date
}

export function getServerTimeOffsetMinutes() {
  return serverOffsetMs == null ? null : serverOffsetMs / 60000
}
