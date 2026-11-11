/*
  静悦盘点与综合报表增量脚本
  执行库：jydb（SQL Server 2008 R2 兼容）
  说明：只新增盘点单据表、明细表、索引和菜单权限，不写入业务数据。
        脚本可重复执行，不会修改 jy_goods_item / jy_material 主表结构。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.jy_stocktake', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.jy_stocktake (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_stocktake PRIMARY KEY,
            TenantId nvarchar(64) NOT NULL,
            StoreId bigint NULL,
            StocktakeNo nvarchar(32) NOT NULL,
            TakeType int NOT NULL,
            TakeTime datetime NOT NULL,
            Status int NOT NULL CONSTRAINT DF_jy_stocktake_Status DEFAULT (0),
            LineCount int NOT NULL CONSTRAINT DF_jy_stocktake_LineCount DEFAULT (0),
            DiffLineCount int NOT NULL CONSTRAINT DF_jy_stocktake_DiffLineCount DEFAULT (0),
            GoodsDiffQty decimal(18,4) NOT NULL CONSTRAINT DF_jy_stocktake_GoodsDiffQty DEFAULT (0),
            MaterialDiffWeight decimal(18,4) NOT NULL CONSTRAINT DF_jy_stocktake_MaterialDiffWeight DEFAULT (0),
            GainAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_stocktake_GainAmount DEFAULT (0),
            LossAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_stocktake_LossAmount DEFAULT (0),
            DiffAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_stocktake_DiffAmount DEFAULT (0),
            OperatorUserId bigint NULL,
            ConfirmUserId bigint NULL,
            ConfirmTime datetime NULL,
            DelFlag int NOT NULL CONSTRAINT DF_jy_stocktake_DelFlag DEFAULT (0),
            Create_by nvarchar(64) NULL,
            Create_time datetime NOT NULL CONSTRAINT DF_jy_stocktake_CreateTime DEFAULT (GETDATE()),
            Update_by nvarchar(64) NULL,
            Update_time datetime NULL,
            Remark nvarchar(500) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.jy_stocktake_line', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.jy_stocktake_line (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_stocktake_line PRIMARY KEY,
            TenantId nvarchar(64) NOT NULL,
            StocktakeId bigint NOT NULL,
            SortNo int NOT NULL,
            ItemType int NOT NULL,
            GoodsItemId bigint NULL,
            MaterialId bigint NULL,
            ItemNo nvarchar(32) NOT NULL,
            ItemName nvarchar(100) NOT NULL,
            Unit nvarchar(16) NULL,
            CategoryKey nvarchar(150) NULL,
            CategoryName nvarchar(100) NULL,
            Barcode nvarchar(64) NULL,
            StyleName nvarchar(100) NULL,
            MetalName nvarchar(100) NULL,
            ItemMode int NOT NULL CONSTRAINT DF_jy_stocktake_line_ItemMode DEFAULT (1),
            BookQty decimal(18,4) NOT NULL,
            ActualQty decimal(18,4) NOT NULL,
            DiffQty decimal(18,4) NOT NULL,
            LockedQty decimal(18,4) NOT NULL CONSTRAINT DF_jy_stocktake_line_LockedQty DEFAULT (0),
            UnitCost decimal(18,6) NOT NULL CONSTRAINT DF_jy_stocktake_line_UnitCost DEFAULT (0),
            DiffAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_stocktake_line_DiffAmount DEFAULT (0),
            ItemStatus int NULL,
            Remark nvarchar(200) NULL,
            DelFlag int NOT NULL CONSTRAINT DF_jy_stocktake_line_DelFlag DEFAULT (0),
            Create_time datetime NOT NULL CONSTRAINT DF_jy_stocktake_line_CreateTime DEFAULT (GETDATE())
        );
    END;

    /* 旧版盘点明细补齐分类与商品描述快照，幂等执行。 */
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'CategoryKey') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD CategoryKey nvarchar(150) NULL;
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'CategoryName') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD CategoryName nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'Barcode') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD Barcode nvarchar(64) NULL;
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'StyleName') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD StyleName nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'MetalName') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD MetalName nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.jy_stocktake_line', N'ItemMode') IS NULL
        ALTER TABLE dbo.jy_stocktake_line ADD ItemMode int NOT NULL CONSTRAINT DF_jy_stocktake_line_ItemMode DEFAULT (1);

    IF OBJECT_ID(N'dbo.jy_stocktake_category', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.jy_stocktake_category (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_stocktake_category PRIMARY KEY,
            TenantId nvarchar(64) NOT NULL,
            StocktakeId bigint NOT NULL,
            ItemType int NOT NULL,
            CategoryKey nvarchar(150) NOT NULL,
            CategoryName nvarchar(100) NOT NULL,
            ItemCount int NOT NULL CONSTRAINT DF_jy_stocktake_category_ItemCount DEFAULT (0),
            IsCompleted int NOT NULL CONSTRAINT DF_jy_stocktake_category_IsCompleted DEFAULT (0),
            IncompleteReason nvarchar(200) NULL,
            Update_by nvarchar(64) NULL,
            Update_time datetime NULL
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake')
        AND name = N'UX_jy_stocktake_Tenant_No')
        CREATE UNIQUE INDEX UX_jy_stocktake_Tenant_No ON dbo.jy_stocktake(TenantId, StocktakeNo);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake')
        AND name = N'IX_jy_stocktake_List')
        CREATE INDEX IX_jy_stocktake_List ON dbo.jy_stocktake(TenantId, TakeType, Status, TakeTime);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_line')
        AND name = N'IX_jy_stocktake_line_Doc')
        CREATE INDEX IX_jy_stocktake_line_Doc ON dbo.jy_stocktake_line(StocktakeId, SortNo);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_line')
        AND name = N'IX_jy_stocktake_line_Goods')
        CREATE INDEX IX_jy_stocktake_line_Goods ON dbo.jy_stocktake_line(TenantId, GoodsItemId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_line')
        AND name = N'IX_jy_stocktake_line_Material')
        CREATE INDEX IX_jy_stocktake_line_Material ON dbo.jy_stocktake_line(TenantId, MaterialId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_category')
        AND name = N'UX_jy_stocktake_category_Doc_Category')
        CREATE UNIQUE INDEX UX_jy_stocktake_category_Doc_Category ON dbo.jy_stocktake_category(StocktakeId, CategoryKey);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_category')
        AND name = N'IX_jy_stocktake_category_Tenant_Doc')
        CREATE INDEX IX_jy_stocktake_category_Tenant_Doc ON dbo.jy_stocktake_category(TenantId, StocktakeId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_stocktake_line')
        AND name = N'IX_jy_stocktake_line_Doc_Category')
        CREATE INDEX IX_jy_stocktake_line_Doc_Category ON dbo.jy_stocktake_line(StocktakeId, CategoryKey);

    DECLARE @rootId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = 0 AND Path = N'business');
    IF @rootId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.sys_menu WHERE ParentId = @rootId AND Path = N'stocktake' AND MenuType = N'C')
    BEGIN
        INSERT INTO dbo.sys_menu
            (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
        VALUES
            (N'盘点管理', @rootId, 7, N'stocktake', N'jingyue/stocktake/index', 0, 0, N'C', N'0', N'0', N'jingyue:stocktake:list', N'checklist', N'system', GETDATE(), N'正品盘点、物料盘点与综合盘点报表');
    END;

    DECLARE @stocktakeMenuId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = @rootId AND Path = N'stocktake' AND MenuType = N'C');
    IF @stocktakeMenuId IS NOT NULL
    BEGIN
        DECLARE @btnSeed TABLE (MenuName nvarchar(50), Perms nvarchar(100), OrderNum int);
        INSERT INTO @btnSeed VALUES
            (N'盘点查询', N'jingyue:stocktake:query', 1),
            (N'盘点新增', N'jingyue:stocktake:add', 2),
            (N'盘点修改', N'jingyue:stocktake:edit', 3),
            (N'盘点删除', N'jingyue:stocktake:remove', 4),
            (N'盘点确认', N'jingyue:stocktake:confirm', 5),
            (N'盘点报表', N'jingyue:stocktake:report', 6);
        INSERT INTO dbo.sys_menu
            (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
        SELECT b.MenuName, @stocktakeMenuId, b.OrderNum, NULL, NULL, 0, 0, N'F', N'0', N'0', b.Perms, N'#', N'system', GETDATE(), N'盘点管理按钮权限'
        FROM @btnSeed b
        WHERE NOT EXISTS (SELECT 1 FROM dbo.sys_menu m WHERE m.ParentId = @stocktakeMenuId AND m.Perms = b.Perms);

        INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
        SELECT 1, m.MenuId, N'system', GETDATE()
        FROM dbo.sys_menu m
        WHERE (m.MenuId = @stocktakeMenuId OR m.ParentId = @stocktakeMenuId)
          AND NOT EXISTS (SELECT 1 FROM dbo.sys_role_menu rm WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    RAISERROR(N'静悦盘点与综合报表增量脚本执行失败，已回滚。', 16, 1);
END CATCH;

PRINT N'静悦盘点与综合报表增量脚本执行完成';
