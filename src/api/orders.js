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

export const createOrder = (data) => {
  return api.post('/orders', data)
}

export const updateOrderStatus = (id, status) => {
  return api.patch(`/orders/${id}/status`, { status })
}