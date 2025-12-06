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
            list-type="picture-card"
            :auto-upload="false"
            :on-change="handleFileChange"
            :limit="3"
          >
            <el-icon><Plus /></el-icon>
            <template #tip>
              <div class="upload-tip">最多上传3张图片，支持jpg/png格式</div>
            </template>
          </el-upload>
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
import { Plus } from '@element-plus/icons-vue'
import { createOrder } from '../api/orders'
import router from '../router'

const formRef = ref()
const loading = ref(false)
const imageFile = ref(null)

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

const handleFileChange = (file) => {
  imageFile.value = file.raw
}

const handleSubmit = async () => {
  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    const formData = new FormData()
    formData.append('Title', form.title)
    formData.append('Location', form.location)
    formData.append('Description', form.description)
    if (imageFile.value) {
      formData.append('image', imageFile.value)
    }

    try {
      const res = await createOrder(formData)
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
</style>
