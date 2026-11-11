/*
  静悦销售定制删除权限增量
  说明：仅新增按钮权限和角色授权，不改业务表结构，不写业务数据。
  适用：SQL Server 2008 R2 及以上。
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @customMenuId bigint = (
    SELECT TOP 1 MenuId FROM dbo.sys_menu
    WHERE MenuType = N'C' AND Path = N'sale-custom'
);

IF @customMenuId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM dbo.sys_menu
        WHERE ParentId = @customMenuId AND Perms = N'jingyue:custom:remove'
    )
    BEGIN
        INSERT INTO dbo.sys_menu
            (MenuName, ParentId, OrderNum, Path, Component, IsCache, IsFrame, MenuType, Visible, Status, Perms, Icon, Create_by, Create_time, Remark)
        VALUES
            (N'定制删除', @customMenuId, 5, NULL, NULL, 0, 0, N'F', N'0', N'0', N'jingyue:custom:remove', N'#', N'system', GETDATE(), N'删除已退清且未对冲的定制单');
    END;

    INSERT INTO dbo.sys_role_menu (Role_id, Menu_id, Create_by, Create_time)
    SELECT 1, m.MenuId, N'system', GETDATE()
    FROM dbo.sys_menu m
    WHERE m.ParentId = @customMenuId
      AND m.Perms = N'jingyue:custom:remove'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.sys_role_menu rm
          WHERE rm.Role_id = 1 AND rm.Menu_id = m.MenuId
      );
END;

PRINT N'静悦销售定制删除权限增量脚本执行完成';
