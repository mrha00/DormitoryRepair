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
 * 获取消息列表
 */
export const getNotifications = (params) => {
  return api.get('/notifications', { params })
}

/**
 * 获取未读消息数量
 */
export const getUnreadCount = () => {
  return api.get('/notifications/unread-count')
}

/**
 * 标记消息为已读
 */
export const markAsRead = (id) => {
  return api.put(`/notifications/${id}/read`)
}

/**
 * 标记所有消息为已读
 */
export const markAllAsRead = () => {
  return api.put('/notifications/read-all')
}

/**
 * 删除消息
 */
export const deleteNotification = (id) => {
  return api.delete(`/notifications/${id}`)
}
