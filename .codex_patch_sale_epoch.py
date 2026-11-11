from pathlib import Path
p = Path(r'D:\work\其他文件\静悦平台\ZrAdminNetCore-master\ZRAdmin-vue-main\src\views\jingyue\sale\index.vue')
s = p.read_text(encoding='utf-8')

def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND:\n' + old[:200])
    s = s.replace(old, new, 1)

rep("let scanBusy = false\n", "let scanBusy = false\n// 单据代次：新开单或离开页面后递增，旧请求即使晚返回也不得污染当前单据。\nlet saleEpoch = 0\n")

rep("""async function runPreview(seq) {
  if (!lines.value.length) {
    if (seq === previewSeq) preview.value = null
    return
  }
  try {
    const res = await previewSale(buildPayload())
    if (seq !== previewSeq) return
    preview.value = res.data || null
  } catch (error) {
    if (seq === previewSeq) preview.value = null
  }
}""", """async function runPreview(seq) {
  const epoch = saleEpoch
  if (!lines.value.length) {
    if (epoch === saleEpoch && seq === previewSeq) preview.value = null
    return
  }
  try {
    const res = await previewSale(buildPayload())
    if (epoch !== saleEpoch || seq !== previewSeq) return
    preview.value = res.data || null
  } catch (error) {
    if (epoch === saleEpoch && seq === previewSeq) preview.value = null
  }
}""")

rep("""async function identifyCustomer(source) {
  const keyword = (source === 'phone' ? form.customerPhone : form.customerName)?.trim()
  if (!keyword) {
    ElMessage.warning(source === 'phone' ? '请先输入手机号' : '请先输入客户姓名')
    return
  }
  try {
    const res = await matchSaleCustomer(keyword)
    const data = res.data || {}
    const candidates = data.candidates || []
    if (data.exists && data.memberId) {
      await applyMember(data.memberId)
      ElMessage.success(`已识别库内会员：${data.memberName || keyword}`)
      return
    }
    if (candidates.length === 1) {
      await applyMember(candidates[0].memberId)
      ElMessage.success(`已识别库内会员：${candidates[0].memberName}`)
      return
    }
    if (candidates.length > 1) {
      customerCandidates.value = candidates
      customerPick.value = candidates[0].memberId
      customerDialog.value = true
      return
    }
    form.memberId = null
    memberInfo.value = null
    customerState.value = 'new'
    preview.value = null
    await loadCustoms()
    ElMessage.info('库内未找到该客户，按新客户开单')
  } catch (error) {
    customerState.value = 'idle'
  }
}""", """async function identifyCustomer(source) {
  const keyword = (source === 'phone' ? form.customerPhone : form.customerName)?.trim()
  if (!keyword) {
    ElMessage.warning(source === 'phone' ? '请先输入手机号' : '请先输入客户姓名')
    return
  }
  const epoch = saleEpoch
  try {
    const res = await matchSaleCustomer(keyword)
    if (epoch !== saleEpoch) return
    const data = res.data || {}
    const candidates = data.candidates || []
    if (data.exists && data.memberId) {
      await applyMember(data.memberId, epoch)
      if (epoch !== saleEpoch) return
      ElMessage.success(`已识别库内会员：${data.memberName || keyword}`)
      return
    }
    if (candidates.length === 1) {
      await applyMember(candidates[0].memberId, epoch)
      if (epoch !== saleEpoch) return
      ElMessage.success(`已识别库内会员：${candidates[0].memberName}`)
      return
    }
    if (candidates.length > 1) {
      customerCandidates.value = candidates
      customerPick.value = candidates[0].memberId
      customerDialog.value = true
      return
    }
    form.memberId = null
    memberInfo.value = null
    customerState.value = 'new'
    preview.value = null
    await loadCustoms(undefined, epoch)
    if (epoch !== saleEpoch) return
    ElMessage.info('库内未找到该客户，按新客户开单')
  } catch (error) {
    if (epoch === saleEpoch) customerState.value = 'idle'
  }
}""")

rep("""async function applyMember(memberId) {
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (seq !== customerSeq) return""", """async function applyMember(memberId, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (epoch !== saleEpoch || seq !== customerSeq) return""")

rep("""    await loadCustoms()
    if (seq !== customerSeq) return
    await autoBindCustomGoods(seq)
    if (seq !== customerSeq) return
    schedulePreview()
  } catch (error) {
    if (seq === customerSeq) customerState.value = 'idle'
  }
}""", """    await loadCustoms(undefined, epoch)
    if (epoch !== saleEpoch || seq !== customerSeq) return
    await autoBindCustomGoods(seq, epoch)
    if (epoch !== saleEpoch || seq !== customerSeq) return
    schedulePreview()
  } catch (error) {
    if (epoch === saleEpoch && seq === customerSeq) customerState.value = 'idle'
  }
}""")

rep("""async function autoBindCustomGoods(seq = customerSeq) {
  const pending = customs.value.filter((row) => row.goodsItemId && !lines.value.some((line) => line.goodsItemId === row.goodsItemId))
  if (!pending.length || seq !== customerSeq) return
  for (const row of pending) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (seq !== customerSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) addLine(found, true)
    } catch (error) {
      if (seq !== customerSeq) return
    }
  }
  if (seq === customerSeq) ElMessage.info('已带入该客户已付定金的商品，收款时自动冲抵定金')
}""", """async function autoBindCustomGoods(seq = customerSeq, epoch = saleEpoch) {
  const pending = customs.value.filter((row) => row.goodsItemId && !lines.value.some((line) => line.goodsItemId === row.goodsItemId))
  if (!pending.length || epoch !== saleEpoch || seq !== customerSeq) return
  for (const row of pending) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (epoch !== saleEpoch || seq !== customerSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) addLine(found, true)
    } catch (error) {
      if (epoch !== saleEpoch || seq !== customerSeq) return
    }
  }
  if (epoch === saleEpoch && seq === customerSeq) ElMessage.info('已带入该客户已付定金的商品，收款时自动冲抵定金')
}""")

rep("""async function handleScan() {
  const keyword = scanKeyword.value?.trim()
  if (!keyword || scanBusy) return
  if (!customerReady.value) {
    ElMessage.warning('请先输入客户姓名或手机号并回车识别，再录入商品')
    scanKeyword.value = ''
    return
  }
  scanBusy = true
  try {
    const res = await findSaleGoods(keyword, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
    const rows = res.data || []
    if (!rows.length) {
      ElMessage.warning('没有找到可售商品，请检查条码或编码')
      return
    }
    if (rows.length === 1) {
      addLine(rows[0])
      scanKeyword.value = ''
      nextTick(() => scanInputRef.value?.focus?.())
      return
    }
    goodsCandidates.value = rows
    goodsDialog.value = true
  } finally {
    scanBusy = false
  }
}""", """async function handleScan() {
  const keyword = scanKeyword.value?.trim()
  if (!keyword || scanBusy) return
  if (!customerReady.value) {
    ElMessage.warning('请先输入客户姓名或手机号并回车识别，再录入商品')
    scanKeyword.value = ''
    return
  }
  const epoch = saleEpoch
  scanBusy = true
  try {
    const res = await findSaleGoods(keyword, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
    if (epoch !== saleEpoch) return
    const rows = res.data || []
    if (!rows.length) {
      ElMessage.warning('没有找到可售商品，请检查条码或编码')
      return
    }
    if (rows.length === 1) {
      addLine(rows[0])
      scanKeyword.value = ''
      nextTick(() => scanInputRef.value?.focus?.())
      return
    }
    goodsCandidates.value = rows
    goodsDialog.value = true
  } finally {
    if (epoch === saleEpoch) scanBusy = false
  }
}""")

rep("""  const seq = draftSeq
  saving.value = true
  try {
    const res = await saveSale(buildPayload())
    if (seq !== draftSeq) return 0""", """  const epoch = saleEpoch
  const seq = draftSeq
  saving.value = true
  try {
    const res = await saveSale(buildPayload())
    if (epoch !== saleEpoch || seq !== draftSeq) return 0""")

rep("""        const detail = await getSale(id)
        if (seq === draftSeq) await applyDetail(detail.data)""", """        const detail = await getSale(id)
        if (epoch === saleEpoch && seq === draftSeq) await applyDetail(detail.data, epoch)""")

rep("""  } finally {
    saving.value = false
  }
}

async function saveDraft()""", """  } finally {
    if (epoch === saleEpoch) saving.value = false
  }
}

async function saveDraft()""")

rep("""  confirming.value = true
  let id = 0
  try {
    id = await persist(true, false)
    if (!id) return
    await confirmSale(id)
    // 先使旧查询/预览/客户识别请求全部过期，再清空页面并移除路由 id。
    currentLoadSeq += 1
    lineStockSeq += 1
    customerSeq += 1
    draftSeq += 1
    await newSale(true)""", """  const epoch = saleEpoch
  confirming.value = true
  let id = 0
  try {
    id = await persist(true, false)
    if (!id || epoch !== saleEpoch) return
    await confirmSale(id)
    if (epoch !== saleEpoch) return
    // 新开单会递增 saleEpoch，所有旧查询、预览、客户识别和扫码请求同时失效。
    await newSale(true)""")

rep("""  } finally {
    confirming.value = false
  }
}

async function applyDetail(detail) {
  if (!detail) return""", """  } finally {
    if (epoch === saleEpoch) confirming.value = false
  }
}

async function applyDetail(detail, epoch = saleEpoch) {
  if (!detail || epoch !== saleEpoch) return""")

rep("""  refreshLineStock()
  if (form.memberId) {
    loadMember(form.memberId)
  } else if (form.customerName || form.customerPhone) {
    customerState.value = 'new'
  } else {
    customerState.value = 'idle'
  }
  await loadCustoms(savedOffsets)
  schedulePreview()
}""", """  refreshLineStock(epoch)
  if (form.memberId) {
    loadMember(form.memberId, epoch)
  } else if (form.customerName || form.customerPhone) {
    customerState.value = 'new'
  } else {
    customerState.value = 'idle'
  }
  await loadCustoms(savedOffsets, epoch)
  if (epoch !== saleEpoch) return
  schedulePreview()
}""")

rep("""async function loadCustoms(savedMap) {
  const seq = customerSeq
  const phone = form.customerPhone?.trim() || ''""", """async function loadCustoms(savedMap, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = customerSeq
  const phone = form.customerPhone?.trim() || ''""")

rep("""  if (!memberId && !phone) {
    if (seq === customerSeq) customs.value = []
    return
  }
  try {
    const res = await listAvailableCustom({ memberId, phone })
    if (seq !== customerSeq) return""", """  if (!memberId && !phone) {
    if (epoch === saleEpoch && seq === customerSeq) customs.value = []
    return
  }
  try {
    const res = await listAvailableCustom({ memberId, phone })
    if (epoch !== saleEpoch || seq !== customerSeq) return""")

rep("""  } catch (error) {
    if (seq === customerSeq) customs.value = []
  }
}

function onOffsetChange""", """  } catch (error) {
    if (epoch === saleEpoch && seq === customerSeq) customs.value = []
  }
}

function onOffsetChange""")

rep("""async function refreshLineStock() {
  const seq = ++lineStockSeq
  const snapshot = lines.value.slice()
  for (const row of snapshot) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (seq !== lineStockSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) {
        row.maxQty = found.itemMode === 1 ? 1 : Math.max(Number(row.qty || 1), Number(found.qty || 1))
        row.itemMode = found.itemMode
      }
    } catch (error) {
      if (seq !== lineStockSeq) return
    }
  }
}

async function loadMember(memberId) {
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (seq !== customerSeq || !res.data) return
    memberInfo.value = res.data
    customerState.value = 'member'
  } catch (error) {}
}""", """async function refreshLineStock(epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++lineStockSeq
  const snapshot = lines.value.slice()
  for (const row of snapshot) {
    try {
      const res = await findSaleGoods(row.barcode || row.goodsNo, { memberId: form.memberId || undefined, phone: form.customerPhone?.trim() || undefined })
      if (epoch !== saleEpoch || seq !== lineStockSeq) return
      const found = (res.data || []).find((it) => it.id === row.goodsItemId)
      if (found) {
        row.maxQty = found.itemMode === 1 ? 1 : Math.max(Number(row.qty || 1), Number(found.qty || 1))
        row.itemMode = found.itemMode
      }
    } catch (error) {
      if (epoch !== saleEpoch || seq !== lineStockSeq) return
    }
  }
}

async function loadMember(memberId, epoch = saleEpoch) {
  if (epoch !== saleEpoch) return
  const seq = ++customerSeq
  try {
    const res = await getMember(memberId)
    if (epoch !== saleEpoch || seq !== customerSeq || !res.data) return
    memberInfo.value = res.data
    customerState.value = 'member'
  } catch (error) {}
}""")

rep("""function resetForm() {
  cancelPendingPreview()
  lineStockSeq += 1""", """function resetForm() {
  saleEpoch += 1
  cancelPendingPreview()
  lineStockSeq += 1""")

rep("""async function loadCurrent() {
  const id = Number(route.query.id || 0)
  const seq = ++currentLoadSeq
  if (!id) return
  loading.value = true
  try {
    const res = await getSale(id)
    if (seq === currentLoadSeq) await applyDetail(res.data)
  } catch (error) {
    if (seq === currentLoadSeq) ElMessage.error('销售单加载失败')
  } finally {
    if (seq === currentLoadSeq) loading.value = false
  }
}""", """async function loadCurrent() {
  const id = Number(route.query.id || 0)
  const epoch = saleEpoch
  const seq = ++currentLoadSeq
  if (!id) return
  loading.value = true
  try {
    const res = await getSale(id)
    if (epoch === saleEpoch && seq === currentLoadSeq) await applyDetail(res.data, epoch)
  } catch (error) {
    if (epoch === saleEpoch && seq === currentLoadSeq) ElMessage.error('销售单加载失败')
  } finally {
    if (epoch === saleEpoch && seq === currentLoadSeq) loading.value = false
  }
}""")

rep("""onBeforeUnmount(() => {
  if (previewTimer) clearTimeout(previewTimer)
})""", """onBeforeUnmount(() => {
  saleEpoch += 1
  if (previewTimer) clearTimeout(previewTimer)
})""")

p.write_text(s, encoding='utf-8', newline='\n')
print('patched', p)