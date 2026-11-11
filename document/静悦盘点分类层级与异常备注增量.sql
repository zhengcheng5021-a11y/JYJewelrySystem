/*
  静悦盘点分类层级与异常备注增量脚本
  执行库：jydb（SQL Server 2008 R2 兼容）
  变更范围：仅 jy_stocktake_category 增加分类层级快照和异常备注字段。
  说明：字段均可空、幂等执行；不修改商品、库存、物料主表，不写入业务数据。
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'dbo.jy_stocktake_category', N'ParentCategoryKey') IS NULL
        ALTER TABLE dbo.jy_stocktake_category ADD ParentCategoryKey nvarchar(150) NULL;

    IF COL_LENGTH(N'dbo.jy_stocktake_category', N'ParentCategoryName') IS NULL
        ALTER TABLE dbo.jy_stocktake_category ADD ParentCategoryName nvarchar(100) NULL;

    IF COL_LENGTH(N'dbo.jy_stocktake_category', N'AnomalyRemark') IS NULL
        ALTER TABLE dbo.jy_stocktake_category ADD AnomalyRemark nvarchar(200) NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    RAISERROR(N'盘点分类字段增量脚本执行失败，已回滚。', 16, 1);
END CATCH;

PRINT N'静悦盘点分类层级与异常备注增量脚本执行完成';
