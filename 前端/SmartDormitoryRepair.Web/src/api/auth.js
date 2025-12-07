import axios from 'axios'
import router from '../router'
import { ElMessage } from 'element-plus'
import notificationService from '../services/signalr'

const api = axios.create({
  baseURL: 'http://localhost:5002/api',
  timeout: 5000
})

// 添加JWT令牌拦截器
api.interceptors.request.use(config => {
  // 🔑 改用sessionStorage，每个窗口独立
  const token = sessionStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export const login = async (username, password) => {
  const res = await api.post('/auth/login', { username, password })
  
  // 🔑 保存到sessionStorage，每个窗口独立
  sessionStorage.setItem('token', res.data.token)
  sessionStorage.setItem('user', JSON.stringify(res.data.user))
  sessionStorage.setItem('permissions', JSON.stringify(res.data.permissions))
  
  // ✅ 启动 SignalR 连接
  try {
    await notificationService.startConnection()
  } catch (err) {
    console.error('SignalR 连接失败:', err)
  }
  
  ElMessage.success(`欢迎回来，${res.data.user.username}！`)
  
  // 登录成功跳转到工单列表
  router.push('/orders')
  
  return res
}

export const logout = () => {
  // 断开 SignalR 连接
  notificationService.stopConnection()
  
  // 🔑 清空sessionStorage
  sessionStorage.clear()
  
  ElMessage.info('已退出登录')
  
  // 跳转到登录页
  router.push('/')
}