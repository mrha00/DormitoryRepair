<template>
  <div class="order-container">
    <el-card class="order-card" shadow="hover">
      <template #header>
        <div class="card-header">
          <h2>🛠️ 工单管理</h2>
          <el-button type="primary" :icon="Plus" @click="handleCreate">
            新建工单
          </el-button>
        </div>
      </template>

      <!-- 搜索栏 -->
      <div class="search-bar">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <el-form-item label="状态">
            <el-select v-model="searchForm.status" placeholder="请选择" clearable>
              <el-option label="待处理" value="Pending" />
              <el-option label="处理中" value="Processing" />
              <el-option label="已完成" value="Completed" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" :icon="Search" @click="handleSearch">搜索</el-button>
            <el-button :icon="Refresh" @click="handleReset">重置</el-button>
          </el-form-item>
        </el-form>
      </div>

      <!-- 工单表格 -->
      <el-table 
        :data="orders" 
        v-loading="loading"
        border 
        stripe
        class="order-table"
        :header-cell-style="{ background: '#f5f7fa', color: '#606266' }"
      >
        <el-table-column prop="id" label="工单号" width="80" align="center" />
        <el-table-column prop="title" label="报修标题" min-width="150" show-overflow-tooltip />
        <el-table-column prop="creator" label="报修人" width="100" align="center" />
        <el-table-column prop="createTime" label="报修时间" width="160" align="center" />
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" :icon="View" @click="$router.push(`/orders/${row.id}`)">详情</el-button>
            <el-button 
              v-if="hasPermission('AssignOrder')" 
              type="success" 
              size="small" 
              :icon="User"
              @click="handleAssign(row)"
            >
              指派
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <!-- 分页 -->
      <div class="pagination">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50, 100]"
          layout="total, sizes, prev, pager, next, jumper"
          :total="total"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { Plus, Search, Refresh, View, User } from '@element-plus/icons-vue'
import { getOrders } from '../api/orders'
import { useRouter } from 'vue-router'

const router = useRouter()

const loading = ref(false)
const orders = ref([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)

const searchForm = ref({
  status: ''
})

// 权限检查
const hasPermission = (permission) => {
  const permissions = JSON.parse(localStorage.getItem('permissions') || '[]')
  return permissions.includes(permission)
}

// 状态样式
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

// 加载数据
const loadOrders = async () => {
  loading.value = true
  try {
    const res = await getOrders({
      page: currentPage.value,
      pageSize: pageSize.value,
      status: searchForm.value.status
    })
    orders.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    ElMessage.error('加载工单失败：' + (error.response?.data?.message || error.message))
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  currentPage.value = 1
  loadOrders()
}

// 重置
const handleReset = () => {
  searchForm.value.status = ''
  handleSearch()
}

// 分页
const handleSizeChange = (val) => {
  pageSize.value = val
  loadOrders()
}

const handleCurrentChange = (val) => {
  currentPage.value = val
  loadOrders()
}

// 操作
const handleCreate = () => {
  router.push('/orders/create')
}

const handleView = (row) => {
  ElMessage.info(`查看工单：${row.title}`)
}

const handleAssign = (row) => {
  ElMessage.info(`指派工单：${row.title}`)
}

// 初始化加载
onMounted(() => {
  loadOrders()
})
</script>

<style scoped>
.order-container {
  padding: 20px;
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.order-card {
  border-radius: 12px;
  overflow: hidden;
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

.search-bar {
  margin-bottom: 20px;
  padding: 15px;
  background: #f5f7fa;
  border-radius: 8px;
}

.search-form {
  display: flex;
  align-items: center;
  gap: 10px;
}

.order-table {
  border-radius: 8px;
  overflow: hidden;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

/* 响应式 */
@media (max-width: 768px) {
  .search-form {
    flex-direction: column;
    align-items: stretch;
  }
  
  .card-header {
    flex-direction: column;
    gap: 15px;
  }
}
</style>