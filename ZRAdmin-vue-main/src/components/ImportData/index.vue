<template>
  <div class="uploadData p-4 border rounded-md bg-white shadow-sm">
    <div class="mb-4 text-right">
      <el-link type="primary" @click="importTemplate" icon="Bottom" class="mb10"> 下载模板 </el-link>
    </div>

    <el-upload
      ref="uploadRef"
      :limit="1"
      name="file"
      accept=".xlsx,.xls"
      :data="uploadData"
      :headers="headers"
      :action="uploadFileUrl"
      :disabled="isUploading"
      :on-progress="handleFileUploadProgress"
      :on-success="handleFileSuccess"
      :on-error="handleFileError"
      :auto-upload="true">
      <el-button type="primary" icon="Upload">上传文件</el-button>

      <template #tip>
        <div class="el-upload__tip mb10">
          <span>仅支持 .xls / .xlsx 格式，模板请先下载后填写。</span>
        </div>
      </template>
    </el-upload>
    <slot></slot>

    <el-dialog v-model="showResultDialog" title="导入结果" width="600px" :append-to-body="true">
      <el-scrollbar height="500px">
        <el-table v-loading="isUploading" :data="importResultList" style="width: 100%">
          <el-table-column v-for="col in props.resultFields" :key="col.prop" :prop="col.prop" :label="col.label" />
        </el-table>
      </el-scrollbar>
    </el-dialog>
  </div>
</template>
<script setup>
import { getToken } from '@/utils/auth'

const { proxy } = getCurrentInstance()
const emit = defineEmits()

const props = defineProps({
  importUrl: {
    type: String
  },
  templateUrl: {
    type: String
  },
  // 上传携带的参数
  data: {
    type: Object
  },
  // 展示导入结果字段
  /**
	 * [
				{ prop: 'orderNo', label: '订单号' },
				{ prop: 'status', label: '发货状态' }
			]
	 */
  resultFields: {
    type: Array,
    default: () => []
  }
})

const baseUrl = import.meta.env.VITE_APP_BASE_API
const uploadFileUrl = ref(baseUrl + props.importUrl) // 上传数据url
const headers = ref({ Authorization: 'Bearer ' + getToken() })
const uploadData = computed(() => props.data)
const isUploading = ref(false)

const importResultList = ref([]) // 导入结果
const showResultDialog = ref(false)
/**文件上传中处理 */
const handleFileUploadProgress = (event, file, fileList) => {
  isUploading.value = true
}
/** 文件上传成功处理 */
const handleFileSuccess = (response, file, fileList) => {
  const { code, msg } = response

  isUploading.value = false
  proxy.$refs['uploadRef'].clearFiles()
  proxy.$refs['uploadRef'].handleRemove(file)

  if (code != 200) {
    proxy.$modal.msgError('导入数据失败,原因：' + msg)
    return
  }
  const list = response.data.result || []
  emit('success', response)

  if (props.resultFields.length && Array.isArray(list) && list.length) {
    importResultList.value = list
    showResultDialog.value = true
  } else {
    proxy.$modal.msgSuccess('导入完成')
  }
}
const handleFileError = function (error) {
  proxy.$modal.msgError('导入数据失败,原因：' + error)
}
function importTemplate() {
  proxy.downFile(props.templateUrl, props.data)
}
</script>
<style lang="scss" scoped>
.uploadData {
  padding: 10px;
}
</style>
