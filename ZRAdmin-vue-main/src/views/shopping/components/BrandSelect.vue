<template>
  <el-form-item label="品牌" :rules="rules" prop="brandId">
    <el-select v-model="localValue" placeholder="请选择品牌" filterable clearable>
      <el-option v-for="brand in brands" :key="brand.id" :label="brand.name" :value="brand.id">
        <span class="fl">{{ brand.name }}</span>
        <span class="fr">
          <el-image :src="brand.logo" style="height: 60px"></el-image>
        </span>
      </el-option>
      <template #footer>
        <el-button text bg size="small" @click="handleAdd"> 去添加 </el-button>

        <el-button text bg size="small" icon="refresh" @click="fetchBrands"> 刷新 </el-button>
      </template>
    </el-select>
  </el-form-item>
</template>

<script setup>
import { listMmsBrand } from '@/api/shopping/brand'
const router = useRouter()
// 接收父组件的 v-model
const props = defineProps({
  modelValue: {
    type: [Number, String],
    default: null
  }
})
const emit = defineEmits(['update:modelValue'])

// 本地变量用于双向绑定
const localValue = ref(props.modelValue)

watch(
  () => props.modelValue,
  (newVal) => {
    localValue.value = newVal
  }
)

watch(
  () => localValue.value,
  (newVal) => {
    emit('update:modelValue', newVal)
  }
)

const brands = ref([])

const rules = [
  {
    required: false,
    message: '请选择品牌',
    trigger: 'change'
  }
]

const fetchBrands = async () => {
  listMmsBrand().then((res) => {
    const { data } = res
    brands.value = data.result || []
  })
}
function handleAdd() {
  router.push({
    path: './productBrand'
  })
}
onMounted(fetchBrands)
</script>
