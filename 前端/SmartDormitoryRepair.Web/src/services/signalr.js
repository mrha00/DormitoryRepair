import * as signalR from '@microsoft/signalr'
import { ElNotification } from 'element-plus'
import router from '../router'

class NotificationService {
  connection = null
  username = null
  onConnectionStateChanged = null // 连接状态回调

  async startConnection() {
    // 🔑 改用sessionStorage，每个窗口独立
    const token = sessionStorage.getItem('token')
    const user = JSON.parse(sessionStorage.getItem('user') || '{}')
    this.username = user.username
    
    // 保存当前窗口的token，避免被sessionStorage更改影响
    this.currentToken = token
    this.currentUsername = this.username

    if (!this.currentToken || !this.currentUsername) {
      console.error('No token or username found')
      return
    }
    
    // ✅ 如果已经连接，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      console.log('✅ SignalR已连接，无需重复连接')
      return
    }
    
    // ✅ 如果正在连接，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Connecting) {
      console.log('⏳ SignalR正在连接，请稍候...')
      return
    }
    
    // ✅ 如果正在重连，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Reconnecting) {
      console.log('🔄 SignalR正在重连，请稍候...')
      return
    }
    
    // 🔔 通知开始连接，这样刷新时不会显示红点
    if (this.onConnectionStateChanged) {
      this.onConnectionStateChanged(false, '连接中...', true) // ✅ 第三个参数表示正在连接中
    }
    
    // ✅ 如果已有连接对象，先停止
    if (this.connection) {
      try {
        await this.connection.stop()
      } catch (e) {
        console.log('停止旧连接失败:', e.message)
      }
    }

    // ✅ 智能重连策略：最多60秒，间隔递增
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5002/notificationHub', {
        // 使用当前窗口保存的token，而不是localStorage
        accessTokenFactory: () => this.currentToken,
        skipNegotiation: true, // ✅ 强制使用WebSocket
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          // ✅ 0-60秒内：随机0-5秒重试
          if (retryContext.elapsedMilliseconds < 60000) {
            const delay = Math.random() * 5000
            console.log(`🔄 将在 ${Math.round(delay/1000)} 秒后重试连接...`)
            return delay
          }
          // ✅ 60秒后停止自动重试
          console.log('⚠️ 超过60秒，停止自动重试')
          return null
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // 接收通知
    this.connection.on('ReceiveNotification', (message, data) => {
      // ✅ 移除敏感信息日志，仅记录收到通知
      console.log('📬 收到新通知')
      
      // 使用 ElNotification 显示在右上角，更加醒目
      ElNotification({
        title: '📢 新工单通知',
        message: message,
        type: 'success',
        duration: 8000,
        position: 'top-right',
        showClose: true,
        onClick: () => {
          if (data?.orderId) {
            router.push(`/orders/${data.orderId}`)
          }
        }
      })
      
      // 播放提示音
      this.playNotificationSound()
    })

    // 监听Pong响应
    this.connection.on('Pong', (timestamp) => {
      console.log('收到心跳响应：', timestamp)
    })

    // 连接状态变更
    this.connection.onreconnecting((error) => {
      console.warn('SignalR 正在重连...', error)
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(false, '重连中...', true) // ✅ connecting = true
      }
    })

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR 重连成功', connectionId)
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(true, '已连接', false) // ✅ connecting = false
      }
    })

    this.connection.onclose((error) => {
      console.error('SignalR 连接关闭', error)
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(false, '连接已断开', false) // ✅ connecting = false
      }
    })

    try {
      await this.connection.start()
      console.log('✅ SignalR 已连接，用户:', this.username)
      console.log('✅ 连接状态:', this.connection.state)
      
      // 通知状态变化：已连接
      if (this.onConnectionStateChanged) {
        console.log('✅ 触发状态回调: 已连接')
        this.onConnectionStateChanged(true, '已连接', false) // ✅ connecting = false
      } else {
        console.warn('⚠️ 状态回调未设置！请确保App.vue已加载')
      }
      
      // 启动心跳检测：每30秒发送一次
      this.startHeartbeat()
    } catch (err) {
      console.error('SignalR 连接失败:', err)
      // 通知状态变化：连接失败
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(false, '连接失败', false) // ✅ connecting = false
      }
      // 可以选择性地重试
      setTimeout(() => this.startConnection(), 5000)
    }
  }

  async stopConnection() {
    if (this.connection) {
      try {
        // 停止心跳检测
        this.stopHeartbeat()
        await this.connection.stop()
        console.log('SignalR 已断开')
      } catch (err) {
        console.error('SignalR 断开失败:', err)
      }
    }
  }

  startHeartbeat() {
    this.heartbeatInterval = setInterval(() => {
      if (this.connection?.state === signalR.HubConnectionState.Connected) {
        this.connection.invoke('Ping').catch(err => console.error('心跳发送失败:', err))
      }
    }, 30000) // 30秒
  }

  stopHeartbeat() {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval)
      this.heartbeatInterval = null
    }
  }

  playNotificationSound() {
    // 简单的提示音（可选）
    try {
      const audio = new Audio('/notification.mp3')
      audio.volume = 0.3
      audio.play().catch(() => {
        // 用户未交互前无法播放，忽略错误
      })
    } catch (err) {
      // 忽略音效错误
    }
  }
}

export default new NotificationService()
