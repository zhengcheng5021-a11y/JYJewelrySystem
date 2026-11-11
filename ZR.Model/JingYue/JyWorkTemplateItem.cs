using ZR.Model.System;

namespace ZR.Model.JingYue
{
    [SugarTable("jy_work_template_item", "静悦目标模板明细")]
    public class JyWorkTemplateItem
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long TemplateId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        [SugarColumn(Length = 200)]
        public string Title { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int ItemKind { get; set; }
        [SugarColumn(DefaultValue = "3")]
        public int PeriodType { get; set; }
        [SugarColumn(DefaultValue = "2")]
        public int Priority { get; set; }
        [SugarColumn(Length = 40, IsNullable = true)]
        public string MetricCode { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TargetValue { get; set; }
        [SugarColumn(IsNullable = true)]
        public string ScopeJson { get; set; }
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Remark { get; set; }
        /// <summary>1 金额目标，2 件数目标。</summary>
        [SugarColumn(DefaultValue = "1")]
        public int TargetMode { get; set; } = 1;
        [SugarColumn(IsNullable = true)]
        public long? CategoryId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string CategoryName { get; set; }
    }
}

