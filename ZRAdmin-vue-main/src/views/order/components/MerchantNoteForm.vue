<template>
  <el-dialog title="添加平台备注" v-model="open" width="400px">
    <el-form :model="form" label-position="right" :rules="rules" ref="formRef" label-width="80px">
      <el-form-item label="订单号" prop="orderNo">
        <el-input v-model="form.orderNo" disabled></el-input>
      </el-form-item>

      <el-form-item label="商家备注" prop="merchantNote">
        <el-input v-model="form.merchantNote" type="textarea" placeholder="请输入商家备注" />
      </el-form-item>
    </el-form>
    <template #footer>
      <div class="footer">
        <el-button text @click="open = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">确定</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup>
import { updateOMSOrder } from '@/api/shopping/omsorder'
import { ElMessage } from 'element-plus'
const { proxy } = getCurrentInstance()
const emit = defineEmits(['success'])
const state = reactive({
  form: {},
  rules: {}
})
const { form, rules } = toRefs(state)
const open = ref(false)
const formRef = ref()
// 重置表单
function reset() {
  state.form = {
    orderNo: '',
    merchantNote: '',
    operType: 2 //修改平台备注
  }

  proxy.resetForm('formRef')
}

function handleOpen(orderNo) {
  reset()
  form.value.orderNo = orderNo
  open.value = true
}

/**
 * 发货
 */
function handleSubmit() {
  formRef.value.validate((valid) => {
    if (!valid) return

    updateOMSOrder(form.value).then(() => {
      ElMessage.success('修改成功')
      open.value = false
      emit('success')
    })
  })
}

defineExpose({
  handleOpen
})
</script>
