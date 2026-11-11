<!--
 * @Descripttion: (文件分组/sys_file_group)
 * @Author: (admin)
 * @Date: (2025-06-08)
-->
<template>
  <div>
    <el-form :model="queryParams" label-position="right" inline ref="queryRef" v-show="showSearch" @submit.prevent>
      <el-form-item>
        <el-button icon="search" type="primary" @click="handleQuery">刷新</el-button>
      </el-form-item>
    </el-form>
    <!-- 工具区域 -->
    <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button type="primary" v-hasPermi="['filegroup:add']" plain icon="plus" @click="handleAdd">
          {{ $t('btn.add') }}
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="info" plain icon="sort" @click="toggleExpandAll">展开/折叠</el-button>
      </el-col>
      <right-toolbar v-model:showSearch="showSearch" @queryTable="getList"></right-toolbar>
    </el-row>

    <!-- 数据区域 -->
    <el-table
      v-if="refreshTable"
      :data="dataList"
      v-loading="loading"
      ref="tableRef"
      border
      highlight-current-row
      @selection-change="handleSelectionChange"
      :default-expand-all="isExpandAll"
      row-key="groupId"
      :tree-props="{ children: 'children', hasChildren: 'hasChildren' }">
      <!-- <el-table-column type="selection" width="50" align="center" /> -->
      <el-table-column prop="groupName" label="名称" align="left" />
      <el-table-column prop="groupId" label="分组id" align="center" />
      <el-table-column prop="parentId" label="父级id" align="center" />
      <el-table-column prop="sort" label="排序" width="90" sortable align="center">
        <template #default="scope">
          <span v-show="editIndex != scope.$index" @click="editCurrRow(scope.$index)">{{ scope.row.sort }}</span>
          <el-input
            :id="'' + scope.$index"
            v-show="editIndex == scope.$index"
            v-model="scope.row.sort"
            @blur="handleChangeSort(scope.row)"></el-input>
        </template>
      </el-table-column>

      <el-table-column prop="addTime" label="添加时间" align="center" />

      <el-table-column label="操作" align="center" width="140">
        <template #default="scope">
          <el-button v-hasPermi="['filegroup:edit']" type="success" icon="edit" title="编辑" @click="handleUpdate(scope.row)"></el-button>
          <el-button v-hasPermi="['filegroup:delete']" type="danger" icon="delete" title="删除" @click="handleDelete(scope.row)"></el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 添加或修改文件分组对话框 -->
    <fileGroupForm ref="fileGroupRef" @success="handleQuery()"></fileGroupForm>
  </div>
</template>

<script setup name="filegroup">
import { treelistFileGroup, delFileGroup, changeSort } from '@/api/tool/filegroup.js'
import fileGroupForm from '@/views/components/form/fileGroupForm.vue'
const { proxy } = getCurrentInstance()
const isExpandAll = ref(false)
const refreshTable = ref(true)
function toggleExpandAll() {
  refreshTable.value = false
  isExpandAll.value = !isExpandAll.value
  nextTick(() => {
    refreshTable.value = true
  })
}

// 选中groupId数组数组
const ids = ref([])
const single = ref(true)
const multiple = ref(true)
const loading = ref(false)
const showSearch = ref(true)
const queryParams = reactive({
  pageNum: 1,
  pageSize: 10,
  sort: '',
  sortType: 'asc'
})
const total = ref(0)
const dataList = ref([])
const queryRef = ref()

// ******************自定义编辑 start **********************
const editIndex = ref(-1)
// 显示编辑排序
function editCurrRow(rowId) {
  editIndex.value = rowId

  setTimeout(() => {
    document.getElementById(rowId).focus()
  }, 100)
}
// 保存排序
function handleChangeSort(info) {
  editIndex.value = -1
  proxy
    .$confirm('是否保存数据?')
    .then(function () {
      return changeSort({ value: info.sort, id: info.groupId })
    })
    .then(() => {
      handleQuery()
      proxy.$modal.msgSuccess('修改成功')
    })
}
// ******************自定义编辑 end **********************

function getList() {
  loading.value = true
  treelistFileGroup(queryParams)
    .then((res) => {
      const { code, data } = res
      if (code == 200) {
        dataList.value = res.data
        loading.value = false
      }
    })
    .catch(() => {
      loading.value = false
    })
}

// 查询
function handleQuery() {
  queryParams.pageNum = 1
  getList()
}

// 多选框选中数据
function handleSelectionChange(selection) {
  ids.value = selection.map((item) => item.groupId)
  single.value = selection.length != 1
  multiple.value = !selection.length
}

/*************** form操作 ***************/
const formRef = ref()
const title = ref('')
// 操作类型 1、add 2、edit
const opertype = ref(0)
const open = ref(false)
const state = reactive({
  form: {},
  rules: {
    groupName: [{ required: true, message: '名称不能为空', trigger: 'blur' }],
    sort: [{ required: true, message: '排序不能为空', trigger: 'blur', type: 'number' }]
  },
  options: {
    // 是否删除 选项列表 格式 eg:{ dictLabel: '标签', dictValue: '0'}
    isDeleteOptions: []
  }
})

const { form, rules, options } = toRefs(state)

// 关闭dialog
function cancel() {
  open.value = false
  reset()
}

// 重置表单
function reset() {
  form.value = {
    sort: 100
  }
  proxy.resetForm('formRef')
}

// 添加按钮操作
function handleAdd() {
  // reset()
  // open.value = true
  // title.value = '添加'
  // opertype.value = 1
  proxy.$refs.fileGroupRef.handleAdd()
}

// 修改按钮操作
function handleUpdate(row) {
  // reset()
  const id = row.groupId || ids.value
  // open.value = true
  // title.value = '修改数据'
  // opertype.value = 2

  // form.value = {
  //   ...row
  // }
  proxy.$refs.fileGroupRef.handleUpdate(row)
}

// 删除按钮操作
function handleDelete(row) {
  const Ids = row.groupId || ids.value

  proxy
    .$confirm('是否确认删除参数编号为"' + Ids + '"的数据项？')
    .then(function () {
      return delFileGroup(Ids)
    })
    .then(() => {
      getList()
      proxy.$modal.msgSuccess('删除成功')
    })
    .catch(() => {})
}

handleQuery()
</script>
