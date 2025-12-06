# 前端项目

智慧宿舍报修平台的前端应用，基于Vue 3构建的现代化单页应用。

## 技术栈

- **Vue 3** - 使用Composition API
- **Element Plus** - 现代化UI组件库
- **Vue Router** - 单页应用路由管理
- **Axios** - HTTP客户端
- **Vite** - 新一代前端构建工具

## 快速开始

### 安装依赖
```bash
npm install
```

### 开发模式运行
```bash
npm run dev
```

### 生产环境构建
```bash
npm run build
```

## 页面结构

- 🔐 **登录页** (`/`) - 用户登录界面
- 📋 **工单列表** (`/orders`) - 工单管理页面
  - 工单查看（分页）
  - 工单创建
  - 工单状态更新

## 项目特色

- ✨ 现代化、简洁优雅的设计风格
- 🎨 渐变色彩和流畅动画效果
- 📱 响应式设计，适配多种设备
- 🔒 JWT Token身份认证
- 🎯 基于角色的权限控制

## 开发说明

### 目录结构
```
src/
├── api/              # API接口封装
│   ├── auth.js      # 认证相关接口
│   └── orders.js    # 工单相关接口
├── router/          # 路由配置
│   └── index.js     # 路由定义
├── views/           # 页面组件
│   ├── Login.vue    # 登录页
│   └── OrderList.vue # 工单列表页
├── App.vue          # 根组件
└── main.js          # 入口文件
```

### API配置

API基础地址配置在各个API文件中，默认为：
```
http://localhost:5062
```

如需修改，请编辑 `src/api/auth.js` 和 `src/api/orders.js` 中的 `baseURL`。

## 浏览器支持

- Chrome (推荐)
- Firefox
- Safari
- Edge

## 许可证

本项目仅供学习交流使用。
