using ZR.Model.System;

namespace ZR.Model.JingYue
{
    /// <summary>目标进度流水。UniqueKey 保证同一业务事件重复回算不会重复记账。</summary>
    [SugarTable("jy_work_item_progress_log", "静悦目标进度流水")]
    public class JyWorkItemProgressLog
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }
        [SugarColumn(Length = 64)]
        public string TenantId { get; set; }
        public long WorkItemId { get; set; }
        [SugarColumn(Length = 40, IsNullable = true)]
        public string MetricCode { get; set; }
        public decimal BeforeValue { get; set; }
        public decimal ChangeValue { get; set; }
        public decimal AfterValue { get; set; }
        public decimal ProgressPercent { get; set; }
        [SugarColumn(Length = 32, IsNullable = true)]
        public string SourceType { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? SourceId { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? BusinessDate { get; set; }
        [SugarColumn(Length = 120, IsNullable = true)]
        public string UniqueKey { get; set; }
        public DateTime Create_time { get; set; } = DateTime.Now;
        [SugarColumn(Length = 500, IsNullable = true)]
        public string Remark { get; set; }
    }
}
