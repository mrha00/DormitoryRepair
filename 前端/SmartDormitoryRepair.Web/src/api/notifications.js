import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5002/api',
  timeout: 5000
})

// 添加JWT令牌拦截器
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

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
