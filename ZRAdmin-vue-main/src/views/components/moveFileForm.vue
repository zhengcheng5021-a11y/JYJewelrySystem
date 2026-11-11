<template>
  <el-dialog title="移动分组" :lock-scroll="false" v-model="open" width="400">
    <el-form ref="formRef" :model="form" :rules="rules" label-width="80px">
      <el-row :gutter="20">
        <el-col :lg="24">
          <el-form-item label="选择分组" prop="groupId">
            <el-cascader
              class="w100"
              :options="dataList"
              :props="{ checkStrictly: true, value: 'groupId', label: 'groupName', emitPath: false }"
              placeholder="请选择上级菜单"
              clearable
              v-model="form.groupId">
              <template #default="{ node, data }">
                <span>{{ data.groupName }}</span>
                <span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
              </template>
            </el-cascader>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <div class="dialog-footer">
        <el-button text @click="cancel">{{ $t('btn.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ $t('btn.submit') }}</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup>
import { treelistFileGroup } from '@/api/tool/filegroup.js'

const { proxy } = getCurrentInstance()
const loading = ref(false)
const dataList = ref([])
const emit = defineEmits(['success'])

function getList() {
  loading.value = true
  treelistFileGroup()
    .then((res) => {
      const { code, data } = res
      if (code == 200) {
        dataList.value = data
        loading.value = false
      }
    })
    .catch(() => {
      loading.value = false
    })
}

/*************** form操作 ***************/
const formRef = ref()

const open = ref(false)
const state = reactive({
  form: {},
  rules: {
    groupId: [{ required: true, message: '分类不能为空', trigger: 'blur' }]
  }
})

const { form, rules } = toRefs(state)

// 关闭dialog
function cancel() {
  open.value = false
  reset()
}

// 重置表单
function reset() {
  proxy.resetForm('formRef')
}
// 添加按钮操作
function handleAdd() {
  reset()
  open.value = true
  getList()
}

// 添加&修改 表单提交
function submitForm() {
  proxy.$refs['formRef'].validate((valid) => {
    if (valid) {
      console.log(JSON.stringify(form.value))
      // proxy.$modal.msgSuccess('修改成功')
      open.value = false

      emit('success', form.value)
    }
  })
}
defineExpose({
  handleAdd
})
</script>
