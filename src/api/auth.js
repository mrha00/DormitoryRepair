import axios from 'axios'
import router from '../router'

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

export const login = async (username, password) => {
  const res = await api.post('/auth/login', { username, password })
  
  // 保存数据
  localStorage.setItem('token', res.data.token)
  localStorage.setItem('user', JSON.stringify(res.data.user))
  localStorage.setItem('permissions', JSON.stringify(res.data.permissions))
  
  // ✅ 登录成功跳转到工单列表
  router.push('/orders')
  
  return res
}