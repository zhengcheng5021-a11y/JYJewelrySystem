<template>
  <div class="legacy-redirect" v-loading="true" element-loading-text="正在打开商品档案…"></div>
</template>

<script setup name="JyGoods">
import { useRouter } from 'vue-router'

const router = useRouter()
router.replace({ path: '/business/inbound', query: { view: 'goods' } })
</script>

<style scoped>
.legacy-redirect { min-height: 60vh; }
</style>
