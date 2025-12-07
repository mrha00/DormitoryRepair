<template>
  <div class="order-container">
    <el-card class="order-card" shadow="hover">
      <template #header>
        <div class="card-header">
          <h2>🛠️ 工单管理</h2>
          <div class="header-actions">
            <!-- 📬 消息中心按钮 -->
            <el-badge :value="unreadCount" :hidden="unreadCount === 0" class="message-badge">
              <el-button 
                type="info" 
                circle 
                @click="$router.push('/notifications')"
                title="消息中心"
              >
                📬
              </el-button>
            </el-badge>
            
            <el-button type="primary" :icon="Plus" @click="handleCreate">
              新建工单
            </el-button>
            
            <!-- 👤 用户下拉菜单 -->
            <el-dropdown @command="handleCommand" trigger="click">
              <div class="user-dropdown">
                <el-avatar :size="40" class="user-avatar">
                  {{ getUserAvatar() }}
                </el-avatar>
                <span class="username">{{ currentUser.username }}</span>
                <el-icon class="el-icon--right"><arrow-down /></el-icon>
              </div>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item disabled>
                    <div class="user-info">
                      <div class="info-label">👤 用户名</div>
                      <div class="info-value">{{ currentUser.username }}</div>
                    </div>
                  </el-dropdown-item>
                  <el-dropdown-item disabled>
                    <div class="user-info">
                      <div class="info-label">🎭 角色</div>
                      <div class="info-value">{{ getRoleText(currentUser.role) }}</div>
                    </div>
                  </el-dropdown-item>
                  <el-dropdown-item disabled>
                    <div class="user-info">
                      <div class="info-label">📞 手机号</div>
                      <div class="info-value">138****8888</div>
                    </div>
                  </el-dropdown-item>
                  <el-dropdown-item divided command="settings">
                    <el-icon><Setting /></el-icon>
                    设置
                  </el-dropdown-item>
                  <el-dropdown-item command="logout">
                    <el-icon><SwitchButton /></el-icon>
                    退出登录
                  </el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </div>
        </div>
      </template>

      <!-- 搜索栏 -->
      <div class="search-bar">
        <el-form :inline="true" :model="searchForm" class="search-form">
          <!-- 👥 维修工显示工单筛选 -->
          <el-form-item v-if="currentUserRole === 'Maintainer'" label="工单范围">
            <el-radio-group v-model="searchForm.scope" @change="handleSearch">
              <el-radio-button value="my">👤 我的工单</el-radio-button>
              <el-radio-button value="all">🌐 全部工单</el-radio-button>
            </el-radio-group>
          </el-form-item>
          
          <el-form-item label="状态">
            <el-select 
              v-model="searchForm.status" 
              placeholder="请选择" 
              clearable 
              style="width: 150px"
              @change="handleSearch"
            >
              <el-option label="待处理" value="Pending" />
              <el-option label="处理中" value="Processing" />
              <el-option label="已完成" value="Completed" />
            </el-select>
          </el-form-item>
          <el-form-item>
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
        <!-- 📋 报修人列只对维修工和管理员可见 -->
        <el-table-column 
          v-if="currentUserRole !== 'Student'" 
          prop="creator" 
          label="报修人" 
          width="100" 
          align="center" 
        />
        <el-table-column prop="createTime" label="报修时间" width="160" align="center">
          <template #default="{ row }">
            {{ formatDateTime(row.createTime) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" :width="currentUserRole === 'Admin' ? 320 : 180" align="center" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" :icon="View" @click="$router.push(`/orders/${row.id}`)">详情</el-button>
            
            <!-- 👑 管理员专属按钮 -->
            <template v-if="currentUserRole === 'Admin'">
              <el-dropdown @command="(cmd) => handleAdminAction(cmd, row)" trigger="click" style="margin-left: 8px">
                <el-button type="warning" size="small" style="vertical-align: middle">
                  管理 <el-icon class="el-icon--right"><arrow-down /></el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="changeStatus">
                      🔄 修改状态
                    </el-dropdown-item>
                    <el-dropdown-item command="reassign" v-if="row.assignedTo">
                      🔧 更换维修工
                    </el-dropdown-item>
                    <el-dropdown-item command="assign" v-else>
                      👤 指派维修工
                    </el-dropdown-item>
                    <el-dropdown-item command="delete" divided>
                      🗑️ 删除工单
                    </el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </template>
            
            <!-- 普通用户指派按钮 -->
            <el-button 
              v-else-if="hasPermission('AssignOrder')" 
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
import { ref, onMounted, onBeforeUnmount, h, reactive } from 'vue'
import { ElMessage, ElMessageBox, ElSelect, ElOption, ElRadio, ElRadioGroup } from 'element-plus'
import { Plus, Search, Refresh, View, User, SwitchButton, ArrowDown, Setting } from '@element-plus/icons-vue'
import { getOrders, getMaintainers, assignOrder, deleteOrder, reassignOrder, updateOrderStatus } from '../api/orders'
import { logout } from '../api/auth'
import { getUnreadCount } from '../api/notifications'
import { useRouter } from 'vue-router'
import notificationService from '../services/signalr'

const router = useRouter()

const loading = ref(false)
const orders = ref([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const unreadCount = ref(0)

const searchForm = ref({
  status: '',
  scope: 'my' // 👥 默认显示“我的工单”
})

// 👤 获取当前用户信息
const currentUser = JSON.parse(sessionStorage.getItem('user') || '{}')
const currentUserRole = currentUser.role || ''

// 👤 获取用户头像显示（取用户名首字符）
const getUserAvatar = () => {
  return currentUser.username ? currentUser.username.charAt(0).toUpperCase() : 'U'
}

// 🎭 角色文本显示
const getRoleText = (role) => {
  const roleMap = {
    'Admin': '👑 管理员',
    'Maintainer': '🔧 维修工',
    'Student': '🎓 学生'
  }
  return roleMap[role] || role
}

// 👤 处理下拉菜单命令
const handleCommand = (command) => {
  if (command === 'logout') {
    handleLogout()
  } else if (command === 'settings') {
    ElMessage.info('🛠️ 设置功能开发中...')
    // 后续可以跳转到设置页面
    // router.push('/settings')
  }
}

// 权限检查
const hasPermission = (permission) => {
  const permissions = JSON.parse(sessionStorage.getItem('permissions') || '[]')
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

// 格式化时间
const formatDateTime = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day} ${hours}:${minutes}`
}

// 加载数据
const loadOrders = async () => {
  loading.value = true
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value,
      status: searchForm.value.status
    }
    
    // 👥 如果是维修工且选择了“我的工单”，只显示分配给自己的
if (currentUserRole === 'Maintainer' && searchForm.value.scope === 'my') {
      params.assignedToMe = true
    }
    
    const res = await getOrders(params)
    orders.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    ElMessage.error('加载工单失败：' + (error.response?.data?.message || error.message))
  } finally {
    loading.value = false
  }
}

// 📬 加载未读消息数量
const loadUnreadCount = async () => {
  try {
    const res = await getUnreadCount()
    unreadCount.value = res.data.count
  } catch (error) {
    console.error('加载未读消息数失败', error)
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
  // 👥 维修工重置时恢复到“我的工单”
  if (currentUserRole === 'Maintainer') {
    searchForm.value.scope = 'my'
  }
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

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm(
      '确定要退出登录吗？',
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    logout()
  } catch (err) {
    // 用户取消
  }
}

// 👑 管理员操作处理
const handleAdminAction = async (command, row) => {
  if (command === 'delete') {
    // 🗑️ 删除工单
    try {
      await ElMessageBox.confirm(
        `确定要删除工单《${row.title}》吗？此操作不可恢复！`,
        '警告',
        {
          confirmButtonText: '确定删除',
          cancelButtonText: '取消',
          type: 'error',
          confirmButtonClass: 'el-button--danger'
        }
      )
      
      await deleteOrder(row.id)
      ElMessage.success('工单已删除')
      loadOrders()
    } catch (error) {
      if (error !== 'cancel') {
        ElMessage.error('删除失败：' + (error.response?.data?.message || error.message))
      }
    }
  } else if (command === 'changeStatus') {
    // 🔄 修改状态 - 使用下拉列表选择
    try {
      // 使用 reactive 对象实现响应式
      const state = reactive({
        selectedStatus: row.status
      })
      
      // 状态选项
      const statusOptions = [
        { value: 'Pending', label: '待处理', color: '#f39c12' },
        { value: 'Processing', label: '处理中', color: '#409eff' },
        { value: 'Completed', label: '已完成', color: '#67c23a' }
      ]
      
      await ElMessageBox({
        title: '修改工单状态',
        message: () => h('div', { style: 'padding: 20px 10px' }, [
          h('div', { 
            style: 'margin-bottom: 20px; padding: 12px; background: linear-gradient(135deg, #667eea15 0%, #764ba215 100%); border-radius: 8px; color: #606266; font-size: 14px' 
          }, `当前状态：${getStatusText(row.status)}`),
          h('div', { style: 'margin-bottom: 15px; font-weight: 600; font-size: 15px; color: #303133' }, '选择新状态：'),
          h(ElSelect, {
            modelValue: state.selectedStatus,
            'onUpdate:modelValue': (val) => { 
              state.selectedStatus = val
            },
            placeholder: '请选择状态',
            style: 'width: 100%',
            size: 'large'
          }, () => statusOptions.map(item => 
            h(ElOption, {
              key: item.value,
              label: item.label,
              value: item.value
            }, () => [
              h('span', { 
                style: `display: inline-block; width: 10px; height: 10px; border-radius: 50%; background: ${item.color}; margin-right: 8px` 
              }),
              item.label
            ])
          ))
        ]),
        showCancelButton: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        beforeClose: async (action, instance, done) => {
          if (action === 'confirm') {
            if (state.selectedStatus !== row.status) {
              try {
                await updateOrderStatus(row.id, state.selectedStatus)
                ElMessage.success('状态修改成功')
                loadOrders()
                done()
              } catch (error) {
                ElMessage.error('修改失败：' + (error.response?.data?.message || error.message))
              }
            } else {
              done()
            }
          } else {
            done()
          }
        }
      })
    } catch (error) {
      // 用户取消
    }
  } else if (command === 'assign' || command === 'reassign') {
    // 👤 指派/更换维修工 - 使用下拉列表选择
    try {
      // 获取维修工列表
      const res = await getMaintainers()
      const maintainers = res.data
      
      if (maintainers.length === 0) {
        ElMessage.warning('暂无可用维修工')
        return
      }
      
      // 使用 reactive 对象实现响应式
      const state = reactive({
        selectedId: row.assignedTo || null
      })
      
      await ElMessageBox({
        title: command === 'reassign' ? '🔧 更换维修工' : '👤 指派维修工',
        message: () => h('div', { style: 'padding: 20px 10px' }, [
          h('div', { 
            style: 'margin-bottom: 20px; padding: 12px; background: linear-gradient(135deg, #667eea15 0%, #764ba215 100%); border-radius: 8px; color: #606266; font-size: 14px' 
          }, `工单：${row.title}`),
          h('div', { style: 'margin-bottom: 15px; font-weight: 600; font-size: 15px; color: #303133' }, '选择维修工：'),
          h(ElSelect, {
            modelValue: state.selectedId,
            'onUpdate:modelValue': (val) => { 
              state.selectedId = val
            },
            placeholder: '请选择维修工',
            style: 'width: 100%',
            size: 'large'
          }, () => maintainers.map(m => 
            h(ElOption, {
              key: m.id,
              label: m.username,
              value: m.id
            }, () => `🔧 ${m.username}`)
          ))
        ]),
        showCancelButton: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        beforeClose: async (action, instance, done) => {
          if (action === 'confirm') {
            if (!state.selectedId) {
              ElMessage.warning('请选择维修工')
              return
            }
            
            try {
              if (command === 'reassign') {
                await reassignOrder(row.id, state.selectedId)
                ElMessage.success('已更换维修工')
              } else {
                await assignOrder(row.id, state.selectedId)
                ElMessage.success('指派成功')
              }
              loadOrders()
              done()
            } catch (error) {
              ElMessage.error('操作失败：' + (error.response?.data?.message || error.message))
            }
          } else {
            done()
          }
        }
      })
    } catch (error) {
      // 用户取消
    }
  }
}

// 初始化加载
onMounted(() => {
  loadOrders()
  loadUnreadCount() // 🔔 加载未读消息数
  
  // 🔔 监听 SignalR 通知，当收到新工单时自动刷新列表
  if (notificationService.connection) {
    // 使用 addEventListener 样的方式，不会覆盖 signalr.js 中的监听器
    const handleNotification = (message, data) => {
      console.log('📢 OrderList 收到工单通知，自动刷新列表', data)
      // 延迟1秒后刷新，确保后端数据已更新
      setTimeout(() => {
        loadOrders()
        loadUnreadCount() // 🔔 更新未读数量
      }, 1000)
    }
    
    // 注册监听器
    notificationService.connection.on('ReceiveNotification', handleNotification)
    
    // 保存监听器引用，用于卸载
    notificationService._orderListHandler = handleNotification
  }
})

// 组件卸载时清理监听
onBeforeUnmount(() => {
  if (notificationService.connection && notificationService._orderListHandler) {
    notificationService.connection.off('ReceiveNotification', notificationService._orderListHandler)
    delete notificationService._orderListHandler
  }
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

.header-actions {
  display: flex;
  gap: 12px;
  align-items: center;
}

.message-badge {
  :deep(.el-badge__content) {
    font-weight: bold;
  }
}

/* 👤 用户下拉菜单样式 */
.user-dropdown {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 5px 12px;
  cursor: pointer;
  border-radius: 20px;
  transition: all 0.3s;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
}

.user-dropdown:hover {
  background: linear-gradient(135deg, #764ba2 0%, #667eea 100%);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
}

.user-avatar {
  background: rgba(255, 255, 255, 0.3);
  color: white;
  font-weight: bold;
  font-size: 16px;
}

.username {
  font-size: 14px;
  font-weight: 500;
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 用户信息显示 */
.user-info {
  padding: 5px 0;
  min-width: 180px;
}

.info-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 4px;
}

.info-value {
  font-size: 14px;
  color: #303133;
  font-weight: 500;
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

/* 🎨 单选按钮选中样式增强 */
:deep(.el-radio.is-checked) {
  background-color: #ecf5ff;
}
</style>