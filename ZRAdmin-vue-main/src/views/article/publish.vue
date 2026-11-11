<template>
  <div class="app-container article-publish-page">
    <el-form :model="form" ref="formRef" :rules="rules" @submit.prevent class="publish-form">
      <el-row :gutter="16" class="publish-row">
        <el-col :xl="18" :lg="16" :md="24">
          <el-card shadow="never" class="editor-card">
            <el-form-item label="" prop="title" class="title-item">
              <el-input v-model="form.title" placeholder="请输入文章标题（必须）" size="large" />
            </el-form-item>

            <el-form-item prop="content" label="" class="editor-item">
              <MdEditor v-model="form.content" :showToolbarName="true" :theme="settingsStore.codeMode" :onUploadImg="onUploadImg" />
            </el-form-item>
          </el-card>
        </el-col>

        <el-col :xl="6" :lg="8" :md="24">
          <div class="side-column">
            <el-card shadow="never" class="meta-card" header="发布设置">
              <el-form-item prop="abstractText" label="文章摘要" label-position="top">
                <el-input
                  v-model="form.abstractText"
                  type="textarea"
                  :rows="4"
                  show-word-limit
                  maxlength="100"
                  placeholder="请输入文章摘要（必须）" />
              </el-form-item>
              <el-form-item prop="categoryId" label="分类" label-position="top">
                <el-cascader
                  class="w100"
                  :options="categoryOptions"
                  :props="{ checkStrictly: true, value: 'categoryId', label: 'name', emitPath: false }"
                  placeholder="请选择文章分类"
                  clearable
                  v-model="form.categoryId" />
              </el-form-item>

              <el-form-item label="标签" label-position="top">
                <el-input-tag v-model="form.dynamicTags" :max="5" :maxlength="10" clearable trigger="Enter" placeholder="请输入标签" />
              </el-form-item>
              <el-form-item label="封面" label-position="top">
                <UploadImage ref="uploadRef" v-model="form.coverUrl" :limit="1" :fileSize="15" style="width: 90px">
                  <template #icon>
                    <div class="upload-wrap">
                      <el-icon class="avatar-uploader-icon"><plus /></el-icon>
                      <div>请选择封面</div>
                    </div>
                  </template>
                </UploadImage>
              </el-form-item>
              <el-form-item>
                <template #label>
                  <span>
                    <el-tooltip content="不公开只有自己会看到" placement="top">
                      <el-icon :size="15">
                        <questionFilled />
                      </el-icon>
                    </el-tooltip>
                    是否公开
                  </span>
                </template>
                <el-switch v-model="form.isPublic" inline-prompt :active-value="1" :in-active-value="0" active-text="是" inactive-text="否" />
              </el-form-item>
            </el-card>
          </div>
        </el-col>
      </el-row>
    </el-form>

    <div class="btn-wrap">
      <el-button type="success" @click="handlePublish('1')">发布文章</el-button>
      <el-button @click="handlePublish('2')" v-if="!info || info.status == 2">存为草稿</el-button>
    </div>
  </div>
</template>
<script setup name="articlepublish">
import { addArticle, updateArticle, getArticle } from '@/api/article/article.js'
import { treelistArticleCategory } from '@/api/article/articlecategory.js'
import useSettingsStore from '@/store/modules/settings'
import { upload } from '@/api/common.js'
import { MdEditor } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'

const settingsStore = useSettingsStore()
const { proxy } = getCurrentInstance()
const route = useRoute()
// 文章目录下拉框
const categoryOptions = ref([])
const inputVisible = ref(false)
const inputValue = ref('')
const formRef = ref()
const inputRef = ref()
const data = reactive({
  form: {
    dynamicTags: [],
    fmtType: 'markdown',
    tags: undefined,
    cid: undefined,
    content: undefined,
    status: undefined,
    categoryId: undefined,
    isPublic: 1,
    abstractText: undefined,
    editorType: 'markdown'
  },
  rules: {
    title: [{ required: true, message: '标题不能为空', trigger: 'blur' }],
    content: [{ required: true, message: '内容不能为空', trigger: 'blur' }],
    abstractText: [{ required: true, message: '摘要不能为空', trigger: 'blur' }]
  }
})
const { form, rules } = toRefs(data)

const cid = route.query.cid
form.value.cid = cid

/** 查询菜单下拉树结构 */
function getCategoryTreeselect() {
  treelistArticleCategory({}).then((res) => {
    if (res.code == 200) {
      categoryOptions.value = res.data
    }
  })
}

// 将图片上传到服务器，返回地址替换到md中
async function onUploadImg(files, callback) {
  const res = await Promise.all(
    Array.from(files).map((file) => {
      return new Promise((rev, rej) => {
        const form = new FormData()
        form.append('file', file)

        upload(form)
          .then((res) => rev(res))
          .catch((error) => rej(error))
      })
    })
  )

  callback(res.map((item) => item.data.url))
}

/** 提交按钮 */
function handlePublish(status) {
  form.value.status = status
  form.value.tags = form.value.dynamicTags.toString()

  proxy.$refs['formRef'].validate((valid) => {
    if (valid) {
      if (form.value.cid != undefined) {
        updateArticle(form.value).then((res) => {
          if (res.code == 200) {
            if (status == 1) {
              proxy.$modal.msgSuccess('发布文章成功')
              proxy.$tab.closeOpenPage({ path: '/article/index' })
            } else {
              proxy.$modal.msgSuccess('保存成功')
            }
          } else {
            proxy.$modal.msgError('修改文章失败')
          }
        })
      } else {
        addArticle(form.value).then((res) => {
          if (res.code == 200) {
            form.value.cid = res.data
            if (status == 1) {
              proxy.$modal.msgSuccess('发布文章成功')
              proxy.$tab.closeOpenPage({ path: '/article/index' })
            } else {
              proxy.$modal.msgSuccess('保存成功')
            }
          } else {
            proxy.$modal.msgError('发布文章失败')
          }
        })
      }
    }
  })
}
function handleCloseTag(tag) {
  form.value.dynamicTags.splice(form.value.dynamicTags.indexOf(tag), 1)
}

const showInput = () => {
  if (form.value.dynamicTags.length >= 5) {
    proxy.$modal.msgError('最多5个标签')
    return
  }
  inputVisible.value = true
  nextTick(() => {
    inputRef.value.input.focus()
  })
}
// 标签确认
function handleInputConfirm() {
  if (inputValue.value) {
    form.value.dynamicTags.push(inputValue.value)
  }
  inputVisible.value = false
  inputValue.value = ''
}
const info = ref()
function getInfo(cid) {
  if (!cid || cid == undefined) return
  getArticle(cid).then((res) => {
    if (res.code == 200) {
      var data = res.data
      info.value = data
      form.value = {
        ...data,
        dynamicTags: data.tags != null && data.tags.length > 0 ? data.tags.split(',') : []
      }
    }
  })
}
getInfo(cid)
getCategoryTreeselect()
</script>
<style scoped>
.article-publish-page {
  position: relative;
  padding-bottom: 74px;
}

.publish-form {
  max-width: 1600px;
}

.publish-row {
  align-items: flex-start;
}

.editor-card,
.meta-card,
.cover-card {
  border-radius: 10px;
}

.title-item {
  margin-bottom: 14px;
}

.editor-item :deep(.md-editor) {
  min-height: 520px;
}

.summary-item {
  margin-top: 14px;
  margin-bottom: 0;
}

.side-column {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.button-new-tag {
  padding-top: 0;
  padding-bottom: 0;
  width: 90px;
  margin-right: 10px;
  vertical-align: bottom;
}

.upload-wrap {
  display: flex;
  align-items: center;
  flex-direction: column;
  color: #ccc;
}

.btn-wrap {
  display: flex;
  align-items: center;
  gap: 10px;
  position: fixed;
  right: 20px;
  bottom: calc(var(--base-footer-height) + 12px);
  z-index: 10;
  background: var(--el-bg-color-overlay);
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 999px;
  padding: 8px 12px;
  box-shadow: 0 6px 20px rgb(0 0 0 / 8%);
}

@media (max-width: 992px) {
  .editor-item :deep(.md-editor) {
    min-height: 420px;
  }

  .btn-wrap {
    right: 12px;
    bottom: calc(var(--base-footer-height) + 8px);
  }
}
</style>
