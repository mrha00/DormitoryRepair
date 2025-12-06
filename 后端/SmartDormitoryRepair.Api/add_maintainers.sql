-- 添加维修工测试账号
-- 密码都是：admin123

INSERT INTO Users (Username, PasswordHash, Role) VALUES 
('张师傅', '$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS', 'Maintainer'),
('李师傅', '$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS', 'Maintainer'),
('王师傅', '$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS', 'Maintainer'),
('刘师傅', '$2a$11$KHLIPm3f2AipGAKax9Ym6Oh3x3A23A93WGNCDO/4riexaJWo6Z.xS', 'Maintainer');

-- 查询确认
SELECT * FROM Users WHERE Role = 'Maintainer';
