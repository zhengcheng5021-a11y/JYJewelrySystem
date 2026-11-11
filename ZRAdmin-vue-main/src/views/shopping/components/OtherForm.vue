<template>
  <el-form ref="formRef" :model="info" :rules="rules" label-position="left" label-width="110px">
    <el-form-item label="视频介绍" prop="videoUrl">
      <UploadImage v-model="info.videoUrl" :limit="1" :fileSize="20" :file-type="['mp4', 'webp']"></UploadImage>
    </el-form-item>
    <el-divider />
    <el-form-item label="商品限购">
      <el-switch v-model="info.purchaseLimit.limit" inline-prompt active-text="开启" inactive-text="关闭"> </el-switch>
    </el-form-item>
    <template v-if="info.purchaseLimit.limit">
      <el-col :lg="24">
        <el-form-item label="总限购" prop="purchaseLimit.totalLimit">
          <el-input-number v-model.number="info.purchaseLimit.totalLimit" :min="1" style="width: 100px"></el-input-number> 件/人
        </el-form-item>
      </el-col>
      <el-col :lg="24">
        <el-form-item label="每单限购" prop="purchaseLimit.singleLimit">
          <el-input-number v-model.number="info.purchaseLimit.singleLimit" :min="1" style="width: 100px"></el-input-number> 件/人
        </el-form-item>
      </el-col>
      <div class="text-muted" style="font-size: 13px">用于限制每人购买该商品的数量</div>
    </template>
    <el-divider />
  </el-form>
</template>
<script setup>
const props = defineProps({
  info: {
    type: Object,
    default: () => {
      return {
        purchaseLimit: {}
      }
    }
  }
})

const rules = {
  'purchaseLimit.limit': [{ required: true, trigger: 'blur' }],
  'purchaseLimit.totalLimit': [{ required: true, trigger: 'blur', message: '总限购不能为空' }],
  'purchaseLimit.singleLimit': [{ required: true, trigger: 'blur', message: '每单限购不能为空' }]
}
</script>
