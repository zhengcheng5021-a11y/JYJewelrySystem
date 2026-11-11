<template>
  <el-form ref="singleSkuFormRef" :model="form.singleSku" :rules="rules" label-position="left" label-width="110px">
    <el-form-item label="规格图片" prop="imageUrl">
      <UploadImage v-model="form.singleSku.imageUrl" :limit="1"></UploadImage>
    </el-form-item>
    <el-form-item :label="'商品价格' + $t('currency')" prop="price">
      <el-input-number v-model="form.singleSku.price" :min="0.01" />
    </el-form-item>
    <el-form-item label="库存数量" prop="stock">
      <el-input-number v-model="form.singleSku.stock" :min="0" />
    </el-form-item>
    <el-form-item label="商品重量(KG)" prop="weight">
      <el-input-number v-model="form.singleSku.weight" :min="0" />
    </el-form-item>
  </el-form>
</template>
<script setup>
const form = reactive({
  singleSku: {
    price: 0,
    stock: 0,
    weight: 0,
    imageUrl: '',
    specs: [{ name: '规格', value: '默认' }]
  },
  skus: []
})
const rules = {
  price: [{ required: true, message: '请输入价格', trigger: 'blur' }],
  stock: [{ required: true, message: '请输入库存', trigger: 'blur' }],
  weight: [{ required: true, message: '请输入重量', trigger: 'blur' }]
}
const props = defineProps({
  skus: {
    type: Array,
    default: () => []
  }
})
const emit = defineEmits(['update:skus', 'update:specList'])

// 监听父组件传进来的 skus 数组，转为 singleSku 对象
watch(
  () => props.skus,
  (val) => {
    if (val && val.length > 0) {
      const sku = val[0]
      // 这里用 Object.assign 保持响应式
      Object.assign(form.singleSku, sku)
    } else {
      Object.assign(form.singleSku, {
        price: 0,
        stock: 0,
        weight: 0,
        imageUrl: '',
        specs: [{ name: '规格', value: '默认' }]
      })
    }
  },
  { immediate: true, deep: true }
)

// 监听 singleSku 变化，转成数组格式回传父组件
watch(
  form.singleSku,
  (val) => {
    emit('update:skus', [{ ...val }])
    emit('update:specList', [{ name: '规格', values: ['默认'] }])
  },
  { deep: true }
)
</script>
