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