using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 待办与目标统一节点。ParentId 为空表示主事项，子行表示主事项下的小目标。
    /// 主事项 ItemKind：1 普通记事，2 客户跟进，3 品类销售，4 期间销售，5 自定义量化，6 汇总容器。
    /// </summary>
    [SugarTable("jy_work_item", "静悦待办与目标")]
    public class JyWorkItem : SysBase
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? StoreId { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ParentId { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string ItemNo { get; set; }
        [SugarColumn(Length = 200)]
        public string Title { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int ItemKind { get; set; }
        [SugarColumn(DefaultValue = "3")]
        public int PeriodType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? DueTime { get; set; }
        /// <summary>子目标计划完成日期，可为空，默认父事项结束日期。</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? TargetFinishDate { get; set; }
        [SugarColumn(DefaultValue = "2")]
        public int Priority { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? OwnerUserId { get; set; }
        [SugarColumn(Length = 50, IsNullable = true)]
        public string OwnerName { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int Status { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int ProgressMode { get; set; }
        [SugarColumn(Length = 40, IsNullable = true)]
        public string MetricCode { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal TargetValue { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CurrentValue { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal CurrentAuxValue { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public decimal ProgressPercent { get; set; }
        [SugarColumn(DefaultValue = "1")]
        public int AutoComplete { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? ReminderTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? FinishTime { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? SettledTime { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int SortNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? TemplateId { get; set; }
        [SugarColumn(DefaultValue = "0")]
        public int DelFlag { get; set; }
        /// <summary>0 普通记事（无日期区间），1 有明确时间区间。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int HasDateRange { get; set; }
        /// <summary>1 关联业绩，子目标必须选择父级款式分类和金额/件数目标。</summary>
        [SugarColumn(DefaultValue = "0")]
        public int IsPerformanceRelated { get; set; }
        /// <summary>1 金额目标，2 件数目标。</summary>
        [SugarColumn(DefaultValue = "1")]
        public int TargetMode { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? CategoryId { get; set; }
        [SugarColumn(Length = 100, IsNullable = true)]
        public string CategoryName { get; set; }
    }
}

