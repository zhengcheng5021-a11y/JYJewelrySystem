<template>
  <el-form ref="basicInfoForm" :model="info" :rules="rules" label-width="100px">
    <el-row :gutter="10">
      <el-col :lg="18">
        <el-row :gutter="10">
          <el-col :lg="12">
            <el-form-item label="商品名称" prop="productName">
              <el-input v-model="info.productName" show-word-limit :maxlength="64" placeholder="请输入商品名称" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <BrandSelect v-model="info.brandId" />
          </el-col>

          <el-col :lg="12">
            <el-form-item label="分类">
              <el-cascader
                class="w100"
                :options="categoryOptions"
                :props="{ checkStrictly: true, value: 'categoryId', label: 'name', emitPath: false }"
                placeholder="请选择上级分类"
                clearable
                v-model="info.categoryId">
                <template #default="{ node, data }">
                  <span>{{ data.name }}</span>
                  <span v-if="!node.isLeaf"> ({{ data.children.length }}) </span>
                </template>
              </el-cascader>
            </el-form-item>
          </el-col>
          <el-col :lg="24">
            <el-form-item label="售卖状态" prop="saleStatus">
              <el-radio-group v-model="info.saleStatus">
                <el-radio-button v-for="item in saleStatusOptions" :key="item.dictValue" :value="parseInt(item.dictValue)">
                  {{ item.dictLabel }}
                </el-radio-button>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :lg="8">
            <el-form-item label="排序" prop="sortId">
              <template #label>
                <span>
                  <el-tooltip content="数字越大排序越靠前" placement="top">
                    <el-icon :size="15">
                      <questionFilled />
                    </el-icon>
                  </el-tooltip>
                  排序
                </span>
              </template>
              <el-input-number type="text" v-model="info.sortId" />
            </el-form-item>
          </el-col>
          <el-col :lg="8">
            <el-form-item label="原价" prop="originalPrice">
              <template #label>
                <span>
                  <el-tooltip content="前端页面显示划线价格" placement="top">
                    <el-icon :size="15">
                      <questionFilled />
                    </el-icon>
                  </el-tooltip>
                  原价
                </span>
              </template>
              <el-input-number type="number" placeholder="请输入商品原价" :min="0" v-model="info.originalPrice" />
            </el-form-item>
          </el-col>
          <el-col :lg="8">
            <el-form-item label="单位" prop="unit">
              <el-input type="text" show-word-limit style="width: 190px" :maxlength="5" placeholder="请输入单位" v-model="info.unit">
                <template #append>
                  <el-select style="width: 70px" v-model="info.unit">
                    <el-option value="位">位</el-option>
                    <el-option value="米">米</el-option>
                    <el-option value="千克">千克</el-option>
                    <el-option value="升">升</el-option>
                    <el-option value="匹">匹</el-option>
                    <el-option value="盏">盏</el-option>
                    <el-option value="扇">扇</el-option>
                    <el-option value="幅">幅</el-option>
                  </el-select>
                </template>
              </el-input>
            </el-form-item>
          </el-col>
          <el-col :lg="24">
            <el-form-item label="商品编码" prop="productCode">
              <el-input type="text" v-model="info.productCode" style="width: 200px" />
            </el-form-item>
          </el-col>
          <el-col :lg="24">
            <el-form-item label="商品卖点" prop="introduce">
              <el-input type="textarea" show-word-limit :maxlength="200" v-model="info.introduce" placeholder="请输入商品卖点" />
            </el-form-item>
          </el-col>
          <el-col :lg="24">
            <el-form-item label="商品主图" prop="mainImage">
              <UploadImage v-model="info.mainImage" :limit="1"></UploadImage>
            </el-form-item>
          </el-col>
          <el-col :lg="24">
            <el-form-item label="轮播图片" prop="imageUrls">
              <UploadImage v-model="info.imageUrls" :limit="8"></UploadImage>
            </el-form-item>
          </el-col>
        </el-row>
      </el-col>
      <el-col :lg="6">
        <el-form-item label="价格区间">
          <template #label>
            <span>
              <el-tooltip content="修改数据时根据sku价格自动计算获得" placement="top">
                <el-icon :size="15">
                  <questionFilled />
                </el-icon>
              </el-tooltip>
              价格区间
            </span>
          </template>

          ￥{{ info.price }}- {{ info.maxPrice }}
        </el-form-item>
        <el-form-item label="规格"> {{ info.specSummary || '-' }} </el-form-item>
        <el-form-item label="库存">
          {{ info.stock || '-' }}
        </el-form-item>
      </el-col>
      <el-col :lg="24">
        <el-form-item v-if="operType == 1">
          <el-button icon="check" type="primary" size="default" @click="handleSubmitInfo">保存</el-button>
        </el-form-item>
      </el-col>
    </el-row>
  </el-form>
</template>
<script setup name="BasicInfoForm">
import { updateInfo } from '@/api/shopping/product'
import { ElMessage } from 'element-plus'
import BrandSelect from './BrandSelect.vue'
const emits = defineEmits('success')
const props = defineProps({
  info: {
    type: Object,
    default: null
  },
  saleStatusOptions: {
    type: Array,
    default: []
  },
  categoryOptions: {
    type: Array,
    default: []
  },
  operType: {
    type: Number
  }
})
const rules = {
  productName: [{ required: true, message: '请输入商品名称', trigger: 'blur' }],
  introduce: [{ required: false, message: '请输入商品描述', trigger: 'blur' }],
  saleStatus: [{ required: true, message: '请选择商品状态', trigger: 'blur' }],
  mainImage: [{ required: true, message: '请上传商品主图片', trigger: 'change' }],
  imageUrls: [{ required: true, message: '请上传商品轮播图片', trigger: 'change' }]
}

const basicInfoForm = ref()
/**
 * 保存表单信息
 */
function handleSubmitInfo() {
  basicInfoForm.value.validate((valid) => {
    if (!valid) return
    updateInfo(props.info).then(() => {
      ElMessage.success('修改成功')
      // emits('success')
    })
  })
}
</script>
