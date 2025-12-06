# 智慧宿舍报修平台

一个基于.NET 8和Vue 3的宿舍维修管理系统，实现了完整的RBAC权限控制和工单管理功能。

## 项目结构

```
毕业设计/
├── 后端/                          # 后端服务
│   ├── SmartDormitoryRepair.Api/   # Web API项目
│   ├── SmartDormitoryRepair.Domain/# 领域模型
│   └── README.md                  # 后端说明
├── 前端/                          # 前端应用
│   ├── SmartDormitoryRepair.Web/   # Vue 3项目
│   └── README.md                  # 前端说明
├── 文档/                          # 项目文档
│   └── 开发文档/                  # 开发相关文档
└── README.md                     # 项目根说明
```

## 技术架构

### 后端技术栈
- .NET 8 Web API
- Entity Framework Core
- MySQL数据库
- JWT身份认证
- BCrypt密码加密
- RBAC权限控制系统

### 前端技术栈
- Vue 3 (Composition API)
- Element Plus UI组件库
- Vue Router
- Axios HTTP客户端
- Vite构建工具

## 核心功能

### 1. 用户认证与权限管理
- 用户注册与登录
- JWT Token认证机制
- 基于角色的访问控制(RBAC)
- 权限动态分配与验证

### 2. 工单管理系统
- 工单创建与提交
- 工单列表展示(分页)
- 工单状态管理(Pending/Processing/Completed)
- 工单查询与筛选

### 3. 响应式界面设计
- 适配桌面端和移动端
- 现代化UI设计
- 直观的操作流程

## 部署指南

### 后端部署
1. 安装.NET 8 SDK
2. 配置数据库连接字符串
3. 执行数据库迁移: `dotnet ef database update`
4. 启动服务: `dotnet run`

### 前端部署
1. 安装Node.js (推荐v16+)
2. 安装依赖: `npm install`
3. 开发模式运行: `npm run dev`
4. 生产构建: `npm run build`

## 项目特点

- **安全性**: 使用JWT和BCrypt保障用户信息安全
- **可扩展性**: 基于RBAC的权限模型便于功能扩展
- **现代化**: 采用最新的.NET 8和Vue 3技术栈
- **响应式**: 前端界面适配不同设备屏幕
- **易维护**: 清晰的项目结构和代码组织

## 许可证

本项目仅供学习交流使用。