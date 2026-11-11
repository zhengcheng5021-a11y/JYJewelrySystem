<template>
  <el-dialog v-model="visible" title="记录客户跟进" width="540px" append-to-body destroy-on-close class="follow-dialog" @closed="resetForm">
    <div v-if="item" class="follow-context">
      <span class="context-icon"><el-icon><ChatDotRound /></el-icon></span>
      <div><b>{{ item.title }}</b><p>跟进完成后，目标人数与次数会由服务器自动重算。</p></div>
    </div>

    <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="follow-form">
      <el-form-item label="选择客户" prop="memberId">
        <el-select v-model="form.memberId" filterable remote clearable :remote-method="searchMember" style="width: 100%" placeholder="搜索姓名、手机号或会员号">
          <el-option v-for="member in matchedMembers" :key="member.id" :label="memberLabel(member)" :value="member.id">
            <div class="member-option"><b>{{ member.memberName || '未命名客户' }}</b><span>{{ member.phone || '无手机号' }}</span><em>{{ member.memberNo }}</em></div>
          </el-option>
        </el-select>
      </el-form-item>
      <div class="form-grid">
        <el-form-item label="跟进方式">
          <el-select v-model="form.followType" style="width: 100%">
            <el-option label="电话沟通" :value="1" /><el-option label="微信沟通" :value="2" />
            <el-option label="到店接待" :value="3" /><el-option label="短信触达" :value="4" /><el-option label="其他" :value="5" />
          </el-select>
        </el-form-item>
        <el-form-item label="下次跟进">
          <el-date-picker v-model="form.nextFollowTime" type="datetime" value-format="YYYY-MM-DD HH:mm:ss" placeholder="可选" style="width: 100%" />
        </el-form-item>
      </div>
      <el-form-item label="跟进内容" prop="content">
        <el-input v-model="form.content" type="textarea" :rows="4" maxlength="1000" show-word-limit placeholder="本次沟通了什么？客户关注哪些款式、预算和顾虑？" />
      </el-form-item>
      <el-form-item label="跟进结果">
        <el-input v-model="form.result" maxlength="500" placeholder="例如：已加微信，约定周六到店试戴" />
      </el-form-item>
    </el-form>

    <template #footer>
      <div class="dialog-footer">
        <span>跟进时间由服务器自动记录</span>
        <div><el-button @click="visible = false">取消</el-button><el-button type="primary" :loading="submitting" @click="submit">保存跟进</el-button></div>
      </div>
    </template>
  </el-dialog>
</template>

<script setup name="MemberFollowDialog">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { ChatDotRound } from '@element-plus/icons-vue'
import { saveMemberFollow } from '@/api/jingyue/workitem'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  options: { type: Object, default: () => ({}) }
})
const emit = defineEmits(['update:modelValue', 'success'])
const visible = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})
const formRef = ref()
const submitting = ref(false)
const keyword = ref('')
const form = reactive({ memberId: undefined, followType: 1, content: '', result: '', nextFollowTime: '', workItemId: null })
const rules = {
  memberId: [{ required: true, message: '请选择客户', trigger: 'change' }],
  content: [{ required: true, message: '请输入跟进内容', trigger: 'blur' }]
}
const members = computed(() => props.options?.members || [])
const matchedMembers = computed(() => {
  const text = keyword.value.trim().toLowerCase()
  if (!text) return members.value.slice(0, 80)
  return members.value.filter((member) => [member.memberName, member.phone, member.memberNo].filter(Boolean).some((value) => String(value).toLowerCase().includes(text))).slice(0, 80)
})
function searchMember(value) { keyword.value = value || '' }
function memberLabel(member) { return [member.memberName, member.phone, member.memberNo].filter(Boolean).join(' · ') }
function resetForm() {
  Object.assign(form, { memberId: undefined, followType: 1, content: '', result: '', nextFollowTime: '', workItemId: null })
  keyword.value = ''
  formRef.value?.clearValidate()
}
async function submit() {
  const valid = await formRef.value?.validate().catch(() => false)
  if (!valid) return
  submitting.value = true
  try {
    await saveMemberFollow({
      memberId: Number(form.memberId),
      followType: Number(form.followType || 1),
      content: form.content.trim(),
      result: form.result || null,
      nextFollowTime: form.nextFollowTime || null,
      workItemId: props.item?.id || null
    })
    ElMessage.success('客户跟进已记录')
    emit('success')
  } finally { submitting.value = false }
}
watch(visible, (value) => {
  if (!value) return
  resetForm()
  form.workItemId = props.item?.id || null
})
</script>

<style scoped>
.follow-dialog :deep(.el-dialog) { border-radius: 10px; overflow: hidden; box-shadow: 0 16px 48px rgba(66, 41, 46, .12); }
.follow-dialog :deep(.el-dialog__header) { margin: 0; padding: 18px 22px; border-bottom: 1px solid #eee3e4; background: #fffafb; }
.follow-dialog :deep(.el-dialog__title) { font-size: 18px; font-weight: 650; color: #3d3133; }
.follow-dialog :deep(.el-dialog__body) { padding: 18px 22px 6px; background: #fdfbfb; }
.follow-dialog :deep(.el-dialog__footer) { padding: 12px 22px; border-top: 1px solid #eee3e4; background: #fff; }
.follow-context { display: flex; gap: 11px; padding: 12px 13px; margin-bottom: 15px; border: 1px solid #eedadd; border-radius: 8px; background: #fff7f8; }
.context-icon { flex: none; width: 34px; height: 34px; display: grid; place-items: center; border-radius: 7px; background: #f7e3e7; color: #ac5868; font-size: 16px; }
.follow-context b { display: block; color: #574448; font-size: 14px; font-weight: 550; line-height: 1.45; }
.follow-context p { margin: 4px 0 0; color: #917b80; font-size: 12px; line-height: 1.55; }
.follow-form :deep(.el-form-item) { margin-bottom: 15px; }
.follow-form :deep(.el-form-item__label) { padding-bottom: 5px; color: #6f5c61; font-size: 13px; line-height: 19px; }
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 14px; }
.member-option { display: flex; align-items: center; gap: 8px; font-size: 13px; }
.member-option b { color: #574448; font-size: 13px; font-weight: 500; }
.member-option span { color: #917b80; font-size: 12px; }
.member-option em { margin-left: auto; color: #aa9297; font-size: 11px; font-style: normal; }
.dialog-footer { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.dialog-footer > span { color: #968085; font-size: 12px; }
@media (max-width: 600px) { .follow-dialog :deep(.el-dialog) { width: 94vw !important; } .form-grid { grid-template-columns: 1fr; } }
</style>