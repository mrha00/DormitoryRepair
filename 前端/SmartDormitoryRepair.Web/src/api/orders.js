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

export const getOrders = (params) => {
  return api.get('/orders', { params })
}

// 获取单个工单
export const getOrder = (id) => {
  return api.get(`/orders/${id}`)
}

export const createOrder = (data) => {
  return api.post('/orders', data)
}

// 📷 上传文件
export const uploadFile = (formData) => {
  return api.post('/file/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

// 更新状态
export const updateOrderStatus = (id, status) => {
  return api.patch(`/orders/${id}/status`, { status })
}

// 获取维修工列表
export const getMaintainers = () => {
  return api.get('/orders/maintainers')
}

// 指派工单
export const assignOrder = (orderId, maintainerId) => {
  return api.post(`/orders/${orderId}/assign`, { maintainerId })
}

// 🗑️ 删除工单（管理员）
export const deleteOrder = (orderId) => {
  return api.delete(`/orders/${orderId}`)
}

// 🔄 重新指派工单（管理员）
export const reassignOrder = (orderId, maintainerId) => {
  return api.put(`/orders/${orderId}/reassign`, { maintainerId })
}