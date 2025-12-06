import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import OrderList from '../views/OrderList.vue'
import OrderCreate from '../views/OrderCreate.vue'

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
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  if (to.meta.requiresAuth && !token) {
    next('/')
  } else {
    next()
  }
})

export default router