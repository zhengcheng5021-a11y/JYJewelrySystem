/*
  静悦销售定制：一单多商品、逐件收/到货/取货/退款/销售核销
  适用：SQL Server 2012+（使用了 IIF / TRY_CONVERT 等语法）
  执行顺序：业务低峰执行，建议先完整备份。
  本脚本幂等，可重复执行；不会删除已有定金、流水或销售数据。
  执行后请重启 Web API。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.jy_custom_order', N'U') IS NULL
    THROW 50001, N'未找到 dbo.jy_custom_order，请确认当前连接的是静悦正式库。', 1;

BEGIN TRANSACTION;

/* 1. 定金商品明细表 */
IF OBJECT_ID(N'dbo.jy_custom_item', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.jy_custom_item
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_jy_custom_item PRIMARY KEY,
        TenantId        NVARCHAR(64) NOT NULL,
        CustomOrderId   BIGINT NOT NULL,
        SortNo          INT NOT NULL CONSTRAINT DF_jy_custom_item_SortNo DEFAULT(0),
        GoodsItemId     BIGINT NULL,
        GoodsNo         NVARCHAR(32) NULL,
        Barcode         NVARCHAR(64) NULL,
        BindTime        DATETIME2 NULL,
        GoodsName       NVARCHAR(100) NOT NULL,
        PickupDate      DATETIME2 NULL,
        ArrivalStatus   INT NOT NULL CONSTRAINT DF_jy_custom_item_ArrivalStatus DEFAULT(0),
        ArrivalTime     DATETIME2 NULL,
        ArrivalBy       NVARCHAR(64) NULL,
        ArrivalRemark   NVARCHAR(500) NULL,
        PickupStatus    INT NOT NULL CONSTRAINT DF_jy_custom_item_PickupStatus DEFAULT(0),
        PickupTime      DATETIME2 NULL,
        PickupBy        NVARCHAR(64) NULL,
        PickupRemark    NVARCHAR(500) NULL,
        StyleItemId     BIGINT NULL,
        StyleItemName   NVARCHAR(100) NULL,
        MaterialItemId  BIGINT NULL,
        MaterialName    NVARCHAR(100) NULL,
        MetalItemId     BIGINT NULL,
        MetalName       NVARCHAR(100) NULL,
        [Weight]        DECIMAL(18,3) NULL,
        ExpectPrice     DECIMAL(18,2) NOT NULL CONSTRAINT DF_jy_custom_item_ExpectPrice DEFAULT(0),
        DepositAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_jy_custom_item_DepositAmount DEFAULT(0),
        OffsetAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_jy_custom_item_OffsetAmount DEFAULT(0),
        RefundAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_jy_custom_item_RefundAmount DEFAULT(0),
        RemainAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_jy_custom_item_RemainAmount DEFAULT(0),
        Status          INT NOT NULL CONSTRAINT DF_jy_custom_item_Status DEFAULT(1),
        DelFlag         INT NOT NULL CONSTRAINT DF_jy_custom_item_DelFlag DEFAULT(0),
        Remark          NVARCHAR(500) NULL,
        Create_by       NVARCHAR(64) NULL,
        Create_time     DATETIME2 NULL,
        Update_by       NVARCHAR(64) NULL,
        Update_time     DATETIME2 NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_custom_item') AND name = N'IX_jy_custom_item_order')
    CREATE INDEX IX_jy_custom_item_order ON dbo.jy_custom_item (TenantId, CustomOrderId, DelFlag) INCLUDE (SortNo, GoodsItemId, ArrivalStatus, PickupStatus);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_custom_item') AND name = N'IX_jy_custom_item_goods')
    CREATE INDEX IX_jy_custom_item_goods ON dbo.jy_custom_item (TenantId, GoodsItemId) WHERE GoodsItemId IS NOT NULL;

/* 2. 已有流水、销售定金对冲增加商品明细关联 */
IF COL_LENGTH(N'dbo.jy_custom_ledger', N'CustomItemId') IS NULL
    ALTER TABLE dbo.jy_custom_ledger ADD CustomItemId BIGINT NULL;

IF COL_LENGTH(N'dbo.jy_sale_custom_offset', N'CustomItemId') IS NULL
    ALTER TABLE dbo.jy_sale_custom_offset ADD CustomItemId BIGINT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_custom_ledger') AND name = N'IX_jy_custom_ledger_item')
    CREATE INDEX IX_jy_custom_ledger_item ON dbo.jy_custom_ledger (TenantId, CustomItemId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.jy_sale_custom_offset') AND name = N'IX_jy_sale_custom_offset_item')
    CREATE INDEX IX_jy_sale_custom_offset_item ON dbo.jy_sale_custom_offset (TenantId, CustomItemId, Status);

/* 3. 历史单商品定金单补一条镜像明细，金额结转以主表为准 */
INSERT INTO dbo.jy_custom_item
(
    TenantId, CustomOrderId, SortNo, GoodsItemId, GoodsNo, Barcode, BindTime, GoodsName, PickupDate,
    ArrivalStatus, ArrivalTime, ArrivalBy, ArrivalRemark, PickupStatus,
    StyleItemId, StyleItemName, MaterialItemId, MaterialName, MetalItemId, MetalName,
    [Weight], ExpectPrice, DepositAmount, OffsetAmount, RefundAmount, RemainAmount,
    Status, DelFlag, Remark, Create_by, Create_time, Update_by, Update_time
)
SELECT
    o.TenantId, o.Id, 1, o.GoodsItemId, o.GoodsNo, o.Barcode, o.BindTime,
    ISNULL(NULLIF(LTRIM(RTRIM(o.GoodsName)), N''), N'定制商品'), o.PickupDate,
    ISNULL(o.ArrivalStatus, 0), o.ArrivalTime, o.ArrivalBy, o.ArrivalRemark, 0,
    o.StyleItemId, o.StyleItemName, o.MaterialItemId, o.MaterialName, o.MetalItemId, o.MetalName,
    o.[Weight], ISNULL(o.ExpectPrice, 0), ISNULL(o.DepositAmount, 0), ISNULL(o.OffsetAmount, 0),
    ISNULL(o.RefundAmount, 0), ISNULL(o.RemainAmount, 0),
    ISNULL(NULLIF(o.Status, 0), 1), 0, N'由旧版单商品定金单自动结转',
    o.Create_by, ISNULL(o.Create_time, SYSDATETIME()), o.Update_by, o.Update_time
FROM dbo.jy_custom_order o
WHERE ISNULL(o.DelFlag, 0) <> 2
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.jy_custom_item i
      WHERE i.TenantId = o.TenantId AND i.CustomOrderId = o.Id AND ISNULL(i.DelFlag, 0) <> 2
  );

/* 4. 旧流水回填到唯一明细；多商品单无法安全判断的不回填，保留主单维度历史 */
UPDATE l
SET l.CustomItemId = i.Id
FROM dbo.jy_custom_ledger l
CROSS APPLY
(
    SELECT TOP 1 x.Id
    FROM dbo.jy_custom_item x
    WHERE x.TenantId = l.TenantId AND x.CustomOrderId = l.CustomOrderId AND ISNULL(x.DelFlag, 0) <> 2
    ORDER BY x.SortNo, x.Id
) i
WHERE l.CustomItemId IS NULL
  AND (SELECT COUNT(1) FROM dbo.jy_custom_item x WHERE x.TenantId = l.TenantId AND x.CustomOrderId = l.CustomOrderId AND ISNULL(x.DelFlag, 0) <> 2) = 1;

/* 5. 旧销售对冲回填到唯一明细 */
UPDATE f
SET f.CustomItemId = i.Id
FROM dbo.jy_sale_custom_offset f
CROSS APPLY
(
    SELECT TOP 1 x.Id
    FROM dbo.jy_custom_item x
    WHERE x.TenantId = f.TenantId AND x.CustomOrderId = f.CustomOrderId AND ISNULL(x.DelFlag, 0) <> 2
    ORDER BY x.SortNo, x.Id
) i
WHERE f.CustomItemId IS NULL
  AND (SELECT COUNT(1) FROM dbo.jy_custom_item x WHERE x.TenantId = f.TenantId AND x.CustomOrderId = f.CustomOrderId AND ISNULL(x.DelFlag, 0) <> 2) = 1;

/* 6. 商品锁定业务 Id 从定金单 Id 改为明细 Id */
UPDATE g
SET g.LockBizId = i.Id
FROM dbo.jy_goods_item g
CROSS APPLY
(
    SELECT TOP 1 x.Id
    FROM dbo.jy_custom_item x
    WHERE x.TenantId = g.TenantId AND x.CustomOrderId = g.LockBizId
      AND ISNULL(x.DelFlag, 0) <> 2 AND x.GoodsItemId = g.Id
    ORDER BY x.SortNo, x.Id
) i
WHERE g.LockBizType = N'custom'
  AND g.LockBizId IS NOT NULL
  AND EXISTS (SELECT 1 FROM dbo.jy_custom_order o WHERE o.Id = g.LockBizId AND o.TenantId = g.TenantId)
  /* 已是明细 Id 的锁定不要再次迁移，即使它与某张定金单 Id 恰好相同 */
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.jy_custom_item y
      WHERE y.Id = g.LockBizId AND y.TenantId = g.TenantId
        AND ISNULL(y.DelFlag, 0) <> 2 AND y.GoodsItemId = g.Id
  );

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(1) FROM dbo.jy_custom_item WHERE ISNULL(DelFlag, 0) = 0) AS 商品明细数,
    (SELECT COUNT(1) FROM dbo.jy_custom_item WHERE ISNULL(DelFlag, 0) = 0 AND RemainAmount > 0) AS 仍有剩余定金件数,
    (SELECT COUNT(1) FROM dbo.jy_goods_item WHERE LockBizType = N'custom' AND LockBizId IS NOT NULL) AS 当前定金锁定商品数;

PRINT N'静悦定金主从与商品明细增量脚本执行完成';
