import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import OrderList from '../views/OrderList.vue'
import OrderCreate from '../views/OrderCreate.vue'
import OrderDetail from '../views/OrderDetail.vue'
import NotificationCenter from '../views/NotificationCenter.vue'
import notificationService from '../services/signalr' // ✅ 导入SignalR服务

const routes = [
  {
    path: '/',
    name: 'Login',
    component: Login
  },
  {
    path: '/orders',
    name: 'OrderList',
    component: OrderList,
    meta: { requiresAuth: true }
  },
  {
    path: '/orders/create',
    name: 'OrderCreate',
    component: OrderCreate,
    meta: { requiresAuth: true }
  },
  {
    path: '/orders/:id',
    name: 'OrderDetail',
    component: OrderDetail,
    meta: { requiresAuth: true }
  },
  {
    path: '/notifications',
    name: 'NotificationCenter',
    component: NotificationCenter,
    meta: { requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach(async (to, from, next) => {
  const token = sessionStorage.getItem('token')
  
  console.log('🔍 路由守卫:', to.path, 'requiresAuth:', to.meta.requiresAuth, 'hasToken:', !!token)
  
  if (to.meta.requiresAuth && !token) {
    // 未登录，跳转到登录页
    console.log('⛔ 未登录，跳转到登录页')
    next('/')
  } else if (to.meta.requiresAuth && token) {
    // ✅ 已登录，确保SignalR连接
    if (!notificationService.connection || notificationService.connection.state === 'Disconnected') {
      console.log('🔌 检测到SignalR未连接，自动启动...')
      await notificationService.startConnection()
    }
    next()
  } else {
    next()
  }
})

export default router