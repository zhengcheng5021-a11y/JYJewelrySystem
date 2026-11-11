-- 静悦销售报表菜单（SQL Server，幂等）
-- 仅新增菜单与权限，不修改任何业务表结构，不写入业务数据。
-- 发布前在目标库执行本脚本，或按下方字段在“菜单管理”手工维护。

SET XACT_ABORT ON;

DECLARE @reportRootId bigint = (
    SELECT TOP 1 MenuId FROM dbo.sys_menu
    WHERE MenuType = N'M' AND Path IN (N'reports', N'report')
    ORDER BY CASE WHEN Path = N'reports' THEN 0 ELSE 1 END, MenuId
);

IF @reportRootId IS NULL
BEGIN
    DECLARE @businessRootId bigint = (
        SELECT TOP 1 MenuId FROM dbo.sys_menu
        WHERE ParentId = 0 AND Path = N'business'
    );
    IF @businessRootId IS NOT NULL
    BEGIN
        INSERT INTO dbo.sys_menu
            (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType,
             Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
        VALUES
            (N'报表管理', @businessRootId, 9, N'report', NULL, 0, 0, N'M',
             N'0', N'0', NULL, N'chart', N'system', GETDATE(), N'经营分析与库存报表');
        SET @reportRootId = SCOPE_IDENTITY();
    END;
END;

IF @reportRootId IS NOT NULL
BEGIN
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

    DECLARE @ledgerMenuId bigint = (
        SELECT TOP 1 MenuId FROM dbo.sys_menu
        WHERE ParentId = @reportRootId AND Path = N'sales-ledger' AND MenuType = N'C'
    );

    IF @ledgerMenuId IS NOT NULL
    BEGIN
        IF NOT EXISTS (
            SELECT 1 FROM dbo.sys_menu
            WHERE ParentId = @ledgerMenuId AND Perms = N'jingyue:report:sales'
        )
        BEGIN
            INSERT INTO dbo.sys_menu
                (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType,
                 Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
            VALUES
                (N'销售报表查询', @ledgerMenuId, 1, NULL, NULL, 0, 0, N'F',
                 N'0', N'0', N'jingyue:report:sales', N'#', N'system', GETDATE(), N'销售报表查询权限');
        END;

        INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
        SELECT 1, m.MenuId, N'system', GETDATE()
        FROM dbo.sys_menu m
        WHERE (m.MenuId = @reportRootId OR m.MenuId = @ledgerMenuId OR m.ParentId = @ledgerMenuId)
          AND NOT EXISTS (
              SELECT 1 FROM dbo.sys_role_menu rm
              WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId
          );
    END;
END;

PRINT N'静悦销售报表菜单增量脚本执行完成';
