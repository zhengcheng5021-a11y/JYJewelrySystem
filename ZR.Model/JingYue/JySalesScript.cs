using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 销售话术。款式可以选大类或子级，空表示不限。
    /// </summary>
    [SugarTable("jy_sales_script", "静悦销售话术")]
    public class JySalesScript : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }

        [SugarColumn(Length = 100)]
        public string Title { get; set; }

        /// <summary>接待开场、产品介绍、异议处理、成交推进、售后维护、其他。</summary>
        [SugarColumn(Length = 20, DefaultValue = "其他")]
        public string ScriptType { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? StyleItemId { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? MaterialItemId { get; set; }

        [SugarColumn(IsNullable = true)]
        public long? MetalItemId { get; set; }

        [SugarColumn(ColumnDataType = "nvarchar(max)")]
        public string Content { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }

        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
    }
}
