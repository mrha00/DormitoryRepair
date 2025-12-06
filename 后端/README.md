# 智慧宿舍报修平台 - 后端服务

## 项目结构

```
后端/
├── SmartDormitoryRepair.Api/       # 主Web API项目
├── SmartDormitoryRepair.Domain/    # 领域模型项目
└── README.md                      # 项目说明文档
```

## 技术栈

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- MySQL (Pomelo.EntityFrameworkCore.MySql)
- JWT Authentication
- BCrypt for password hashing

## 功能特性

1. **用户认证与授权**
   - JWT Token认证
   - BCrypt密码加密
   - RBAC权限控制系统

2. **RBAC权限模型**
   - 用户(User) - 角色(Role) - 权限(Permission)三级权限管理
   - 支持动态分配用户角色和角色权限

3. **工单管理**
   - 工单创建、查询、状态更新
   - 分页查询和状态筛选
   - 工单指派功能

## 数据库设计

### 核心实体

- **User**: 用户信息
- **Role**: 角色定义
- **Permission**: 权限定义
- **UserRole**: 用户角色关联
- **RolePermission**: 角色权限关联
- **Order**: 工单信息

## API端点

### 认证相关
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录

### 工单管理
- `GET /api/orders` - 获取工单列表(分页)
- `POST /api/orders` - 创建新工单
- `PATCH /api/orders/{id}/status` - 更新工单状态

## 部署说明

1. 确保已安装.NET 8 SDK
2. 配置数据库连接字符串
3. 运行数据库迁移: `dotnet ef database update`
4. 启动应用: `dotnet run`