-- 静悦报表菜单与超级管理员授权恢复（SQL Server 2005+，幂等）
-- 背景：jydb 在 2026-11-11 10:16 被还原到当天 10:11 的备份，销售报表菜单丢失；
--       进销存报表、库龄分析菜单仍在，但未关联 sys_role_menu。
-- 本脚本只补菜单、权限记录和角色关联，不修改业务表结构，不写入业务数据。
-- 执行前请备份；执行后重新登录或刷新路由。
-- 若用旧版 sqlcmd 执行，请加 -f 65001（脚本含中文）；SSMS 直接打开执行即可。

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @reportRootId bigint = (
    SELECT TOP 1 MenuId FROM dbo.sys_menu
    WHERE MenuType = N'M' AND Path IN (N'reports', N'report')
    ORDER BY CASE WHEN Path = N'reports' THEN 0 ELSE 1 END, MenuId
);

IF @reportRootId IS NULL
BEGIN
    RAISERROR(N'未找到报表管理菜单，请先确认当前连接的是静悦业务库。', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

/* 1. 销售报表菜单 */
IF NOT EXISTS (
    SELECT 1 FROM dbo.sys_menu
    WHERE ParentId = @reportRootId AND Path = N'sales-ledger' AND MenuType = N'C'
)
BEGIN
    INSERT INTO dbo.sys_menu
        (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType,
         Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
    VALUES
        (N'销售报表', @reportRootId, 6, N'sales-ledger', N'jingyue/report/sales-ledger',
         0, 0, N'C', N'0', N'0', N'jingyue:report:sales', N'chart',
         N'system', GETDATE(), N'按材质/款式分类汇总销售、退货、成本与利润');
END;

DECLARE @salesMenuId bigint = (
    SELECT TOP 1 MenuId FROM dbo.sys_menu
    WHERE ParentId = @reportRootId AND Path = N'sales-ledger' AND MenuType = N'C'
);

/* 2. 销售报表查询按钮权限 */
IF NOT EXISTS (
    SELECT 1 FROM dbo.sys_menu
    WHERE ParentId = @salesMenuId AND Perms = N'jingyue:report:sales'
)
BEGIN
    INSERT INTO dbo.sys_menu
        (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType,
         Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
    VALUES
        (N'销售报表查询', @salesMenuId, 1, NULL, NULL, 0, 0, N'F',
         N'0', N'0', N'jingyue:report:sales', N'#', N'system', GETDATE(), N'销售报表查询权限');
END;

/* 3. 补超级管理员授权：报表根目录、销售报表、进销存、库龄及各自按钮权限 */
INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
SELECT 1, m.MenuId, N'system', GETDATE()
FROM dbo.sys_menu m
WHERE (
        m.MenuId = @reportRootId
        OR m.ParentId = @reportRootId
        OR m.ParentId IN (
            SELECT MenuId FROM dbo.sys_menu
            WHERE ParentId = @reportRootId AND Path IN (N'sales-ledger', N'inventory-report', N'stock-age')
        )
    )
  AND NOT EXISTS (
      SELECT 1 FROM dbo.sys_role_menu rm
      WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId
  );

COMMIT TRANSACTION;

PRINT N'静悦报表菜单权限恢复完成';
