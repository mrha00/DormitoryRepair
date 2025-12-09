<template>
  <div class="detail-container">
    <el-card class="detail-card" shadow="hover">
      <template #header>
        <div class="card-header">
          <h2>📋 工单详情</h2>
          <el-button type="primary" :icon="Back" @click="$router.back()">返回</el-button>
        </div>
      </template>

      <div v-if="order" class="order-info" v-loading="loading">
        <!-- 基本信息 -->
        <el-descriptions :column="2" border class="info-descriptions">
          <el-descriptions-item label="工单号">{{ order.id }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(order.status)" size="large">
              {{ getStatusText(order.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="报修标题">{{ order.title }}</el-descriptions-item>
          <el-descriptions-item label="报修人">{{ order.creator }}</el-descriptions-item>
          <el-descriptions-item label="维修工">
            <span :class="order.assignedToName ? 'assigned-worker' : 'unassigned-worker'">
              {{ order.assignedToName || '未分配' }}
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="宿舍位置">{{ order.location }}</el-descriptions-item>
          <el-descriptions-item label="报修时间">{{ formatDate(order.createTime) }}</el-descriptions-item>
        </el-descriptions>

        <!-- 问题描述 -->
        <div class="description-section">
          <h3>💬 问题描述</h3>
          <p>{{ order.description }}</p>
        </div>

        <!-- 图片预览 -->
        <div v-if="order.imageUrl" class="image-section">
          <h3>📷 图片附件</h3>
          <el-image 
            :src="`${fileBase}${order.imageUrl}`" 
            :preview-src-list="[`${fileBase}${order.imageUrl}`]"
            fit="cover"
            class="order-image"
          />
        </div>

        <!-- 状态按钮 -->
        <div class="action-section">
          <!-- 维修工：Pending → Processing -->
          <el-button 
            v-if="hasPermission('ProcessOrder') && order.status === 'Pending'"
            type="primary" 
            size="large"
            @click="updateStatus('Processing')"
            :loading="statusLoading"
          >
            🔧 开始处理
          </el-button>

          <!-- 维修工：Processing → Completed -->
          <el-button 
            v-if="hasPermission('CompleteOrder') && order.status === 'Processing'"
            type="success" 
            size="large"
            @click="updateStatus('Completed')"
            :loading="statusLoading"
            :disabled="!isMyOrder"
          >
            ✅ 标记完成
          </el-button>
          
          <!-- 🚫 提示：不能完成他人工单 -->
          <el-tooltip 
            v-if="hasPermission('CompleteOrder') && order.status === 'Processing' && !isMyOrder"
            content="该工单由其他维修工负责，您无权标记完成"
            placement="top"
          >
            <span style="margin-left: 10px; color: #909399; font-size: 14px;">
              📌 非本人工单
            </span>
          </el-tooltip>

          <!-- Admin：指派按钮 -->
          <el-button 
            v-if="hasPermission('AssignOrder') && order.status === 'Pending'"
            type="warning" 
            size="large"
            @click="showAssignDialog"
          >
            👤 指派维修工
          </el-button>
        </div>
      </div>

      <el-empty v-else description="工单不存在" />
    </el-card>

    <!-- 指派对话框 -->
    <AssignDialog ref="assignDialogRef" :orderId="orderId" @assigned="handleAssigned" />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Back } from '@element-plus/icons-vue'
import { getOrder, updateOrderStatus } from '../api/orders'
import AssignDialog from '../components/AssignDialog.vue'

const route = useRoute()
const router = useRouter()
const order = ref(null)
const loading = ref(false)
const statusLoading = ref(false)
const assignDialogRef = ref(null)

const orderId = computed(() => parseInt(route.params.id))

const apiBase = import.meta.env.VITE_API_BASE_URL || ''
const fileBase = import.meta.env.VITE_FILE_BASE_URL || apiBase.replace(/\/api$/, '')

// 👥 判断当前用户是否是该工单的负责维修工
const isMyOrder = computed(() => {
  if (!order.value) return false
  
  const currentUser = JSON.parse(sessionStorage.getItem('user') || '{}')
  const currentUsername = currentUser.username
  
  // 👑 管理员可以操作任何工单
  if (currentUser.role === 'Admin') return true
  
  // 🔧 维修工只能操作自己的工单
  if (currentUser.role === 'Maintainer') {
    return order.value.assignedToName === currentUsername
  }
  
  return false
})

const hasPermission = (permission) => {
  const permissions = JSON.parse(sessionStorage.getItem('permissions') || '[]')
  return permissions.includes(permission)
}

const getStatusType = (status) => {
  const types = { 
    'Pending': 'warning', 
    'Processing': 'primary', 
    'Completed': 'success' 
  }
  return types[status] || 'info'
}

const getStatusText = (status) => {
  const texts = { 
    'Pending': '待处理', 
    'Processing': '处理中', 
    'Completed': '已完成' 
  }
  return texts[status] || status
}

const formatDate = (date) => {
  return new Date(date).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const loadOrder = async () => {
  loading.value = true
  try {
    const res = await getOrder(orderId.value)
    order.value = res.data
  } catch (error) {
    ElMessage.error('加载工单失败：' + (error.response?.data || error.message))
    setTimeout(() => router.back(), 1500)
  } finally {
    loading.value = false
  }
}

const updateStatus = async (newStatus) => {
  const statusText = getStatusText(newStatus)
  
  try {
    await ElMessageBox.confirm(
      `确定要将工单状态更新为"${statusText}"吗？`,
      '确认操作',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    statusLoading.value = true
    await updateOrderStatus(order.value.id, newStatus)
    ElMessage.success('状态更新成功！')
    loadOrder()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('更新失败：' + (error.response?.data || error.message))
    }
  } finally {
    statusLoading.value = false
  }
}

const showAssignDialog = () => {
  assignDialogRef.value.open()
}

const handleAssigned = () => {
  loadOrder()
}

onMounted(loadOrder)
</script>

<style scoped>
.detail-container {
  padding: 20px;
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.detail-card {
  max-width: 900px;
  margin: 0 auto;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.detail-card:hover {
  box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header h2 {
  margin: 0;
  color: #303133;
  font-size: 24px;
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.info-descriptions {
  border-radius: 8px;
  overflow: hidden;
}

.description-section, 
.image-section, 
.action-section {
  padding: 20px;
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.description-section h3,
.image-section h3 {
  margin: 0 0 15px 0;
  color: #409EFF;
  font-size: 18px;
}

.description-section p {
  margin: 0;
  line-height: 1.8;
  color: #606266;
  font-size: 15px;
}

.order-image {
  width: 100%;
  max-height: 400px;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  cursor: pointer;
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.order-image:hover {
  transform: scale(1.02);
}

.action-section {
  display: flex;
  gap: 15px;
  justify-content: center;
  flex-wrap: wrap;
}

.action-section .el-button {
  min-width: 150px;
  font-weight: 500;
}

/* 维修工显示样式 */
.assigned-worker {
  color: #409EFF;
  font-weight: 600;
  padding: 4px 12px;
  background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);
  border-radius: 4px;
  display: inline-block;
}

.unassigned-worker {
  color: #909399;
  font-style: italic;
  padding: 4px 12px;
  background: #f5f7fa;
  border-radius: 4px;
  display: inline-block;
}

/* 📱 移动端深度优化 */
@media (max-width: 768px) {
  .detail-container {
    padding: 10px;
  }
  
  .detail-card {
    border-radius: 8px;
  }
  
  .card-header {
    flex-direction: column;
    gap: 12px;
    align-items: stretch;
  }
  
  .card-header h2 {
    font-size: 18px;
    text-align: center;
  }
  
  .card-header .el-button {
    width: 100%;
  }
  
  .order-info {
    gap: 20px;
  }
  
  /* 描述表单元优化 */
  .info-descriptions {
    font-size: 13px;
  }
  
  .info-descriptions :deep(.el-descriptions__label) {
    width: 80px;
    font-size: 13px;
  }
  
  .info-descriptions :deep(.el-descriptions__content) {
    font-size: 13px;
  }
  
  /* 单列布局 */
  .info-descriptions :deep(.el-descriptions__body) {
    display: flex;
    flex-direction: column;
  }
  
  .info-descriptions :deep(.el-descriptions__row) {
    display: flex;
    flex-direction: column;
  }
  
  /* 问题描述区域 */
  .description-section,
  .image-section,
  .action-section {
    padding: 15px;
  }
  
  .description-section h3,
  .image-section h3 {
    font-size: 16px;
    margin-bottom: 10px;
  }
  
  .description-section p {
    font-size: 14px;
    line-height: 1.6;
  }
  
  /* 图片优化 */
  .order-image {
    max-height: 300px;
  }
  
  /* 按钮区域 */
  .action-section {
    flex-direction: column;
    gap: 12px;
  }
  
  .action-section .el-button {
    width: 100%;
    min-width: auto;
    height: 44px;
    font-size: 15px;
  }
  
  /* 维修工显示样式 */
  .assigned-worker,
  .unassigned-worker {
    font-size: 13px;
    padding: 3px 10px;
  }
}
</style>
