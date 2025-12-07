<template>
  <div id="app">
    <!-- 右上角连接状态 - 只在登录后且连接完成后显示 -->
    <div v-if="shouldShowStatus && !isConnecting" class="connection-status" :class="{ connected: isConnected }">
      <el-tooltip :content="connectionText">
        <span class="status-dot"></span>
      </el-tooltip>
    </div>
    <router-view />
  </div>
</template>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
}

.connection-status {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 9999;
}

.status-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #f56c6c;
  display: inline-block;
  animation: pulse-red 2s infinite;
}

.connection-status.connected .status-dot {
  background: #67c23a;
  animation: pulse-green 2s infinite; /* ✅ 绿色也有扩散动画 */
}

/* 红色点的扩散动画 */
@keyframes pulse-red {
  0% {
    box-shadow: 0 0 0 0 rgba(245, 108, 108, 0.7);
  }
  70% {
    box-shadow: 0 0 0 10px rgba(245, 108, 108, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(245, 108, 108, 0);
  }
}

/* 绿色点的扩散动画 */
@keyframes pulse-green {
  0% {
    box-shadow: 0 0 0 0 rgba(103, 194, 58, 0.7);
  }
  70% {
    box-shadow: 0 0 0 10px rgba(103, 194, 58, 0);
  }
  100% {
    box-shadow: 0 0 0 0 rgba(103, 194, 58, 0);
  }
}

/* 📱 全局移动端优化 */
@media (max-width: 768px) {
  /* 禁用移动端双击放大 */
  * {
    touch-action: manipulation;
  }
  
  /* 优化滚动体验 */
  body {
    -webkit-overflow-scrolling: touch;
  }
  
  /* Element Plus 组件移动端优化 */
  .el-message-box {
    width: 90% !important;
    max-width: 400px;
  }
  
  .el-dialog {
    width: 90% !important;
    margin-top: 15vh !important;
  }
  
  .el-drawer {
    width: 85% !important;
  }
  
  /* 分页器移动端优化 */
  .el-pagination {
    padding: 10px 0;
  }
  
  .el-pagination .btn-prev,
  .el-pagination .btn-next,
  .el-pagination .el-pager li {
    min-width: 32px;
    height: 32px;
    line-height: 32px;
    font-size: 14px;
  }
  
  /* 表单项优化 */
  .el-form-item {
    margin-bottom: 18px;
  }
  
  /* 输入框优化 */
  .el-input__inner,
  .el-textarea__inner {
    font-size: 16px !important; /* 防止iOS自动缩放 */
  }
  
  /* 按钮优化 */
  .el-button {
    min-height: 40px;
    padding: 10px 15px;
  }
  
  /* 卡片优化 */
  .el-card {
    border-radius: 8px;
  }
  
  .el-card__header {
    padding: 15px;
  }
  
  .el-card__body {
    padding: 15px;
  }
  
  /* 下拉菜单优化 */
  .el-dropdown-menu {
    max-width: 90vw;
  }
  
  /* 连接状态指示器移动端位置 */
  .connection-status {
    top: 10px;
    right: 10px;
  }
  
  .status-dot {
    width: 10px;
    height: 10px;
  }
}
</style>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import * as signalR from '@microsoft/signalr'
import notificationService from './services/signalr'

const route = useRoute()
const isConnected = ref(false)
const connectionText = ref('连接中...')
const isConnecting = ref(false) // ✅ 初始为false，刷新后立即显示状态
let reconnectInterval = null // ✅ 重连检测定时器

// 监听路由变化，在非登录页面才显示连接状态
const shouldShowStatus = ref(false)

watch(() => route.path, (newPath) => {
  // 只在登录后的页面显示连接状态（不在登录页显示）
  shouldShowStatus.value = newPath !== '/' && newPath !== '/login'
}, { immediate: true })

onMounted(() => {
  console.log('🔧 App.vue 已加载')
  
  // 设置连接状态回调
  notificationService.onConnectionStateChanged = (connected, text, connecting = false) => {
    console.log('🔔 收到状态变化:', connected, text, '连接中:', connecting)
    isConnected.value = connected
    connectionText.value = text
    isConnecting.value = connecting // ✅ 使用传入的connecting参数
  }
  console.log('✅ 状态回调已设置')
  
  // 如果已经连接，直接更新状态
  if (notificationService.connection?.state === signalR.HubConnectionState.Connected) {
    console.log('✅ 检测到已存在的连接')
    isConnected.value = true
    connectionText.value = '已连接'
    isConnecting.value = false
  }
  
  // ✅ 每5秒检查一次连接状态，如果断开则尝试重连
  reconnectInterval = setInterval(async () => {
    const token = sessionStorage.getItem('token')
    if (token && notificationService.connection?.state === signalR.HubConnectionState.Disconnected) {
      console.log('🔍 检测到SignalR断开，尝试重连...')
      await notificationService.startConnection()
    }
  }, 5000) // 5秒检测一次
})

onUnmounted(() => {
  // 清理回调
  notificationService.onConnectionStateChanged = null
  
  // ✅ 清理重连定时器
  if (reconnectInterval) {
    clearInterval(reconnectInterval)
    reconnectInterval = null
  }
})
</script>

