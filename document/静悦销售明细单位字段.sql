-- 销售柜台手工录入数量单位、银重所需的最小结构变更（SQL Server）
-- 请在发布本版后端前执行；脚本幂等，可重复运行。
-- 银重列 jy_sale_order_item.Weight 已存在，本脚本不再重复添加。

IF COL_LENGTH('dbo.jy_sale_order_item', 'Unit') IS NULL
BEGIN
    ALTER TABLE dbo.jy_sale_order_item
    ADD Unit NVARCHAR(16) NULL;
END;
GO
