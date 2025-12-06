import * as signalR from '@microsoft/signalr'
import { ElNotification } from 'element-plus'
import router from '../router'

class NotificationService {
  connection = null
  username = null

  async startConnection() {
    const token = localStorage.getItem('token')
    const user = JSON.parse(localStorage.getItem('user') || '{}')
    this.username = user.username

    if (!token || !this.username) {
      console.error('No token or username found')
      return
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5002/notificationHub', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // 断线重连策略
      .configureLogging(signalR.LogLevel.Information)
      .build()

    // 接收通知
    this.connection.on('ReceiveNotification', (message, data) => {
      console.log('Received notification:', message, data)
      
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

    // 连接状态变更
    this.connection.onreconnecting((error) => {
      console.warn('SignalR 正在重连...', error)
    })

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR 重连成功', connectionId)
    })

    this.connection.onclose((error) => {
      console.error('SignalR 连接关闭', error)
    })

    try {
      await this.connection.start()
      console.log('SignalR 已连接，用户:', this.username)
    } catch (err) {
      console.error('SignalR 连接失败:', err)
      // 可以选择性地重试
      setTimeout(() => this.startConnection(), 5000)
    }
  }

  async stopConnection() {
    if (this.connection) {
      try {
        await this.connection.stop()
        console.log('SignalR 已断开')
      } catch (err) {
        console.error('SignalR 断开失败:', err)
      }
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
