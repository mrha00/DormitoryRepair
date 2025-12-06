<template>
  <el-dialog 
    v-model="visible" 
    title="👤 指派维修工" 
    width="500px" 
    @close="reset"
    :close-on-click-modal="false"
    class="assign-dialog"
  >
    <el-form :model="form" :rules="rules" ref="formRef" label-width="80px">
      <el-form-item label="维修工" prop="maintainerId">
        <el-select 
          v-model="form.maintainerId" 
          placeholder="请选择维修工" 
          size="large" 
          filterable
          :loading="loadingMaintainers"
        >
          <el-option
            v-for="user in maintainers"
            :key="user.id"
            :label="user.username"
            :value="user.id"
          >
            <span style="float: left">{{ user.username }}</span>
            <span style="float: right; color: #8492a6; font-size: 13px">ID: {{ user.id }}</span>
          </el-option>
        </el-select>
      </el-form-item>
      
      <el-form-item label="备注">
        <el-input 
          v-model="form.note" 
          type="textarea" 
          :rows="3" 
          placeholder="可添加指派备注信息（可选）"
          maxlength="200"
          show-word-limit
        />
      </el-form-item>
    </el-form>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="visible = false" size="large">取消</el-button>
        <el-button 
          type="primary" 
          @click="handleAssign" 
          :loading="loading" 
          size="large"
        >
          确认指派
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { ElMessage } from 'element-plus'
import { getMaintainers, assignOrder } from '../api/orders'

const props = defineProps({
  orderId: {
    type: Number,
    required: true
  }
})

const emit = defineEmits(['assigned'])

const visible = ref(false)
const loading = ref(false)
const loadingMaintainers = ref(false)
const maintainers = ref([])
const formRef = ref(null)

const form = reactive({
  maintainerId: '',
  note: ''
})

const rules = reactive({
  maintainerId: [
    { required: true, message: '请选择维修工', trigger: 'change' }
  ]
})

const open = async () => {
  visible.value = true
  await loadMaintainers()
}

const loadMaintainers = async () => {
  loadingMaintainers.value = true
  try {
    const res = await getMaintainers()
    maintainers.value = res.data
    
    if (maintainers.value.length === 0) {
      ElMessage.warning('暂无可用的维修工')
    }
  } catch (error) {
    ElMessage.error('获取维修工列表失败：' + (error.response?.data || error.message))
  } finally {
    loadingMaintainers.value = false
  }
}

const reset = () => {
  form.maintainerId = ''
  form.note = ''
  formRef.value?.resetFields()
}

const handleAssign = async () => {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      await assignOrder(props.orderId, form.maintainerId)
      ElMessage.success('指派成功！')
      visible.value = false
      emit('assigned')
      reset()
    } catch (error) {
      ElMessage.error('指派失败：' + (error.response?.data || error.message))
    } finally {
      loading.value = false
    }
  })
}

defineExpose({ open })
</script>

<style scoped>
.assign-dialog :deep(.el-dialog__header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 20px;
  margin: 0;
}

.assign-dialog :deep(.el-dialog__title) {
  color: white;
  font-size: 18px;
  font-weight: 600;
}

.assign-dialog :deep(.el-dialog__headerbtn .el-dialog__close) {
  color: white;
  font-size: 20px;
}

.assign-dialog :deep(.el-dialog__headerbtn .el-dialog__close):hover {
  color: #f0f0f0;
}

.assign-dialog :deep(.el-dialog__body) {
  padding: 30px;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

/* 表单样式优化 */
.el-select {
  width: 100%;
}

.el-form-item {
  margin-bottom: 24px;
}

/* 动画效果 */
.assign-dialog {
  animation: slideDown 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
