/* Compact UTF-8 MD5 implementation for account login payloads. */
const S = [
  7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
  5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
  4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
  6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
]
const K = Array.from({ length: 64 }, (_, i) => Math.floor(Math.abs(Math.sin(i + 1)) * 0x100000000) >>> 0)

function words(text) {
  const bytes = []
  for (let i = 0; i < text.length; i += 1) {
    let code = text.charCodeAt(i)
    if (code >= 0xd800 && code <= 0xdbff && i + 1 < text.length) {
      code = 0x10000 + ((code - 0xd800) << 10) + (text.charCodeAt(++i) - 0xdc00)
    }
    if (code < 0x80) bytes.push(code)
    else if (code < 0x800) bytes.push(0xc0 | (code >> 6), 0x80 | (code & 63))
    else if (code < 0x10000) bytes.push(0xe0 | (code >> 12), 0x80 | ((code >> 6) & 63), 0x80 | (code & 63))
    else bytes.push(0xf0 | (code >> 18), 0x80 | ((code >> 12) & 63), 0x80 | ((code >> 6) & 63), 0x80 | (code & 63))
  }
  const length = bytes.length
  bytes.push(0x80)
  while ((bytes.length % 64) !== 56) bytes.push(0)
  const bitsLow = (length * 8) >>> 0
  const bitsHigh = Math.floor(length / 0x20000000) >>> 0
  for (let i = 0; i < 4; i += 1) bytes.push((bitsLow >>> (i * 8)) & 255)
  for (let i = 0; i < 4; i += 1) bytes.push((bitsHigh >>> (i * 8)) & 255)
  const result = []
  for (let i = 0; i < bytes.length; i += 4) {
    result.push((bytes[i] | (bytes[i + 1] << 8) | (bytes[i + 2] << 16) | (bytes[i + 3] << 24)) >>> 0)
  }
  return result
}

function rotate(value, amount) { return ((value << amount) | (value >>> (32 - amount))) >>> 0 }

function md5(text) {
  const input = words(String(text))
  let a0 = 0x67452301, b0 = 0xefcdab89, c0 = 0x98badcfe, d0 = 0x10325476
  for (let offset = 0; offset < input.length; offset += 16) {
    let a = a0, b = b0, c = c0, d = d0
    for (let i = 0; i < 64; i += 1) {
      let f, g
      if (i < 16) { f = (b & c) | (~b & d); g = i }
      else if (i < 32) { f = (d & b) | (~d & c); g = (5 * i + 1) % 16 }
      else if (i < 48) { f = b ^ c ^ d; g = (3 * i + 5) % 16 }
      else { f = c ^ (b | ~d); g = (7 * i) % 16 }
      const nextD = d
      d = c
      c = b
      b = (b + rotate((a + f + K[i] + input[offset + g]) >>> 0, S[i])) >>> 0
      a = nextD
    }
    a0 = (a0 + a) >>> 0
    b0 = (b0 + b) >>> 0
    c0 = (c0 + c) >>> 0
    d0 = (d0 + d) >>> 0
  }
  return [a0, b0, c0, d0].map((value) => {
    let out = ''
    for (let i = 0; i < 4; i += 1) out += ('0' + ((value >>> (i * 8)) & 255).toString(16)).slice(-2)
    return out
  }).join('')
}

module.exports = md5
