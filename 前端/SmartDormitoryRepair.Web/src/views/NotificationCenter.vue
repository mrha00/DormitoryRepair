<template>
  <div class="notification-center">
    <el-card class="notification-card" shadow="hover">
      <template #header>
        <div class="card-header">
          <h2>📬 消息中心</h2>
          <div class="header-actions">
            <el-button 
              v-if="unreadCount > 0" 
              type="primary" 
              size="small" 
              @click="handleMarkAllRead"
            >
              全部标为已读
            </el-button>
            <el-button type="default" size="small" @click="$router.back()">
              返回
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

      <!-- 筛选栏 -->
      <div class="filter-bar">
        <el-radio-group v-model="filterType" @change="handleFilterChange">
          <el-radio-button label="all">全部</el-radio-button>
          <el-radio-button label="unread">
            未读 
            <el-badge v-if="unreadCount > 0" :value="unreadCount" class="badge" />
          </el-radio-button>
          <el-radio-button label="read">已读</el-radio-button>
        </el-radio-group>
      </div>

      <!-- 消息列表 -->
      <div v-loading="loading" class="notification-list">
        <el-empty v-if="notifications.length === 0" description="暂无消息" />
        
        <div
          v-for="item in notifications"
          :key="item.id"
          class="notification-item"
          :class="{ 'unread': !item.isRead }"
          @click="handleItemClick(item)"
        >
          <div class="notification-icon">
            <span v-if="!item.isRead" class="unread-dot">●</span>
            📢
          </div>
          
          <div class="notification-content">
            <div class="notification-title">{{ item.title }}</div>
            <div class="notification-message">{{ item.message }}</div>
            <div class="notification-time">{{ formatTime(item.createTime) }}</div>
          </div>

          <div class="notification-actions">
            <el-button
              v-if="!item.isRead"
              type="primary"
              size="small"
              text
              @click.stop="handleMarkRead(item.id)"
            >
              标为已读
            </el-button>
            <el-button
              type="danger"
              size="small"
              text
              @click.stop="handleDelete(item.id)"
            >
              ✕
            </el-button>
          </div>
        </div>
      </div>

      <!-- 分页 -->
      <div class="pagination" v-if="total > 0">
        <el-pagination
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :page-sizes="[10, 20, 50]"
          layout="total, sizes, prev, pager, next"
          :total="total"
          @size-change="loadNotifications"
          @current-change="loadNotifications"
        />
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ArrowDown, Setting, SwitchButton } from '@element-plus/icons-vue'
import { useRouter } from 'vue-router'
import { getNotifications, getUnreadCount, markAsRead, markAllAsRead, deleteNotification } from '../api/notifications'
import { logout } from '../api/auth'

const router = useRouter()

const loading = ref(false)
const notifications = ref([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const filterType = ref('all')
const unreadCount = ref(0)

// 👤 获取当前用户信息
const currentUser = JSON.parse(localStorage.getItem('user') || '{}')

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
  }
}

// 退出登录
const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    logout()
  } catch (err) {
    // 用户取消
  }
}

// 加载消息列表
const loadNotifications = async () => {
  loading.value = true
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value
    }
    
    if (filterType.value === 'unread') {
      params.isRead = false
    } else if (filterType.value === 'read') {
      params.isRead = true
    }

    const res = await getNotifications(params)
    notifications.value = res.data.items
    total.value = res.data.total
  } catch (error) {
    ElMessage.error('加载消息失败：' + (error.response?.data?.message || error.message))
  } finally {
    loading.value = false
  }
}

// 加载未读数量
const loadUnreadCount = async () => {
  try {
    const res = await getUnreadCount()
    unreadCount.value = res.data.count
  } catch (error) {
    console.error('加载未读数量失败', error)
  }
}

// 筛选变更
const handleFilterChange = () => {
  currentPage.value = 1
  loadNotifications()
}

// 标记为已读
const handleMarkRead = async (id) => {
  try {
    await markAsRead(id)
    ElMessage.success('已标记为已读')
    loadNotifications()
    loadUnreadCount()
  } catch (error) {
    ElMessage.error('操作失败：' + (error.response?.data?.message || error.message))
  }
}

// 全部标为已读
const handleMarkAllRead = async () => {
  try {
    await ElMessageBox.confirm('确定要将所有消息标为已读吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await markAllAsRead()
    ElMessage.success('已全部标为已读')
    loadNotifications()
    loadUnreadCount()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败：' + (error.response?.data?.message || error.message))
    }
  }
}

// 删除消息
const handleDelete = async (id) => {
  try {
    await ElMessageBox.confirm('确定要删除这条消息吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    
    await deleteNotification(id)
    ElMessage.success('消息已删除')
    loadNotifications()
    loadUnreadCount()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('操作失败：' + (error.response?.data?.message || error.message))
    }
  }
}

// 点击消息项
const handleItemClick = async (item) => {
  // 如果未读，先标记为已读
  if (!item.isRead) {
    await handleMarkRead(item.id)
  }
  
  // 如果有关联工单，跳转到工单详情
  if (item.relatedOrderId) {
    router.push(`/orders/${item.relatedOrderId}`)
  }
}

// 格式化时间
const formatTime = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  const now = new Date()
  const diff = now - date
  
  // 1分钟内
  if (diff < 60000) {
    return '刚刚'
  }
  // 1小时内
  if (diff < 3600000) {
    return `${Math.floor(diff / 60000)}分钟前`
  }
  // 今天
  if (date.toDateString() === now.toDateString()) {
    return `今天 ${date.getHours()}:${String(date.getMinutes()).padStart(2, '0')}`
  }
  // 昨天
  const yesterday = new Date(now)
  yesterday.setDate(yesterday.getDate() - 1)
  if (date.toDateString() === yesterday.toDateString()) {
    return `昨天 ${date.getHours()}:${String(date.getMinutes()).padStart(2, '0')}`
  }
  // 其他
  return `${date.getMonth() + 1}-${date.getDate()} ${date.getHours()}:${String(date.getMinutes()).padStart(2, '0')}`
}

onMounted(() => {
  loadNotifications()
  loadUnreadCount()
})
</script>

<style scoped>
.notification-center {
  padding: 20px;
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.notification-card {
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
  gap: 10px;
  align-items: center;
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

.filter-bar {
  margin-bottom: 20px;
  padding: 15px;
  background: #f5f7fa;
  border-radius: 8px;
}

.badge {
  margin-left: 5px;
}

.notification-list {
  min-height: 300px;
}

.notification-item {
  display: flex;
  align-items: flex-start;
  padding: 15px;
  margin-bottom: 10px;
  background: #fff;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
}

.notification-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.2);
  transform: translateY(-2px);
}

.notification-item.unread {
  background: #f0f9ff;
  border-color: #b3d8ff;
}

.notification-icon {
  font-size: 24px;
  margin-right: 15px;
  position: relative;
}

.unread-dot {
  position: absolute;
  top: -5px;
  right: -5px;
  color: #f56c6c;
  font-size: 16px;
}

.notification-content {
  flex: 1;
  min-width: 0;
}

.notification-title {
  font-size: 16px;
  font-weight: bold;
  color: #303133;
  margin-bottom: 5px;
}

.notification-message {
  font-size: 14px;
  color: #606266;
  margin-bottom: 8px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notification-time {
  font-size: 12px;
  color: #909399;
}

.notification-actions {
  display: flex;
  gap: 5px;
  flex-shrink: 0;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

@media (max-width: 768px) {
  .card-header {
    flex-direction: column;
    gap: 15px;
    align-items: stretch;
  }
  
  .notification-item {
    flex-direction: column;
  }
  
  .notification-actions {
    margin-top: 10px;
  }
}
</style>
