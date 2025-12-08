import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '../router'

const api = axios.create({
  baseURL: 'http://localhost:5002/api',
  timeout: 5000
})

// 添加JWT令牌拦截器
api.interceptors.request.use(config => {
  const token = sessionStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// 🚨 添加响应拦截器，处理 401 错误
api.interceptors.response.use(
  response => response,
  error => {
    // 处理 401 未授权错误
    if (error.response?.status === 401) {
      console.warn('🔐 Token 已失效，清除登录信息')
      // 清除失效的认证信息
      sessionStorage.removeItem('token')
      sessionStorage.removeItem('user')
      // 提示用户
      ElMessage.warning('🔒 登录已过期，请重新登录')
      // 跳转到登录页
      router.push('/login')
    }
    return Promise.reject(error)
  }
)

/**
 * 获取当前用户信息
 */
export const getProfile = () => {
  return api.get('/users/profile')
}

/**
 * 修改密码
 */
export const changePassword = (data) => {
  return api.put('/users/change-password', data)
}

/**
 * 更新个人资料
 */
export const updateProfile = (data) => {
  return api.put('/users/profile', data)
}

/**
 * 获取用户列表（管理员）
 */
export const getUsers = (params) => {
  return api.get('/users', { params })
}

/**
 * 创建新用户（管理员）
 */
export const createUser = (data) => {
  return api.post('/users', data)
}

/**
 * 重置用户密码（管理员）
 * @param {number} userId - 用户ID
 * @param {string} password - 新密码（可选，默认a123456）
 */
export const resetUserPassword = (userId, password = null) => {
  const data = password ? { password } : {}
  return api.post(`/users/${userId}/reset-password`, data)
}

/**
 * 修改用户角色（管理员）
 */
export const updateUserRole = (userId, role) => {
  return api.put(`/users/${userId}/role`, { role })
}

/**
 * 启用/禁用用户（管理员）
 */
export const toggleUserStatus = (userId) => {
  return api.put(`/users/${userId}/toggle-status`)
}

/**
 * 删除用户（管理员）
 */
export const deleteUser = (userId) => {
  return api.delete(`/users/${userId}`)
}
