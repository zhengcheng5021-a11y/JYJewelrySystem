const AGENT_BASE = 'http://127.0.0.1:18180'

async function callAgent(path, options = {}) {
  let response
  try {
    response = await fetch(AGENT_BASE + path, {
      ...options,
      headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }
    })
  } catch (error) {
    throw new Error('无法连接本机标签打印服务，请先启动 127.0.0.1:18180。')
  }
  let payload = null
  try { payload = await response.json() } catch (error) {}
  if (!response.ok || payload?.ok === false) {
    throw new Error(payload?.message || ('本机打印服务返回 HTTP ' + response.status))
  }
  return payload
}

export function agentHealth() {
  return callAgent('/api/health', { method: 'GET' })
}

export function agentStatus(port = 'USB,') {
  return callAgent('/api/status?port=' + encodeURIComponent(port), { method: 'GET' })
}

export function agentPreview(label) {
  return callAgent('/api/preview', { method: 'POST', body: JSON.stringify(label) })
}

export function agentPrint(label) {
  return callAgent('/api/print', { method: 'POST', body: JSON.stringify(label) })
}
