-- 添加用户表新字段
-- 执行前请备份数据库！

USE dormitory_repair;

-- 1. 添加手机号字段
ALTER TABLE Users 
ADD COLUMN PhoneNumber VARCHAR(20) NULL COMMENT '手机号';

-- 2. 添加账号状态字段
ALTER TABLE Users 
ADD COLUMN IsActive TINYINT(1) NOT NULL DEFAULT 1 COMMENT '账号状态（1启用 0禁用）';

-- 3. 添加创建时间字段
ALTER TABLE Users 
ADD COLUMN CreateTime DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间';

-- 验证更改
SELECT * FROM Users LIMIT 5;
