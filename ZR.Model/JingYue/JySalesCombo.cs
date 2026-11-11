using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>品类联动组合配置。组合占比在查询时按销售净额实时计算，不落冗余统计。</summary>
    [SugarTable("jy_sales_combo", "静悦品类联动组合")]
    public class JySalesCombo : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(Length = 100)]
        public string ComboName { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? CategoryId1 { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string CategoryName1 { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? CategoryId2 { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string CategoryName2 { get; set; }

        [SugarColumn(Length = 200, IsNullable = true)]
        public string FitCrowd { get; set; }

        [SugarColumn(Length = 200, IsNullable = true)]
        public string UseScene { get; set; }

        /// <summary>0 启用，1 停用。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
