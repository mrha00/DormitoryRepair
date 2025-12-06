# 智慧宿舍报修平台 - 前端应用

## 项目结构

```
前端/
├── SmartDormitoryRepair.Web/       # Vue 3前端项目
│   ├── src/                        # 源代码目录
│   │   ├── api/                    # API调用封装
│   │   ├── views/                  # 页面组件
│   │   ├── router/                 # 路由配置
│   │   └── App.vue                 # 根组件
│   ├── package.json                # 依赖配置
│   └── README.md                  # 项目说明文档
```

## 技术栈

- Vue 3 (Composition API)
- Element Plus UI 组件库
- Vue Router 路由管理
- Axios HTTP客户端
- Vite 构建工具

## 功能特性

1. **用户认证**
   - 登录页面
   - JWT Token存储与管理
   - 路由守卫

2. **工单管理**
   - 工单列表展示(带分页)
   - 工单状态筛选
   - 响应式设计适配移动端

3. **权限控制**
   - 基于用户权限的按钮显示/隐藏
   - 路由级别的访问控制

## 项目结构详解

### src/api/
- `auth.js`: 认证相关API封装
- `orders.js`: 工单相关API封装

### src/views/
- `Login.vue`: 登录页面组件
- `OrderList.vue`: 工单列表页面组件

### src/router/
- `index.js`: Vue Router配置，包含路由守卫

## 开发环境搭建

1. 安装依赖: `npm install`
2. 启动开发服务器: `npm run dev`
3. 构建生产版本: `npm run build`

## 部署说明

1. 构建项目: `npm run build`
2. 将dist目录下的文件部署到Web服务器
3. 配置反向代理指向后端API服务