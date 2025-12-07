<template>
  <div class="create-container">
    <el-card class="create-card" shadow="hover">
      <template #header>
        <h2>📝 新建工单</h2>
      </template>

      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px" class="create-form">
        <el-form-item label="报修标题" prop="title">
          <el-input v-model="form.title" placeholder="例如：宿舍灯坏了" size="large" />
        </el-form-item>

        <el-form-item label="宿舍位置" prop="location">
          <el-input v-model="form.location" placeholder="例如：3号楼301" size="large" />
        </el-form-item>

        <el-form-item label="详细描述" prop="description">
          <el-input
            v-model="form.description"
            type="textarea"
            :rows="4"
            placeholder="请详细描述问题..."
            size="large"
          />
        </el-form-item>

        <el-form-item label="上传图片">
          <el-upload
            action="#"
            :auto-upload="false"
            :on-change="handleFileChange"
            :show-file-list="false"
            accept="image/*"
          >
            <el-button type="primary" :disabled="uploading">
              <el-icon v-if="uploading"><Loading /></el-icon>
              {{ uploading ? '压缩上传中...' : '选择图片' }}
            </el-button>
          </el-upload>
          
          <!-- 图片预览 -->
          <div v-if="imagePreview" class="image-preview-container">
            <img :src="imagePreview" class="image-preview" />
            <el-button type="danger" size="small" @click="removeImage" class="remove-btn">
              ×
            </el-button>
            <div class="image-info">
              <span>💾 {{ imageSizeInfo }}</span>
            </div>
          </div>
          
          <div class="upload-tip">
            支持jpg/png/gif格式，图片将自动压缩至200KB以内
          </div>
        </el-form-item>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="loading" size="large">
            提交工单
          </el-button>
          <el-button @click="$router.push('/orders')" size="large">返回列表</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus, Loading } from '@element-plus/icons-vue'
import { createOrder, uploadFile } from '../api/orders'
import { compressImage } from '../utils/compressImage'
import router from '../router'

const formRef = ref()
const loading = ref(false)
const uploading = ref(false)
const imagePreview = ref('')
const imageUrl = ref('')
const imageSizeInfo = ref('')

const form = reactive({
  title: '',
  location: '',
  description: ''
})

const rules = reactive({
  title: [{ required: true, message: '请输入报修标题', trigger: 'blur' }],
  location: [{ required: true, message: '请输入宿舍位置', trigger: 'blur' }],
  description: [{ required: true, message: '请输入详细描述', trigger: 'blur' }]
})

// 处理文件选择：压缩并上传
const handleFileChange = async (file) => {
  try {
    uploading.value = true
    
    // 记录原始大小
    const originalSize = file.raw.size
    
    // 使用Canvas压缩图片
    const compressedBlob = await compressImage(file.raw, 0.2) // 200KB限制
    
    // 创建FormData上传
    const formData = new FormData()
    formData.append('file', compressedBlob, 'image.jpg')
    
    // 上传到服务器
    const res = await uploadFile(formData)
    imageUrl.value = res.data.url
    imagePreview.value = URL.createObjectURL(compressedBlob)
    
    // 显示压缩信息
    imageSizeInfo.value = `${(originalSize / 1024).toFixed(2)}KB → ${(compressedBlob.size / 1024).toFixed(2)}KB`
    
    ElMessage.success('图片上传成功！')
  } catch (error) {
    ElMessage.error('图片处理失败：' + error.message)
  } finally {
    uploading.value = false
  }
}

// 移除图片
const removeImage = () => {
  imagePreview.value = ''
  imageUrl.value = ''
  imageSizeInfo.value = ''
  ElMessage.info('已移除图片')
}

const handleSubmit = async () => {
  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true

    try {
      const orderData = {
        title: form.title,
        location: form.location,
        description: form.description,
        imageUrl: imageUrl.value || null
      }
      
      const res = await createOrder(orderData)
      ElMessage.success('工单创建成功！')
      router.push('/orders')
    } catch (error) {
      ElMessage.error('创建失败：' + (error.response?.data?.message || error.message))
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
.create-container {
  padding: 20px;
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.create-card {
  max-width: 800px;
  margin: 0 auto;
  border-radius: 12px;
}

.create-form {
  padding: 20px 0;
}

.upload-tip {
  font-size: 12px;
  color: #909399;
  margin-top: 5px;
}

/* 📷 图片预览样式 */
.image-preview-container {
  position: relative;
  display: inline-block;
  margin-top: 10px;
}

.image-preview {
  max-width: 300px;
  max-height: 200px;
  border-radius: 8px;
  border: 2px solid #dcdfe6;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.remove-btn {
  position: absolute;
  top: -10px;
  right: -10px;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  padding: 0;
  font-size: 20px;
  line-height: 1;
}

.image-info {
  margin-top: 5px;
  font-size: 12px;
  color: #67c23a;
  text-align: center;
}

/* 📱 移动端深度优化 */
@media (max-width: 768px) {
  .create-container {
    padding: 10px;
  }
  
  .create-card {
    border-radius: 8px;
  }
  
  .create-card :deep(.el-card__header) {
    padding: 15px;
  }
  
  .create-card :deep(.el-card__header h2) {
    font-size: 18px;
    margin: 0;
  }
  
  .create-form {
    padding: 10px 0;
  }
  
  /* 表单标签垂直布局 */
  .create-form :deep(.el-form-item) {
    display: flex;
    flex-direction: column;
    margin-bottom: 20px;
  }
  
  .create-form :deep(.el-form-item__label) {
    width: 100% !important;
    text-align: left;
    margin-bottom: 8px;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
  }
  
  .create-form :deep(.el-form-item__content) {
    margin-left: 0 !important;
  }
  
  /* 按钮全宽 */
  .create-form :deep(.el-form-item:last-child) {
    margin-top: 25px;
  }
  
  .create-form :deep(.el-form-item:last-child .el-button) {
    width: 100%;
    margin-bottom: 10px;
    height: 44px;
    font-size: 15px;
  }
  
  /* 上传组件优化 */
  .create-form :deep(.el-upload-list--picture-card) {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
    gap: 8px;
  }
  
  .create-form :deep(.el-upload--picture-card) {
    width: 100px;
    height: 100px;
  }
  
  .create-form :deep(.el-upload-list__item) {
    width: 100px;
    height: 100px;
  }
  
  .upload-tip {
    font-size: 11px;
  }
}
</style>
