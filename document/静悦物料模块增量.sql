/*
  静悦物料基础模块增量脚本
  执行库：jydb（SQL Server 2008 R2 兼容）
  范围：物料档案、物料单据头/明细、物料重量流水、物料分类字典、单据编号、菜单权限
  说明：脚本可重复执行；不修改既有业务表，不初始化业务库存数据。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
/* SQL Server 2008 R2 创建索引要求会话级 SET 选项。 */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.jy_material', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_material (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_material PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        StoreId bigint NULL,
        MaterialCode nvarchar(32) NOT NULL,
        MaterialName nvarchar(100) NOT NULL,
        CategoryItemId bigint NULL,
        MetalItemId bigint NULL,
        SupplierId bigint NULL,
        StockWeight decimal(18,4) NOT NULL CONSTRAINT DF_jy_material_StockWeight DEFAULT (0),
        LockedWeight decimal(18,4) NOT NULL CONSTRAINT DF_jy_material_LockedWeight DEFAULT (0),
        AvgCostPerGram decimal(18,6) NOT NULL CONSTRAINT DF_jy_material_AvgCost DEFAULT (0),
        SalePricePerGram decimal(18,2) NOT NULL CONSTRAINT DF_jy_material_SalePrice DEFAULT (0),
        Status int NOT NULL CONSTRAINT DF_jy_material_Status DEFAULT (0),
        DelFlag int NOT NULL CONSTRAINT DF_jy_material_DelFlag DEFAULT (0),
        Create_by nvarchar(64) NULL,
        Create_time datetime NOT NULL CONSTRAINT DF_jy_material_CreateTime DEFAULT (GETDATE()),
        Update_by nvarchar(64) NULL,
        Update_time datetime NULL,
        Remark nvarchar(500) NULL
    );
    CREATE UNIQUE INDEX UX_jy_material_code ON dbo.jy_material(TenantId, MaterialCode) WHERE DelFlag = 0;
    CREATE INDEX IX_jy_material_category ON dbo.jy_material(TenantId, CategoryItemId);
    CREATE INDEX IX_jy_material_supplier ON dbo.jy_material(TenantId, SupplierId);
    CREATE INDEX IX_jy_material_metal ON dbo.jy_material(TenantId, MetalItemId, Status);
END;

IF OBJECT_ID(N'dbo.jy_material_doc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_material_doc (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_material_doc PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        StoreId bigint NULL,
        DocNo nvarchar(32) NOT NULL,
        Direction int NOT NULL,
        BizType nvarchar(32) NOT NULL,
        SupplierId bigint NULL,
        DocTime datetime NOT NULL,
        Status int NOT NULL CONSTRAINT DF_jy_material_doc_Status DEFAULT (0),
        TotalWeight decimal(18,4) NOT NULL CONSTRAINT DF_jy_material_doc_Weight DEFAULT (0),
        TotalAmount decimal(18,2) NOT NULL CONSTRAINT DF_jy_material_doc_Amount DEFAULT (0),
        OperatorUserId bigint NULL,
        ConfirmUserId bigint NULL,
        ConfirmTime datetime NULL,
        DelFlag int NOT NULL CONSTRAINT DF_jy_material_doc_DelFlag DEFAULT (0),
        Create_by nvarchar(64) NULL,
        Create_time datetime NOT NULL CONSTRAINT DF_jy_material_doc_CreateTime DEFAULT (GETDATE()),
        Update_by nvarchar(64) NULL,
        Update_time datetime NULL,
        Remark nvarchar(500) NULL
    );
    CREATE UNIQUE INDEX UX_jy_material_doc_no ON dbo.jy_material_doc(TenantId, DocNo) WHERE DelFlag = 0;
    CREATE INDEX IX_jy_material_doc_time ON dbo.jy_material_doc(TenantId, Direction, DocTime);
    CREATE INDEX IX_jy_material_doc_supplier ON dbo.jy_material_doc(TenantId, SupplierId, DocTime);
    CREATE INDEX IX_jy_material_doc_status ON dbo.jy_material_doc(TenantId, Status, DocTime);
END;

IF OBJECT_ID(N'dbo.jy_material_doc_line', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_material_doc_line (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_material_doc_line PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        DocId bigint NOT NULL,
        SortNo int NOT NULL,
        MaterialId bigint NOT NULL,
        MaterialCode nvarchar(32) NOT NULL,
        MaterialName nvarchar(100) NOT NULL,
        CategoryItemId bigint NULL,
        MetalItemId bigint NULL,
        Weight decimal(18,4) NOT NULL,
        UnitCost decimal(18,6) NOT NULL CONSTRAINT DF_jy_material_line_UnitCost DEFAULT (0),
        Amount decimal(18,2) NOT NULL CONSTRAINT DF_jy_material_line_Amount DEFAULT (0),
        Remark nvarchar(200) NULL,
        DelFlag int NOT NULL CONSTRAINT DF_jy_material_line_DelFlag DEFAULT (0),
        Create_time datetime NOT NULL CONSTRAINT DF_jy_material_line_CreateTime DEFAULT (GETDATE())
    );
    CREATE UNIQUE INDEX UX_jy_material_line_sort ON dbo.jy_material_doc_line(DocId, SortNo);
    CREATE INDEX IX_jy_material_line_material ON dbo.jy_material_doc_line(TenantId, MaterialId);
END;

IF OBJECT_ID(N'dbo.jy_material_ledger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_material_ledger (
        Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_material_ledger PRIMARY KEY,
        TenantId nvarchar(64) NOT NULL,
        StoreId bigint NULL,
        LedgerNo nvarchar(32) NOT NULL,
        MaterialId bigint NOT NULL,
        MaterialCode nvarchar(32) NOT NULL,
        ChangeType int NOT NULL,
        ChangeWeight decimal(18,4) NOT NULL,
        BeforeWeight decimal(18,4) NOT NULL,
        AfterWeight decimal(18,4) NOT NULL,
        UnitCost decimal(18,6) NOT NULL CONSTRAINT DF_jy_material_ledger_UnitCost DEFAULT (0),
        Amount decimal(18,2) NOT NULL CONSTRAINT DF_jy_material_ledger_Amount DEFAULT (0),
        BizType nvarchar(32) NOT NULL,
        BizId bigint NOT NULL,
        BizNo nvarchar(32) NOT NULL,
        BizTime datetime NOT NULL,
        OperatorUserId bigint NULL,
        Create_by nvarchar(64) NULL,
        Create_time datetime NOT NULL CONSTRAINT DF_jy_material_ledger_CreateTime DEFAULT (GETDATE()),
        Remark nvarchar(500) NULL
    );
    CREATE UNIQUE INDEX UX_jy_material_ledger_no ON dbo.jy_material_ledger(TenantId, LedgerNo);
    CREATE INDEX IX_jy_material_ledger_material ON dbo.jy_material_ledger(TenantId, MaterialId, BizTime);
    CREATE INDEX IX_jy_material_ledger_biz ON dbo.jy_material_ledger(TenantId, BizType, BizId);
END;

/* 物料分类字典：复用现有两级字典，不新建分类表 */
IF NOT EXISTS (SELECT 1 FROM dbo.jy_lookup_type WHERE TypeCode = N'material_category' AND DelFlag = 0)
BEGIN
    INSERT INTO dbo.jy_lookup_type
        (TenantId, TypeCode, TypeName, LevelLimit, IsPreset, Status, SortNo, DelFlag, Create_by, Create_time, Remark)
    VALUES
        (N'0', N'material_category', N'物料分类', 2, 1, 0, 7, 0, N'system', GETDATE(), N'物料模块两级分类');
END;

/* 单据编号：与现有 jy_bill_rule 取号服务共用 */
IF NOT EXISTS (SELECT 1 FROM dbo.jy_bill_rule WHERE TenantId = N'0' AND BillType = N'material_inbound')
BEGIN
    INSERT INTO dbo.jy_bill_rule
        (TenantId, BillType, BillName, Prefix, DatePattern, SeqLength, ResetCycle, CurrentKey, CurrentSeq, IsPreset, Status, Create_by, Create_time, Remark)
    VALUES
        (N'0', N'material_inbound', N'物料入库', N'MR', N'yyyyMMdd', 4, N'day', N'', 0, 1, 0, N'system', GETDATE(), N'物料模块预置');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.jy_bill_rule WHERE TenantId = N'0' AND BillType = N'material_outbound')
BEGIN
    INSERT INTO dbo.jy_bill_rule
        (TenantId, BillType, BillName, Prefix, DatePattern, SeqLength, ResetCycle, CurrentKey, CurrentSeq, IsPreset, Status, Create_by, Create_time, Remark)
    VALUES
        (N'0', N'material_outbound', N'物料出库', N'MC', N'yyyyMMdd', 4, N'day', N'', 0, 1, 0, N'system', GETDATE(), N'物料模块预置');
END;

/* 菜单：物料档案/物料入库/物料出库/物料库存/物料流水，独立一级菜单 */
DECLARE @materialRootId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = 0 AND Path = N'material-center');
IF @materialRootId IS NULL
BEGIN
    INSERT INTO dbo.sys_menu
        (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
    VALUES
        (N'物料管理', 0, 8, N'material-center', NULL, 0, 0, N'M', N'0', N'0', NULL, N'box', N'system', GETDATE(), N'物料档案、出入库、库存与流水');
    SET @materialRootId = SCOPE_IDENTITY();
END;

/* 兼容旧脚本曾把物料页面挂在静悦业务下的情况。 */
DECLARE @businessRootId bigint = (SELECT TOP 1 MenuId FROM dbo.sys_menu WHERE ParentId = 0 AND Path = N'business');
IF @businessRootId IS NOT NULL
BEGIN
    UPDATE dbo.sys_menu SET ParentId = @materialRootId
    WHERE ParentId = @businessRootId AND MenuType = N'C'
      AND Path IN (N'material', N'material-inbound', N'material-outbound', N'material-stock', N'material-ledger')
      AND NOT EXISTS (SELECT 1 FROM dbo.sys_menu x WHERE x.ParentId = @materialRootId AND x.Path = dbo.sys_menu.Path);
END;

DECLARE @menuSeed TABLE (MenuName nvarchar(50), Path nvarchar(255), Component nvarchar(255), Perms nvarchar(100), Icon nvarchar(255), OrderNum int);
INSERT INTO @menuSeed VALUES
    (N'物料档案', N'material', N'jingyue/material/index', N'jingyue:material:list', N'box', 30),
    (N'物料入库', N'material-inbound', N'jingyue/material/doc', N'jingyue:materialdoc:list', N'document-add', 31),
    (N'物料出库', N'material-outbound', N'jingyue/material/doc', N'jingyue:materialdoc:list', N'document-remove', 32),
    (N'物料库存', N'material-stock', N'jingyue/material/stock', N'jingyue:material:list', N'files', 33),
    (N'物料流水', N'material-ledger', N'jingyue/material/ledger', N'jingyue:materialledger:list', N'clock', 34);

INSERT INTO dbo.sys_menu
    (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
SELECT s.MenuName, @materialRootId, s.OrderNum, s.Path, s.Component, 0, 0, N'C', N'0', N'0', s.Perms, s.Icon, N'system', GETDATE(), N'静悦物料模块'
FROM @menuSeed s
WHERE NOT EXISTS (SELECT 1 FROM dbo.sys_menu m WHERE m.ParentId = @materialRootId AND m.MenuName = s.MenuName AND m.MenuType = N'C');

DECLARE @btnSeed TABLE (ParentName nvarchar(50), MenuName nvarchar(50), Perms nvarchar(100), OrderNum int);
INSERT INTO @btnSeed VALUES
    (N'物料档案', N'物料查询', N'jingyue:material:query', 1),
    (N'物料档案', N'物料新增', N'jingyue:material:add', 2),
    (N'物料档案', N'物料修改', N'jingyue:material:edit', 3),
    (N'物料档案', N'物料删除', N'jingyue:material:remove', 4),
    (N'物料入库', N'入库查询', N'jingyue:materialdoc:query', 1),
    (N'物料入库', N'入库新增', N'jingyue:materialdoc:add', 2),
    (N'物料入库', N'入库修改', N'jingyue:materialdoc:edit', 3),
    (N'物料入库', N'入库删除', N'jingyue:materialdoc:remove', 4),
    (N'物料入库', N'确认入库', N'jingyue:materialdoc:confirm', 5),
    (N'物料出库', N'出库查询', N'jingyue:materialdoc:query', 1),
    (N'物料出库', N'出库新增', N'jingyue:materialdoc:add', 2),
    (N'物料出库', N'出库修改', N'jingyue:materialdoc:edit', 3),
    (N'物料出库', N'出库删除', N'jingyue:materialdoc:remove', 4),
    (N'物料出库', N'确认出库', N'jingyue:materialdoc:confirm', 5),
    (N'物料流水', N'流水查询', N'jingyue:materialledger:query', 1);

INSERT INTO dbo.sys_menu
    (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
SELECT b.MenuName, p.MenuId, b.OrderNum, NULL, NULL, 0, 0, N'F', N'0', N'0', b.Perms, N'#', N'system', GETDATE(), N'静悦物料按钮'
FROM @btnSeed b
JOIN dbo.sys_menu p ON p.ParentId = @materialRootId AND p.MenuName = b.ParentName AND p.MenuType = N'C'
WHERE NOT EXISTS (SELECT 1 FROM dbo.sys_menu m WHERE m.ParentId = p.MenuId AND m.Perms = b.Perms);

/* 超级管理员授权 */
INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
SELECT 1, m.MenuId, N'system', GETDATE()
FROM dbo.sys_menu m
WHERE (m.MenuId = @materialRootId
    OR m.ParentId = @materialRootId
    OR m.ParentId IN (SELECT MenuId FROM dbo.sys_menu WHERE ParentId = @materialRootId))
  AND NOT EXISTS (SELECT 1 FROM dbo.sys_role_menu rm WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId);

PRINT N'静悦物料模块增量脚本执行完成';
