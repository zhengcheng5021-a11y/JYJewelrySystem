-- 静悦菜单图标修复（SQL Server 2008 R2+，幂等）
-- 仅更新 sys_menu.Icon，不新增/删除表，不修改任何业务数据或表结构。
-- 修复空图标，以及引用了不存在图标名称而实际显示空白的菜单。

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE dbo.sys_menu
SET Icon = CASE
    WHEN Icon = N'aim' THEN N'star'
    WHEN Icon = N'back' THEN N'backup'
    WHEN Icon IN (N'box', N'goods', N'shopping-cart') THEN N'shopping'
    WHEN Icon = N'calendar' THEN N'ele-Calendar'
    WHEN Icon = N'chat-dot-round' THEN N'message'
    WHEN Icon = N'checklist' THEN N'clipboard'
    WHEN Icon = N'clock' THEN N'time'
    WHEN Icon = N'connection' THEN N'chain'
    WHEN Icon IN (N'data-line', N'histogram') THEN N'chart'
    WHEN Icon = N'document-add' THEN N'ele-DocumentAdd'
    WHEN Icon = N'document-remove' THEN N'ele-DocumentRemove'
    WHEN Icon = N'files' THEN N'ele-Files'
    WHEN Icon = N'price-tag' THEN N'money'
    WHEN Icon = N'refresh-left' THEN N'ele-RefreshLeft'
    WHEN Icon = N'ele-refresh' THEN N'ele-Refresh'
    WHEN ISNULL(LTRIM(RTRIM(Icon)), N'') = N'' THEN
        CASE
            WHEN MenuName = N'生成修改' THEN N'edit'
            WHEN MenuName LIKE N'%报表%' OR MenuName LIKE N'%分析%' THEN N'chart'
            WHEN MenuName LIKE N'%库存%' OR MenuName LIKE N'%物料%'
                 OR MenuName LIKE N'%入库%' OR MenuName LIKE N'%出库%' THEN N'shopping'
            WHEN MenuName LIKE N'%权限%' THEN N'permission'
            WHEN MenuName LIKE N'%用户%' OR MenuName LIKE N'%会员%' THEN N'user'
            WHEN MenuName LIKE N'%角色%' THEN N'people'
            WHEN MenuName LIKE N'%部门%' OR MenuName LIKE N'%菜单%' THEN N'tree-table'
            WHEN MenuName LIKE N'%日志%' THEN N'log'
            WHEN MenuName LIKE N'%邮件%' THEN N'email'
            WHEN MenuName LIKE N'%文件%' THEN N'upload'
            WHEN MenuName LIKE N'%监控%' THEN N'monitor'
            WHEN MenuName LIKE N'%配置%' OR MenuName LIKE N'%设置%' THEN N'edit'
            ELSE N'app'
        END
    ELSE Icon
END
WHERE MenuType IN (N'M', N'C')
  AND (
      ISNULL(LTRIM(RTRIM(Icon)), N'') = N''
      OR Icon IN (
          N'aim', N'back', N'box', N'calendar', N'chat-dot-round', N'checklist',
          N'clock', N'connection', N'data-line', N'document-add', N'document-remove',
          N'files', N'goods', N'histogram', N'price-tag', N'refresh-left',
          N'shopping-cart', N'ele-refresh'
      )
  );

COMMIT TRANSACTION;

SELECT MenuId, MenuName, Icon
FROM dbo.sys_menu
WHERE MenuType IN (N'M', N'C')
  AND (ISNULL(LTRIM(RTRIM(Icon)), N'') = N'' OR Icon = N'#')
ORDER BY ParentId, OrderNum, MenuId;

PRINT N'静悦菜单图标修复完成';
