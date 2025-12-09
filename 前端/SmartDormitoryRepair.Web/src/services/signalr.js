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
      // 生产环境中不应暴露具体的错误信息
      return
    }
    
    // ✅ 如果已经连接，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return
    }
    
    // ✅ 如果正在连接，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Connecting) {
      return
    }
    
    // ✅ 如果正在重连，不重复连接
    if (this.connection?.state === signalR.HubConnectionState.Reconnecting) {
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
        // 生产环境中忽略具体的错误信息
      }
    }

    // ✅ 智能重连策略：最多60秒，间隔递增
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_URL, {
        
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
            return delay
          }
          // ✅ 60秒后停止自动重试
          return null
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // 接收通知
    this.connection.on('ReceiveNotification', (message, data) => {
      // ✅ 移除敏感信息日志，仅记录收到通知
      
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
      // 生产环境中移除心跳日志
    })

    // 连接状态变更
    this.connection.onreconnecting((error) => {
      // 生产环境中移除具体的错误信息
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(false, '重连中...', true) // ✅ connecting = true
      }
    })

    this.connection.onreconnected((connectionId) => {
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(true, '已连接', false) // ✅ connecting = false
      }
    })

    this.connection.onclose((error) => {
      // 生产环境中移除具体的错误信息
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(false, '连接已断开', false) // ✅ connecting = false
      }
    })

    try {
      await this.connection.start()
      
      // 通知状态变化：已连接
      if (this.onConnectionStateChanged) {
        this.onConnectionStateChanged(true, '已连接', false) // ✅ connecting = false
      }
      
      // 启动心跳检测：每30秒发送一次
      this.startHeartbeat()
    } catch (err) {
      // 生产环境中移除具体的错误信息
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
      } catch (err) {
        // 生产环境中忽略具体的错误信息
      }
    }
  }

  startHeartbeat() {
    this.heartbeatInterval = setInterval(() => {
      if (this.connection?.state === signalR.HubConnectionState.Connected) {
        this.connection.invoke('Ping').catch(err => {
          // 生产环境中移除具体的错误信息
        })
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
