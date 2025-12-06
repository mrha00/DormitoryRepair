# 后端API

智慧宿舍报修平台的后端服务，基于.NET 8构建的RESTful API。

## 技术栈

- **.NET 8** - 最新的.NET框架
- **Entity Framework Core** - ORM框架
- **MySQL** - 数据库
- **JWT** - 身份认证
- **BCrypt** - 密码加密
- **RBAC** - 基于角色的权限控制

## API端点

### 认证相关
- `POST /api/auth/register` - 用户注册
  - 请求体：`{ "username": "string", "password": "string" }`
  - 响应：`{ "message": "注册成功" }`

- `POST /api/auth/login` - 用户登录
  - 请求体：`{ "username": "string", "password": "string" }`
  - 响应：`{ "token": "JWT_TOKEN" }`

### 工单管理
- `GET /api/orders` - 查询工单（分页）
  - 查询参数：`page`, `pageSize`
  - 响应：工单列表及分页信息

- `POST /api/orders` - 创建工单
  - 请求体：`{ "title": "string", "description": "string", "location": "string" }`
  - 需要认证：✅

- `PATCH /api/orders/{id}/status` - 更新工单状态
  - 请求体：`{ "status": "Pending|Processing|Completed" }`
  - 需要认证：✅
  - 权限要求：Repairman或Admin

## 权限说明

系统实现了完整的RBAC权限控制：

| 角色 | 权限 |
|------|------|
| **Student（学生）** | 创建工单、查看自己的工单 |
| **Repairman（维修员）** | 查看指派给自己的工单、更新工单状态 |
| **Admin（管理员）** | 所有权限（用户管理、工单管理、权限分配） |

## 快速开始

### 1. 安装依赖
确保已安装 .NET 8 SDK：
```bash
dotnet --version
```

### 2. 配置数据库
编辑 `appsettings.json`，配置MySQL连接字符串：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=DormitoryRepair;user=root;password=yourpassword"
  }
}
```

### 3. 执行数据库迁移
```bash
dotnet ef database update
```

### 4. 运行项目
```bash
dotnet run
```

API将在 `http://localhost:5062` 启动。

## 项目结构

```
SmartDormitoryRepair.Api/
├── Data/                    # 数据访问层
│   └── AppDbContext.cs     # EF Core上下文
├── Services/               # 服务层
│   └── PasswordHasher.cs   # 密码加密服务
├── Migrations/             # 数据库迁移文件
├── AuthController.cs       # 认证控制器
├── OrdersController.cs     # 工单控制器
├── Program.cs             # 程序入口
└── SeedData.cs            # 种子数据

SmartDormitoryRepair.Domain/
├── User.cs                # 用户实体
├── Role.cs                # 角色实体
├── Permission.cs          # 权限实体
├── Order.cs               # 工单实体
├── UserRole.cs            # 用户角色关联
└── RolePermission.cs      # 角色权限关联
```

## 数据库设计

### 核心表
- `Users` - 用户信息
- `Roles` - 角色定义
- `Permissions` - 权限定义
- `UserRoles` - 用户角色多对多关系
- `RolePermissions` - 角色权限多对多关系
- `Orders` - 工单信息

## 安全性

- ✅ 密码使用BCrypt加密存储
- ✅ JWT Token认证机制
- ✅ 基于角色的访问控制
- ✅ API请求验证和授权

## 开发规范

- 使用异步编程（async/await）
- RESTful API设计原则
- 统一的错误处理
- 日志记录

## 测试

默认种子数据账号：
```
管理员：admin / admin123
学生：student / student123
维修员：repairman / repair123
```

## 许可证

本项目仅供学习交流使用。
