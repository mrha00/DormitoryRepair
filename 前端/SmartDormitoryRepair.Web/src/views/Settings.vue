<template>
  <div class="settings-container">
    <el-card class="settings-card">
      <template #header>
        <div class="card-header">
          <h2>⚙️ 设置</h2>
        </div>
      </template>

      <el-tabs v-model="activeTab" type="border-card">
        <!-- 个人设置 -->
        <el-tab-pane label="👤 个人设置" name="profile">
          <div class="tab-content">
            <el-form :model="profileForm" label-width="120px" style="max-width: 600px">
              <el-form-item label="用户名">
                <el-input v-model="profileForm.username" disabled />
              </el-form-item>
              
              <el-form-item label="角色">
                <el-tag :type="getRoleType(profileForm.role)" size="large">
                  {{ getRoleText(profileForm.role) }}
                </el-tag>
              </el-form-item>

              <el-form-item label="手机号">
                <el-input 
                  v-model="profileForm.phoneNumber" 
                  placeholder="请输入手机号"
                  maxlength="11"
                />
              </el-form-item>

              <el-form-item>
                <el-button type="primary" @click="handleUpdateProfile">保存修改</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <!-- 修改密码 -->
        <el-tab-pane label="🔒 修改密码" name="password">
          <div class="tab-content">
            <el-form :model="passwordForm" label-width="120px" style="max-width: 600px">
              <el-form-item label="原密码">
                <el-input 
                  v-model="passwordForm.oldPassword" 
                  type="password" 
                  placeholder="请输入原密码"
                  show-password
                />
              </el-form-item>

              <el-form-item label="新密码">
                <el-input 
                  v-model="passwordForm.newPassword" 
                  type="password" 
                  placeholder="请输入新密码（至少6位）"
                  show-password
                />
              </el-form-item>

              <el-form-item label="确认密码">
                <el-input 
                  v-model="passwordForm.confirmPassword" 
                  type="password" 
                  placeholder="请再次输入新密码"
                  show-password
                />
              </el-form-item>

              <el-form-item>
                <el-button type="primary" @click="handleChangePassword">修改密码</el-button>
                <el-button @click="resetPasswordFormData">重置</el-button>
              </el-form-item>
            </el-form>
          </div>
        </el-tab-pane>

        <!-- 账号管理（仅管理员） -->
        <el-tab-pane label="👥 账号管理" name="users" v-if="isAdmin">
          <div class="tab-content">
            <!-- 搜索栏 -->
            <div class="search-bar">
              <el-button 
                type="success" 
                @click="showCreateDialog = true"
                style="margin-right: 10px"
              >
                ➕ 创建用户
              </el-button>
              <el-input 
                v-model="searchKeyword" 
                placeholder="搜索用户名或手机号"
                style="width: 300px; margin-right: 10px"
                clearable
              />
              <el-select 
                v-model="searchRole" 
                placeholder="角色筛选"
                style="width: 150px; margin-right: 10px"
                clearable
              >
                <el-option label="全部角色" value="" />
                <el-option label="👑 管理员" value="Admin" />
                <el-option label="🔧 维修工" value="Maintainer" />
                <el-option label="🎓 学生" value="Student" />
              </el-select>
              <el-select 
                v-model="searchStatus" 
                placeholder="状态筛选"
                style="width: 150px; margin-right: 10px"
                clearable
              >
                <el-option label="全部状态" :value="null" />
                <el-option label="✅ 已启用" :value="true" />
                <el-option label="🚫 已禁用" :value="false" />
              </el-select>
              <el-button type="primary" @click="handleSearch">🔍 搜索</el-button>
              <el-button @click="handleReset">🔄 重置</el-button>
            </div>

            <!-- 用户表格 -->
            <el-table :data="users" stripe style="width: 100%; margin-top: 20px" v-loading="loading">
              <el-table-column prop="id" label="ID" width="80" />
              <el-table-column prop="username" label="用户名" width="150" />
              <el-table-column prop="role" label="角色" width="120">
                <template #default="{ row }">
                  <el-tag :type="getRoleType(row.role)">
                    {{ getRoleText(row.role) }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="phoneNumber" label="手机号" width="150">
                <template #default="{ row }">
                  {{ row.phoneNumber || '-' }}
                </template>
              </el-table-column>
              <el-table-column prop="isActive" label="状态" width="100">
                <template #default="{ row }">
                  <el-tag :type="row.isActive ? 'success' : 'danger'">
                    {{ row.isActive ? '✅ 启用' : '🚫 禁用' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="createTime" label="创建时间" width="180">
                <template #default="{ row }">
                  {{ formatDateTime(row.createTime) }}
                </template>
              </el-table-column>
              <el-table-column label="操作" fixed="right" width="300">
                <template #default="{ row }">
                  <el-button 
                    type="primary" 
                    size="small" 
                    @click="handleResetPassword(row)"
                    :disabled="row.role === 'Admin' && row.username !== currentUsername"
                  >
                    🔑 重置密码
                  </el-button>
                  <el-button 
                    type="warning" 
                    size="small" 
                    @click="handleChangeRole(row)"
                    :disabled="row.role === 'Admin' && row.username !== currentUsername"
                  >
                    🔄 改角色
                  </el-button>
                  <el-button 
                    :type="row.isActive ? 'danger' : 'success'" 
                    size="small" 
                    @click="handleToggleStatus(row)"
                    :disabled="row.role === 'Admin' && row.username !== currentUsername"
                  >
                    {{ row.isActive ? '🚫 禁用' : '✅ 启用' }}
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
          </div>
        </el-tab-pane>
      </el-tabs>
    </el-card>

    <!-- ➕ 创建用户对话框 -->
    <el-dialog 
      v-model="showCreateDialog" 
      title="创建新用户" 
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form :model="createForm" label-width="100px">
        <el-form-item label="用户名" required>
          <el-input 
            v-model="createForm.username" 
            placeholder="请输入用户名"
            maxlength="20"
          />
        </el-form-item>
        
        <el-form-item label="密码" required>
          <el-input 
            v-model="createForm.password" 
            type="password" 
            placeholder="请输入密码（至少6位）"
            show-password
          />
        </el-form-item>

        <el-form-item label="角色" required>
          <el-select 
            v-model="createForm.role" 
            placeholder="请选择角色"
            style="width: 100%"
          >
            <el-option label="👑 管理员" value="Admin" />
            <el-option label="🔧 维修工" value="Maintainer" />
            <el-option label="🎓 学生" value="Student" />
          </el-select>
        </el-form-item>

        <el-form-item label="手机号">
          <el-input 
            v-model="createForm.phoneNumber" 
            placeholder="请输入手机号（可选）"
            maxlength="11"
          />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showCreateDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreateUser" :loading="creating">创建</el-button>
      </template>
    </el-dialog>

    <!-- 🔑 重置密码对话框 -->
    <el-dialog 
      v-model="showResetPasswordDialog" 
      title="重置密码" 
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form :model="resetPasswordForm" label-width="100px">
        <el-form-item label="用户">
          <el-input v-model="resetPasswordForm.username" disabled />
        </el-form-item>
        
        <el-form-item label="新密码">
          <el-input 
            v-model="resetPasswordForm.newPassword" 
            type="password" 
            placeholder="请输入新密码（至少6位）或留空使用默认密码"
            show-password
            clearable
          />
        </el-form-item>

        <el-alert
          title="提示：不输入密码将重置为默认密码 a123456（7位）"
          type="info"
          :closable="false"
          show-icon
        />
      </el-form>

      <template #footer>
        <el-button @click="showResetPasswordDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmResetPassword" :loading="resettingPassword">确定重置</el-button>
      </template>
    </el-dialog>

    <!-- 🔄 修改角色对话框 -->
    <el-dialog 
      v-model="showChangeRoleDialog" 
      title="修改角色" 
      width="500px"
      :close-on-click-modal="false"
    >
      <el-form :model="changeRoleForm" label-width="100px">
        <el-form-item label="用户">
          <el-input v-model="changeRoleForm.username" disabled />
        </el-form-item>
        
        <el-form-item label="当前角色">
          <el-tag :type="getRoleType(changeRoleForm.currentRole)" size="large">
            {{ getRoleText(changeRoleForm.currentRole) }}
          </el-tag>
        </el-form-item>

        <el-form-item label="新角色" required>
          <el-select 
            v-model="changeRoleForm.newRole" 
            placeholder="请选择新角色"
            style="width: 100%"
          >
            <el-option label="👑 管理员" value="Admin" />
            <el-option label="🔧 维修工" value="Maintainer" />
            <el-option label="🎓 学生" value="Student" />
          </el-select>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="showChangeRoleDialog = false">取消</el-button>
        <el-button type="primary" @click="confirmChangeRole" :loading="changingRole">确定修改</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRouter } from 'vue-router'
import { 
  getProfile, 
  changePassword, 
  updateProfile,
  getUsers,
  createUser,  // ➕ 添加创建用户 API
  resetUserPassword,
  updateUserRole,
  toggleUserStatus
} from '../api/users'

const router = useRouter()

// 当前用户
const currentUser = JSON.parse(sessionStorage.getItem('user') || '{}')
const currentUsername = currentUser.username || ''
const isAdmin = computed(() => currentUser.role === 'Admin')

// 选项卡
const activeTab = ref('profile')

// 个人资料表单
const profileForm = ref({
  username: '',
  role: '',
  phoneNumber: ''
})

// 修改密码表单
const passwordForm = ref({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

// 用户管理
const loading = ref(false)
const users = ref([])
const total = ref(0)
const currentPage = ref(1)
const pageSize = ref(10)
const searchKeyword = ref('')
const searchRole = ref('')
const searchStatus = ref(null)

// ➕ 创建用户对话框
const showCreateDialog = ref(false)
const creating = ref(false)
const createForm = ref({
  username: '',
  password: '',
  role: 'Student',  // 默认角色
  phoneNumber: ''
})

// 🔑 重置密码对话框
const showResetPasswordDialog = ref(false)
const resettingPassword = ref(false)
const resetPasswordForm = ref({
  userId: null,
  username: '',
  newPassword: ''  // 留空则使用默认密码 a123456
})

// 🔄 修改角色对话框
const showChangeRoleDialog = ref(false)
const changingRole = ref(false)
const changeRoleForm = ref({
  userId: null,
  username: '',
  currentRole: '',
  newRole: ''
})

// 加载个人资料
const loadProfile = async () => {
  try {
    const res = await getProfile()
    const data = res.data.data
    profileForm.value = {
      username: data.username,
      role: data.role,
      phoneNumber: data.phoneNumber || ''
    }
  } catch (error) {
    ElMessage.error('加载个人资料失败：' + (error.response?.data?.message || error.message))
  }
}

// 更新个人资料
const handleUpdateProfile = async () => {
  try {
    await updateProfile({
      phoneNumber: profileForm.value.phoneNumber
    })
    ElMessage.success('个人资料更新成功')
  } catch (error) {
    ElMessage.error('更新失败：' + (error.response?.data?.message || error.message))
  }
}

// 修改密码
const handleChangePassword = async () => {
  // 验证
  if (!passwordForm.value.oldPassword) {
    ElMessage.warning('请输入原密码')
    return
  }
  if (!passwordForm.value.newPassword) {
    ElMessage.warning('请输入新密码')
    return
  }
  if (passwordForm.value.newPassword.length < 6) {
    ElMessage.warning('新密码至少6位')
    return
  }
  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) {
    ElMessage.warning('两次输入的密码不一致')
    return
  }

  try {
    await changePassword({
      oldPassword: passwordForm.value.oldPassword,
      newPassword: passwordForm.value.newPassword
    })
    ElMessage.success('密码修改成功，请重新登录')
    // 清除登录信息
    sessionStorage.clear()
    // 跳转到登录页
    setTimeout(() => {
      router.push('/login')
    }, 1500)
  } catch (error) {
    ElMessage.error('修改失败：' + (error.response?.data?.message || error.message))
  }
}

// 重置修改密码表单
const resetPasswordFormData = () => {
  passwordForm.value = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  }
}

// 加载用户列表
const loadUsers = async () => {
  loading.value = true
  try {
    const params = {
      page: currentPage.value,
      pageSize: pageSize.value,
      role: searchRole.value,
      keyword: searchKeyword.value,
      isActive: searchStatus.value
    }
    
    const res = await getUsers(params)
    users.value = res.data.data.items
    total.value = res.data.data.total
  } catch (error) {
    ElMessage.error('加载用户列表失败：' + (error.response?.data?.message || error.message))
  } finally {
    loading.value = false
  }
}

// 搜索
const handleSearch = () => {
  currentPage.value = 1
  loadUsers()
}

// 重置搜索
const handleReset = () => {
  searchKeyword.value = ''
  searchRole.value = ''
  searchStatus.value = null
  handleSearch()
}

// 分页
const handleSizeChange = (val) => {
  pageSize.value = val
  loadUsers()
}

const handleCurrentChange = (val) => {
  currentPage.value = val
  loadUsers()
}

// 🔑 重置密码 - 打开对话框
const handleResetPassword = (row) => {
  resetPasswordForm.value = {
    userId: row.id,
    username: row.username,
    newPassword: ''  // 清空，留空则使用默认密码
  }
  showResetPasswordDialog.value = true
}

// 🔑 确认重置密码
const confirmResetPassword = async () => {
  // 验证密码长度
  const inputPassword = resetPasswordForm.value.newPassword.trim()
  if (inputPassword && inputPassword.length < 6) {
    ElMessage.warning('密码至少6位！')
    return
  }
  
  resettingPassword.value = true
  try {
    // 如果没有输入密码，使用默认密码 a123456
    const password = inputPassword || 'a123456'
    
    const res = await resetUserPassword(resetPasswordForm.value.userId, password)
    ElMessage.success(res.data.message || `密码已重置为：${password}`)
    showResetPasswordDialog.value = false
  } catch (error) {
    ElMessage.error('重置失败：' + (error.response?.data?.message || error.message))
  } finally {
    resettingPassword.value = false
  }
}

// 🔄 修改角色 - 打开对话框
const handleChangeRole = (row) => {
  changeRoleForm.value = {
    userId: row.id,
    username: row.username,
    currentRole: row.role,
    newRole: row.role  // 默认选中当前角色
  }
  showChangeRoleDialog.value = true
}

// 🔄 确认修改角色
const confirmChangeRole = async () => {
  if (!changeRoleForm.value.newRole) {
    ElMessage.warning('请选择新角色')
    return
  }

  changingRole.value = true
  try {
    await updateUserRole(changeRoleForm.value.userId, changeRoleForm.value.newRole)
    ElMessage.success('角色修改成功')
    showChangeRoleDialog.value = false
    loadUsers()
  } catch (error) {
    ElMessage.error('修改失败：' + (error.response?.data?.message || error.message))
  } finally {
    changingRole.value = false
  }
}

// 启用/禁用用户
const handleToggleStatus = async (row) => {
  const action = row.isActive ? '禁用' : '启用'
  try {
    await ElMessageBox.confirm(
      `确定要${action}用户 ${row.username} 吗？`,
      `${action}账号`,
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    const res = await toggleUserStatus(row.id)
    ElMessage.success(res.data.message)
    loadUsers()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error(`${action}失败：` + (error.response?.data?.message || error.message))
    }
  }
}

// ➕ 创建用户
const handleCreateUser = async () => {
  // 验证
  if (!createForm.value.username) {
    ElMessage.warning('请输入用户名')
    return
  }
  if (!createForm.value.password) {
    ElMessage.warning('请输入密码')
    return
  }
  if (createForm.value.password.length < 6) {
    ElMessage.warning('密码至少6位')
    return
  }
  if (!createForm.value.role) {
    ElMessage.warning('请选择角色')
    return
  }

  creating.value = true
  try {
    await createUser({
      username: createForm.value.username,
      password: createForm.value.password,
      role: createForm.value.role,
      phoneNumber: createForm.value.phoneNumber || null
    })
    
    ElMessage.success('用户创建成功')
    showCreateDialog.value = false
    
    // 重置表单
    createForm.value = {
      username: '',
      password: '',
      role: 'Student',
      phoneNumber: ''
    }
    
    // 刷新用户列表
    loadUsers()
  } catch (error) {
    ElMessage.error('创建失败：' + (error.response?.data?.message || error.message))
  } finally {
    creating.value = false
  }
}

// 角色样式
const getRoleType = (role) => {
  const types = {
    'Admin': 'danger',
    'Maintainer': 'warning',
    'Student': 'primary'
  }
  return types[role] || 'info'
}

const getRoleText = (role) => {
  const texts = {
    'Admin': '👑 管理员',
    'Maintainer': '🔧 维修工',
    'Student': '🎓 学生'
  }
  return texts[role] || role
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

onMounted(() => {
  loadProfile()
  if (isAdmin.value) {
    loadUsers()
  }
})
</script>

<style scoped>
.settings-container {
  padding: 20px;
  min-height: 100vh;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
}

.settings-card {
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

.tab-content {
  padding: 20px;
}

.search-bar {
  display: flex;
  align-items: center;
  margin-bottom: 20px;
}

.pagination {
  margin-top: 20px;
  display: flex;
  justify-content: center;
}

:deep(.el-tabs--border-card) {
  border: none;
  box-shadow: none;
}

:deep(.el-tabs__header) {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
}

:deep(.el-tabs__item) {
  color: rgba(255, 255, 255, 0.8);
  border: none;
}

:deep(.el-tabs__item.is-active) {
  color: white;
  background: rgba(255, 255, 255, 0.2);
}

:deep(.el-tabs__item:hover) {
  color: white;
}
</style>
