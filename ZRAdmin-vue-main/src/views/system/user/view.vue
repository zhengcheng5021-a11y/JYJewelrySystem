<template>
  <el-drawer
    title="用户信息详情"
    v-model="visible"
    direction="rtl"
    size="50%"
    append-to-body
    :before-close="handleClose"
    class="detail-drawer user-detail-drawer">
    <div v-loading="loading" class="drawer-content user-detail-content">
      <!-- 基本信息 -->
      <h4 class="section-header">
        <el-icon style="vertical-align: middle"><User /></el-icon> 基本信息
      </h4>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">用户名称：</label>
            <span class="info-value plaintext">{{ info.user.nickName }}</span>
          </div>
        </el-col>
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">用户编号：</label>
            <span class="info-value plaintext copy-value">{{ info.user.userId }}<el-tooltip content="复制用户编号" placement="top"><button type="button" class="copy-code" aria-label="复制用户编号" @click.stop="copyValue(info.user.userId, '用户编号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
          </div>
        </el-col>
        <!-- <el-col :span="12">
          <div class="info-item">
            <label class="info-label">归属部门：</label>
            <span class="info-value plaintext">{{ info.dept && info.dept.deptName }}</span>
          </div>
        </el-col> -->
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">手机号码：</label>
            <span class="info-value plaintext copy-value">{{ info.user.phonenumber || '-' }}<el-tooltip v-if="info.user.phonenumber" content="复制手机号码" placement="top"><button type="button" class="copy-code" aria-label="复制手机号码" @click.stop="copyValue(info.user.phonenumber, '手机号码')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">邮箱：</label>
            <span class="info-value plaintext">{{ info.user.email }}</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">登录账号：</label>
            <span class="info-value plaintext copy-value">{{ info.user.userName }}<el-tooltip content="复制登录账号" placement="top"><button type="button" class="copy-code" aria-label="复制登录账号" @click.stop="copyValue(info.user.userName, '登录账号')"><el-icon><CopyDocument /></el-icon></button></el-tooltip></span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">用户状态：</label>
            <span class="info-value plaintext">
              <el-tag size="small" :type="info.user.status == 0 ? 'success' : 'danger'">{{ info.user.status == 0 ? '正常' : '停用' }}</el-tag>
            </span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">岗位：</label>
            <span class="info-value plaintext">{{ postNames || '无岗位' }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">用户性别：</label>
            <span class="info-value plaintext">{{ sexLabel }}</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="24" class="info-col">
          <div class="info-item full-width">
            <label class="info-label">微信账号：</label>
            <span v-if="info.user.wechats?.length" class="info-value plaintext">
              <el-tag v-for="item in info.user.wechats" :key="item.id" class="wechat-tag" :type="item.isPrimary ? 'success' : 'info'">
                {{ item.wechatNo }}{{ item.isPrimary ? '（默认）' : '' }}{{ item.status ? '（停用）' : '' }}
              </el-tag>
            </span>
            <span v-else class="info-value plaintext">未维护</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="24" class="info-col">
          <div class="info-item full-width">
            <label class="info-label">角色：</label>
            <span class="info-value plaintext">{{ roleNames || '无角色' }}</span>
          </div>
        </el-col>
      </el-row>
      <!-- 其他信息 -->
      <h4 class="section-header">
        <el-icon style="vertical-align: middle"><InfoFilled /></el-icon> 其他信息
      </h4>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">创建者：</label>
            <span class="info-value plaintext">{{ info.user.create_by }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">创建时间：</label>
            <span class="info-value plaintext">{{ info.user.create_time }}</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">更新者：</label>
            <span class="info-value plaintext">{{ info.user.update_by }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">更新时间：</label>
            <span class="info-value plaintext">{{ info.user.update_time }}</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="12" class="info-col">
          <div class="info-item">
            <label class="info-label">最后登录IP：</label>
            <span class="info-value plaintext">{{ info.user.loginIP }}</span>
          </div>
        </el-col>
        <el-col :span="12">
          <div class="info-item">
            <label class="info-label">最后登录时间：</label>
            <span class="info-value plaintext">{{ info.user.loginDate }}</span>
          </div>
        </el-col>
      </el-row>
      <el-row :gutter="24" class="mb16">
        <el-col :span="24" class="info-col">
          <div class="info-item full-width">
            <label class="info-label">备注：</label>
            <span class="info-value plaintext">{{ info.user.remark }}</span>
          </div>
        </el-col>
      </el-row>
    </div>
  </el-drawer>
</template>

<script setup>
import { User, InfoFilled, CopyDocument } from '@element-plus/icons-vue'
import { getUser } from '@/api/system/user'
import { useClipboard } from '@vueuse/core'

const visible = ref(false)
const loading = ref(false)
const info = reactive({
  user: {}
})
const postOptions = ref([])
const roleOptions = ref([])
const sexOptions = ref([])
const { proxy } = getCurrentInstance()
const { copy, isSupported } = useClipboard({ legacy: true })

async function copyValue(value, label) {
  const text = String(value || '').trim()
  if (!text) return proxy.$modal.msgWarning(`没有可复制的${label}`)
  if (!isSupported.value) return proxy.$modal.msgWarning('当前浏览器不支持自动复制')
  await copy(text)
  proxy.$modal.msgSuccess(`${label}已复制`)
}
proxy.getDicts('sys_user_sex').then((response) => {
  sexOptions.value = response.data
})

const sexLabel = computed(() => proxy.selectDictLabel(sexOptions.value, info.user.sex) || '-')

const postNames = computed(() => {
  if (!postOptions.value.length || !info.postIds) return ''
  return (
    postOptions.value
      .filter((p) => info.postIds?.includes(p.postId))
      .map((p) => p.postName)
      .join('、') || ''
  )
})

const roleNames = computed(() => {
  if (!roleOptions.value.length || !info.roleIds) return ''
  return (
    roleOptions.value
      .filter((r) => info.roleIds?.includes(r.roleId))
      .map((r) => r.roleName)
      .join('、') || ''
  )
})

const open = async (userId) => {
  visible.value = true
  loading.value = true
  try {
    const { code, data } = await getUser(userId)
    Object.assign(info, data || {})
    postOptions.value = data.posts || []
    roleOptions.value = data.roles || []
    info.postIds = data.postIds || []
    info.roleIds = data.roleIds || []
  } catch (error) {
    console.error('获取用户信息失败:', error)
  } finally {
    loading.value = false
  }
}

function handleClose() {
  visible.value = false
}

defineExpose({
  open
})
</script>

<style scoped>
.user-detail-drawer .el-drawer__header {
  font-size: 20px;
  font-weight: 600;
  color: #333;
  border-bottom: 1px solid #f0f0f0;
  padding-bottom: 12px;
  margin-bottom: 18px;
}
.user-detail-content {
  padding: 0 18px 18px 18px;
}
.section-header {
  font-size: 16px;
  font-weight: 500;
  color: #409eff;
  margin: 24px 0 12px 0;
  display: flex;
  align-items: center;
  gap: 6px;
}
.info-item {
  display: flex;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px dashed #f0f0f0;
}
.info-label {
  min-width: 90px;
  color: #666;
  font-weight: 400;
  text-align: left;
  margin-right: 8px;
}
.info-value {
  color: #222;
  font-size: 15px;
  word-break: break-all;
}
.copy-value { display: inline-flex; align-items: center; gap: 2px; }
.copy-code { display: inline-grid; flex: none; place-items: center; width: 21px; height: 21px; padding: 0; color: #9aa4b2; background: transparent; border: 0; border-radius: 4px; cursor: pointer; }
.copy-code:hover { color: #409eff; background: #ecf5ff; }
.full-width .info-item {
  width: 100%;
}
.el-tag {
  margin-left: 0;
}
.wechat-tag { margin: 0 6px 4px 0; }
@media (max-width: 900px) {
  .user-detail-drawer {
    width: 100% !important;
    min-width: unset !important;
  }
  .user-detail-content {
    padding: 0 6px 12px 6px;
  }
  .section-header {
    font-size: 15px;
  }
  .info-label {
    min-width: 70px;
    font-size: 13px;
  }
  .info-value {
    font-size: 13px;
  }
}
</style>
