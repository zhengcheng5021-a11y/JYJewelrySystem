import request from '@/utils/request'

/** 获取后端服务器当前时间，不弹出业务错误提示。 */
export function getServerTime() {
  return request({
    url: 'jingyue/time/now',
    method: 'get',
    skipErrorNotify: true
  })
}
