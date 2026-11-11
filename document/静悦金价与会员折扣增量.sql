/*
  静悦金价 / 按克计价 / 会员折扣矩阵增量脚本
  执行库：jydb（SQL Server 2008 R2 兼容）
  说明：脚本可重复执行；只新增表、字段、菜单，不写入任何业务数据。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.jy_metal_price', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_metal_price (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_metal_price PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        StoreId bigint NULL,
        MetalItemId bigint NOT NULL,
        MetalName nvarchar(100) NOT NULL,
        PricePerGram decimal(18,2) NOT NULL,
        EffectiveTime datetime NOT NULL,
        ExpireTime datetime NULL,
        Status int NOT NULL CONSTRAINT DF_jy_metal_price_Status DEFAULT (0),
        DelFlag int NOT NULL CONSTRAINT DF_jy_metal_price_DelFlag DEFAULT (0),
        Create_by nvarchar(64) NULL,
        Create_time datetime NOT NULL CONSTRAINT DF_jy_metal_price_CreateTime DEFAULT (GETDATE()),
        Update_by nvarchar(64) NULL,
        Update_time datetime NULL,
        Remark nvarchar(500) NULL
    );
    CREATE INDEX IX_jy_metal_price_lookup ON dbo.jy_metal_price(TenantId, MetalItemId, Status, EffectiveTime);
    CREATE INDEX IX_jy_metal_price_effective ON dbo.jy_metal_price(TenantId, EffectiveTime, ExpireTime);
END;

IF OBJECT_ID(N'dbo.jy_member_discount_rule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_member_discount_rule (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_member_discount_rule PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        MemberLevelId bigint NOT NULL,
        StyleItemId bigint NULL,
        PricingMode int NULL,
        DiscountRate decimal(10,4) NOT NULL CONSTRAINT DF_jy_member_discount_Rate DEFAULT (1),
        DiscountBase int NOT NULL CONSTRAINT DF_jy_member_discount_Base DEFAULT (1),
        Priority int NOT NULL CONSTRAINT DF_jy_member_discount_Priority DEFAULT (0),
        EffectiveTime datetime NULL,
        ExpireTime datetime NULL,
        Status int NOT NULL CONSTRAINT DF_jy_member_discount_Status DEFAULT (0),
        DelFlag int NOT NULL CONSTRAINT DF_jy_member_discount_DelFlag DEFAULT (0),
        Create_by nvarchar(64) NULL,
        Create_time datetime NOT NULL CONSTRAINT DF_jy_member_discount_CreateTime DEFAULT (GETDATE()),
        Update_by nvarchar(64) NULL,
        Update_time datetime NULL,
        Remark nvarchar(500) NULL
    );
    CREATE INDEX IX_jy_member_discount_lookup ON dbo.jy_member_discount_rule(TenantId, MemberLevelId, Status, DelFlag);
    CREATE INDEX IX_jy_member_discount_style ON dbo.jy_member_discount_rule(TenantId, StyleItemId, PricingMode);
END;

IF COL_LENGTH(N'dbo.jy_goods_item', N'PricingMode') IS NULL
    ALTER TABLE dbo.jy_goods_item ADD PricingMode int NOT NULL CONSTRAINT DF_jy_goods_item_PricingMode DEFAULT (1);
IF COL_LENGTH(N'dbo.jy_goods_item', N'LaborFee') IS NULL
    ALTER TABLE dbo.jy_goods_item ADD LaborFee decimal(18,2) NOT NULL CONSTRAINT DF_jy_goods_item_LaborFee DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_goods_item', N'LaborFeeMode') IS NULL
    ALTER TABLE dbo.jy_goods_item ADD LaborFeeMode int NOT NULL CONSTRAINT DF_jy_goods_item_LaborFeeMode DEFAULT (1);

IF COL_LENGTH(N'dbo.jy_inbound_line', N'PricingMode') IS NULL
    ALTER TABLE dbo.jy_inbound_line ADD PricingMode int NOT NULL CONSTRAINT DF_jy_inbound_line_PricingMode DEFAULT (1);
IF COL_LENGTH(N'dbo.jy_inbound_line', N'LaborFee') IS NULL
    ALTER TABLE dbo.jy_inbound_line ADD LaborFee decimal(18,2) NOT NULL CONSTRAINT DF_jy_inbound_line_LaborFee DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_inbound_line', N'LaborFeeMode') IS NULL
    ALTER TABLE dbo.jy_inbound_line ADD LaborFeeMode int NOT NULL CONSTRAINT DF_jy_inbound_line_LaborFeeMode DEFAULT (1);

IF COL_LENGTH(N'dbo.jy_sale_order_item', N'PricingMode') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD PricingMode int NOT NULL CONSTRAINT DF_jy_sale_item_PricingMode DEFAULT (1);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MetalPriceId') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MetalPriceId bigint NULL;
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MarketMetalPricePerGram') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MarketMetalPricePerGram decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_MarketMetalPrice DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MetalPricePerGram') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MetalPricePerGram decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_MetalPrice DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'PriceSnapshotTime') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD PriceSnapshotTime datetime NULL;
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'LaborFee') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD LaborFee decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_LaborFee DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'LaborFeeMode') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD LaborFeeMode int NOT NULL CONSTRAINT DF_jy_sale_item_LaborFeeMode DEFAULT (1);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MaterialAmount') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MaterialAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_MaterialAmount DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'LaborAmount') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD LaborAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_LaborAmount DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'ListAmount') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD ListAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_ListAmount DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MemberDiscountRuleId') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MemberDiscountRuleId bigint NULL;
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MemberDiscountRate') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MemberDiscountRate decimal(10,4) NOT NULL CONSTRAINT DF_jy_sale_item_MemberDiscountRate DEFAULT (1);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'MemberDiscountAmount') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD MemberDiscountAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_MemberDiscountAmount DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'ActivityBenefit') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD ActivityBenefit decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_ActivityBenefit DEFAULT (0);
IF COL_LENGTH(N'dbo.jy_sale_order_item', N'PayAmount') IS NULL
    ALTER TABLE dbo.jy_sale_order_item ADD PayAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_sale_item_PayAmount DEFAULT (0);

/* 菜单：价格与折扣 */
DECLARE @rootId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = 0 AND Path = N'business');
IF @rootId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.sys_menu WHERE ParentId = @rootId AND Path = N'pricing' AND MenuType = N'C')
BEGIN
    INSERT INTO dbo.sys_menu
        (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
    VALUES
        (N'价格与折扣', @rootId, 6, N'pricing', N'jingyue/pricing/index', 0, 0, N'C', N'0', N'0', N'jingyue:pricing:list', N'price-tag', N'system', GETDATE(), N'金价与会员折扣矩阵');
END;

DECLARE @pricingMenuId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = @rootId AND Path = N'pricing' AND MenuType = N'C');
IF @pricingMenuId IS NOT NULL
BEGIN
    DECLARE @btnSeed TABLE (MenuName nvarchar(50), Perms nvarchar(100), OrderNum int);
    INSERT INTO @btnSeed VALUES
        (N'价格查询', N'jingyue:pricing:query', 1),
        (N'价格新增', N'jingyue:pricing:add', 2),
        (N'价格修改', N'jingyue:pricing:edit', 3),
        (N'价格删除', N'jingyue:pricing:remove', 4);
    INSERT INTO dbo.sys_menu
        (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
    SELECT b.MenuName, @pricingMenuId, b.OrderNum, NULL, NULL, 0, 0, N'F', N'0', N'0', b.Perms, N'#', N'system', GETDATE(), N'价格与折扣按钮'
    FROM @btnSeed b
    WHERE NOT EXISTS (SELECT 1 FROM dbo.sys_menu m WHERE m.ParentId = @pricingMenuId AND m.Perms = b.Perms);

    INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
    SELECT 1, m.MenuId, N'system', GETDATE()
    FROM dbo.sys_menu m
    WHERE (m.MenuId = @pricingMenuId OR m.ParentId = @pricingMenuId)
      AND NOT EXISTS (SELECT 1 FROM dbo.sys_role_menu rm WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId);
END;

PRINT N'静悦金价与会员折扣增量脚本执行完成';
